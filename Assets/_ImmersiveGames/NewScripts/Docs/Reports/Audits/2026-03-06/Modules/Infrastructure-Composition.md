# Infrastructure-Composition Module Audit

## A) Scope
- Entra: `Infrastructure/Composition/**` (Global root, scene scope root, modules por stage).
- EventBus inputs: limpeza/prime de buses em `GlobalCompositionRoot.Events.cs`.
- Chamadas publicas: bootstrap de composicao (`GlobalCompositionRoot.Entry/Pipeline`).
- Unity callbacks: inicializacao de root global e root de cena.

## B) Outputs
- Registra servicos globais no DI para todos os modulos runtime.
- Inicializa bridges de Gate/InputMode/Navigation/LevelFlow.
- Efeito em Gate/InputMode: registra `GamePauseGateBridge`, `GameReadinessService`, `SceneFlowInputModeBridge`.

## C) DI Registration
- Owner principal: `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`.
- Stages e modulos: `Infrastructure/Composition/Modules/*.cs`.
- Registros relevantes:
  - Runtime policy: `IRuntimeModeProvider`, `IDegradedModeReporter`, `IWorldResetPolicy`.
  - Gates: `IUniqueIdFactory`, `ISimulationGateService`.
  - SceneFlow: `ISceneTransitionService`, `ISceneTransitionCompletionGate`, `ISceneFlowSignatureCache`, `IRouteResetPolicy`.
  - Navigation/Levels/ContentSwap/DevQA: `IGameNavigationService`, `ILevelFlowRuntimeService`, `ILevelMacroPrepareService`, `IContentSwapContextService`, `IContentSwapChangeService`.

## D) Canonical Runtime Rail
- `GlobalCompositionRoot.Entry.cs` -> `RegisterEssentialServicesOnly` (`GlobalCompositionRoot.Pipeline.cs`) -> `InstallCompositionModules()` por stage -> registro de bridges e coordenadores finais.

## E) LEGACY/Compat
- `RegisterRestartSnapshotContentSwapBridge()` existe em `GlobalCompositionRoot.NavigationInputModes.cs`, mas nao e chamado no pipeline atual.

## F) Redundancy Candidates
- `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs` metodo `RegisterRestartSnapshotContentSwapBridge()` sem callsite; risco baixo, mas gera superficie legacy viva.
- `Infrastructure/Composition/Modules/*.cs` com padrao quase igual de install por stage; risco baixo, alta repeticao.
- `Infrastructure/Composition/SceneScopeCompositionRoot.cs` e `GlobalCompositionRoot.*` com responsabilidades de DI em escopos diferentes; risco medio de ownership difuso.

## G) Deletion/Merge Plan (DOC-ONLY)
- Remover do pipeline APIs de registro legacy nao utilizadas (primeiro documentar e confirmar zero callsites).
- Consolidar padrao de modulos de composicao para reduzir boilerplate por stage.
- Separar arquivo de NavigationInputModes em sub-areas (Navigation, Levels, InputMode) para reduzir acoplamento.

| FilePath | Type (Runtime/Editor/Shared) | Public Entry Points | EventBus IN | EventBus OUT | DI Provides | Notes (Canon/Legacy/Dead) |
|---|---|---|---|---|---|---|
| Infrastructure/Composition/GlobalCompositionRoot.Entry.cs | Runtime | bootstrap root | N/A | N/A | inicia registro global | Canon |
| Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs | Runtime | `RegisterEssentialServicesOnly` | N/A | N/A | registra modulos por stage | Canon |
| Infrastructure/Composition/GlobalCompositionRoot.SceneFlowWorldLifecycle.cs | Runtime | registro SceneFlow/WorldLifecycle | N/A | N/A | `ISceneTransitionService`, gates, policies | Canon |
| Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs | Runtime | registro Navigation/Levels/InputMode | N/A | N/A | `IGameNavigationService`, `ILevelFlowRuntimeService`, bridges | Canon + Legacy hooks |
| Infrastructure/Composition/GlobalCompositionRoot.PauseReadiness.cs | Runtime | registro Pause/Readiness | N/A | N/A | `GamePauseGateBridge`, `GameReadinessService` | Canon |
| Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs | Runtime | registro QA installers | N/A | N/A | installers dev/qa | Canon |
| Infrastructure/Composition/SceneScopeCompositionRoot.cs | Runtime | scene-scope composition | N/A | N/A | scene services e hooks | Canon |
| Infrastructure/Composition/Modules/NavigationCompositionModule.cs | Runtime | stage install | N/A | N/A | delega install navigation | Canon |