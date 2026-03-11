# Navigation

## CANONICO
- Owner canonico de restart: `Modules/Navigation/Runtime/MacroRestartCoordinator.cs`.
- Trilho canonico de restart:
  - `GameCommands` publica `GameResetRequestedEvent`.
  - `MacroRestartCoordinator` consome o evento.
  - Executa, nesta ordem: `IRestartContextService.Clear(...)` -> `IGameLoopService.RequestReset()` -> `ILevelFlowRuntimeService.StartGameplayDefaultAsync(...)`.
- `GameLoopCommandEventBridge` nao e listener de reset.

## LEGACY/Compat
- Conteudo legacy restante isolado em `Modules/Navigation/Legacy/`:
  - `Modules/Navigation/Legacy/ExitToMenuNavigationBridge.cs` foi removido no BATCH-CLEANUP-STD-1 por prova de tipo morto (callsite + GUID = 0).
  - `Modules/Navigation/Legacy/RestartNavigationBridge.cs` foi removido no BATCH-CLEANUP-STD-1 por prova de tipo morto (callsite + GUID = 0).
  - `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs` foi removido no BATCH-CLEANUP-STD-1 por prova de tipo morto (callsite + GUID = 0).
- Nao ha callsite canonico no `Infrastructure/Composition` para registrar bridges legacy de Navigation.

## Notas
- Limpeza v1 manteve o comportamento do baseline por analise estatica.
- Nao houve mudanca de contrato publico em `IGameNavigationService`/`GameNavigationService` neste passo.
- Batch cleanup `BATCH-CLEANUP-STD-1`: removed dead legacy bridges (proof: callsite + GUID).

## GRS-1.3b - ExitToMenu owner canonico
- Owner canonico de `GameExitToMenuRequestedEvent`: `Modules/Navigation/Runtime/ExitToMenuCoordinator.cs`.
- Ordem canonica do exit:
  1. `IGameLoopService.RequestReady()`
  2. `IGameNavigationService.ExitToMenuAsync(reason)`
- `GameLoopCommandEventBridge` nao consome mais `ExitToMenu`.
- O coordinator faz same-frame dedupe por `exit|reason=<normalized>` e coalesce de in-flight (`pending`, last reason wins), com logs `[OBS][Navigation] ExitToMenuRequested/Start/Queued/Deduped/Completed`.

## GRS-1.3c - GC final de ExitToMenu
- A observabilidade canonica de `ExitToMenu` ficou concentrada apenas em `Modules/Navigation/Runtime/ExitToMenuCoordinator.cs`.
- Nao ha mais logs `[OBS][LEGACY]`, mensagens `listener disabled` ou `bridge disabled` no trilho runtime de `ExitToMenu`.
- Invariantes finais:
  - `EventBus<GameExitToMenuRequestedEvent>.Register(...)` aparece uma unica vez, no coordinator.
  - `RegisterExitToMenuNavigationBridge(...)` nao existe mais no workspace local.
  - Contratos publicos e ordem do pipeline permaneceram intactos.
