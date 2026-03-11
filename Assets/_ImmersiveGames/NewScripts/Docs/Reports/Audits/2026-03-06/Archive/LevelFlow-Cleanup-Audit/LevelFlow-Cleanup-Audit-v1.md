# LevelFlow Cleanup Audit v1 (LF-1.1)

Date: 2026-03-06
Scope: `Modules/LevelFlow/**` (with read-only evidence from `Modules/SceneFlow/**`, `Modules/Navigation/**`, `Modules/GameLoop/**`, `Infrastructure/Composition/**`).

## Status atual (Baseline 3.1 — NAO MEXER)

- Trilho canonico de runtime permanece:
  - `ILevelFlowRuntimeService` -> `LevelFlowRuntimeService`
  - `ILevelMacroPrepareService` -> `LevelMacroPrepareService`
  - `ILevelSwapLocalService` -> `LevelSwapLocalService`
  - `IPostLevelActionsService` -> `PostLevelActionsService`
- Registro canonico de DI confirmado em `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`.
- Nenhuma logica/ordem/contrato publico foi alterado no cleanup LF-1.1.

## Ownership map

| Componente | Owner de | Nao-owner de | Logs ancora |
|---|---|---|---|
| `Runtime/LevelFlowRuntimeService.cs` | Start/restart canonico de gameplay | Reset world e preparo de gate SceneFlow | `[OBS][LevelFlow] StartGameplayDefaultAsync ...` |
| `Runtime/LevelMacroPrepareService.cs` | `LevelPrepare` na fase macro + publish `LevelSelectedEvent` | Swap local intra-macro | `[OBS][LevelFlow] LevelPrepared ...` |
| `Runtime/LevelSwapLocalService.cs` | Swap local intra-macro + publish `LevelSelectedEvent` e `LevelSwapLocalAppliedEvent` | Start macro padrao | `[OBS][LevelFlow] LevelSwapLocalApplied ...` |
| `Runtime/LevelStageOrchestrator.cs` | IntroStage trigger/dedupe por completion + swap applied | Selecao/aplicacao de level | `[OBS][LevelFlow] IntroStage...` |
| `Runtime/PostLevelActionsService.cs` | Proximo nivel/restart pos-run | Scene transition core | `[OBS][LevelFlow] NextLevel...` / restart logs |
| `Legacy/Bindings/LevelCatalogAsset.cs` | Compat catalog (`LevelId -> route/content`) | Pipeline canonico Baseline 3.1 | `[OBS][LEGACY][SceneFlow] MacroRouteResolvedViaLevelCatalogLegacy ...` |

## Inventario A/B/C (com evidencia)

### A) Canonicos (pipeline/dependencia direta do baseline)

- `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs`
- `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs`
- `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs`
- `Modules/LevelFlow/Runtime/PostLevelActionsService.cs`
- `Modules/LevelFlow/Runtime/RestartContextService.cs`
- `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs`
- `Modules/LevelFlow/Runtime/LevelSelectedEvent.cs`
- `Modules/LevelFlow/Runtime/LevelSwapLocalAppliedEvent.cs`
- `Modules/LevelFlow/Config/LevelCollectionAsset.cs`
- `Modules/LevelFlow/Config/LevelDefinitionAsset.cs`
- `Modules/LevelFlow/Config/SceneBuildIndexRef.cs`
- `Modules/LevelFlow/Runtime/LevelAdditiveSceneRuntimeApplier.cs`

Evidence (`rg`):
```text
Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs:303 RegisterGlobal<ILevelSwapLocalService>
Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs:316 RegisterGlobal<ILevelFlowRuntimeService>
Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs:337 RegisterGlobal<IPostLevelActionsService>
Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs:352 RegisterGlobal<ILevelMacroPrepareService>
Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs:41 TryGetGlobal<ILevelMacroPrepareService>
Modules/Navigation/Runtime/MacroRestartCoordinator.cs:186 TryGetGlobal<ILevelFlowRuntimeService>
Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:18 EventBus<LevelSelectedEvent>.Register
Modules/LevelFlow/Runtime/LevelSwapLocalService.cs:170 EventBus<LevelSwapLocalAppliedEvent>.Raise
Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs:28 EventBus<LevelSwapLocalAppliedEvent>.Register
```

### B) Legacy/Compat (movidos nesta etapa)

- `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs`

Racional:
- Sem callsite no pipeline canonico de runtime (`Infrastructure/Composition/**` + runtime services canonicos).
- Uso remanescente em editor/QA/legacy fields (compat), nao no trilho baseline A-E.

Evidence (`rg`):
```text
Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs:77 LevelCatalogAsset (editor validation)
Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs:21 levelCatalog (legacy hidden field)
QA/LevelFlow/Compat/ScenarioB/LevelFlowCompatScenarioBEditor.cs:54 LevelCatalogAsset
QA/LevelFlow/NTo1/LevelFlowNTo1QaEditor.cs:29 LevelCatalogAsset
```

### C) Dead candidates (sem delete em LF-1.1)

- Nenhum arquivo confirmado como morto com evidencia conclusiva nesta etapa.
- Itens com baixa atividade, mas com risco de reflexao/uso editor (`LevelCatalogAsset` stack) foram tratados como `Legacy/Compat` e nao como dead.

## Mudancas aplicadas (moves)

- `Modules/LevelFlow/Bindings/LevelCatalogAsset.cs` -> `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs`
- `Modules/LevelFlow/Runtime/ILevelFlowService.cs` -> `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs`
- `Modules/LevelFlow/Runtime/ILevelMacroRouteCatalog.cs` -> `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs`
- `Modules/LevelFlow/Runtime/ILevelContentResolver.cs` -> `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs`
- Todos os `.meta` correspondentes foram movidos junto.

## Validacao estatica (behavior-preserving)

Checks executados:
```text
rg -n "ILevelFlowService|ILevelMacroRouteCatalog|ILevelContentResolver|LevelCatalogAsset" Infrastructure/Composition Modules/LevelFlow/Runtime Modules/Navigation Modules/SceneFlow Modules/GameLoop
rg -n "EventBus<LevelSelectedEvent>\.(Raise|Register)" Modules
rg -n "EventBus<LevelSwapLocalAppliedEvent>\.(Raise|Register)" Modules
rg -n "RegisterGlobal<ILevelSwapLocalService>|RegisterGlobal<ILevelFlowRuntimeService>|RegisterGlobal<IPostLevelActionsService>|RegisterGlobal<ILevelMacroPrepareService>" Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs
```

Resultado:
- Nenhuma nova instancia/registro canonico depende dos arquivos movidos para `Modules/LevelFlow/Legacy/**`.
- Owners canonicos permanecem unicos no pipeline:
  - `LevelSelectedEvent`: 2 publishers esperados (`LevelMacroPrepareService`, `LevelSwapLocalService`) + 1 consumer canonico (`LevelSelectedRestartSnapshotBridge`).
  - `LevelSwapLocalAppliedEvent`: 1 publisher (`LevelSwapLocalService`) + 1 consumer (`LevelStageOrchestrator`).
- Declaracao: **Nada de comportamento alterado** (por analise estatica de callsites/DI/eventos).
