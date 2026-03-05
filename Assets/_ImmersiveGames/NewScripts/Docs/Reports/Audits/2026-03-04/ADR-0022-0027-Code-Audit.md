# Auditoria de conformidade ADR-0022..ADR-0027 vs código atual

Data: 2026-03-04  
Fonte de verdade: **código atual** (não logs/descrição histórica de ADR)

## Resumo executivo

| ADR | Status de implementação | Observação curta |
|---|---|---|
| ADR-0022 | Implementado | Assinaturas separadas (macro/level) + dedupe macro e dedupe por `SelectionVersion`. |
| ADR-0023 | Implementado | Dois comandos de reset (`Macro`/`Level`) + eventos V2 request/completion. |
| ADR-0024 | Implementado | `macroRouteRef` obrigatório + catálogo com caches por macro + fallback determinístico de seleção. |
| ADR-0025 | Implementado | LevelPrepare explícito no completion gate antes do FadeOut. |
| ADR-0026 | Implementado | API/runtime de swap local pronta + serviço dedicado + QA proof sem transição macro. |
| ADR-0027 | Implementado | IntroStage orquestrado por Level + ações pós-level (Restart/Next/Exit) em serviço próprio. |

---

## ADR-0022 — Assinaturas e Dedupe por domínio

**Implementado**

### Evidências

- Macro signature canônica em `SceneTransitionContext.ContextSignature` + `ComputeSignature(...)`.  
  Arquivo: `Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs` (struct `SceneTransitionContext`, método `ComputeSignature`).
- `SceneTransitionSignature.Compute(...)` retorna `context.ContextSignature`.  
  Arquivo: `Modules/SceneFlow/Transition/Runtime/SceneTransitionSignature.cs` (classe `SceneTransitionSignature`, método `Compute`).
- Level signature em `LevelContextSignature.Create(...)`.  
  Arquivo: `Modules/LevelFlow/Runtime/LevelContextSignature.cs` (struct `LevelContextSignature`, método `Create`).
- Dedupe macro em `SceneTransitionService.ShouldDedupe(...)`.  
  Arquivo: `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` (classe `SceneTransitionService`, métodos `ShouldDedupe`, `MarkStarted`, `MarkCompleted`).
- Dedupe macro no driver de reset em `WorldLifecycleSceneFlowResetDriver.ShouldSkipDuplicate(...)`.  
  Arquivo: `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs` (classe `WorldLifecycleSceneFlowResetDriver`, métodos `ShouldSkipDuplicate`, `MarkInFlight`, `MarkCompleted`).
- Dedupe de level por versão em `LevelStageOrchestrator` (`_lastProcessedSelectionVersion`).  
  Arquivo: `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs` (métodos `OnSceneTransitionCompleted`, `OnLevelSwapLocalApplied`).

---

## ADR-0023 — MacroReset vs LevelReset

**Implementado**

### Evidências

- Contrato explícito de dois resets.  
  Arquivo: `Modules/WorldLifecycle/Runtime/IWorldResetCommands.cs` (interface `IWorldResetCommands`).
- Implementação de reset macro/level.  
  Arquivo: `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs` (classe `WorldResetCommands`, métodos `ResetMacroAsync`, `ResetLevelAsync`).
- Enum de domínio de reset.  
  Arquivo: `Modules/WorldLifecycle/Runtime/ResetKind.cs` (enum `ResetKind`).
- Eventos V2 para requested/completed com contexto macro+level.  
  Arquivo: `Modules/WorldLifecycle/Runtime/WorldLifecycleResetV2Events.cs` (structs `WorldLifecycleResetRequestedV2Event`, `WorldLifecycleResetCompletedV2Event`).
- Registro no DI global do comando de reset e driver SceneFlow→WorldLifecycle.  
  Arquivo: `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs` (método `InstallWorldLifecycleServices`).

---

## ADR-0024 — LevelCatalog por MacroRoute e seleção ativa

**Implementado**

### Evidências

- `macroRouteRef` obrigatório no `LevelDefinition` como rota canônica.  
  Arquivo: `Modules/LevelFlow/Runtime/LevelDefinition.cs` (campo `macroRouteRef`, método `ResolveMacroRouteId`).
