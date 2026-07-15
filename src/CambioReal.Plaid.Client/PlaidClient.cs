using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CambioReal.Plaid.Http;
using CambioReal.Plaid.Resources;
using CambioReal.Plaid.Serialization;
using Microsoft.Extensions.Options;

namespace CambioReal.Plaid;

/// <summary>
/// Cliente HTTP da Plaid API.
/// </summary>
/// <remarks>
/// A Plaid não usa OAuth: <c>client_id</c>/<c>secret</c> vão no CORPO de todo POST (confirmado no
/// legado <c>RequestPlaid::request()</c> e na doc oficial). O cliente injeta as credenciais no
/// JSON serializado de cada request — os DTOs não as carregam (e nunca as expõem). Toda a API é
/// POST-only, snake_case.
/// </remarks>
public sealed class PlaidClient
{
    private readonly HttpClient httpClient;
    private readonly PlaidOptions options;

    /// <summary>Cria o cliente sobre um <see cref="HttpClient"/> já configurado.</summary>
    public PlaidClient(HttpClient httpClient, IOptions<PlaidOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        this.httpClient = httpClient;
        this.options = options.Value;

        LinkTokens = new LinkTokensResource(this);
        Items = new ItemsResource(this);
        Accounts = new AccountsResource(this);
        Identity = new IdentityResource(this);
        Transactions = new TransactionsResource(this);
        Institutions = new InstitutionsResource(this);
        Processor = new ProcessorResource(this);
        Sandbox = new SandboxResource(this);
    }

    /// <summary>Link tokens (Plaid Link). <c>link/token/create</c>.</summary>
    public LinkTokensResource LinkTokens { get; }

    /// <summary>Items (vínculos de conta). <c>item/*</c>.</summary>
    public ItemsResource Items { get; }

    /// <summary>Saldos. <c>accounts/balance/get</c>.</summary>
    public AccountsResource Accounts { get; }

    /// <summary>Identidade na instituição. <c>identity/get</c>.</summary>
    public IdentityResource Identity { get; }

    /// <summary>Transações. <c>transactions/get</c>.</summary>
    public TransactionsResource Transactions { get; }

    /// <summary>Instituições. <c>institutions/*</c>.</summary>
    public InstitutionsResource Institutions { get; }

    /// <summary>Processor tokens (ponte para Cybrid/Dwolla/etc.). <c>processor/token/create</c>.</summary>
    public ProcessorResource Processor { get; }

    /// <summary>Helpers exclusivos de sandbox. <c>sandbox/*</c> — nunca existem em produção.</summary>
    public SandboxResource Sandbox { get; }

    internal async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path, TRequest body, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (path.StartsWith('/'))
        {
            throw new ArgumentException(
                $"O path deve ser relativo e não pode começar com '/'. Recebido: '{path}'.",
                nameof(path));
        }

        // Credenciais injetadas no corpo — convenção Plaid (nunca em header).
        var node = JsonSerializer.SerializeToNode(body, PlaidJson.Options) as JsonObject
            ?? throw new InvalidOperationException("O corpo da requisição Plaid precisa serializar para um objeto JSON.");

        node["client_id"] = options.ClientId;
        node["secret"] = options.Secret;

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative))
        {
            Content = JsonContent.Create(node, options: PlaidJson.Options),
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfUnsuccessfulAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<TResponse>(PlaidJson.Options, cancellationToken);

        return payload ?? throw new PlaidApiException(
            response.StatusCode,
            errorType: null,
            errorCode: null,
            "A Plaid devolveu um corpo JSON vazio onde um valor era esperado.",
            requestId: null,
            responseBody: null);
    }

    private static async Task ThrowIfUnsuccessfulAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var (errorType, errorCode, errorMessage, requestId) = TryExtractError(body);
        var message = $"A Plaid respondeu HTTP {(int)response.StatusCode} ({response.StatusCode})"
            + (errorMessage is null ? "." : $": {errorMessage}.");

        // A Plaid sinaliza credencial inválida com error_code INVALID_API_KEYS (HTTP 400), não 401.
        var isAuthFailure = response.StatusCode == HttpStatusCode.Unauthorized
            || string.Equals(errorCode, "INVALID_API_KEYS", StringComparison.Ordinal);

        throw isAuthFailure
            ? new PlaidAuthenticationException(response.StatusCode, errorType, errorCode, message, requestId, body)
            : new PlaidApiException(response.StatusCode, errorType, errorCode, message, requestId, body);
    }

    /// <summary>
    /// Extrai a forma de erro canônica da Plaid:
    /// <c>{error_type, error_code, error_message, display_message, request_id}</c>.
    /// </summary>
    private static (string? Type, string? Code, string? Message, string? RequestId) TryExtractError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (null, null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return (null, null, null, null);
            }

            string? Get(string name) =>
                root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

            return (Get("error_type"), Get("error_code"), Get("error_message") ?? Get("display_message"), Get("request_id"));
        }
        catch (JsonException)
        {
            return (null, null, null, null);
        }
    }
}
