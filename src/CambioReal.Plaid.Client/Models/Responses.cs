using System.Text.Json;

namespace CambioReal.Plaid.Models;

/// <summary>Resposta de <c>link/token/create</c>.</summary>
public sealed record LinkTokenResponse(string? LinkToken, DateTimeOffset? Expiration, string? RequestId);

/// <summary>Resposta de <c>item/public_token/exchange</c> — validada ao vivo (2026-07-15).</summary>
public sealed record ExchangePublicTokenResponse(string? AccessToken, string? ItemId, string? RequestId);

/// <summary>Resposta de <c>item/remove</c> — validada ao vivo (<c>{"removed":true}</c>).</summary>
public sealed record RemoveItemResponse(bool Removed, string? RequestId);

/// <summary>Conta bancária com saldos — <c>accounts/balance/get</c>/<c>identity/get</c>.</summary>
public sealed record PlaidAccount
{
    public string? AccountId { get; init; }
    public PlaidBalances? Balances { get; init; }
    public string? Mask { get; init; }
    public string? Name { get; init; }
    public string? OfficialName { get; init; }

    /// <summary>Valores conhecidos: <c>depository</c>, <c>credit</c>, <c>loan</c>, <c>investment</c> (conjunto aberto).</summary>
    public string? Type { get; init; }

    public string? Subtype { get; init; }

    /// <summary>Presente em <c>identity/get</c> — donos declarados na instituição.</summary>
    public JsonElement? Owners { get; init; }
}

/// <summary>Saldos — valores DECIMAIS em unidade monetária (diferente da Cybrid).</summary>
public sealed record PlaidBalances
{
    public decimal? Available { get; init; }
    public decimal? Current { get; init; }
    public decimal? Limit { get; init; }
    public string? IsoCurrencyCode { get; init; }
    public string? UnofficialCurrencyCode { get; init; }
}

/// <summary>Item (vínculo instituição↔usuário).</summary>
public sealed record PlaidItem
{
    public string? ItemId { get; init; }
    public string? InstitutionId { get; init; }
    public IReadOnlyList<string>? AvailableProducts { get; init; }
    public IReadOnlyList<string>? BilledProducts { get; init; }
    public string? Webhook { get; init; }
    public JsonElement? Error { get; init; }
}

/// <summary>Resposta de <c>accounts/balance/get</c>.</summary>
public sealed record BalanceResponse(IReadOnlyList<PlaidAccount>? Accounts, PlaidItem? Item, string? RequestId);

/// <summary>Resposta de <c>identity/get</c>.</summary>
public sealed record IdentityResponse(IReadOnlyList<PlaidAccount>? Accounts, PlaidItem? Item, string? RequestId);

/// <summary>Resposta de <c>transactions/get</c>.</summary>
public sealed record TransactionsResponse(
    IReadOnlyList<PlaidAccount>? Accounts,
    IReadOnlyList<PlaidTransaction>? Transactions,
    int? TotalTransactions,
    PlaidItem? Item,
    string? RequestId);

/// <summary>Transação — subconjunto estável dos campos.</summary>
public sealed record PlaidTransaction
{
    public string? TransactionId { get; init; }
    public string? AccountId { get; init; }
    public decimal? Amount { get; init; }
    public string? IsoCurrencyCode { get; init; }
    public string? Date { get; init; }
    public string? Name { get; init; }
    public string? MerchantName { get; init; }
    public bool? Pending { get; init; }
    public string? PaymentChannel { get; init; }
}

/// <summary>Instituição.</summary>
public sealed record PlaidInstitution
{
    public string? InstitutionId { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<string>? Products { get; init; }
    public IReadOnlyList<string>? CountryCodes { get; init; }
    public string? Url { get; init; }
}

/// <summary>Resposta de <c>institutions/get</c>.</summary>
public sealed record InstitutionsResponse(IReadOnlyList<PlaidInstitution>? Institutions, int? Total, string? RequestId);

/// <summary>Resposta de <c>institutions/get_by_id</c>.</summary>
public sealed record InstitutionByIdResponse(PlaidInstitution? Institution, string? RequestId);

/// <summary>Resposta de <c>processor/token/create</c>.</summary>
public sealed record ProcessorTokenResponse(string? ProcessorToken, string? RequestId);

/// <summary>Resposta de <c>sandbox/public_token/create</c> — validada ao vivo.</summary>
public sealed record SandboxPublicTokenResponse(string? PublicToken, string? RequestId);
