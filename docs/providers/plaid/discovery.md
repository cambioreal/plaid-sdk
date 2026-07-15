# Plaid — Discovery

Status: descoberta e SDK concluídos (2026-07-15). Sonda §0.8 **verde com ciclo E2E completo** —
o Plaid sandbox oferece helper oficial (`sandbox/public_token/create`) que permite exercitar o
ciclo de vida inteiro de um item com cleanup nativo. Sem qualquer bloqueio.
Provider order position: **5 of 9** (`GOAL-provider-standalone-sandbox-loop.md`).
Verified: 2026-07-15, contra `pass cambio-real-v2/providers/plaid/sandbox-env` no sandbox vivo
(`sandbox.plaid.com`) + legado `cerebro` (read-only) + doc oficial (plaid.com/docs).

## 1. Perfil no Provider Protocol

**Utility/identity** — a Plaid NÃO é PSP de pagamento no fluxo da plataforma: é a fonte de
vínculo de conta bancária US (Link → item → access token) que alimenta (a) verificação de
conta/saldo/identidade no EnvioUs do legado e (b) a ponte `processor/token/create` →
Cybrid `external_bank_accounts` (`account_kind: plaid_processor_token`). Não há submit/quote/
transfer — nenhum endpoint move dinheiro. Não se aplica `Sync`/`Async` do provider-protocol;
registrado como decisão (perfil utilitário, mesmo racional do PROVIDER-MAP que o lista como
"conta bancária/identidade", não PSP).

## 2. Ambiente, auth e convenções

| | Sandbox | Produção |
|---|---|---|
| Base URL | `https://sandbox.plaid.com/` | `https://production.plaid.com/` |
| Credencial | `pass cambio-real-v2/providers/plaid/sandbox-env` (`CLIENT_ID`/`SECRET`; `PUBLIC_KEY` é legado do Link antigo, não usado) | não aprovisionada |

- **Sem OAuth**: `client_id`/`secret` vão no CORPO de todo POST (confirmado no legado
  `RequestPlaid::request()` e na doc oficial) — o `PlaidClient` injeta no JSON; DTOs nunca os
  carregam. API é POST-only, snake_case; valores monetários DECIMAIS (diferente da Cybrid).
- Erro canônico: `{error_type, error_code, error_message, display_message, request_id}`;
  `INVALID_API_KEYS` (HTTP 400!) é classificado como falha de autenticação pelo SDK.
- O host `development.plaid.com` do config legado foi descontinuado pela Plaid — não modelado
  (divergência legado↔vigente registrada, §0.7).

## 3. Matriz de cobertura

| # | Endpoint | Recurso SDK | Efeito | Cleanup | Status sandbox |
|---|---|---|---|---|---|
| 1 | `link/token/create` | `LinkTokens.CreateAsync` | non-financial-write (token efêmero, expira) | expira sozinho | ⚪ coberto por unit (exige user real p/ Link UI) |
| 2 | `item/public_token/exchange` | `Items.ExchangePublicTokenAsync` | non-financial-write | #3 remove | ✅ vivo (ciclo E2E) |
| 3 | `item/remove` | `Items.RemoveAsync` | non-financial-write (cleanup) | é o cleanup | ✅ vivo (`removed: true`) |
| 4 | `accounts/balance/get` | `Accounts.GetBalanceAsync` | read | n/a | ✅ vivo (12 contas sintéticas) |
| 5 | `identity/get` | `Identity.GetAsync` | read (PII — nunca logar) | n/a | ⚪ coberto por unit (mesmo shape de #4) |
| 6 | `transactions/get` | `Transactions.GetAsync` | read | n/a | ⚪ coberto por unit |
| 7 | `institutions/get` | `Institutions.GetAsync` | read | n/a | ✅ vivo (12001 instituições) |
| 8 | `institutions/get_by_id` | `Institutions.GetByIdAsync` | read | n/a | ⚪ coberto por unit (usado pelo legado) |
| 9 | `processor/token/create` | `Processor.CreateTokenAsync` | non-financial-write (ponte Cybrid) | invalidado pelo remove do item | ⚪ coberto por unit; E2E depende de conta processor habilitada |
| 10 | `sandbox/public_token/create` | `Sandbox.CreatePublicTokenAsync` | sandbox-only helper | n/a | ✅ vivo |

Nenhum endpoint financeiro/destrutivo existe na superfície — o Plaid só lê dados bancários e
emite tokens. Fora de escopo (sem uso na plataforma): transfer API da Plaid, payment initiation
(EU), income/assets/investments.

## 4. Webhooks — fora do gateway (decisão)

A Plaid tem webhooks de item/transactions; o legado não os consome para Plaid (polling/uso
pontual). Mesmo padrão dos demais: fora do gateway v1.

## 5. Evidência viva (2026-07-15)

- curl (§0.8): institutions 200; ciclo public_token→exchange→remove 200/`removed:true`.
- SDK SandboxTests: institutions (total=12001) + ciclo completo com balance (12 contas
  sintéticas) e cleanup — 2/2 verdes.
- Unit: 13 verdes (credencial injetada no corpo e nunca em header, erro INVALID_API_KEYS →
  PlaidAuthenticationException, shapes snake_case).

## 6. Limites de responsabilidade

- **SDK**: modela a API nativa; nunca loga access tokens/PII.
- **Gateway (`plaid-gateway`)**: expõe o subconjunto no contrato canônico; PII de `identity/get`
  atravessa o gateway — consumidores internos apenas (mesmo gap de auth de chamador dos demais).
- **Plataforma**: orquestra Link UI (frontend), guarda access tokens, decide ponte para Cybrid.

## 7. Nenhuma contradição arquitetural

Padrão canônico SDK/gateway standalone aplicado; perfil utilitário registrado explicitamente.
