# Fix de duplicação — WorldLifecycleResetCompletedEvent

## Problema

Antes do ajuste, havia três publishers para `WorldLifecycleResetCompletedEvent`:

- `WorldLifecycleSceneFlowResetDriver.PublishResetCompleted` (driver)
- `ResetWorldService.TriggerResetAsync` (fluxo normal)
- `ResetWorldService.TriggerResetAsync` (catch)

Isso gerava duplicação quando o driver usava `ResetWorldService` via DI, pois ambos publicavam o mesmo evento para o mesmo reset.

## Ajuste aplicado

- O driver agora **não publica** `WorldLifecycleResetCompletedEvent` quando o reset é executado via `ResetWorldService` (DI).
- O publisher canônico passa a ser o **`ResetWorldService`** para o fluxo principal de gameplay.
- A semântica de erro do `ResetWorldService` foi mantida (as publicações normal/catch permanecem e não ocorrem na mesma execução).

## Ramo(s) onde o driver ainda publica (SKIP/fallback)

O driver continua publicando **apenas** para liberar o gate do SceneFlow em cenários onde **não há** `ResetWorldService` responsável pelo publish:

- **SKIP**: profile diferente de gameplay (startup/frontend).
- **Fallback**: não há `WorldLifecycleController` na cena alvo.
- **Fallback**: reset executado diretamente nos controllers porque `ResetWorldService` não está disponível via DI.
- **Defensivo**: assinatura vazia em `SceneTransitionScenesReadyEvent`.

Esses ramos são considerados exceções deliberadas para evitar deadlock no gate do SceneFlow.
