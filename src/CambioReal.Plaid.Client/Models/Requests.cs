using System.Text.Json;

namespace CambioReal.Plaid.Models;

/// <summary>
/// Corpo de <c>POST link/token/create</c> — subconjunto usado pela plataforma
/// (<c>cerebro/app/Libraries/LinkToken</c> + doc oficial). <c>client_id</c>/<c>secret</c> são
/// injetados pelo <see cref="PlaidClient"/>, nunca aparecem nos DTOs.
/// </summary>
public sealed record CreateLinkTokenRequest
{
    public required string ClientName { get; init; }
    public required string Language { get; init; }
    public required IReadOnlyList<string> CountryCodes { get; init; }
    public required PlaidLinkUser User { get; init; }
    public IReadOnlyList<string>? Products { get; init; }
    public string? AccessToken { get; init; }
    public string? Webhook { get; init; }
    public string? LinkCustomizationName { get; init; }
    public string? RedirectUri { get; init; }
}

/// <summary>Usuário do Link (<c>user.client_user_id</c> obrigatório).</summary>
public sealed record PlaidLinkUser
{
    public required string ClientUserId { get; init; }
    public string? LegalName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
}

/// <summary><c>POST item/public_token/exchange</c>.</summary>
public sealed record ExchangePublicTokenRequest
{
    public required string PublicToken { get; init; }
}

/// <summary><c>POST item/remove</c> e demais operações por access token.</summary>
public sealed record AccessTokenRequest
{
    public required string AccessToken { get; init; }
}

/// <summary><c>POST accounts/balance/get</c> — <c>options.account_ids</c> opcional (legado envia).</summary>
public sealed record BalanceRequest
{
    public required string AccessToken { get; init; }
    public PlaidAccountIdsOptions? Options { get; init; }
}

/// <summary>
/// <c>POST accounts/get</c> — lista contas do item + metadados + saldo em cache. Distinto de
/// <c>accounts/balance/get</c> (<see cref="BalanceRequest"/>, saldo em tempo real): mesmo corpo,
/// endpoint diferente na Plaid — DTO próprio para manter 1 tipo por endpoint (convenção deste SDK).
/// </summary>
public sealed record AccountsGetRequest
{
    public required string AccessToken { get; init; }
    public PlaidAccountIdsOptions? Options { get; init; }
}

/// <summary>Bloco <c>options</c> com filtro de contas.</summary>
public sealed record PlaidAccountIdsOptions
{
    public IReadOnlyList<string>? AccountIds { get; init; }
}

/// <summary><c>POST identity/get</c>.</summary>
public sealed record IdentityRequest
{
    public required string AccessToken { get; init; }
    public PlaidAccountIdsOptions? Options { get; init; }
}

/// <summary><c>POST transactions/get</c> — datas <c>YYYY-MM-DD</c> (formato do legado).</summary>
public sealed record TransactionsRequest
{
    public required string AccessToken { get; init; }
    public required string StartDate { get; init; }
    public required string EndDate { get; init; }
    public PlaidAccountIdsOptions? Options { get; init; }
}

/// <summary>
/// <c>POST transactions/sync</c> — modelo incremental por cursor, abordagem recomendada pela
/// própria Plaid desde 2023 (substitui <c>transactions/get</c> em integrações novas).
/// </summary>
public sealed record TransactionsSyncRequest
{
    public required string AccessToken { get; init; }

    /// <summary>
    /// Cursor da sincronização anterior. Omitir/<see langword="null"/> na primeira chamada
    /// (devolve todo o histórico, paginado por <c>has_more</c>); nas chamadas seguintes, enviar o
    /// <c>NextCursor</c> da resposta anterior. O valor especial <c>"now"</c> só existe para migrar
    /// a partir de <c>transactions/get</c> — não aplicável aqui (este SDK não expunha
    /// <c>transactions/get</c> antes deste cursor, então não há estado legado para migrar).
    /// </summary>
    public string? Cursor { get; init; }

    /// <summary>Quantidade de updates por página (1-500; default da Plaid é 100 se omitido).</summary>
    public int? Count { get; init; }

    public TransactionsSyncOptions? Options { get; init; }
}

/// <summary>Bloco <c>options</c> de <c>transactions/sync</c>.</summary>
public sealed record TransactionsSyncOptions
{
    public bool? IncludeOriginalDescription { get; init; }
    public string? PersonalFinanceCategoryVersion { get; init; }

    /// <summary>Só tem efeito em item recém-criado, ainda sem histórico inicial carregado (1-730).</summary>
    public int? DaysRequested { get; init; }

    public string? AccountId { get; init; }
}

/// <summary><c>POST institutions/get</c>.</summary>
public sealed record InstitutionsRequest
{
    public required int Count { get; init; }
    public required int Offset { get; init; }
    public required IReadOnlyList<string> CountryCodes { get; init; }
}

/// <summary><c>POST institutions/get_by_id</c> — <c>country_codes: ["US"]</c> no legado.</summary>
public sealed record InstitutionByIdRequest
{
    public required string InstitutionId { get; init; }
    public required IReadOnlyList<string> CountryCodes { get; init; }
}

/// <summary>
/// <c>POST institutions/search</c> — busca textual por <c>query</c> (até 10 resultados),
/// DISTINTO de <c>institutions/get</c> (<see cref="InstitutionsRequest"/>, listagem paginada por
/// <c>count</c>/<c>offset</c>). Antes deste DTO, a rota do gateway chamada "search" na verdade
/// invocava a semântica de <c>institutions/get</c> — corrigido junto com este gap
/// (ver <c>coverage/plaid.md</c> §Divergências).
/// </summary>
public sealed record InstitutionsSearchRequest
{
    public required string Query { get; init; }
    public required IReadOnlyList<string> CountryCodes { get; init; }

    /// <summary>Filtra por produtos suportados (ex.: <c>["auth","transactions"]</c>).</summary>
    public IReadOnlyList<string>? Products { get; init; }
}

/// <summary>
/// <c>POST processor/token/create</c> — ponte para parceiros (ex.: Cybrid consome o
/// <c>processor_token</c> em <c>external_bank_accounts</c> com <c>account_kind:
/// plaid_processor_token</c>).
/// </summary>
public sealed record CreateProcessorTokenRequest
{
    public required string AccessToken { get; init; }
    public required string AccountId { get; init; }

    /// <summary>Nome do parceiro na doc Plaid (ex.: <c>"cybrid"</c>).</summary>
    public required string Processor { get; init; }
}

/// <summary><c>POST sandbox/public_token/create</c> — SÓ existe no sandbox; usado pelos testes E2E.</summary>
public sealed record SandboxPublicTokenRequest
{
    public required string InstitutionId { get; init; }
    public required IReadOnlyList<string> InitialProducts { get; init; }
    public JsonElement? Options { get; init; }
}
