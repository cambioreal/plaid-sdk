# CambioReal.Plaid.Client.SandboxTests

Testes de integração sandbox real, opt-in — nunca incluído em `Plaid.slnx`, nunca em CI.

## Rodar

```bash
set -a
eval "$(pass show cambio-real-v2/providers/plaid/sandbox-env | sed -n \
  's/^CLIENT_ID=/PLAID_SANDBOX_CLIENT_ID=/p;s/^SECRET=/PLAID_SANDBOX_SECRET=/p')"
set +a
PLAID_SANDBOX_ALLOW_WRITE=1 dotnet test tests/CambioReal.Plaid.Client.SandboxTests/CambioReal.Plaid.Client.SandboxTests.csproj
unset PLAID_SANDBOX_CLIENT_ID PLAID_SANDBOX_SECRET
```

## Última execução ao vivo — 2026-07-15

```
Passed InstitutionsReadLive [1 s]
  POST institutions/get: 200, total=12001 — auth (client_id/secret no corpo) validada.
Passed ItemLifecycleRoundTripsLiveWithCleanup [5 s]
  POST sandbox/public_token/create: 200 (helper oficial de sandbox).
  POST item/public_token/exchange: 200.
  POST accounts/balance/get: 200, 12 conta(s) sintética(s).
  POST item/remove: 200, removed=true (cleanup).

Passed: 2, Failed: 0, Total: 2
```
