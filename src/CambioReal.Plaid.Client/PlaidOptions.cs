namespace CambioReal.Plaid;

/// <summary>Configuração do <see cref="PlaidClient"/>.</summary>
public sealed class PlaidOptions
{
    /// <summary>Nome da seção de configuração sugerida.</summary>
    public const string SectionName = "Plaid";

    /// <summary>Client ID da Plaid.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Secret do ambiente. Deve vir do <c>pass</c> ou de um secret store — nunca do código.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Ambiente alvo. O padrão é <see cref="PlaidEnvironment.Sandbox"/>, deliberadamente.</summary>
    public PlaidEnvironment Environment { get; set; } = PlaidEnvironment.Sandbox;

    /// <summary>Sobrescreve o endereço base. Precisa terminar em <c>/</c>.</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>Timeout de cada requisição HTTP.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Endereço base efetivo.</summary>
    public Uri ResolveBaseAddress() => BaseAddress ?? Environment.GetBaseAddress();

    /// <summary>Valida a configuração e lança se estiver inconsistente.</summary>
    /// <exception cref="InvalidOperationException">Alguma credencial obrigatória está ausente ou o base address é inválido.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException($"{nameof(PlaidOptions)}.{nameof(ClientId)} é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(Secret))
        {
            throw new InvalidOperationException($"{nameof(PlaidOptions)}.{nameof(Secret)} é obrigatório.");
        }

        var baseAddress = ResolveBaseAddress();

        if (!baseAddress.IsAbsoluteUri)
        {
            throw new InvalidOperationException($"{nameof(BaseAddress)} precisa ser absoluto.");
        }

        if (!baseAddress.AbsolutePath.EndsWith('/'))
        {
            throw new InvalidOperationException($"{nameof(BaseAddress)} precisa terminar em '/' (recebido: '{baseAddress}').");
        }

        if (Timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(Timeout)} precisa ser positivo.");
        }
    }
}
