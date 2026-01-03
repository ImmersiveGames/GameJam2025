# Baseline Audit — NewScripts (2026-01-03)

## Objetivo
Consolidar a **baseline real** (código + evidência) para os módulos críticos do NewScripts, indicando o que está:
- **Validado** (com QA/Reports/logs existentes), ou
- **Implementado, não validado** (sem evidência documentada).

> Escopo: `Assets/_ImmersiveGames/NewScripts` (Unity 6, multiplayer local).

## Legenda de status
- **Validado (QA/Logs):** há evidência explícita em QA/Reports.
- **Implementado, não validado:** existe no código, mas **não há** evidência/log/QA documentado.

## Matriz de evidência (baseline)

| Item | Evidência em código (paths/classes) | Evidência QA/Reports/Logs | Status |
|---|---|---|---|
| **SceneTransitionService pipeline + eventos** (`Started/ScenesReady/Completed`) | `Infrastructure/Scene/SceneTransitionService.cs`, `Infrastructure/Scene/SceneTransitionEvents.cs` | `Reports/SceneFlow-Smoke-Result.md` (logs `Started/ScenesReady/Completed`) | **Validado (QA/Logs)** |
| **Fade (INewScriptsFadeService + FadeScene + adapter + resolver)** | `Infrastructure/SceneFlow/Fade/INewScriptsFadeService.cs`, `Infrastructure/SceneFlow/Fade/NewScriptsFadeService.cs`, `Infrastructure/SceneFlow/Fade/NewScriptsFadeController.cs`, `Infrastructure/SceneFlow/NewScriptsSceneFlowAdapters.cs`, `Infrastructure/SceneFlow/NewScriptsSceneTransitionProfileResolver.cs` | `Reports/Report-SceneFlow-Production-Log-2025-12-31.md` (profiles resolvidos + ordem com FadeInCompleted), `Reports/SceneFlow-Production-EndToEnd-Validation.md` (master) | **Validado (QA/Logs)** |
| **Loading HUD (INewScriptsLoadingHudService + SceneFlowLoadingService)** | `Infrastructure/SceneFlow/Loading/INewScriptsLoadingHudService.cs`, `Infrastructure/SceneFlow/Loading/NewScriptsLoadingHudService.cs`, `Infrastructure/SceneFlow/Loading/SceneFlowLoadingService.cs` | `Reports/Report-SceneFlow-Production-Log-2025-12-31.md` (FadeInCompleted → Show, BeforeFadeOut → Hide, Completed → Safety Hide), `Reports/SceneFlow-Smoke-Result.md` | **Validado (QA/Logs)** |
| **GameReadinessService + SimulationGate (token SceneTransition)** | `Infrastructure/Scene/GameReadinessService.cs`, `Infrastructure/Gate/ISimulationGateService.cs`, `Infrastructure/Gate/SimulationGateService.cs`, `Infrastructure/Gate/SimulationGateTokens.cs` | Não há log/QA explícito do token `flow.scene_transition` nos reports atuais. | **Implementado, não validado** |
| **Pause gate (token `state.pause`)** | `Infrastructure/Gate/GamePauseGateBridge.cs`, `Infrastructure/Gate/SimulationGateTokens.cs` | `Reports/Report-SceneFlow-Production-Log-2025-12-31.md` e `Reports/SceneFlow-Production-Evidence-2025-12-31.md` (Acquire/Release `state.pause`) | **Validado (QA/Logs)** |
| **World reset gate (token `WorldLifecycle.WorldReset`)** | `Infrastructure/WorldLifecycle/Runtime/WorldLifecycleOrchestrator.cs` (`WorldLifecycleTokens.WorldResetToken`) | `Reports/Report-SceneFlow-Production-Log-2025-12-31.md` (Gate Acquired `WorldLifecycle.WorldReset`) | **Validado (QA/Logs)** |
| **WorldLifecycleRuntimeCoordinator + WorldLifecycleResetCompletionGate** | `Infrastructure/WorldLifecycle/Runtime/WorldLifecycleRuntimeCoordinator.cs`, `Infrastructure/WorldLifecycle/Runtime/WorldLifecycleResetCompletionGate.cs` | `Reports/Report-SceneFlow-Production-Log-2025-12-31.md` e `Reports/SceneFlow-Production-Evidence-2025-12-31.md` (reset + gate antes do FadeOut) | **Validado (QA/Logs)** |
| **WorldLifecycleController + WorldLifecycleOrchestrator + ResetCompletedEvent** | `Infrastructure/WorldLifecycle/Runtime/WorldLifecycleController.cs`, `Infrastructure/WorldLifecycle/Runtime/WorldLifecycleOrchestrator.cs`, `Infrastructure/WorldLifecycle/Runtime/WorldLifecycleResetCompletedEvent.cs` | `Reports/Report-SceneFlow-Production-Log-2025-12-31.md` (reset concluído + `WorldLifecycleResetCompletedEvent`) | **Validado (QA/Logs)** *(ordem detalhada dos hooks não validada em report)* |
| **WorldDefinition + NewSceneBootstrapper + Spawn Registry + Player/Eater spawn** | `Infrastructure/WorldLifecycle/Spawn/WorldDefinition.cs`, `Infrastructure/Scene/NewSceneBootstrapper.cs`, `Infrastructure/WorldLifecycle/Spawn/WorldSpawnServiceRegistry.cs`, `Infrastructure/WorldLifecycle/Spawn/PlayerSpawnService.cs`, `Infrastructure/WorldLifecycle/Spawn/EaterSpawnService.cs` | `Reports/Report-SceneFlow-Production-Log-2025-12-31.md`, `Reports/SceneFlow-Production-Evidence-2025-12-31.md` (spawn Player/Eater) | **Validado (QA/Logs)** |
| **GameplayReset (orchestrator + classifier + targets + fases)** | `Gameplay/Reset/GameplayResetOrchestrator.cs`, `Gameplay/Reset/DefaultGameplayResetTargetClassifier.cs`, `Gameplay/Reset/GameplayResetContracts.cs` | `Reports/QA-GameplayReset-RequestMatrix.md`, `Reports/QA-GameplayResetKind.md` (targets/fases) | **Validado (QA/Logs)** |

## Itens implementados sem evidência documentada
- `GameReadinessService` com token `flow.scene_transition` não possui log/QA explícito nos reports atuais.
- Ordem detalhada dos hooks (`OnBeforeDespawn → Despawn → Scoped Participants → OnBeforeSpawn → Spawn → OnAfterSpawn`) ainda não foi validada por logs dedicados.

## Referências rápidas (evidência)
- **Master de produção:** [SceneFlow-Production-EndToEnd-Validation.md](SceneFlow-Production-EndToEnd-Validation.md)
- **Log de produção (trechos):** [Report-SceneFlow-Production-Log-2025-12-31.md](Report-SceneFlow-Production-Log-2025-12-31.md)
- **QA de reset (targets):** [QA-GameplayReset-RequestMatrix.md](QA-GameplayReset-RequestMatrix.md)
- **QA por ActorKind:** [QA-GameplayResetKind.md](QA-GameplayResetKind.md)
