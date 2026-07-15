using System.Net;
using Microsoft.Extensions.Options;

namespace CambioReal.Plaid.Tests.Fakes;

internal static class TestClient
{
    public static PlaidOptions NewOptions() => new()
    {
        Environment = PlaidEnvironment.Sandbox,
        ClientId = "client-1",
        Secret = "secret-1",
    };

    /// <summary>Monta um <see cref="PlaidClient"/> sobre um transporte gravado, sem rede.</summary>
    public static (PlaidClient Client, RecordingHttpMessageHandler Transport) Create(
        params (HttpStatusCode Status, string Json)[] responses)
    {
        var transport = new RecordingHttpMessageHandler();

        foreach (var (status, json) in responses)
        {
            transport.RespondWith(status, json);
        }

        var options = NewOptions();
        var httpClient = new HttpClient(transport) { BaseAddress = options.ResolveBaseAddress() };

        return (new PlaidClient(httpClient, Options.Create(options)), transport);
    }

    public static (PlaidClient Client, RecordingHttpMessageHandler Transport) CreateOk(string json = "{}") =>
        Create((HttpStatusCode.OK, json));
}
