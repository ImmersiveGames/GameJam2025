# Auditoria de Conformidade ADR-0022..ADR-0027 (Fonte de verdade: código)

Data: 2026-03-05
Escopo auditado: `Assets/_ImmersiveGames/NewScripts/Modules` e `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition`

## Resumo executivo

| ADR | Status | Evidência principal |
|---|---|---|
| ADR-0022 | **Implementado** | `SceneTransitionContext.ContextSignature`, `LevelContextSignature`, dedupe macro em `SceneTransitionService.ShouldDedupe`, dedupe de reset macro em `WorldLifecycleSceneFlowResetDriver.ShouldSkipDuplicate`, dedupe por versão em `LevelStageOrchestrator`. |
| ADR-0023 | **Implementado** | `IWorldResetCommands`, `WorldResetCommands.ResetMacroAsync/ResetLevelAsync`, eventos `WorldLifecycleResetRequestedV2Event`/`WorldLifecycleResetCompletedV2Event`. |
| ADR-0024 | **Implementado** | `LevelDefinition.macroRouteRef` obrigatório + caches por macro em `LevelCatalogAsset`. |
| ADR-0025 | **Implementado** | `MacroLevelPrepareCompletionGate` + `LevelMacroPrepareService` integrados ao gate antes do FadeOut em `SceneTransitionService`. |
| ADR-0026 | **Implementado** | `ILevelFlowRuntimeService.SwapLevelLocalAsync` + `LevelSwapLocalService` + QA proof em `LevelFlowDevContextMenu`. |
| ADR-0027 | **Implementado** | IntroStage no `LevelStageOrchestrator` + `IPostLevelActionsService/PostLevelActionsService` com Restart/NextLevel/ExitToMenu. |

## Evidências detalhadas por requisito

### 1) MacroSignature: SceneFlow/Transition (`SceneTransitionContext.ContextSignature`)
- `SceneTransitionContext` possui propriedade `ContextSignature` e computa assinatura canônica em `ComputeSignature(...)`.
- `SceneTransitionSignature.Compute(context)` retorna `context.ContextSignature`.
- `SceneTransitionService` usa assinatura para logs, dedupe e correlação.

**Arquivos/símbolos:**
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs` → `SceneTransitionContext.ContextSignature`, `ComputeSignature`.
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionSignature.cs` → `Compute(...)`.
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` → `TransitionAsync(...)`.

### 2) LevelSignature: LevelFlow (`LevelContextSignature`)
- `LevelContextSignature` implementa contrato de assinatura de level com `Create(levelId, routeId, reason, contentId)`.
- Publicações de seleção usam `LevelContextSignature` no `LevelSelectedEvent`.

**Arquivos/símbolos:**
- `Modules/LevelFlow/Runtime/LevelContextSignature.cs` → `LevelContextSignature`, `Create(...)`.
- `Modules/LevelFlow/Runtime/LevelSelectedEvent.cs` → `LevelSignature`.
- `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs` e `LevelSwapLocalService.cs` → `PublishLevelSelected(...)`.

### 3) Dedupe macro: `SceneTransitionService.ShouldDedupe` + `WorldLifecycleSceneFlowResetDriver`
- Dedupe de transição macro implementado em `SceneTransitionService.ShouldDedupe(...)`.
- Dedupe de reset em `WorldLifecycleSceneFlowResetDriver.ShouldSkipDuplicate(...)` + caches `_inFlightSignatures/_completedTicks`.

**Arquivos/símbolos:**
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` → `ShouldDedupe(...)`.
- `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs` → `ShouldSkipDuplicate(...)`, `MarkInFlight(...)`, `MarkCompleted(...)`.

### 4) Dedupe level: `SelectionVersion` + `LevelStageOrchestrator`
- `LevelSelectedEvent` carrega `SelectionVersion`.
- `LevelStageOrchestrator` dedupa por `_lastProcessedSelectionVersion` tanto em SceneFlowCompleted quanto em LevelSwapLocalApplied.

