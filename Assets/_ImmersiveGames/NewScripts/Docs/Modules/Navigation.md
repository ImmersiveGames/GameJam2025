# Navigation

## CANÔNICO
- Owner canônico de restart: `Modules/Navigation/Runtime/MacroRestartCoordinator.cs`.
- Trilho canônico de restart:
  - `GameCommands` publica `GameResetRequestedEvent`.
  - `MacroRestartCoordinator` consome o evento.
  - Executa, nesta ordem: `IRestartContextService.Clear(...)` -> `IGameLoopService.RequestReset()` -> `ILevelFlowRuntimeService.StartGameplayDefaultAsync(...)`.
- `GameLoopCommandEventBridge` **não** é listener de reset (apenas pausa/resume/exit).

## LEGACY/Compat
- Conteúdo legacy restante ficou isolado em `Modules/Navigation/Legacy/`:
  - `Modules/Navigation/Legacy/RestartNavigationBridge.cs` (stub desativado)
  - `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs` (no-op)
- Não há callsite canônico no `Infrastructure/Composition` para registrar esses bridges.

## Notas
- Limpeza v1 manteve o comportamento do baseline por análise estática.
- Não houve mudança de contrato público em `IGameNavigationService`/`GameNavigationService` neste passo.

## GRS-1.3b - ExitToMenu owner can?nico
- Owner can?nico de `GameExitToMenuRequestedEvent`: `Modules/Navigation/Runtime/ExitToMenuCoordinator.cs`.
- Ordem can?nica do exit:
  1. `IGameLoopService.RequestReady()`
  2. `IGameNavigationService.ExitToMenuAsync(reason)`
- `GameLoopCommandEventBridge` n?o consome mais `ExitToMenu`.
- `ExitToMenuNavigationBridge` foi movido para `Modules/Navigation/Legacy/` e permanece como stub LEGACY/no-op; o composition root n?o o registra mais.
- O coordinator faz same-frame dedupe por `exit|reason=<normalized>` e coalesce de in-flight (`pending`, last reason wins), com logs `[OBS][Navigation] ExitToMenuRequested/Start/Queued/Deduped/Completed`.

