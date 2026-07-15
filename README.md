# plaid-sdk

Cliente .NET tipado (`CambioReal.Plaid.Client`) para a **Plaid API** — link tokens, items
(exchange/remove), saldos, identidade, transações, instituições e processor tokens (ponte para a
Cybrid). Mesmo padrão do kira/ripple/bs2/bexs/cybrid-sdk: SDK modela a API nativa, zero
dependência de `CambioReal.Contracts`.

Particularidades da Plaid encapsuladas:

- **Sem OAuth** — `client_id`/`secret` são injetados pelo cliente no CORPO de todo POST
  (convenção Plaid); os DTOs nunca os carregam nem expõem.
- API POST-only, snake_case; valores monetários decimais.
- `INVALID_API_KEYS` (HTTP 400) classificado como `PlaidAuthenticationException`.
- `Sandbox.CreatePublicTokenAsync` (helper oficial sandbox-only) habilita o ciclo E2E de item com
  cleanup — usado pelos SandboxTests.

## Validação ao vivo (2026-07-15)

Unit 13 verdes; SandboxTests 2 verdes ao vivo: `institutions/get` (12001 instituições) e o ciclo
completo `sandbox/public_token/create` → `item/public_token/exchange` → `accounts/balance/get`
(12 contas sintéticas) → `item/remove` (`removed: true`, cleanup). Nenhum endpoint da superfície
move dinheiro. Ver `docs/providers/plaid/discovery.md` (matriz completa) e o README dos
SandboxTests.

## Secrets

Somente via `pass cambio-real-v2/providers/plaid/sandbox-env` → variáveis de ambiente. Nunca em
appsettings/fixtures; nenhum teste imprime secrets, access tokens ou PII.