**Arquivos/símbolos:**
- `Modules/LevelFlow/Runtime/LevelSelectedEvent.cs` → `SelectionVersion`.
- `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs` → `_lastProcessedSelectionVersion`, comparações de versão.

### 5) MacroReset/LevelReset: `IWorldResetCommands` + `WorldResetCommands` + eventos V2
- Interface separa `ResetMacroAsync` e `ResetLevelAsync`.
- Implementação publica eventos V2 requested/completed para ambos os tipos.

**Arquivos/símbolos:**
- `Modules/WorldLifecycle/Runtime/IWorldResetCommands.cs`.
- `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs`.
- `Modules/WorldLifecycle/Runtime/WorldLifecycleResetV2Events.cs`.

### 6) LevelCatalog por macro: `LevelDefinition.macroRouteRef` + caches em `LevelCatalogAsset`
- `macroRouteRef` obrigatório em `ResolveMacroRouteId()`.
- `LevelCatalogAsset` mantém caches por macro/default/listagem/ambiguidade.

**Arquivos/símbolos:**
- `Modules/LevelFlow/Runtime/LevelDefinition.cs` → `macroRouteRef`, `ResolveMacroRouteId()`.
- `Modules/LevelFlow/Bindings/LevelCatalogAsset.cs` → `_macroRouteToDefaultLevelCache`, `_macroRouteToLevelsCache`, `_levelToMacroRouteCache`, `TryGetDefaultLevelId`, `TryGetNextLevelInMacro`, `TryGetLevelsForMacroRoute`.

### 7) LevelPrepare antes do FadeOut: `MacroLevelPrepareCompletionGate` + `LevelMacroPrepareService` + DI no `GlobalCompositionRoot`
- Gate composto executa `PrepareAsync(...)` antes de liberar fade out no gameplay.
- `LevelMacroPrepareService` resolve level por macro e executa `ResetLevelAsync`.
- Registro no DI global confirmado em composição.

**Arquivos/símbolos:**
- `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs`.
- `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs`.
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlowWorldLifecycle.cs`.
- `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`.

### 8) Swap local sem transição macro: `ILevelFlowRuntimeService.SwapLevelLocalAsync` + `LevelSwapLocalService` + QA proof (`LevelFlowDevContextMenu`)
- API exposta na interface e implementada no runtime.
- Execução local usa LevelReset (não navegação macro).
- QA menu contém prova explícita de ausência de macro transition.

**Arquivos/símbolos:**
- `Modules/LevelFlow/Runtime/ILevelFlowRuntimeService.cs` → `SwapLevelLocalAsync(...)`.
- `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs` → `SwapLevelLocalAsync(...)`.
- `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs` → `SwapLocalAsync(...)`.
- `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs` → `QA/LevelFlow/SwapLocal/ProofNoMacroTransition->level.2`, checagem de `SceneTransitionStartedEvent` em `NextLevelAsync()`.

### 9) IntroStage no domínio Level: `LevelStageOrchestrator`
- `LevelStageOrchestrator` dispara IntroStage após transição macro e após swap local.

**Arquivos/símbolos:**
- `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs` → `OnSceneTransitionCompleted(...)`, `OnLevelSwapLocalApplied(...)`.

### 10) PostLevel actions: `IPostLevelActionsService`/`PostLevelActionsService` (Restart/Exit/NextLevel)
- Interface e implementação dedicadas para ações de pós-level.

**Arquivos/símbolos:**
- `Modules/LevelFlow/Runtime/IPostLevelActionsService.cs`.
- `Modules/LevelFlow/Runtime/PostLevelActionsService.cs` → `RestartLevelAsync(...)`, `NextLevelAsync(...)`, `ExitToMenuAsync(...)`.

## Conclusão

As ADRs 0022..0027 foram auditadas contra o código atual e estão **implementadas** no shape hoje existente. As pendências observadas são de hardening (principalmente testes automatizados), não de ausência estrutural dos contratos.
