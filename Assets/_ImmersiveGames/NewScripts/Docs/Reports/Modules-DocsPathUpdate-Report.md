# Modules Docs Path Update Report

## Old reference -> New reference
| Old reference | New reference |
| --- | --- |
| `Runtime/Scene/SceneTransitionService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` |
| `Runtime/Scene/SceneTransitionEvents.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs` |
| `Runtime/SceneFlow/SceneFlowAdapters.cs` (Fade) | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Adapters/SceneFlowFadeAdapter.cs` |
| `Runtime/SceneFlow/SceneFlowAdapters.cs` (Factory) | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Runtime/SceneFlowAdapterFactory.cs` |
| `Runtime/SceneFlow/SceneFlowLoadingService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs` |
| `Presentation/LoadingHud/LoadingHudService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs` |
| `Presentation/LoadingHud/LoadingHudController.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Bindings/LoadingHudController.cs` |
| `NewScripts/Presentation/LoadingHud/ILoadingHudService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/ILoadingHudService.cs` |
| `Runtime/Mode/IRuntimeModeProvider.cs` | `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/IRuntimeModeProvider.cs` |
| `Runtime/Mode/UnityRuntimeModeProvider.cs` | `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/UnityRuntimeModeProvider.cs` |
| `Runtime/Mode/IDegradedModeReporter.cs` | `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/IDegradedModeReporter.cs` |
| `Runtime/Mode/DegradedModeReporter.cs` | `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/DegradedModeReporter.cs` |
| `Runtime/Bootstrap/GlobalBootstrap.cs` | `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.cs` |
| `Runtime/Bootstrap/SceneBootstrapper.cs` | `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.cs` |
| `Runtime/InputSystems/InputModeSceneFlowBridge.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` |
| `Runtime/Gates/SimulationGateTokens.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/Gates/SimulationGateTokens.cs` |
| `Gameplay/CoreGameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs` |
| `Gameplay/CoreGameplay/ContentSwap/ContentSwapContextService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/ContentSwapContextService.cs` |
| `QA/ContentSwap/ContentSwapQaContextMenu.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs` |
| `Gameplay/CoreGameplay/GameLoop/GameRunOutcomeService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Services/GameRunOutcomeService.cs` |
| `Gameplay/CoreGameplay/GameLoop/GameRunStatusService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Services/GameRunStateService.cs` |
| `Runtime/GameLoop/Bridges/GameRunOutcomeEventInputBridge.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Bridges/GameRunOutcomeCommandBridge.cs` |
| `Gameplay/CoreGameplay/GameLoop/GameLoopSceneFlowCoordinator.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs` |
| `Gameplay/CoreGameplay/GameLoop/GameLoopStateMachine.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/GameLoopStateMachine.cs` |
| `Runtime/Navigation/ExitToMenuNavigationBridge.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/ExitToMenuNavigationBridge.cs` |
| `Presentation/PostGame/PostGameOverlayController.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/PostGame/Bindings/PostGameOverlayController.cs` |
| `Gameplay/CoreGameplay/PostGame/PostPlayOwnershipService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/PostGame/PostGameOwnershipService.cs` |
| `Lifecycle/World/Reset/Application/WorldResetService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs` |
| `Runtime/World/Bridges/SceneFlow/WorldLifecycleSceneFlowResetDriver.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs` |
| `Runtime/World/Spawn/WorldDefinition.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Spawning/Definitions/WorldDefinition.cs` |
| `Runtime/World/Spawn/PlayerSpawnService.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Spawning/PlayerSpawnService.cs` |
| `Assets/_ImmersiveGames/NewScripts/Gameplay/CoreGameplay/Reset/*` | `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/RunRearm/Runtime/*` |
| `Runtime/Reset/PlayersResetParticipant.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/RunRearm/Interop/PlayersRunRearmWorldParticipant.cs` |
| `QA/GameplayReset/GameplayResetRequestQaDriver.cs` | `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/RunRearm/Dev/RunRearmRequestDevDriver.cs` |
| `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/...` | `Assets/_ImmersiveGames/NewScripts/Modules/Levels/...` |
| `Runtime/World/README.md` | `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/README.md` |

## Docs alterados
- `Assets/_ImmersiveGames/NewScripts/Docs/CHANGELOG.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/Guides.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/Overview/Overview.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0009-FadeSceneFlow.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0017-LevelManager-Config-Catalog.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-01-31/ADR-Sync-Audit-NewScripts.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-02-01/ADR-0014-Integration-Map.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Modules-DocsPathUpdate-Report.md`

## TODOs (referencias nao resolvidas)
- `QA/GameplayReset-QA.md` (sem equivalente atual em Docs/Modules)
- `QA/Deprecated` (sem pasta equivalente em Modules)

## Observacoes de padronizacao
- Paths de modulos normalizados para `Assets/_ImmersiveGames/NewScripts/Modules/<Feature>/{Runtime|Bindings|Dev|Interop}` quando aplicavel.
- Renomeacoes consolidadas: GameplayReset -> RunRearm, GlobalBootstrap -> GlobalCompositionRoot, SceneBootstrapper -> SceneScopeCompositionRoot.
- Ferramentas de QA migradas para `Dev` (ex.: `RunRearmRequestDevDriver`, `ContentSwapDevContextMenu`, `LevelDev*`).
