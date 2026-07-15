using System.Text.Json;
using System.Text.Json.Serialization;

namespace CambioReal.Plaid.Serialization;

/// <summary>Convenções de JSON da Plaid API.</summary>
public static class PlaidJson
{
    /// <summary>
    /// Nomes de campo em <c>snake_case</c> (<c>public_token</c>, <c>access_token</c>,
    /// <c>institution_id</c>, …) — confirmado no legado (<c>cerebro/app/Libraries/EnvioUs/
    /// RequestPlaid.php</c>) e nas respostas vivas do sandbox (2026-07-15). Enums/códigos são
    /// strings abertas (a Plaid versiona o vocabulário) — DTOs usam <see cref="string"/>.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
