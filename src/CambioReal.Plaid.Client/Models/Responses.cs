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

    /// <summary>Presente em <c>item/get</c> (não vem em balance/identity/transactions).</summary>
    public string? InstitutionName { get; init; }

    public IReadOnlyList<string>? AvailableProducts { get; init; }
    public IReadOnlyList<string>? BilledProducts { get; init; }

    /// <summary>Produtos ativos no item — presente em <c>item/get</c>.</summary>
    public IReadOnlyList<string>? Products { get; init; }

    public string? Webhook { get; init; }

    /// <summary><c>background</c> ou <c>user_present_required</c> — presente em <c>item/get</c>.</summary>
    public string? UpdateType { get; init; }

    /// <summary>Timestamp ISO 8601 de criação do item — presente em <c>item/get</c>.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    public JsonElement? Error { get; init; }
}

/// <summary>Resposta de <c>accounts/balance/get</c>.</summary>
public sealed record BalanceResponse(IReadOnlyList<PlaidAccount>? Accounts, PlaidItem? Item, string? RequestId);

/// <summary>
/// Resposta de <c>accounts/get</c> — contas + metadados + saldo em cache. Mesmo shape de
/// <see cref="BalanceResponse"/> (a Plaid reaproveita o objeto <c>Account</c>/<c>Item</c> nos dois
/// endpoints), DTO de resposta próprio para simetria com <see cref="AccountsGetRequest"/>.
/// </summary>
public sealed record AccountsGetResponse(IReadOnlyList<PlaidAccount>? Accounts, PlaidItem? Item, string? RequestId);

/// <summary>
/// Resposta de <c>item/get</c> — introspecção do item sem chamar um endpoint de dado. <c>Status</c>
/// não é tipado (bloco rico de saúde de webhook/transactions pouco estável entre versões da Plaid,
/// mesmo tratamento de escape hatch já usado em <see cref="PlaidAccount.Owners"/>/<see cref="PlaidItem.Error"/>).
/// </summary>
public sealed record ItemGetResponse(PlaidItem? Item, JsonElement? Status, string? RequestId);

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

/// <summary>
/// Resposta de <c>transactions/sync</c>. <c>Added</c>/<c>Modified</c> reaproveitam
/// <see cref="PlaidTransaction"/> (mesmo objeto <c>Transaction</c> base de <c>transactions/get</c>);
/// <c>Removed</c> só tem <c>transaction_id</c>/<c>account_id</c> (a Plaid não reenvia a transação
/// inteira para remoções). <c>NextCursor</c> alimenta a próxima chamada (válido por ≥1 ano segundo
/// a doc oficial). <c>HasMore</c>: se <see langword="true"/>, há mais páginas — o SDK NÃO pagina
/// automaticamente (cada chamada HTTP é 1 página; o loop de paginação é responsabilidade do
/// chamador). <c>TransactionsUpdateStatus</c> é conjunto aberto (string, não enum): valores
/// conhecidos <c>TRANSACTIONS_UPDATE_STATUS_UNKNOWN</c>/<c>NOT_READY</c>/
/// <c>INITIAL_UPDATE_COMPLETE</c>/<c>HISTORICAL_UPDATE_COMPLETE</c>.
/// </summary>
public sealed record TransactionsSyncResponse(
    IReadOnlyList<PlaidAccount>? Accounts,
    IReadOnlyList<PlaidTransaction>? Added,
    IReadOnlyList<PlaidTransaction>? Modified,
    IReadOnlyList<PlaidRemovedTransaction>? Removed,
    string? NextCursor,
    bool? HasMore,
    string? TransactionsUpdateStatus,
    string? RequestId);

/// <summary>Transação removida em <c>transactions/sync</c> — só o par de ids, sem o objeto completo.</summary>
public sealed record PlaidRemovedTransaction(string? TransactionId, string? AccountId);

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

/// <summary>
/// Resposta de <c>institutions/search</c> — busca textual (até 10 resultados). Sem <c>total</c>
/// (diferente de <see cref="InstitutionsResponse"/>, que é paginada por <c>count</c>/<c>offset</c>).
/// </summary>
public sealed record InstitutionsSearchResponse(IReadOnlyList<PlaidInstitution>? Institutions, string? RequestId);

/// <summary>Resposta de <c>processor/token/create</c>.</summary>
public sealed record ProcessorTokenResponse(string? ProcessorToken, string? RequestId);

/// <summary>Resposta de <c>sandbox/public_token/create</c> — validada ao vivo.</summary>
public sealed record SandboxPublicTokenResponse(string? PublicToken, string? RequestId);
