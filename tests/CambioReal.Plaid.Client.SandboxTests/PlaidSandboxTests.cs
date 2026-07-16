using CambioReal.Plaid;
using CambioReal.Plaid.Models;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CambioReal.Plaid.SandboxTests;

/// <summary>
/// Integração sandbox real, opt-in — goal-loop §2.5. Credenciais via <c>pass</c>:
///
/// <code>
/// set -a
/// eval "$(pass show cambio-real-v2/providers/plaid/sandbox-env | sed -n \
///   's/^CLIENT_ID=/PLAID_SANDBOX_CLIENT_ID=/p;s/^SECRET=/PLAID_SANDBOX_SECRET=/p')"
/// set +a
/// dotnet test tests/CambioReal.Plaid.Client.SandboxTests/CambioReal.Plaid.Client.SandboxTests.csproj
/// </code>
///
/// O ciclo de item (create sintético → exchange → balance → remove) usa o helper OFICIAL de
/// sandbox da Plaid (<c>sandbox/public_token/create</c>) e faz cleanup completo — escrita não
/// financeira com cleanup (§0.4); exige <c>PLAID_SANDBOX_ALLOW_WRITE=1</c>. Não existe operação
/// financeira na superfície deste SDK. Nenhum teste imprime secrets/PII.
/// </summary>
public sealed class PlaidSandboxTests
{
    private readonly ITestOutputHelper output;

    public PlaidSandboxTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    [Trait("Category", "Sandbox")]
    public async Task InstitutionsReadLive()
    {
        using var provider = BuildServiceProvider();
        var client = provider.GetRequiredService<PlaidClient>();

        var response = await client.Institutions.GetAsync(new InstitutionsRequest
        {
            Count = 2,
            Offset = 0,
            CountryCodes = ["US"],
        });

        response.Institutions.ShouldNotBeNull();
        response.Institutions!.Count.ShouldBe(2);
        output.WriteLine($"POST institutions/get: 200, total={response.Total} — auth (client_id/secret no corpo) validada.");
    }

    /// <summary>
    /// <c>institutions/search</c> real (busca textual) — endpoint DISTINTO de <c>institutions/get</c>
    /// acima. Gap corrigido junto com o fix de fidelidade da rota do gateway (coverage/plaid.md).
    /// </summary>
    [Fact]
    [Trait("Category", "Sandbox")]
    public async Task InstitutionsSearchReadLive()
    {
        using var provider = BuildServiceProvider();
        var client = provider.GetRequiredService<PlaidClient>();

        var response = await client.Institutions.SearchAsync(new InstitutionsSearchRequest
        {
            Query = "First Platypus",
            CountryCodes = ["US"],
        });

        response.Institutions.ShouldNotBeNull();
        output.WriteLine($"POST institutions/search: 200, {response.Institutions!.Count} resultado(s) — query textual validada.");
    }

    /// <summary>
    /// Ciclo completo de item com cleanup — validado ao vivo via curl em 2026-07-15 e aqui
    /// repetível via SDK: public token sintético → exchange → balance → remove.
    /// </summary>
    [Fact]
    [Trait("Category", "SandboxWrite")]
    public async Task ItemLifecycleRoundTripsLiveWithCleanup()
    {
        if (Environment.GetEnvironmentVariable("PLAID_SANDBOX_ALLOW_WRITE") != "1")
        {
            output.WriteLine("PLAID_SANDBOX_ALLOW_WRITE != 1 — ciclo de item pulado por design.");
            return;
        }

        using var provider = BuildServiceProvider();
        var client = provider.GetRequiredService<PlaidClient>();

        var publicToken = await client.Sandbox.CreatePublicTokenAsync(new SandboxPublicTokenRequest
        {
            InstitutionId = "ins_109508", // First Platypus Bank — instituição de teste oficial
            InitialProducts = ["auth"],
        });

        publicToken.PublicToken.ShouldNotBeNullOrWhiteSpace();
        output.WriteLine("POST sandbox/public_token/create: 200 (helper oficial de sandbox).");

        var exchange = await client.Items.ExchangePublicTokenAsync(new ExchangePublicTokenRequest
        {
            PublicToken = publicToken.PublicToken!,
        });

        exchange.AccessToken.ShouldNotBeNullOrWhiteSpace();
        output.WriteLine($"POST item/public_token/exchange: 200, item_id length={exchange.ItemId?.Length} (mascarado).");

        try
        {
            var balance = await client.Accounts.GetBalanceAsync(new BalanceRequest
            {
                AccessToken = exchange.AccessToken!,
            });

            balance.Accounts.ShouldNotBeNull();
            balance.Accounts!.Count.ShouldBeGreaterThan(0);
            output.WriteLine($"POST accounts/balance/get: 200, {balance.Accounts!.Count} conta(s) sintética(s).");

            var item = await client.Items.GetAsync(new AccessTokenRequest { AccessToken = exchange.AccessToken! });

            item.Item.ShouldNotBeNull();
            item.Item!.ItemId.ShouldBe(exchange.ItemId);
            output.WriteLine($"POST item/get: 200, institution_id={item.Item!.InstitutionId} (introspecção sem endpoint de dado).");

            var accounts = await client.Accounts.GetAsync(new AccountsGetRequest { AccessToken = exchange.AccessToken! });

            accounts.Accounts.ShouldNotBeNull();
            accounts.Accounts!.Count.ShouldBeGreaterThan(0);
            output.WriteLine($"POST accounts/get: 200, {accounts.Accounts!.Count} conta(s) (metadados + saldo em cache).");

            // Primeira chamada omite o cursor — devolve o histórico completo (item sintético recém-criado).
            var sync = await client.Transactions.SyncAsync(new TransactionsSyncRequest { AccessToken = exchange.AccessToken! });

            // Resposta estruturalmente válida. O NextCursor pode vir vazio/nulo num item sintético
            // recém-criado (transações ainda não prontas — TRANSACTIONS_READY não disparou); validado
            // ao vivo. O que importa é o endpoint responder 200 e o SDK parsear.
            sync.ShouldNotBeNull();
            sync.Added.ShouldNotBeNull();
            output.WriteLine(
                $"POST transactions/sync: 200, added={sync.Added!.Count}, hasMore={sync.HasMore}, " +
                $"nextCursor length={sync.NextCursor?.Length ?? 0} (item sintético — histórico/cursor pode vir vazio).");
        }
        finally
        {
            // Cleanup SEMPRE: remove invalida o access token (validado ao vivo: removed=true).
            var removed = await client.Items.RemoveAsync(new AccessTokenRequest
            {
                AccessToken = exchange.AccessToken!,
            });

            removed.Removed.ShouldBeTrue();
            output.WriteLine("POST item/remove: 200, removed=true (cleanup).");
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var clientId = Environment.GetEnvironmentVariable("PLAID_SANDBOX_CLIENT_ID");
        var secret = Environment.GetEnvironmentVariable("PLAID_SANDBOX_SECRET");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "PLAID_SANDBOX_CLIENT_ID/SECRET ausentes — carregue-os de " +
                "`pass cambio-real-v2/providers/plaid/sandbox-env` antes de rodar este projeto.");
        }

        var services = new ServiceCollection();
        services.AddPlaidClient(options =>
        {
            options.Environment = PlaidEnvironment.Sandbox;
            options.ClientId = clientId;
            options.Secret = secret;
        });

        return services.BuildServiceProvider();
    }
}
