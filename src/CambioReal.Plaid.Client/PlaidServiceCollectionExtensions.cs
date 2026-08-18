using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CambioReal.Plaid;

/// <summary>Registro do cliente Plaid no container.</summary>
public static class PlaidServiceCollectionExtensions
{
    /// <summary>Registra o cliente a partir de uma seção de configuração.</summary>
    /// <remarks>
    /// As credenciais precisam chegar por um provider seguro. Nunca versione
    /// <c>ClientId</c>/<c>Secret</c> — a fonte da verdade é o <c>pass</c>,
    /// <c>cambio-real-v2/providers/plaid/sandbox-env</c>.
    /// </remarks>
    public static IServiceCollection AddPlaidClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return services.AddPlaidClient(configuration.Bind);
    }

    /// <summary>Registra <see cref="PlaidClient"/> e o pipeline HTTP (sem handler de auth — credenciais vão no corpo).</summary>
    public static IServiceCollection AddPlaidClient(this IServiceCollection services, Action<PlaidOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddOptions<PlaidOptions>().Validate(
            options =>
            {
                try
                {
                    options.Validate();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            },
            "A configuração do PlaidOptions é inválida.")
            .ValidateOnStart();

        services.AddHttpClient("plaid.api", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<PlaidOptions>>().Value;
            options.Validate();

            client.BaseAddress = options.ResolveBaseAddress();
            client.Timeout = options.Timeout;
        });

        services.TryAddTransient(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return new PlaidClient(
                factory.CreateClient("plaid.api"),
                provider.GetRequiredService<IOptions<PlaidOptions>>());
        });

        return services;
    }
}
