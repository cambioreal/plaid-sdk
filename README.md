# plaid-sdk

Cliente .NET tipado (`CambioReal.Plaid.Client`) para a **Plaid API** — link tokens, items
(exchange/remove/get), saldos, contas, identidade, transações (por data e incremental via
`transactions/sync`), instituições (listagem e busca textual) e processor tokens (ponte para a
Cybrid). Mesmo padrão do kira/ripple/bs2/bexs/cybrid-sdk: SDK modela a API nativa, zero
dependência de `CambioReal.Contracts`.

Particularidades da Plaid encapsuladas:

- **Sem OAuth** — `client_id`/`secret` são injetados pelo cliente no CORPO de todo POST
  (convenção Plaid); os DTOs nunca os carregam nem expõem.
- API POST-only, snake_case; valores monetários decimais.
- `INVALID_API_KEYS` (HTTP 400) classificado como `PlaidAuthenticationException`.
- `Sandbox.CreatePublicTokenAsync` (helper oficial sandbox-only) habilita o ciclo E2E de item com
  cleanup — usado pelos SandboxTests.
- `Institutions.GetAsync` (`institutions/get`, listagem paginada por `count`/`offset`) e
  `Institutions.SearchAsync` (`institutions/search`, busca textual por `query`) são endpoints
  DISTINTOS — não confundir (gap de fidelidade corrigido em 2026-07-15 no `plaid-gateway`, onde a
  rota `/institutions/search` chegou a implementar a semântica de `institutions/get`).
- `Transactions.SyncAsync` (`transactions/sync`) é a abordagem incremental por cursor recomendada
  pela Plaid desde 2023; o SDK não pagina automaticamente (`HasMore`/`NextCursor` na resposta —
  paginação é responsabilidade do chamador).

## Validação ao vivo (2026-07-15)

Unit 18 verdes (13 originais + 5 dos gaps P0: `item/get`, `accounts/get`, `transactions/sync` ×2,
`institutions/search`), rodados nesta sessão. SandboxTests (opt-in, live, requerem
`pass cambio-real-v2/providers/plaid/sandbox-env`): `institutions/get`/`sandbox/public_token/
create`/`item/public_token/exchange`/`accounts/balance/get`/`item/remove` seguem validados ao vivo
de uma sessão anterior; `institutions/search` e o encaixe de `item/get`/`accounts/get`/
`transactions/sync` no mesmo ciclo de item (create→exchange→…→remove) foram ADICIONADOS ao
SandboxTests nesta sessão mas **não executados** (sem acesso a credenciais/`pass` neste ambiente)
— rodar `dotnet test tests/CambioReal.Plaid.Client.SandboxTests` com
`PLAID_SANDBOX_ALLOW_WRITE=1` para validar antes do próximo release. Nenhum endpoint da superfície
move dinheiro. Ver `docs/providers/plaid/discovery.md` (matriz completa) e o README dos
SandboxTests.

**Versão 0.2.0** (gaps P0 do `plaid-gateway`: `item/get`, `accounts/get`, `transactions/sync`,
`institutions/search` real). A versão publicada continua sendo definida pela tag `v*` no release
(não pelo `Directory.Build.props`) — ver `.github/workflows/release.yml`.

## Secrets

Somente via `pass cambio-real-v2/providers/plaid/sandbox-env` → variáveis de ambiente. Nunca em
appsettings/fixtures; nenhum teste imprime secrets, access tokens ou PII.

## Instalação e uso

Pacote no GitHub Packages da org `cambioreal` (feed configurado no `NuGet.config` do repo consumidor):

```bash
dotnet add package CambioReal.Plaid.Client
```

```csharp
// Registro via DI — credenciais vêm de config segura (env/Secret/pass), nunca versionadas.
builder.Services.AddPlaidClient(builder.Configuration.GetSection(PlaidOptions.SectionName));

// ...injete CambioReal.Plaid.PlaidClient onde precisar.
```

Também há a sobrecarga `AddPlaidClient(Action<PlaidOptions>)` para configuração inline.