- Catálogo mantém caches por macro/level e resolve ambiguidades.  
  Arquivo: `Modules/LevelFlow/Bindings/LevelCatalogAsset.cs` (campos `_levelToMacroRouteCache`, `_macroRouteToLevelCache`, `_macroRouteToLevelsCache`; método `EnsureCache`).
- API de navegação por macro dentro do catálogo.  
  Arquivo: `Modules/LevelFlow/Bindings/LevelCatalogAsset.cs` (métodos `TryGetLevelsForMacroRoute`, `TryGetNextLevelInMacro`, `TryResolveMacroRouteId`).
- Seleção default determinística no prepare macro quando snapshot não pertence ao macro.  
  Arquivo: `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs` (método `PrepareAsync`, branches `snapshotBelongsToMacro`, `source='catalog_first'`, `selectedLevelId = levelIds[0]`).

---

## ADR-0025 — LevelPrepare antes do FadeOut

**Implementado**

### Evidências

- Ordem do pipeline: `ScenesReady -> AwaitCompletionGateAsync -> BeforeFadeOut -> FadeOut`.  
  Arquivo: `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` (método `TransitionAsync`).
- Gate composto com etapa de level prepare.  
  Arquivo: `Infrastructure/Composition/GlobalCompositionRoot.SceneFlowWorldLifecycle.cs` (método `RegisterSceneFlowNative`, uso de `MacroLevelPrepareCompletionGate`).
- Etapa explícita de level prepare no gate de completion.  
  Arquivo: `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs` (método `AwaitBeforeFadeOutAsync`).
- Serviço de domínio da etapa de prepare com reset local.  
  Arquivo: `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs` (método `PrepareAsync`, chamada `IWorldResetCommands.ResetLevelAsync`).
- Registro do `ILevelMacroPrepareService` no DI global.  
  Arquivo: `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs` (registro de `LevelMacroPrepareService`).

---

## ADR-0026 — Swap local sem transição macro

**Implementado**

### Evidências

- API pública para swap local.  
  Arquivo: `Modules/LevelFlow/Runtime/ILevelFlowRuntimeService.cs` (método `SwapLevelLocalAsync`).
- Runtime delega swap local para serviço dedicado.  
  Arquivo: `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs` (método `SwapLevelLocalAsync`).
- Serviço de swap local implementa fluxo completo (seleção, versionamento, reset local, evento aplicado).  
  Arquivo: `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs` (método `SwapLocalAsync`, chamadas `PublishLevelSelected`, `ResetLevelAsync`, `PublishLevelSwapLocalApplied`).
- Registro DI do `ILevelSwapLocalService`.  
  Arquivo: `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs` (registro de `LevelSwapLocalService`).
- Prova QA sem transição macro.  
  Arquivo: `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs` (método `Qa_SwapLocal_ProofNoMacroTransition`, contagem de `SceneTransitionStartedEvent`).

---

## ADR-0027 — IntroStage e PostLevel no domínio Level

**Implementado**

### Evidências

- IntroStage iniciada por orquestrador de level em transição completa e swap local.  
  Arquivo: `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs` (métodos `OnSceneTransitionCompleted`, `OnLevelSwapLocalApplied`).
- Dedupe por `SelectionVersion` no orquestrador.  
  Arquivo: `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs` (campo `_lastProcessedSelectionVersion` e guardas por versão).
- Contrato de ações pós-level (`Restart`, `NextLevel`, `ExitToMenu`).  
  Arquivo: `Modules/LevelFlow/Runtime/IPostLevelActionsService.cs`.
- Implementação das ações pós-level.  
  Arquivo: `Modules/LevelFlow/Runtime/PostLevelActionsService.cs` (métodos `RestartLevelAsync`, `NextLevelAsync`, `ExitToMenuAsync`).
- UI de pós-jogo consumindo o serviço de domínio.  
  Arquivo: `Modules/PostGame/Bindings/PostGameOverlayController.cs` (injeção `IPostLevelActionsService`, chamadas em `OnClickRestart`/`OnClickExitToMenu`).
- Registro DI do orquestrador e do serviço pós-level.  
  Arquivo: `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs` (métodos de registro de `LevelStageOrchestrator` e `IPostLevelActionsService`).

---

## Observações finais da auditoria

- Os ADRs 0022..0027 foram atualizados para refletir o shape atual do código e remover dependência de “evidência de log” como fonte primária.
- Não houve alteração de código runtime nesta tarefa.
