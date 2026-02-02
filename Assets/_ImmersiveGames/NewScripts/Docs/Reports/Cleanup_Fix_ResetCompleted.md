# Fix de duplicação — WorldLifecycleResetCompletedEvent

## Antes

- O driver podia publicar `ResetCompleted` mesmo quando o reset era executado via `ResetWorldService` (DI).

## Depois

- **Publisher canônico:** `ResetWorldService.TriggerResetAsync` (fluxo normal e catch).
- **Driver** só publica em **SKIP/fallback/defensivo** (profile != gameplay, sem controllers, assinatura inválida) ou quando executa reset via controllers.
