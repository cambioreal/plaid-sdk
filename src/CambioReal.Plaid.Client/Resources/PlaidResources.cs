using CambioReal.Plaid.Models;

namespace CambioReal.Plaid.Resources;

/// <summary>Link tokens (Plaid Link). <c>link/token/create</c>.</summary>
public sealed class LinkTokensResource
{
    private readonly PlaidClient client;

    internal LinkTokensResource(PlaidClient client) => this.client = client;

    /// <summary>Cria um link token (efêmero, expira sozinho). <c>POST link/token/create</c>.</summary>
    public Task<LinkTokenResponse> CreateAsync(CreateLinkTokenRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<CreateLinkTokenRequest, LinkTokenResponse>("link/token/create", request, cancellationToken);
}

/// <summary>Items (vínculos de conta). <c>item/*</c>.</summary>
public sealed class ItemsResource
{
    private readonly PlaidClient client;

    internal ItemsResource(PlaidClient client) => this.client = client;

    /// <summary>
    /// Troca o public token do Link por um access token permanente.
    /// <c>POST item/public_token/exchange</c> — validado ao vivo (2026-07-15). Escrita não
    /// financeira; cleanup = <see cref="RemoveAsync"/>.
    /// </summary>
    public Task<ExchangePublicTokenResponse> ExchangePublicTokenAsync(
        ExchangePublicTokenRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<ExchangePublicTokenRequest, ExchangePublicTokenResponse>(
            "item/public_token/exchange", request, cancellationToken);

    /// <summary>
    /// Remove um item (invalida o access token). <c>POST item/remove</c> — validado ao vivo
    /// (<c>removed: true</c>). É o cleanup canônico do ciclo de vida do item.
    /// </summary>
    public Task<RemoveItemResponse> RemoveAsync(AccessTokenRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<AccessTokenRequest, RemoveItemResponse>("item/remove", request, cancellationToken);
}

/// <summary>Saldos. <c>accounts/balance/get</c>.</summary>
public sealed class AccountsResource
{
    private readonly PlaidClient client;

    internal AccountsResource(PlaidClient client) => this.client = client;

    /// <summary>Consulta saldos em tempo real. <c>POST accounts/balance/get</c> (leitura).</summary>
    public Task<BalanceResponse> GetBalanceAsync(BalanceRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<BalanceRequest, BalanceResponse>("accounts/balance/get", request, cancellationToken);
}

/// <summary>Identidade declarada na instituição. <c>identity/get</c>.</summary>
public sealed class IdentityResource
{
    private readonly PlaidClient client;

    internal IdentityResource(PlaidClient client) => this.client = client;

    /// <summary>Consulta identidade (leitura, PII — nunca logar o payload).</summary>
    public Task<IdentityResponse> GetAsync(IdentityRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<IdentityRequest, IdentityResponse>("identity/get", request, cancellationToken);
}

/// <summary>Transações. <c>transactions/get</c>.</summary>
public sealed class TransactionsResource
{
    private readonly PlaidClient client;

    internal TransactionsResource(PlaidClient client) => this.client = client;

    /// <summary>Consulta transações (leitura).</summary>
    public Task<TransactionsResponse> GetAsync(TransactionsRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<TransactionsRequest, TransactionsResponse>("transactions/get", request, cancellationToken);
}

/// <summary>Instituições. <c>institutions/*</c>.</summary>
public sealed class InstitutionsResource
{
    private readonly PlaidClient client;

    internal InstitutionsResource(PlaidClient client) => this.client = client;

    /// <summary>Lista instituições. <c>POST institutions/get</c> — validado ao vivo (leitura pública).</summary>
    public Task<InstitutionsResponse> GetAsync(InstitutionsRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<InstitutionsRequest, InstitutionsResponse>("institutions/get", request, cancellationToken);

    /// <summary>Consulta uma instituição. <c>POST institutions/get_by_id</c> (usado pelo legado).</summary>
    public Task<InstitutionByIdResponse> GetByIdAsync(InstitutionByIdRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<InstitutionByIdRequest, InstitutionByIdResponse>("institutions/get_by_id", request, cancellationToken);
}

/// <summary>Processor tokens — ponte para parceiros (Cybrid). <c>processor/token/create</c>.</summary>
public sealed class ProcessorResource
{
    private readonly PlaidClient client;

    internal ProcessorResource(PlaidClient client) => this.client = client;

    /// <summary>
    /// Cria um processor token para um parceiro (ex.: <c>"cybrid"</c> — consumido em
    /// <c>external_bank_accounts.plaid_processor_token</c>). Escrita não financeira.
    /// </summary>
    public Task<ProcessorTokenResponse> CreateTokenAsync(
        CreateProcessorTokenRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<CreateProcessorTokenRequest, ProcessorTokenResponse>(
            "processor/token/create", request, cancellationToken);
}

/// <summary>Helpers exclusivos do sandbox — NUNCA existem em produção.</summary>
public sealed class SandboxResource
{
    private readonly PlaidClient client;

    internal SandboxResource(PlaidClient client) => this.client = client;

    /// <summary>
    /// Cria um public token sintético sem passar pelo Link UI.
    /// <c>POST sandbox/public_token/create</c> — validado ao vivo (2026-07-15); base do ciclo E2E
    /// (create → exchange → balance → remove) dos SandboxTests.
    /// </summary>
    public Task<SandboxPublicTokenResponse> CreatePublicTokenAsync(
        SandboxPublicTokenRequest request, CancellationToken cancellationToken = default) =>
        client.PostAsync<SandboxPublicTokenRequest, SandboxPublicTokenResponse>(
            "sandbox/public_token/create", request, cancellationToken);
}
