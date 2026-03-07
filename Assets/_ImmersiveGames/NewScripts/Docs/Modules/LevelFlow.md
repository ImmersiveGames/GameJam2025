# LevelFlow

## Status atual (Baseline 3.1)

- Baseline 3.1 mantida sem alteracao de comportamento.
- Trilho canonico:
  - `ILevelFlowRuntimeService` (`LevelFlowRuntimeService`) para start/restart padrao.
  - `ILevelMacroPrepareService` (`LevelMacroPrepareService`) na fase macro de `SceneFlow`.
  - `ILevelSwapLocalService` (`LevelSwapLocalService`) para swap intra-macro.
  - `IPostLevelActionsService` (`PostLevelActionsService`) para next/restart no pos-run.
- Trilha antiga de catalogo (`LevelCatalogAsset` + interfaces antigas) foi isolada em `Modules/LevelFlow/Legacy/**`.

## Ownership

| Componente | Owner de | Nao-owner de | Anchors |
|---|---|---|---|
| `Runtime/LevelFlowRuntimeService.cs` | Start/restart gameplay default | Swap local e gates SceneFlow | `StartGameplayDefaultAsync` |
| `Runtime/LevelMacroPrepareService.cs` | Preparar/limpar nivel na entrada macro + `LevelSelectedEvent` | Swap local intra-macro | `[OBS][LevelFlow] LevelPrepared` |
| `Runtime/LevelSwapLocalService.cs` | Troca local + `LevelSelectedEvent` + `LevelSwapLocalAppliedEvent` | Navegacao macro | `[OBS][LevelFlow] LevelSwapLocalApplied` |
| `Runtime/LevelStageOrchestrator.cs` | IntroStage trigger/dedupe | Selecao/aplicacao de nivel | `SceneTransitionCompleted + LevelSwapLocalApplied` |
| `Legacy/Bindings/LevelCatalogAsset.cs` | Compatibilidade `LevelId`/catalogo legado | Pipeline canonico de runtime | `[OBS][LEGACY][SceneFlow] MacroRouteResolvedViaLevelCatalogLegacy` |

## Timeline (encaixe no A-E)

1. `SceneFlow` entra em gameplay route.
2. `MacroLevelPrepareCompletionGate` chama `ILevelMacroPrepareService.PrepareOrClearAsync`.
3. `LevelMacroPrepareService` seleciona/aplica nivel e publica `LevelSelectedEvent`.
4. `WorldResetCommands.ResetLevelAsync(...)` executa reset de nivel no trilho atual.
5. Durante gameplay, `ILevelSwapLocalService.SwapLocalAsync(...)` pode trocar nivel localmente e publicar `LevelSwapLocalAppliedEvent`.
6. `LevelStageOrchestrator` reage a `SceneTransitionCompletedEvent` e `LevelSwapLocalAppliedEvent` para IntroStage.
7. `IPostLevelActionsService` executa next/restart no pos-run sem alterar ownership acima.

## LEGACY/Compat

- `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs`

Notas:
- Esses itens continuam existentes para editor/QA/compat, mas fora do trilho canonico Baseline 3.1.
- Candidatos a remocao definitiva ficam para LF-1.2 com validacao adicional.

## LF-1.2 monotonic selectionVersion

- `selectionVersion` agora e monotônico por snapshot histórico (`last`) no `LevelMacroPrepareService`.
- `RestartContextService.Clear(...)` limpa apenas o `current`, mantendo `last`/contador para evitar rewind após MacroRestart.
- Isso evita regressão para `v=1` quando o `current` foi limpo, sem alterar contratos públicos/event payloads.
