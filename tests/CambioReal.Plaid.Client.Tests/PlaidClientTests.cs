using System.Net;
using CambioReal.Plaid.Http;
using CambioReal.Plaid.Models;
using CambioReal.Plaid.Tests.Fakes;
using Shouldly;
using Xunit;

namespace CambioReal.Plaid.Tests;

public sealed class PlaidClientTests
{
    [Fact]
    public void ValidOptionsPassValidation()
        => Should.NotThrow(() => TestClient.NewOptions().Validate());

    [Theory]
    [InlineData("", "secret-1")]
    [InlineData("client-1", "")]
    public void MissingRequiredFieldThrows(string clientId, string secret)
    {
        var options = TestClient.NewOptions();
        options.ClientId = clientId;
        options.Secret = secret;

        Should.Throw<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void EnvironmentsResolveToOfficialHosts()
    {
        PlaidEnvironment.Sandbox.GetBaseAddress().ToString().ShouldBe("https://sandbox.plaid.com/");
        PlaidEnvironment.Production.GetBaseAddress().ToString().ShouldBe("https://production.plaid.com/");
    }

    [Fact]
    public async Task CredentialsAreInjectedIntoBodyNotHeaders()
    {
        var (client, transport) = TestClient.CreateOk("""{"institutions":[],"total":0,"request_id":"r-1"}""");

        await client.Institutions.GetAsync(new InstitutionsRequest
        {
            Count = 2,
            Offset = 0,
            CountryCodes = ["US"],
        });

        var request = transport.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri!.ToString().ShouldBe("https://sandbox.plaid.com/institutions/get");

        // Convenção Plaid: credenciais no CORPO (confirmado no legado), nunca em header.
        request.Authorization.ShouldBeNull();
        request.Body!.ShouldContain("\"client_id\":\"client-1\"");
        request.Body!.ShouldContain("\"secret\":\"secret-1\"");
        request.Body!.ShouldContain("\"country_codes\":[\"US\"]");
    }

    [Fact]
    public async Task ExchangePublicTokenSerializesSnakeCase()
    {
        var (client, transport) = TestClient.CreateOk(
            """{"access_token":"access-sandbox-1","item_id":"item-1","request_id":"r-1"}""");

        var response = await client.Items.ExchangePublicTokenAsync(new ExchangePublicTokenRequest
        {
            PublicToken = "public-sandbox-1",
        });

        response.AccessToken.ShouldBe("access-sandbox-1");
        response.ItemId.ShouldBe("item-1");
        transport.Requests.Single().Body!.ShouldContain("\"public_token\":\"public-sandbox-1\"");
    }

    [Fact]
    public async Task RemoveItemParsesRemovedFlag()
    {
        var (client, _) = TestClient.CreateOk("""{"removed":true,"request_id":"r-2"}""");

        var response = await client.Items.RemoveAsync(new AccessTokenRequest { AccessToken = "access-1" });

        response.Removed.ShouldBeTrue();
    }

    [Fact]
    public async Task BalanceParsesDecimalAmounts()
    {
        var (client, transport) = TestClient.CreateOk("""
            {"accounts":[{"account_id":"a-1","name":"Checking","type":"depository",
              "balances":{"available":100.50,"current":110.25,"iso_currency_code":"USD"}}],
             "item":{"item_id":"item-1"},"request_id":"r-3"}
            """);

        var response = await client.Accounts.GetBalanceAsync(new BalanceRequest
        {
            AccessToken = "access-1",
            Options = new PlaidAccountIdsOptions { AccountIds = ["a-1"] },
        });

        response.Accounts![0].Balances!.Available.ShouldBe(100.50m);
        response.Accounts![0].Balances!.IsoCurrencyCode.ShouldBe("USD");
        transport.Requests.Single().Body!.ShouldContain("\"options\":{\"account_ids\":[\"a-1\"]}");
    }

    /// <summary>Forma de erro canônica da Plaid — error_code específico + request_id.</summary>
    [Fact]
    public async Task PlaidErrorShapeIsMappedToException()
    {
        var (client, _) = TestClient.Create((HttpStatusCode.BadRequest, """
            {"error_type":"ITEM_ERROR","error_code":"ITEM_NOT_FOUND",
             "error_message":"The Item you requested cannot be found.",
             "display_message":null,"request_id":"req-9"}
            """));

        var error = await Should.ThrowAsync<PlaidApiException>(
            async () => await client.Items.RemoveAsync(new AccessTokenRequest { AccessToken = "x" }));

        error.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.ErrorType.ShouldBe("ITEM_ERROR");
        error.ErrorCode.ShouldBe("ITEM_NOT_FOUND");
        error.RequestId.ShouldBe("req-9");
        error.Message.ShouldContain("cannot be found");
    }

    /// <summary>INVALID_API_KEYS chega como HTTP 400 — ainda assim é falha de credencial.</summary>
    [Fact]
    public async Task InvalidApiKeysMapsToAuthenticationException()
    {
        var (client, _) = TestClient.Create((HttpStatusCode.BadRequest, """
            {"error_type":"INVALID_INPUT","error_code":"INVALID_API_KEYS",
             "error_message":"invalid client_id or secret provided","request_id":"req-10"}
            """));

        await Should.ThrowAsync<PlaidAuthenticationException>(
            async () => await client.Institutions.GetAsync(new InstitutionsRequest
            {
                Count = 1,
                Offset = 0,
                CountryCodes = ["US"],
            }));
    }

    [Fact]
    public async Task ProcessorTokenCreateTargetsCybridBridge()
    {
        var (client, transport) = TestClient.CreateOk("""{"processor_token":"processor-sandbox-1","request_id":"r-4"}""");

        var response = await client.Processor.CreateTokenAsync(new CreateProcessorTokenRequest
        {
            AccessToken = "access-1",
            AccountId = "a-1",
            Processor = "cybrid",
        });

        response.ProcessorToken.ShouldBe("processor-sandbox-1");
        transport.Requests.Single().Body!.ShouldContain("\"processor\":\"cybrid\"");
    }

    [Fact]
    public async Task CancellationTokenIsHonored()
    {
        var (client, _) = TestClient.CreateOk();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await client.Items.RemoveAsync(
                new AccessTokenRequest { AccessToken = "x" }, cts.Token));
    }

    [Fact]
    public async Task EmptySuccessBodyWhereValueExpectedThrows()
    {
        var (client, _) = TestClient.Create((HttpStatusCode.OK, "null"));

        var error = await Should.ThrowAsync<PlaidApiException>(
            async () => await client.Items.RemoveAsync(new AccessTokenRequest { AccessToken = "x" }));

        error.Message.ShouldContain("corpo JSON vazio");
    }
}
