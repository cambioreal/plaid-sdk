namespace CambioReal.Plaid;

/// <summary>Ambiente da Plaid API.</summary>
public enum PlaidEnvironment
{
    /// <summary>Sandbox — <c>https://sandbox.plaid.com/</c>.</summary>
    Sandbox = 0,

    /// <summary>Produção — <c>https://production.plaid.com/</c>.</summary>
    Production = 1,
}

/// <summary>Resolve o endereço base de cada <see cref="PlaidEnvironment"/>.</summary>
public static class PlaidEnvironmentExtensions
{
    /// <summary>
    /// Endereço base, confirmado em <c>cerebro/config/plaid.php</c> e validado ao vivo
    /// (2026-07-15). O host <c>development.plaid.com</c> do legado foi descontinuado pela Plaid —
    /// não é modelado.
    /// </summary>
    public static Uri GetBaseAddress(this PlaidEnvironment environment) => environment switch
    {
        PlaidEnvironment.Production => new Uri("https://production.plaid.com/", UriKind.Absolute),
        PlaidEnvironment.Sandbox => new Uri("https://sandbox.plaid.com/", UriKind.Absolute),
        _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Ambiente Plaid desconhecido."),
    };
}
