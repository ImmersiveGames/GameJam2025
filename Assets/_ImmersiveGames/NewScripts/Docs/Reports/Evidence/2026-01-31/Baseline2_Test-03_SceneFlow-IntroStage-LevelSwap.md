# Evidence — Baseline 2.0 (Combined Run) — Startup→Gameplay→PostGame→Restart→ExitToMenu

**Date:** 2026-01-31
**Timezone:** America/Sao_Paulo
**Purpose:** Consolidate evidence from the *combined* run covering Startup/Menu (skip reset), Gameplay (reset + spawn), IntroStage gating, Pause/Resume, PostGame Victory/Defeat, Restart (Boot cycle), and ExitToMenu (frontend skip).

---

## Coverage (Baseline Matrix A–E)

| ID | Scenario | Expected | Observed | Status |
|---:|---|---|---|---|
| A | Boot → Menu (profile=startup) | **ResetWorld SKIP** (non-gameplay) + ResetCompleted (skip reason) | ResetWorld SKIP; ResetCompleted with `Skipped_StartupOrFrontend:profile=startup;scene=MenuScene` | ✅ |
| B | Menu → Gameplay (profile=gameplay) | **ResetWorld** triggered by `SceneFlow/ScenesReady`; deterministic pipeline; spawns | ResetRequested reason `SceneFlow/ScenesReady`; Reset completed; spawned Player+Eater; ActorRegistry=2 | ✅ |
| C | IntroStage gating | `sim.gameplay` acquired on start; release on UI confirm; GameLoop enters Playing | `GameplaySimulationBlocked token='sim.gameplay'`; UIConfirm; `GameplaySimulationUnblocked`; GameLoop `ENTER: Playing` | ✅ |
| D | Pause/Resume | token `state.pause` acquire/release; InputMode switches; GameLoop Playing↔Paused | Pause acquires `state.pause` and switches mode `PauseOverlay`; resume releases token; returns to Gameplay | ✅ |
| E | PostGame + Restart + ExitToMenu | Victory/Defeat -> PostGame overlay; Restart triggers Boot cycle and re-reset; ExitToMenu navigates to frontend and SKIPs reset | Victory then Restart: `Restart->Boot confirmado`; reset pipeline runs again; later Defeat then ExitToMenu -> profile=frontend SKIP reset | ✅ |

---

## Key invariants demonstrated (high-signal lines exist in log)

- `SceneTransitionStarted` acquires `flow.scene_transition` and closes gate.
- `WorldLifecycleSceneFlowResetDriver` issues ResetRequested on `ScenesReady` and produces ResetCompleted (or SKIP) for every transition.
- Gameplay transitions use `profile='gameplay'` → **ResetWorld runs**; Startup/Frontend profiles → **ResetWorld SKIP**.
- Spawn pipeline completes with `ActorRegistry count at 'After Spawn': 2` (Player + Eater).
- IntroStage uses `sim.gameplay` gate token and only unblocks on UI confirm (`IntroStage/UIConfirm`).
- Pause uses `state.pause` gate token; InputMode switches accordingly.
- Restart triggers deterministic cycle `Restart->Boot confirmado`, then SceneFlow transition continues and WorldLifecycle reset executes again.
- ExitToMenu uses route `to-menu` and `profile='frontend'`, reset SKIP is logged.

---

## Note about “Teste 4”
**Not necessary** given this combined run already covers the typical “missing” slice (Restart + Boot cycle + ExitToMenu + frontend skip) in the same evidence set.

---

## Raw log (no cuts)

NEWSCRIPTS_MODE ativo: EventBusUtil.InitializeEditor ignorado.

NEWSCRIPTS_MODE ativo: DebugUtility.Initialize executando reset de estado.

[INFO] [DebugUtility] DebugUtility inicializado antes de todos os sistemas.

[INFO] [AnimationBootstrapper] NEWSCRIPTS_MODE ativo: AnimationBootstrapper ignorado.

NEWSCRIPTS_MODE ativo: DebugUtility.Initialize ignorado.

[INFO] [DependencyBootstrapper] NEWSCRIPTS_MODE ativo: ResetStatics ignorado.

[VERBOSE] [GlobalBootstrap] NewScripts logging configured. (@ 3,39s)

<color=#00BCD4>[VERBOSE] [SceneServiceCleaner] SceneServiceCleaner inicializado. (@ 3,39s)</color>

<color=#00BCD4>[VERBOSE] [DependencyManager] DependencyManager inicializado (DontDestroyOnLoad). (@ 3,39s)</color>

[VERBOSE] [GlobalBootstrap] DependencyManager created for global scope. (@ 3,39s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [EventBus] EventBus inicializado (GameLoop + SceneFlow + WorldLifecycle). (@ 3,40s)</color>

<color=yellow>[INFO] [PromotionGateService] PromotionGate: nenhum config encontrado em Resources/NewScripts/Config/PromotionGateConfig; usando defaults: defaultEnabled=true.</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço PromotionGateService registrado no escopo global. (@ 3,40s)</color>

<color=#A8DEED>[INFO] [PromotionGateInstaller] PromotionGate registrado (global).</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IUniqueIdFactory registrado no escopo global. (@ 3,40s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: IUniqueIdFactory. (@ 3,40s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService registrado no escopo global. (@ 3,40s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: ISimulationGateService. (@ 3,40s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 3,40s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço INewScriptsFadeService registrado no escopo global. (@ 3,40s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: INewScriptsFadeService. (@ 3,40s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [Fade] INewScriptsFadeService registrado no DI global. (@ 3,40s)</color>

[VERBOSE] [GamePauseGateBridge] [PauseBridge] Registrado nos eventos GamePauseCommandEvent/GameResumeRequestedEvent/GameExitToMenuRequestedEvent → SimulationGate. (@ 3,41s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço GamePauseGateBridge registrado no escopo global. (@ 3,41s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [Pause] GamePauseGateBridge registrado (EventBus → SimulationGate). (@ 3,41s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService registrado no escopo global. (@ 3,41s)</color>

[VERBOSE] [GameLoopService] [GameLoop] ENTER: Boot (active=False) (@ 3,41s)

[VERBOSE] [GameLoopService] [GameLoop] Initialized. (@ 3,41s)

[VERBOSE] [GameLoopEventInputBridge] [GameLoop] Bridge de entrada registrado no EventBus (pause/resume/reset). (@ 3,41s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço GameLoopEventInputBridge registrado no escopo global. (@ 3,41s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 3,41s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [GameLoop] GameLoopBootstrap.EnsureRegistered() executado (serviço + bridge no escopo global). (@ 3,41s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageCoordinator registrado no escopo global. (@ 3,41s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [IntroStage] IntroStageCoordinator registrado no DI global. (@ 3,41s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageControlService registrado no escopo global. (@ 3,41s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [IntroStage] IntroStageControlService registrado no DI global. (@ 3,41s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IGameplaySceneClassifier registrado no escopo global. (@ 3,41s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [Gameplay] IGameplaySceneClassifier registrado no DI global. (@ 3,41s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameplaySceneClassifier encontrado no escopo global (tipo registrado: IGameplaySceneClassifier). (@ 3,41s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStagePolicyResolver registrado no escopo global. (@ 3,41s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [IntroStage] DefaultIntroStagePolicyResolver registrado no DI global. (@ 3,41s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageStep registrado no escopo global. (@ 3,41s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [IntroStage] ConfirmToStartIntroStageStep registrado no DI global. (@ 3,41s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IGameRunEndRequestService registrado no escopo global. (@ 3,41s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: IGameRunEndRequestService. (@ 3,41s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [GameLoop] GameRunEndRequestService registrado no DI global. (@ 3,41s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameRunEndRequestService encontrado no escopo global (tipo registrado: IGameRunEndRequestService). (@ 3,41s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IGameCommands registrado no escopo global. (@ 3,41s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: IGameCommands. (@ 3,41s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [GameCommands] GameCommands registrado no DI global. (@ 3,41s)</color>

[VERBOSE] [GameRunStatusService] [GameLoop] GameRunStatusService registrado no EventBus<GameRunEndedEvent> e EventBus<GameRunStartedEvent>. (@ 3,41s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IGameRunStatusService registrado no escopo global. (@ 3,41s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: IGameRunStatusService. (@ 3,41s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [GameLoop] GameRunStatusService registrado no DI global. (@ 3,41s)</color>

[VERBOSE] [GameRunOutcomeService] [GameLoop] GameRunOutcomeService registrado no EventBus<GameRunStartedEvent> e observando EventBus<GameRunEndedEvent>. (@ 3,41s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IGameRunOutcomeService registrado no escopo global. (@ 3,41s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: IGameRunOutcomeService. (@ 3,41s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [GameLoop] GameRunOutcomeService registrado no DI global. (@ 3,41s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameRunOutcomeService encontrado no escopo global (tipo registrado: IGameRunOutcomeService). (@ 3,41s)

[VERBOSE] [GameRunOutcomeEventInputBridge] GameRunOutcomeEventInputBridge registered. (@ 3,41s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço GameRunOutcomeEventInputBridge registrado no escopo global. (@ 3,41s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: GameRunOutcomeEventInputBridge. (@ 3,41s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [GameLoop] GameRunOutcomeEventInputBridge registrado no DI global. (@ 3,41s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IPostPlayOwnershipService registrado no escopo global. (@ 3,41s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [PostPlay] PostPlayOwnershipService registrado no DI global. (@ 3,41s)</color>

[VERBOSE] [NewScriptsSceneFlowAdapters] [SceneFlow] Usando SceneManagerLoaderAdapter (loader nativo). (@ 3,41s)

[VERBOSE] [GlobalServiceRegistry] Serviço INewScriptsFadeService encontrado no escopo global (tipo registrado: INewScriptsFadeService). (@ 3,42s)

[VERBOSE] [NewScriptsSceneFlowAdapters] [SceneFlow] Usando INewScriptsFadeService via adapter (NewScripts). (@ 3,42s)

<color=#A8DEED>[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] WorldLifecycleResetCompletionGate registrado. timeoutMs=20000. (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ISceneTransitionCompletionGate registrado no escopo global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [SceneFlow] ISceneTransitionCompletionGate registrado (WorldLifecycleResetCompletionGate). (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ISceneTransitionService registrado no escopo global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [SceneFlow] SceneTransitionService nativo registrado (Loader=SceneManagerLoaderAdapter, FadeAdapter=NewScriptsSceneFlowFadeAdapter, Gate=WorldLifecycleResetCompletionGate). (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ISceneFlowSignatureCache registrado no escopo global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [SceneFlow] SceneFlowSignatureCache registrado no DI global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] Driver registrado: SceneFlow ScenesReady -> ResetWorld -> ResetCompleted. reason='SceneFlow/ScenesReady'. (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço WorldLifecycleSceneFlowResetDriver registrado no escopo global. (@ 3,42s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: WorldLifecycleSceneFlowResetDriver. (@ 3,42s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IWorldResetRequestService registrado no escopo global. (@ 3,42s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: IWorldResetRequestService. (@ 3,42s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISceneTransitionService encontrado no escopo global (tipo registrado: ISceneTransitionService). (@ 3,42s)

<color=#A8DEED>[VERBOSE] [GameNavigationService] [Navigation] GameNavigationService inicializado. Rotas: to-menu, to-gameplay (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IGameNavigationService registrado no escopo global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [Navigation] GameNavigationService registrado no DI global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [ExitToMenuNavigationBridge] [Navigation] ExitToMenuNavigationBridge registrado (GameExitToMenuRequestedEvent -> RequestMenuAsync). (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ExitToMenuNavigationBridge registrado no escopo global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [Navigation] ExitToMenuNavigationBridge registrado no DI global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [RestartNavigationBridge] [Navigation] RestartNavigationBridge registrado (GameResetRequestedEvent -> RequestGameplayAsync). (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço RestartNavigationBridge registrado no escopo global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [Navigation] RestartNavigationBridge registrado no DI global. (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço INewScriptsLoadingHudService registrado no escopo global. (@ 3,42s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: INewScriptsLoadingHudService. (@ 3,42s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [Loading] INewScriptsLoadingHudService registrado no DI global. (@ 3,42s)</color>

[VERBOSE] [SceneFlowLoadingService] [Loading] SceneFlowLoadingService registrado nos eventos de Scene Flow. (@ 3,42s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço SceneFlowLoadingService registrado no escopo global. (@ 3,42s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: SceneFlowLoadingService. (@ 3,42s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [Loading] SceneFlowLoadingService registrado no DI global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputMode] InputModeSceneFlowBridge registrado nos eventos de SceneTransitionStartedEvent e SceneTransitionCompletedEvent. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] Bridge registrado para SceneTransitionCompletedEvent (Frontend/GamePlay -> GameLoop sync). (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço InputModeSceneFlowBridge registrado no escopo global. (@ 3,42s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [InputMode] InputModeSceneFlowBridge registrado no DI global. (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IStateDependentService registrado no escopo global. (@ 3,42s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: IStateDependentService. (@ 3,42s)

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [StateDependent] Registrado StateDependentService (gate-aware) como IStateDependentService. (@ 3,42s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ICameraResolver registrado no escopo global. (@ 3,42s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: ICameraResolver. (@ 3,42s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IContentSwapContextService registrado no escopo global. (@ 3,42s)</color>

[VERBOSE] [GlobalBootstrap] Registered global service: IContentSwapContextService. (@ 3,42s)

<color=#A8DEED>[INFO] [IntroStageQaInstaller] [QA][IntroStage] IntroStageQaContextMenu instalado (DontDestroyOnLoad).</color>

<color=#A8DEED>[INFO] [IntroStageQaInstaller] [QA][IntroStage] IntroStageQaContextMenu ausente; componente adicionado.</color>

<color=#A8DEED>[INFO] [IntroStageQaInstaller] [QA][IntroStage] Para acessar o ContextMenu, selecione o GameObject 'QA_IntroStage' no Hierarchy (DontDestroyOnLoad).</color>

<color=#A8DEED>[INFO] [ContentSwapQaInstaller] [QA][ContentSwap] ContentSwapQaContextMenu instalado (DontDestroyOnLoad).</color>

<color=#A8DEED>[INFO] [ContentSwapQaInstaller] [QA][ContentSwap] ContentSwapQaContextMenu ausente; componente adicionado.</color>

<color=#4CAF50>[INFO] [ContentSwapQaInstaller] [QA][ContentSwap] Para acessar o ContextMenu, selecione o GameObject 'QA_ContentSwap' no Hierarchy (DontDestroyOnLoad).</color>

<color=#A8DEED>[INFO] [SceneFlowQaInstaller] [QA][SceneFlow] SceneFlowQaContextMenu instalado (DontDestroyOnLoad).</color>

<color=#A8DEED>[INFO] [SceneFlowQaInstaller] [QA][SceneFlow] SceneFlowQaContextMenu ausente; componente adicionado.</color>

<color=#4CAF50>[INFO] [SceneFlowQaInstaller] [QA][SceneFlow] Para acessar o ContextMenu, selecione o GameObject 'QA_SceneFlow' no Hierarchy (DontDestroyOnLoad).</color>

[VERBOSE] [GlobalServiceRegistry] Serviço PromotionGateService encontrado no escopo global (tipo registrado: PromotionGateService). (@ 3,43s)

[VERBOSE] [GlobalServiceRegistry] Serviço IContentSwapContextService encontrado no escopo global (tipo registrado: IContentSwapContextService). (@ 3,43s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IContentSwapChangeService registrado no escopo global. (@ 3,43s)</color>

<color=#4CAF50>[INFO] [GlobalBootstrap] [ContentSwap] Registered IContentSwapChangeService (InPlaceOnly).</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ILevelCatalogProvider registrado no escopo global. (@ 3,43s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ILevelDefinitionProvider registrado no escopo global. (@ 3,43s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ILevelCatalogResolver registrado no escopo global. (@ 3,43s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IContentSwapChangeService encontrado no escopo global (tipo registrado: IContentSwapChangeService). (@ 3,43s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ILevelManager registrado no escopo global. (@ 3,43s)</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço ILevelSessionService registrado no escopo global. (@ 3,43s)</color>

<color=#4CAF50>[INFO] [LevelManagerInstaller] [LevelManager] Registro concluído (bootstrap).</color>

<color=#A8DEED>[INFO] [LevelQaInstaller] [QA][Level] LevelQaContextMenu instalado (DontDestroyOnLoad).</color>

<color=#A8DEED>[INFO] [LevelQaInstaller] [QA][Level] LevelQaContextMenu ausente; componente adicionado.</color>

<color=#4CAF50>[INFO] [LevelQaInstaller] [QA][Level] Para acessar o ContextMenu, selecione o GameObject 'QA_Level' no Hierarchy (DontDestroyOnLoad).</color>

[VERBOSE] [GameReadinessService] [Readiness] GameReadinessService registrado nos eventos de Scene Flow. (@ 3,43s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=True, activeTokens=0, reason='bootstrap'. (@ 3,43s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço GameReadinessService registrado no escopo global. (@ 3,43s)</color>

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [Readiness] GameReadinessService inicializado e registrado no DI global (Scene Flow → SimulationGate). (@ 3,43s)</color>

[INFO] [GameLoopSceneFlowCoordinator] [GameLoopSceneFlow] Coordinator registrado. StartPlan: Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'.

<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [GameLoopSceneFlow] Coordinator registrado (startPlan production, profile='startup'). (@ 3,44s)</color>

<color=#4CAF50>[INFO] [GlobalBootstrap] ✅ NewScripts global infrastructure initialized (Commit 1 minimal).</color>

[INFO] [CompassRuntimeService] NEWSCRIPTS_MODE ativo: CompassRuntimeService ignorado.

[INFO] [DependencyBootstrapper] NEWSCRIPTS_MODE ativo: DependencyBootstrapper ignorado.

NEWSCRIPTS_MODE ativo: EventBusUtil.Initialize ignorado.

[VERBOSE] [GlobalServiceRegistry] Serviço ICameraResolver encontrado no escopo global (tipo registrado: ICameraResolver). (@ 3,48s)

<color=#A8DEED>[VERBOSE] [NewGameplayCameraBinder] ICameraResolver resolved in Awake. (@ 3,48s)</color>

<color=#A8DEED>[VERBOSE] [CameraResolverService] Camera registered for playerId=0: Camera. (@ 3,48s)</color>

<color=#A8DEED>[VERBOSE] [CameraResolverService] Default camera updated to Camera (playerId=0). (@ 3,48s)</color>

<color=#A8DEED>[INFO] [NewGameplayCameraBinder] Gameplay camera registrada (playerId=0): Camera.</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço INewSceneScopeMarker registrado para a cena NewBootstrap. (@ 3,48s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IWorldSpawnContext registrado para a cena NewBootstrap. (@ 3,48s)</color>

[INFO] [NewSceneBootstrapper] WorldRoot ready: NewBootstrap/WorldRoot

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IActorRegistry registrado para a cena NewBootstrap. (@ 3,48s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IWorldSpawnServiceRegistry registrado para a cena NewBootstrap. (@ 3,48s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IGameplayResetTargetClassifier registrado para a cena NewBootstrap. (@ 3,48s)</color>

[VERBOSE] [NewSceneBootstrapper] IGameplayResetTargetClassifier registrado para a cena 'NewBootstrap'. (@ 3,48s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IGameplayResetOrchestrator registrado para a cena NewBootstrap. (@ 3,48s)</color>

[VERBOSE] [NewSceneBootstrapper] IGameplayResetOrchestrator registrado para a cena 'NewBootstrap'. (@ 3,48s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço WorldLifecycleHookRegistry registrado para a cena NewBootstrap. (@ 3,48s)</color>

[VERBOSE] [NewSceneBootstrapper] WorldLifecycleHookRegistry registrado para a cena 'NewBootstrap'. (@ 3,48s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IResetScopeParticipant registrado para a cena NewBootstrap. (@ 3,48s)</color>

[VERBOSE] [NewSceneBootstrapper] PlayersResetParticipant registrado para a cena 'NewBootstrap'. (@ 3,48s)

[VERBOSE] [NewSceneBootstrapper] Hook de cena registrado: SceneLifecycleHookLoggerA (@ 3,48s)

[INFO] [NewSceneBootstrapper] WorldDefinition loaded: WorldDefinitionDummy

[VERBOSE] [NewSceneBootstrapper] WorldDefinition entries count: 1 (@ 3,48s)

[VERBOSE] [NewSceneBootstrapper] Spawn entry #0: Enabled=True, Kind=DummyActor, Prefab=DummyActor (@ 3,48s)

[VERBOSE] [GlobalServiceRegistry] Serviço IUniqueIdFactory encontrado no escopo global (tipo registrado: IUniqueIdFactory). (@ 3,49s)

[VERBOSE] [GlobalServiceRegistry] Serviço IStateDependentService encontrado no escopo global (tipo registrado: IStateDependentService). (@ 3,49s)

[VERBOSE] [WorldSpawnServiceRegistry] Spawn service registrado: DummyActorSpawnService (ordem 1). (@ 3,49s)

[VERBOSE] [NewSceneBootstrapper] Spawn entry #0 REGISTERED: DummyActorSpawnService (Kind=DummyActor, Prefab=DummyActor) (@ 3,49s)

[INFO] [NewSceneBootstrapper] Spawn services registered from definition: 1

[VERBOSE] [NewSceneBootstrapper] Spawn services summary => Total=1, Enabled=1, Disabled=0, Created=1, FailedCreate=0 (@ 3,49s)

[INFO] [NewSceneBootstrapper] Scene scope created: NewBootstrap

<color=#00BCD4>[INFO] [ContentSwapQaInstaller] [QA][ContentSwap] ContentSwapQaContextMenu já instalado (instância existente).</color>

<color=#A8DEED>[VERBOSE] [WorldResetRequestHotkeyDevBootstrap] [WorldLifecycle] Hotkey DEV instalado (Shift+R) para RequestResetAsync. (@ 3,80s)</color>

[INFO] [AudioSystemBootstrap] NEWSCRIPTS_MODE ativo: AudioSystemBootstrap ignorado.

<color=#A8DEED>[INFO] [GameStartRequestProductionBootstrapper] [Production][StartRequest] Start solicitado (GameStartRequestedEvent).</color>

[INFO] [GameLoopSceneFlowCoordinator] [GameLoopSceneFlow] Start REQUEST recebido. Disparando transição de cenas...

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup' requestedBy='n/a' Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'</color>

[VERBOSE] [SceneFlowLoadingService] [Loading] Started → Ensure only (Show após FadeIn). signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 3,81s)

[VERBOSE] [GlobalServiceRegistry] Serviço INewScriptsLoadingHudService encontrado no escopo global (tipo registrado: INewScriptsLoadingHudService). (@ 3,81s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Carregando cena 'LoadingHudScene' (Additive)... (@ 3,81s)

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] SceneFlow/Started -> reset dedupe. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 3,81s)</color>

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='gate_closed'. (@ 3,81s)

[VERBOSE] [SimulationGateService] [Gate] Acquire token='flow.scene_transition'. Active=1. IsOpen=False (@ 3,81s)

[VERBOSE] [GameReadinessService] [Readiness] SimulationGate adquirido com token='flow.scene_transition'. Active=1. IsOpen=False (@ 3,81s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup' (@ 3,81s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_started'. (@ 3,81s)

[VERBOSE] [GameLoopSceneFlowCoordinator] [GameLoopSceneFlow] TransitionStarted recebido. expectedSignature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 3,81s)

[VERBOSE] [NewScriptsSceneTransitionProfileResolver] [SceneFlow] Profile resolvido: name='startup', path='SceneFlow/Profiles/startup', type='_ImmersiveGames.NewScripts.Infrastructure.SceneFlow.NewScriptsSceneTransitionProfile'. (@ 3,82s)

[VERBOSE] [NewScriptsSceneFlowFadeAdapter] [SceneFlow] Profile 'startup' aplicado (path='SceneFlow/Profiles/startup'): fadeIn=0,5, fadeOut=0,5. (@ 3,82s)

[VERBOSE] [NewScriptsFadeService] [Fade] Carregando FadeScene 'FadeScene' (Additive)... (@ 3,82s)

[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageControlService encontrado no escopo global (tipo registrado: IIntroStageControlService). (@ 3,83s)

[VERBOSE] [NewScriptsLoadingHudController] [LoadingHUD] Controller inicializado (CanvasGroup pronto). (@ 4,93s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] NewScriptsLoadingHudController localizado com sucesso. (@ 5,00s)

[VERBOSE] [NewScriptsFadeController] [Fade] Awake - CanvasGroup: OK (@ 5,01s)

[VERBOSE] [NewScriptsFadeController] [Fade] Canvas sorting configurado. isRootCanvas=True, overrideSorting=False, sortingOrder=11000 (@ 5,01s)

[VERBOSE] [NewScriptsFadeService] [Fade] NewScriptsFadeController localizado com sucesso. (@ 5,03s)

[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=1 (dur=0,5) (@ 5,03s)

[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=1 (@ 5,52s)

[VERBOSE] [SceneFlowLoadingService] [Loading] FadeInCompleted → Show. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,52s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', phase='AfterFadeIn'. (@ 5,52s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Carregando cena 'MenuScene' (Additive)... (@ 5,52s)

<color=#A8DEED>[VERBOSE] [FrontendButtonBinderBase] [FrontendButton] Click-guard armado por 0,250s (label='OnEnable/Guard', go='Quit_Btn', btn='Quit_Btn'). (@ 5,57s)</color>

<color=#A8DEED>[VERBOSE] [FrontendButtonBinderBase] [FrontendButton] Click-guard armado por 0,250s (label='OnEnable/Guard', go='HowToPlay_Btn', btn='HowToPlay_Btn'). (@ 5,57s)</color>

<color=#A8DEED>[VERBOSE] [FrontendButtonBinderBase] [FrontendButton] Click-guard armado por 0,250s (label='OnEnable/Guard', go='Options_Btn', btn='Options_Btn'). (@ 5,57s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameNavigationService encontrado no escopo global (tipo registrado: IGameNavigationService). (@ 5,57s)

<color=#A8DEED>[VERBOSE] [FrontendButtonBinderBase] [FrontendButton] Click-guard armado por 0,250s (label='OnEnable/Guard', go='Play_Btn', btn='Play_Btn'). (@ 5,57s)</color>

<color=#A8DEED>[VERBOSE] [FrontendPanelsController] [FrontendPanels] Panel='main' (reason='Awake/Initial'). (@ 5,57s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço INewSceneScopeMarker registrado para a cena MenuScene. (@ 5,57s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IWorldSpawnContext registrado para a cena MenuScene. (@ 5,57s)</color>

[INFO] [NewSceneBootstrapper] WorldRoot ready: MenuScene/WorldRoot

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IActorRegistry registrado para a cena MenuScene. (@ 5,57s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IWorldSpawnServiceRegistry registrado para a cena MenuScene. (@ 5,57s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IGameplayResetTargetClassifier registrado para a cena MenuScene. (@ 5,57s)</color>

[VERBOSE] [NewSceneBootstrapper] IGameplayResetTargetClassifier registrado para a cena 'MenuScene'. (@ 5,57s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IGameplayResetOrchestrator registrado para a cena MenuScene. (@ 5,57s)</color>

[VERBOSE] [NewSceneBootstrapper] IGameplayResetOrchestrator registrado para a cena 'MenuScene'. (@ 5,57s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço WorldLifecycleHookRegistry registrado para a cena MenuScene. (@ 5,57s)</color>

[VERBOSE] [NewSceneBootstrapper] WorldLifecycleHookRegistry registrado para a cena 'MenuScene'. (@ 5,57s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IResetScopeParticipant registrado para a cena MenuScene. (@ 5,57s)</color>

[VERBOSE] [NewSceneBootstrapper] PlayersResetParticipant registrado para a cena 'MenuScene'. (@ 5,57s)

[VERBOSE] [NewSceneBootstrapper] Hook de cena registrado: SceneLifecycleHookLoggerA (@ 5,57s)

[VERBOSE] [NewSceneBootstrapper] WorldDefinition não atribuída (scene='MenuScene'). Isto é permitido em cenas sem spawn (ex.: Ready). Serviços de spawn não serão registrados. (@ 5,57s)

[INFO] [NewSceneBootstrapper] Spawn services registered from definition: 0

[INFO] [NewSceneBootstrapper] Scene scope created: MenuScene

[VERBOSE] [SceneTransitionService] [SceneFlow] Carregando cena 'UIGlobalScene' (Additive)... (@ 5,57s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService registrado no escopo global. (@ 5,63s)</color>

<color=#4CAF50>[INFO] [InputModeBootstrap] [InputMode] IInputModeService registrado no DI global.</color>

<color=#A8DEED>[VERBOSE] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] Instância duplicada detectada; destruindo duplicata. (@ 5,63s)</color>

<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] Bindings de GameRunEnded/GameRunStarted registrados. (@ 5,63s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 5,64s)

[VERBOSE] [DependencyInjector] Injetando IInputModeService do escopo global para PauseOverlayController. (@ 5,64s)

[VERBOSE] [DependencyInjector] Injeção bem-sucedida: IInputModeService -> PauseOverlayController (implementação: InputModeService) (@ 5,64s)

<color=#A8DEED>[VERBOSE] [PauseOverlayController] [PauseOverlay] Overlay desativado. (@ 5,64s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameRunStatusService encontrado no escopo global (tipo registrado: IGameRunStatusService). (@ 5,64s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Cena ativa definida para 'MenuScene'. (@ 5,69s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Descarregando cena 'NewBootstrap'... (@ 5,70s)

<color=#A8DEED>[VERBOSE] [CameraResolverService] Camera unregistered for playerId=0: Camera. (@ 5,71s)</color>

<color=#A8DEED>[VERBOSE] [CameraResolverService] Default camera updated to null (playerId=0). (@ 5,71s)</color>

[VERBOSE] [ServiceRegistry] Dicionário retornado ao pool. Tamanho do pool: 1. (@ 5,71s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Removidos 8 serviços para a cena NewBootstrap. (@ 5,71s)</color>

[INFO] [NewSceneBootstrapper] Scene scope cleared: NewBootstrap

<color=#4CAF50>[VERBOSE] [SceneServiceCleaner] Cena NewBootstrap descarregada, serviços limpos. (@ 5,71s)</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' sourceSignature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,71s)</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] ResetWorld SKIP (profile != gameplay). signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', profile='startup', targetScene='MenuScene'. (@ 5,71s)</color>

<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,71s)</color>

[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,71s)

[VERBOSE] [GameLoopSceneFlowCoordinator] [GameLoopSceneFlow] WorldLifecycle reset concluído (ou skip). reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,71s)

[VERBOSE] [SceneFlowLoadingService] [Loading] ScenesReady → Update pending. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,72s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', phase='ScenesReady'. (@ 5,72s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context=Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup' (@ 5,72s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_scenes_ready'. (@ 5,72s)

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] ScenesReady id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.</color>

[VERBOSE] [SceneTransitionService] [SceneFlow] Aguardando completion gate antes do FadeOut. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,72s)

[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] Já concluído (cached). signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,72s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,72s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', phase='BeforeFadeOut'. (@ 5,72s)

[VERBOSE] [SceneFlowLoadingService] [Loading] BeforeFadeOut → Hide. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 5,72s)

[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=0 (dur=0,5) (@ 5,72s)

[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=0 (@ 6,19s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', phase='Completed'. (@ 6,19s)

[VERBOSE] [SceneFlowLoadingService] [Loading] Completed → Safety hide. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 6,19s)

[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 6,19s)

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'FrontendMenu' (SceneFlow/Completed:Frontend).</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Nenhum PlayerInput ativo encontrado ao aplicar modo 'FrontendMenu'. Isto é esperado em Menu/Frontend. Em Gameplay, verifique se o Player foi spawnado. (@ 6,19s)</color>

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [OBS][InputMode] Applied mode='FrontendMenu' map='UI' signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' scene='MenuScene' profile='startup' reason='SceneFlow/Completed:Frontend'. (@ 6,19s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 6,19s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=True, activeTokens=0, reason='gate_opened'. (@ 6,20s)

[VERBOSE] [SimulationGateService] [Gate] Release token='flow.scene_transition'. Active=0. IsOpen=True (@ 6,20s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=False. Context=Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup' (@ 6,20s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=True, activeTokens=0, reason='scene_transition_completed'. (@ 6,20s)

[VERBOSE] [GameLoopSceneFlowCoordinator] [GameLoopSceneFlow] TransitionCompleted recebido. expectedSignature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'. (@ 6,20s)

<color=#A8DEED>[VERBOSE] [GameLoopSceneFlowCoordinator] [GameLoopSceneFlow] Profile não-gameplay (profileId='startup'). Chamando RequestReady() no GameLoop. (@ 6,20s)</color>

<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.</color>

[VERBOSE] [GameLoopService] [GameLoop] EXIT: Boot (@ 6,20s)

[VERBOSE] [GameLoopService] [GameLoop] ENTER: Ready (active=False) (@ 6,20s)

<color=#A8DEED>[VERBOSE] [MenuPlayButtonBinder] [Navigation] Play solicitado. reason='Menu/PlayButton'. (@ 7,31s)</color>

<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='Menu/PlayButton', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' requestedBy='n/a' Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'</color>

[VERBOSE] [SceneFlowLoadingService] [Loading] Started → Ensure only (Show após FadeIn). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 7,31s)

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] SceneFlow/Started -> reset dedupe. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 7,31s)</color>

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='gate_closed'. (@ 7,31s)

[VERBOSE] [SimulationGateService] [Gate] Acquire token='flow.scene_transition'. Active=1. IsOpen=False (@ 7,31s)

[VERBOSE] [GameReadinessService] [Readiness] SimulationGate adquirido com token='flow.scene_transition'. Active=1. IsOpen=False (@ 7,31s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 7,31s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_started'. (@ 7,31s)

[VERBOSE] [NewScriptsSceneTransitionProfileResolver] [SceneFlow] Profile resolvido: name='gameplay', path='SceneFlow/Profiles/gameplay', type='_ImmersiveGames.NewScripts.Infrastructure.SceneFlow.NewScriptsSceneTransitionProfile'. (@ 7,31s)

[VERBOSE] [NewScriptsSceneFlowFadeAdapter] [SceneFlow] Profile 'gameplay' aplicado (path='SceneFlow/Profiles/gameplay'): fadeIn=0,5, fadeOut=0,5. (@ 7,31s)

[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=1 (dur=0,5) (@ 7,31s)

[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=1 (@ 7,79s)

[VERBOSE] [SceneFlowLoadingService] [Loading] FadeInCompleted → Show. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 7,79s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='AfterFadeIn'. (@ 7,79s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Carregando cena 'GameplayScene' (Additive)... (@ 7,79s)

<color=#A8DEED>[VERBOSE] [GameplayEndConditionsController] [GameplayEndConditionsController] State reset. reason='OnEnable', startTime=2,983. (@ 7,81s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameRunEndRequestService encontrado no escopo global (tipo registrado: IGameRunEndRequestService). (@ 7,81s)

[VERBOSE] [ServiceRegistry] Dicionário obtido do pool para serviços. (@ 7,81s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço INewSceneScopeMarker registrado para a cena GameplayScene. (@ 7,81s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IWorldSpawnContext registrado para a cena GameplayScene. (@ 7,81s)</color>

[INFO] [NewSceneBootstrapper] WorldRoot ready: GameplayScene/WorldRoot

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IActorRegistry registrado para a cena GameplayScene. (@ 7,81s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IWorldSpawnServiceRegistry registrado para a cena GameplayScene. (@ 7,81s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IGameplayResetTargetClassifier registrado para a cena GameplayScene. (@ 7,81s)</color>

[VERBOSE] [NewSceneBootstrapper] IGameplayResetTargetClassifier registrado para a cena 'GameplayScene'. (@ 7,81s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IGameplayResetOrchestrator registrado para a cena GameplayScene. (@ 7,81s)</color>

[VERBOSE] [NewSceneBootstrapper] IGameplayResetOrchestrator registrado para a cena 'GameplayScene'. (@ 7,81s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço WorldLifecycleHookRegistry registrado para a cena GameplayScene. (@ 7,81s)</color>

[VERBOSE] [NewSceneBootstrapper] WorldLifecycleHookRegistry registrado para a cena 'GameplayScene'. (@ 7,81s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IResetScopeParticipant registrado para a cena GameplayScene. (@ 7,81s)</color>

[VERBOSE] [NewSceneBootstrapper] PlayersResetParticipant registrado para a cena 'GameplayScene'. (@ 7,81s)

[VERBOSE] [NewSceneBootstrapper] Hook de cena registrado: SceneLifecycleHookLoggerA (@ 7,81s)

[INFO] [NewSceneBootstrapper] WorldDefinition loaded: WorldDefinition

[VERBOSE] [NewSceneBootstrapper] WorldDefinition entries count: 2 (@ 7,81s)

[VERBOSE] [NewSceneBootstrapper] Spawn entry #0: Enabled=True, Kind=Player, Prefab=Player_NewScripts (@ 7,81s)

[VERBOSE] [GlobalServiceRegistry] Serviço IUniqueIdFactory encontrado no escopo global (tipo registrado: IUniqueIdFactory). (@ 7,81s)

[VERBOSE] [GlobalServiceRegistry] Serviço IStateDependentService encontrado no escopo global (tipo registrado: IStateDependentService). (@ 7,81s)

[VERBOSE] [WorldSpawnServiceRegistry] Spawn service registrado: PlayerSpawnService (ordem 1). (@ 7,82s)

[VERBOSE] [NewSceneBootstrapper] Spawn entry #0 REGISTERED: PlayerSpawnService (Kind=Player, Prefab=Player_NewScripts) (@ 7,82s)

[VERBOSE] [NewSceneBootstrapper] Spawn entry #1: Enabled=True, Kind=Eater, Prefab=Eater_NewScripts (@ 7,82s)

[VERBOSE] [WorldSpawnServiceRegistry] Spawn service registrado: EaterSpawnService (ordem 2). (@ 7,82s)

[VERBOSE] [NewSceneBootstrapper] Spawn entry #1 REGISTERED: EaterSpawnService (Kind=Eater, Prefab=Eater_NewScripts) (@ 7,82s)

[INFO] [NewSceneBootstrapper] Spawn services registered from definition: 2

[VERBOSE] [NewSceneBootstrapper] Spawn services summary => Total=2, Enabled=2, Disabled=0, Created=2, FailedCreate=0 (@ 7,82s)

[INFO] [NewSceneBootstrapper] Scene scope created: GameplayScene

[VERBOSE] [GlobalServiceRegistry] Serviço ICameraResolver encontrado (tipo registrado: ICameraResolver). (@ 7,82s)

<color=#A8DEED>[VERBOSE] [NewGameplayCameraBinder] ICameraResolver resolved in Awake. (@ 7,82s)</color>

<color=#A8DEED>[VERBOSE] [CameraResolverService] Camera registered for playerId=0: Main Camera. (@ 7,82s)</color>

<color=#A8DEED>[VERBOSE] [CameraResolverService] Default camera updated to Main Camera (playerId=0). (@ 7,82s)</color>

<color=#A8DEED>[INFO] [NewGameplayCameraBinder] Gameplay camera registrada (playerId=0): Main Camera.</color>

[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 7,82s)

[VERBOSE] [DependencyInjector] Injetando ISimulationGateService do escopo global para WorldLifecycleController. (@ 7,82s)

[VERBOSE] [DependencyInjector] Injeção bem-sucedida: ISimulationGateService -> WorldLifecycleController (implementação: SimulationGateService) (@ 7,82s)

[VERBOSE] [SceneServiceRegistry] Serviço IWorldSpawnServiceRegistry encontrado para a cena GameplayScene (tipo registrado: IWorldSpawnServiceRegistry). (@ 7,82s)

[VERBOSE] [DependencyInjector] Injetando IWorldSpawnServiceRegistry do escopo cena 'GameplayScene' para WorldLifecycleController. (@ 7,82s)

[VERBOSE] [DependencyInjector] Injeção bem-sucedida: IWorldSpawnServiceRegistry -> WorldLifecycleController (implementação: WorldSpawnServiceRegistry) (@ 7,82s)

[VERBOSE] [SceneServiceRegistry] Serviço IActorRegistry encontrado para a cena GameplayScene (tipo registrado: IActorRegistry). (@ 7,82s)

[VERBOSE] [DependencyInjector] Injetando IActorRegistry do escopo cena 'GameplayScene' para WorldLifecycleController. (@ 7,82s)

[VERBOSE] [DependencyInjector] Injeção bem-sucedida: IActorRegistry -> WorldLifecycleController (implementação: ActorRegistry) (@ 7,82s)

[VERBOSE] [SceneServiceRegistry] Serviço WorldLifecycleHookRegistry encontrado para a cena GameplayScene (tipo registrado: WorldLifecycleHookRegistry). (@ 7,82s)

[VERBOSE] [DependencyInjector] Injetando WorldLifecycleHookRegistry do escopo cena 'GameplayScene' para WorldLifecycleController. (@ 7,82s)

[VERBOSE] [DependencyInjector] Injeção bem-sucedida: WorldLifecycleHookRegistry -> WorldLifecycleController (implementação: WorldLifecycleHookRegistry) (@ 7,82s)

[INFO] [WorldLifecycleController] AutoInitializeOnStart desabilitado — aguardando acionamento externo (scene='GameplayScene').

[VERBOSE] [SceneTransitionService] [SceneFlow] Cena 'UIGlobalScene' já está carregada. Pulando load. (@ 7,82s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Cena ativa definida para 'GameplayScene'. (@ 7,87s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Descarregando cena 'MenuScene'... (@ 7,87s)

[VERBOSE] [ServiceRegistry] Dicionário retornado ao pool. Tamanho do pool: 1. (@ 7,89s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Removidos 8 serviços para a cena MenuScene. (@ 7,89s)</color>

[INFO] [NewSceneBootstrapper] Scene scope cleared: MenuScene

<color=#4CAF50>[VERBOSE] [SceneServiceCleaner] Cena MenuScene descarregada, serviços limpos. (@ 7,89s)</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' sourceSignature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'. (@ 7,89s)</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] Disparando ResetWorld para 1 controller(s). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', targetScene='GameplayScene'. (@ 7,89s)</color>

[INFO] [WorldLifecycleController] Processando reset. label='WorldReset(reason='SceneFlow/ScenesReady')', scene='GameplayScene'.

[INFO] [WorldLifecycleController] Reset iniciado. reason='SceneFlow/ScenesReady', scene='GameplayScene'.

[INFO] [WorldLifecycleController] Spawn services coletados para a cena 'GameplayScene': 2 (registry total: 2).

[INFO] [WorldLifecycleController] ActorRegistry count antes de orquestrar: 0

[INFO] [WorldLifecycleOrchestrator] World Reset Started

[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'Reset start': 0

[VERBOSE] [SimulationGateService] [Gate] Acquire token='WorldLifecycle.WorldReset'. Active=2. IsOpen=False (@ 7,90s)

[INFO] [WorldLifecycleOrchestrator] Gate Acquired (WorldLifecycle.WorldReset)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn step started (hooks=1) (@ 7,90s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn started: SceneLifecycleHookLoggerA (@ 7,91s)

[INFO] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnBeforeDespawnAsync

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn duration: SceneLifecycleHookLoggerA => 0ms (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn completed: SceneLifecycleHookLoggerA (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn step duration: 3ms (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn step completed (@ 7,91s)

[INFO] [WorldLifecycleOrchestrator] Despawn started

[INFO] [WorldLifecycleOrchestrator] Despawn service started: PlayerSpawnService

[VERBOSE] [PlayerSpawnService] DespawnAsync iniciado (scene=GameplayScene). (@ 7,91s)

<color=cyan>[VERBOSE] [PlayerSpawnService] Despawn ignorado (no actor). (@ 7,91s)</color>

[VERBOSE] [WorldLifecycleOrchestrator] Despawn service duration: PlayerSpawnService => 0ms (@ 7,91s)

[INFO] [WorldLifecycleOrchestrator] Despawn service completed: PlayerSpawnService

[INFO] [WorldLifecycleOrchestrator] Despawn service started: EaterSpawnService

[VERBOSE] [EaterSpawnService] DespawnAsync iniciado (scene=GameplayScene). (@ 7,91s)

<color=cyan>[VERBOSE] [EaterSpawnService] Despawn ignorado (no actor). (@ 7,91s)</color>

[VERBOSE] [WorldLifecycleOrchestrator] Despawn service duration: EaterSpawnService => 0ms (@ 7,91s)

[INFO] [WorldLifecycleOrchestrator] Despawn service completed: EaterSpawnService

[VERBOSE] [WorldLifecycleOrchestrator] Despawn duration: 0ms (@ 7,91s)

[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'After Despawn': 0

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn step started (hooks=1) (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn started: SceneLifecycleHookLoggerA (@ 7,91s)

[VERBOSE] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnAfterDespawnAsync (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn duration: SceneLifecycleHookLoggerA => 0ms (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn completed: SceneLifecycleHookLoggerA (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn step duration: 0ms (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn step completed (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn step started (hooks=1) (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn started: SceneLifecycleHookLoggerA (@ 7,91s)

[VERBOSE] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnBeforeSpawnAsync (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn duration: SceneLifecycleHookLoggerA => 0ms (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn completed: SceneLifecycleHookLoggerA (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn step duration: 0ms (@ 7,91s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn step completed (@ 7,91s)

[INFO] [WorldLifecycleOrchestrator] Spawn started

[INFO] [WorldLifecycleOrchestrator] Spawn service started: PlayerSpawnService

[VERBOSE] [PlayerSpawnService] SpawnAsync iniciado (scene=GameplayScene). (@ 7,91s)

[VERBOSE] [GlobalServiceRegistry] Serviço IUniqueIdFactory encontrado no escopo global (tipo registrado: IUniqueIdFactory). (@ 7,93s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 7,93s)

[VERBOSE] [GlobalServiceRegistry] Serviço IStateDependentService encontrado no escopo global (tipo registrado: IStateDependentService). (@ 7,93s)

[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=False, scene='GameplayScene', actor='A_75bb36ca_1_Player_NewScriptsClone'. (@ 7,93s)

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: GateClosed (gateOpen=False, gameplayReady=False, paused=False, serviceState=Ready, gameLoopState='Ready', activeTokens=2). (@ 7,93s)

[VERBOSE] [ActorRegistry] Ator registrado: A_75bb36ca_1_Player_NewScriptsClone. (@ 7,93s)

[INFO] [PlayerSpawnService] Actor spawned: A_75bb36ca_1_Player_NewScriptsClone (prefab=Player_NewScripts, instance=Player_NewScripts, root=WorldRoot, scene=GameplayScene)

[INFO] [PlayerSpawnService] Registry count: 1

[VERBOSE] [WorldLifecycleOrchestrator] Spawn service duration: PlayerSpawnService => 21ms (@ 7,93s)

[INFO] [WorldLifecycleOrchestrator] Spawn service completed: PlayerSpawnService

[INFO] [WorldLifecycleOrchestrator] Spawn service started: EaterSpawnService

[VERBOSE] [EaterSpawnService] SpawnAsync iniciado (scene=GameplayScene). (@ 7,93s)

[VERBOSE] [ActorRegistry] Ator registrado: A_75bb36ca_2_Eater_NewScriptsClone. (@ 7,93s)

[INFO] [EaterSpawnService] Actor spawned: A_75bb36ca_2_Eater_NewScriptsClone (prefab=Eater_NewScripts, instance=Eater_NewScripts, root=WorldRoot, scene=GameplayScene)

[INFO] [EaterSpawnService] Registry count: 2

[VERBOSE] [WorldLifecycleOrchestrator] Spawn service duration: EaterSpawnService => 1ms (@ 7,93s)

[INFO] [WorldLifecycleOrchestrator] Spawn service completed: EaterSpawnService

[VERBOSE] [WorldLifecycleOrchestrator] Spawn duration: 22ms (@ 7,93s)

[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'After Spawn': 2

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor hooks step started (actors=2) (@ 7,93s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor started: A_75bb36ca_1_Player_NewScriptsClone (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor duration: A_75bb36ca_1_Player_NewScriptsClone => 0ms (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor completed: A_75bb36ca_1_Player_NewScriptsClone (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor started: A_75bb36ca_2_Eater_NewScriptsClone (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor duration: A_75bb36ca_2_Eater_NewScriptsClone => 0ms (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor completed: A_75bb36ca_2_Eater_NewScriptsClone (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor hooks step duration: 1ms (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn step started (hooks=1) (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn started: SceneLifecycleHookLoggerA (@ 7,94s)

[INFO] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnAfterSpawnAsync

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn duration: SceneLifecycleHookLoggerA => 0ms (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn completed: SceneLifecycleHookLoggerA (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn step duration: 0ms (@ 7,94s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn step completed (@ 7,94s)

[VERBOSE] [SimulationGateService] [Gate] Release token='WorldLifecycle.WorldReset'. Active=1. IsOpen=False (@ 7,94s)

[INFO] [WorldLifecycleOrchestrator] Gate Released

[INFO] [WorldLifecycleOrchestrator] World Reset Completed

[VERBOSE] [WorldLifecycleOrchestrator] Reset duration: 38ms (@ 7,94s)

[INFO] [WorldLifecycleController] Reset concluído. reason='SceneFlow/ScenesReady', scene='GameplayScene'.

<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] ResetWorld concluído (ScenesReady). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', targetScene='GameplayScene'. (@ 7,94s)</color>

<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'. (@ 7,94s)</color>

[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='SceneFlow/ScenesReady'. (@ 7,94s)

[VERBOSE] [SceneFlowLoadingService] [Loading] ScenesReady → Update pending. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 7,94s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='ScenesReady'. (@ 7,94s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 7,94s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_scenes_ready'. (@ 7,94s)

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] ScenesReady id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.</color>

[VERBOSE] [SceneTransitionService] [SceneFlow] Aguardando completion gate antes do FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 7,94s)

[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] Já concluído (cached). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 7,94s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 7,94s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='BeforeFadeOut'. (@ 7,94s)

[VERBOSE] [SceneFlowLoadingService] [Loading] BeforeFadeOut → Hide. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 7,94s)

[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=0 (dur=0,5) (@ 7,94s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] Movement blocked by IStateDependentService. (@ 7,95s)

[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=0 (@ 8,38s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='Completed'. (@ 8,38s)

[VERBOSE] [SceneFlowLoadingService] [Loading] Completed → Safety hide. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 8,38s)

[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 8,38s)

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (SceneFlow/Completed:Gameplay).</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (SceneFlow/Completed:Gameplay). (@ 8,38s)</color>

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [OBS][InputMode] Applied mode='Gameplay' map='Gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' scene='GameplayScene' profile='gameplay' reason='SceneFlow/Completed:Gameplay'. (@ 8,38s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 8,38s)

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] SceneFlow/Completed:Gameplay -> sincronizando GameLoop. (@ 8,38s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameplaySceneClassifier encontrado no escopo global (tipo registrado: IGameplaySceneClassifier). (@ 8,38s)

[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageCoordinator encontrado no escopo global (tipo registrado: IIntroStageCoordinator). (@ 8,38s)

[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStagePolicyResolver encontrado no escopo global (tipo registrado: IIntroStagePolicyResolver). (@ 8,39s)

[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageStep encontrado no escopo global (tipo registrado: IIntroStageStep). (@ 8,39s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 8,39s)

<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageStarted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageControlService encontrado no escopo global (tipo registrado: IIntroStageControlService). (@ 8,39s)

[VERBOSE] [SimulationGateService] [Gate] Acquire token='sim.gameplay'. Active=2. IsOpen=False (@ 8,39s)

<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>

<color=#A8DEED>[INFO] [IntroStageCoordinator] [IntroStage] IntroStage ativa: simulação gameplay bloqueada; aguardando confirmação (UI).</color>

<color=#A8DEED>[VERBOSE] [IntroStageCoordinator] [QA][IntroStage] ContextMenu/MenuItem disponíveis para Complete/Skip em Editor/Dev. (@ 8,39s)</color>

<color=#A8DEED>[INFO] [ConfirmToStartIntroStageStep] [OBS][InputMode] Apply mode='FrontendMenu' map='UI' phase='IntroStage' reason='IntroStage/ConfirmToStart' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' scene='GameplayScene' profile='gameplay'.</color>

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'FrontendMenu' (IntroStage/ConfirmToStart).</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'UI' em 1/1 PlayerInput(s) (IntroStage/ConfirmToStart). (@ 8,39s)</color>

[VERBOSE] [SimulationGateService] [Gate] Release token='flow.scene_transition'. Active=1. IsOpen=False (@ 8,39s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=True. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 8,39s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=False, activeTokens=1, reason='scene_transition_completed'. (@ 8,39s)

<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.</color>

[VERBOSE] [GameLoopService] [GameLoop] EXIT: Ready (@ 8,40s)

[VERBOSE] [GameLoopService] [GameLoop] ENTER: IntroStage (active=False) (@ 8,40s)

[INFO] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] GUI exibido.

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: GateClosed (gateOpen=False, gameplayReady=True, paused=False, serviceState=Ready, gameLoopState='IntroStage', activeTokens=1). (@ 8,40s)

[INFO] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] Botão Concluir IntroStage clicado.

[INFO] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] Solicitando CompleteIntroStage reason='IntroStage/UIConfirm'.

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 11,78s)

<color=#A8DEED>[INFO] [IntroStageControlService] [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' skip=false decision='applied' state='IntroStage' isActive=true signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>

[INFO] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] GUI oculto.

<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' result='completed' profile='gameplay' target='GameplayScene'.</color>

<color=#A8DEED>[VERBOSE] [IntroStageCoordinator] [IntroStage] Solicitando RequestStart após conclusão explícita da IntroStage. (@ 11,78s)</color>

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: NotPlaying (gateOpen=True, gameplayReady=True, paused=False, serviceState=Ready, gameLoopState='IntroStage', activeTokens=0). (@ 11,78s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=True, activeTokens=0, reason='gate_opened'. (@ 11,78s)

[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=True, scene='GameplayScene', actor='A_75bb36ca_1_Player_NewScriptsClone'. (@ 11,78s)

[VERBOSE] [SimulationGateService] [Gate] Release token='sim.gameplay'. Active=0. IsOpen=True (@ 11,78s)

<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationUnblocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>

[VERBOSE] [NewPlayerMovementController] [Movement][StateDependent] Movimento bloqueado por IStateDependentService. (@ 11,79s)

[VERBOSE] [GameLoopService] [GameLoop] EXIT: IntroStage (@ 11,79s)

[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 11,79s)

[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 11,79s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISceneFlowSignatureCache encontrado no escopo global (tipo registrado: ISceneFlowSignatureCache). (@ 11,79s)

<color=#A8DEED>[INFO] [GameLoopService] [OBS][InputMode] Apply mode='Gameplay' map='Player' phase='Playing' reason='GameLoop/Playing' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' scene='GameplayScene' profile='gameplay' frame=1361.</color>

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (GameLoop/Playing).</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (GameLoop/Playing). (@ 11,79s)</color>

[VERBOSE] [GameRunStatusService] [GameLoop] GameRunStartedEvent inicial observado (state=Playing). (@ 11,79s)

[VERBOSE] [GameRunOutcomeService] [GameLoop] GameRunStartedEvent observado -> rearmando GameRunOutcomeService. state=Playing (@ 11,79s)

<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] GameRunStartedEvent recebido. Ocultando overlay. (@ 11,79s)</color>

[VERBOSE] [DependencyInjector] Injetando IInputModeService do escopo global para PostGameOverlayController. (@ 11,79s)

[VERBOSE] [DependencyInjector] Injeção bem-sucedida: IInputModeService -> PostGameOverlayController (implementação: InputModeService) (@ 11,79s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 11,79s)

[VERBOSE] [DependencyInjector] Injetando ISimulationGateService do escopo global para PostGameOverlayController. (@ 11,79s)

[VERBOSE] [DependencyInjector] Injeção bem-sucedida: ISimulationGateService -> PostGameOverlayController (implementação: SimulationGateService) (@ 11,79s)

[VERBOSE] [GlobalServiceRegistry] Serviço IPostPlayOwnershipService encontrado no escopo global (tipo registrado: IPostPlayOwnershipService). (@ 11,79s)

[VERBOSE] [DependencyInjector] Injetando IPostPlayOwnershipService do escopo global para PostGameOverlayController. (@ 11,79s)

[VERBOSE] [DependencyInjector] Injeção bem-sucedida: IPostPlayOwnershipService -> PostGameOverlayController (implementação: PostPlayOwnershipService) (@ 11,79s)

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Modo 'Gameplay' ja ativo. Reaplicando (PostGame/RunStarted). (@ 11,79s)</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (PostGame/RunStarted). (@ 11,79s)</color>

<color=#A8DEED>[VERBOSE] [GameplayEndConditionsController] [GameplayEndConditionsController] State reset. reason='GameRunStartedEvent', startTime=6,962. (@ 11,79s)</color>

<color=#A8DEED>[VERBOSE] [GameplayEndConditionsController] [GameplayEndConditionsController] Rearmed on GameRunStartedEvent. (@ 11,79s)</color>

[VERBOSE] [GameLoopService] [GameLoop] Activity: True (@ 11,79s)

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' liberada (gateOpen=True, gameplayReady=True, paused=False, serviceState=Playing, gameLoopState='Playing', activeTokens=0). (@ 11,79s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (-0.23, 0.00, -0.97) (scene='GameplayScene'). (@ 11,80s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (-0.73, 0.00, -0.68) (scene='GameplayScene'). (@ 13,79s)

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: Paused (gateOpen=False, gameplayReady=True, paused=True, serviceState=Paused, gameLoopState='Playing', activeTokens=1). (@ 14,98s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=False, activeTokens=1, reason='gate_closed'. (@ 14,98s)

[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=False, scene='GameplayScene', actor='A_75bb36ca_1_Player_NewScriptsClone'. (@ 14,98s)

[VERBOSE] [SimulationGateService] [Gate] Acquire token='state.pause'. Active=1. IsOpen=False (@ 14,98s)

[VERBOSE] [GamePauseGateBridge] [PauseBridge] Gate adquirido com token='state.pause'. IsOpen=False Active=1 (@ 14,98s)

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 14,98s)

<color=#A8DEED>[VERBOSE] [PauseOverlayController] [PauseOverlay] Overlay ativado. (@ 14,98s)</color>

<color=#A8DEED>[VERBOSE] [PauseOverlayController] [PauseOverlay] ShowLocal (reason='PauseCommand'). (@ 14,99s)</color>

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'PauseOverlay' (PauseOverlay/Show).</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'UI' em 1/1 PlayerInput(s) (PauseOverlay/Show). (@ 14,99s)</color>

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] Movement blocked by IStateDependentService. (@ 14,99s)

[VERBOSE] [GameLoopService] [GameLoop] EXIT: Playing (@ 14,99s)

[VERBOSE] [GameLoopService] [GameLoop] ENTER: Paused (active=False) (@ 14,99s)

[VERBOSE] [GameLoopService] [GameLoop] Activity: False (@ 14,99s)

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: Paused (gateOpen=False, gameplayReady=True, paused=True, serviceState=Paused, gameLoopState='Paused', activeTokens=1). (@ 14,99s)

<color=#A8DEED>[VERBOSE] [PauseOverlayController] [PauseOverlay] Overlay desativado. (@ 17,67s)</color>

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: NotPlaying (gateOpen=True, gameplayReady=True, paused=False, serviceState=Paused, gameLoopState='Paused', activeTokens=0). (@ 17,67s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=True, activeTokens=0, reason='gate_opened'. (@ 17,67s)

[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=True, scene='GameplayScene', actor='A_75bb36ca_1_Player_NewScriptsClone'. (@ 17,67s)

[VERBOSE] [SimulationGateService] [Gate] Release token='state.pause'. Active=0. IsOpen=True (@ 17,67s)

[VERBOSE] [GamePauseGateBridge] [PauseBridge] Gate liberado (GameResumeRequestedEvent) token='state.pause'. IsOpen=True Active=0 (@ 17,67s)

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 17,67s)

<color=#A8DEED>[VERBOSE] [PauseOverlayController] [PauseOverlay] Hide -> GameResumeRequestedEvent publicado. (@ 17,67s)</color>

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (PauseOverlay/Hide).</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (PauseOverlay/Hide). (@ 17,67s)</color>

[VERBOSE] [GameLoopService] [GameLoop] EXIT: Paused (@ 17,67s)

[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 17,67s)

[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 17,67s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISceneFlowSignatureCache encontrado no escopo global (tipo registrado: ISceneFlowSignatureCache). (@ 17,67s)

<color=#A8DEED>[INFO] [GameLoopService] [OBS][InputMode] Apply mode='Gameplay' map='Player' phase='Playing' reason='GameLoop/Playing' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' scene='GameplayScene' profile='gameplay' frame=2650.</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Modo 'Gameplay' ja ativo. Reaplicando (GameLoop/Playing). (@ 17,67s)</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (GameLoop/Playing). (@ 17,67s)</color>

[VERBOSE] [GameLoopService] [GameLoop] Activity: True (@ 17,67s)

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' liberada (gateOpen=True, gameplayReady=True, paused=False, serviceState=Playing, gameLoopState='Playing', activeTokens=0). (@ 17,67s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (-0.45, 0.00, 0.89) (scene='GameplayScene'). (@ 18,48s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (-0.26, 0.00, 0.97) (scene='GameplayScene'). (@ 20,48s)

[VERBOSE] [GameRunEndRequestService] RequestEnd(Victory, reason='Gameplay/DevManualVictory') (@ 21,17s)

[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Victory, Reason='Gameplay/DevManualVictory'.

[INFO] [GameRunStatusService] [GameLoop] GameRunStatus atualizado. Outcome=Victory, Reason='Gameplay/DevManualVictory'.

[VERBOSE] [GameRunStatusService] [GameLoop] GameRunEndedEvent (Outcome=Victory) -> PostGame sem PauseOverlay (pausa suprimida). (@ 21,17s)

<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] GameRunEndedEvent recebido. Exibindo overlay. (@ 21,17s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameRunStatusService encontrado no escopo global (tipo registrado: IGameRunStatusService). (@ 21,17s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (-0.89, 0.00, -0.45) (scene='GameplayScene'). (@ 22,49s)

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 23,00s)

<color=#A8DEED>[INFO] [GameLoopEventInputBridge] [GameLoop] RestartRequested -> RequestReset (expect Boot cycle). reason='PostGame/Restart'.</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameNavigationService encontrado no escopo global (tipo registrado: IGameNavigationService). (@ 23,00s)

<color=#A8DEED>[INFO] [RestartNavigationBridge] [Navigation] GameResetRequestedEvent recebido -> RequestGameplayAsync. reason='PostGame/Restart'.</color>

<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='PostGame/Restart', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=3 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' requestedBy='n/a' Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'</color>

[VERBOSE] [SceneFlowLoadingService] [Loading] Started → Ensure only (Show após FadeIn). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 23,00s)

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] SceneFlow/Started -> reset dedupe. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 23,00s)</color>

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: GateClosed (gateOpen=False, gameplayReady=False, paused=False, serviceState=Ready, gameLoopState='Playing', activeTokens=1). (@ 23,00s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='gate_closed'. (@ 23,00s)

[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=False, scene='GameplayScene', actor='A_75bb36ca_1_Player_NewScriptsClone'. (@ 23,00s)

[VERBOSE] [SimulationGateService] [Gate] Acquire token='flow.scene_transition'. Active=1. IsOpen=False (@ 23,00s)

[VERBOSE] [GameReadinessService] [Readiness] SimulationGate adquirido com token='flow.scene_transition'. Active=1. IsOpen=False (@ 23,00s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 23,00s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_started'. (@ 23,00s)

[VERBOSE] [NewScriptsSceneFlowFadeAdapter] [SceneFlow] Profile 'gameplay' aplicado (path='<cache>'): fadeIn=0,5, fadeOut=0,5. (@ 23,00s)

[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=1 (dur=0,5) (@ 23,00s)

[INFO] [PostGameOverlayController] [PostGame] Restart solicitado via overlay. reason='PostGame/Restart'.

[VERBOSE] [GameLoopService] [GameLoop] EXIT: Playing (@ 23,00s)

[VERBOSE] [GameLoopService] [GameLoop] ENTER: Boot (active=False) (@ 23,01s)

<color=#A8DEED>[INFO] [GameLoopService] [GameLoop] Restart->Boot confirmado (reinício determinístico).</color>

[VERBOSE] [GameLoopService] [GameLoop] Activity: False (@ 23,01s)

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: GateClosed (gateOpen=False, gameplayReady=False, paused=False, serviceState=Ready, gameLoopState='Boot', activeTokens=1). (@ 23,01s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] Movement blocked by IStateDependentService. (@ 23,01s)

[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=1 (@ 23,50s)

[VERBOSE] [SceneFlowLoadingService] [Loading] FadeInCompleted → Show. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 23,50s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='AfterFadeIn'. (@ 23,50s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Cena 'GameplayScene' já está carregada. Pulando load. (@ 23,50s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Cena 'UIGlobalScene' já está carregada. Pulando load. (@ 23,50s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Cena ativa definida para 'GameplayScene'. (@ 23,50s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Cena 'MenuScene' já está descarregada. Pulando unload. (@ 23,50s)

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' sourceSignature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'. (@ 23,50s)</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] Disparando ResetWorld para 1 controller(s). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', targetScene='GameplayScene'. (@ 23,50s)</color>

[INFO] [WorldLifecycleController] Processando reset. label='WorldReset(reason='SceneFlow/ScenesReady')', scene='GameplayScene'.

[INFO] [WorldLifecycleController] Reset iniciado. reason='SceneFlow/ScenesReady', scene='GameplayScene'.

[INFO] [WorldLifecycleController] Spawn services coletados para a cena 'GameplayScene': 2 (registry total: 2).

[INFO] [WorldLifecycleController] ActorRegistry count antes de orquestrar: 2

[INFO] [WorldLifecycleOrchestrator] World Reset Started

[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'Reset start': 2

[VERBOSE] [SimulationGateService] [Gate] Acquire token='WorldLifecycle.WorldReset'. Active=2. IsOpen=False (@ 23,50s)

[INFO] [WorldLifecycleOrchestrator] Gate Acquired (WorldLifecycle.WorldReset)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn step started (hooks=1) (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn started: SceneLifecycleHookLoggerA (@ 23,50s)

[INFO] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnBeforeDespawnAsync

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn duration: SceneLifecycleHookLoggerA => 0ms (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn completed: SceneLifecycleHookLoggerA (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn step duration: 0ms (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeDespawn step completed (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeActorDespawn actor hooks step started (actors=2) (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeActorDespawn actor started: A_75bb36ca_1_Player_NewScriptsClone (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeActorDespawn actor duration: A_75bb36ca_1_Player_NewScriptsClone => 0ms (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeActorDespawn actor completed: A_75bb36ca_1_Player_NewScriptsClone (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeActorDespawn actor started: A_75bb36ca_2_Eater_NewScriptsClone (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeActorDespawn actor duration: A_75bb36ca_2_Eater_NewScriptsClone => 0ms (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeActorDespawn actor completed: A_75bb36ca_2_Eater_NewScriptsClone (@ 23,50s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeActorDespawn actor hooks step duration: 0ms (@ 23,50s)

[INFO] [WorldLifecycleOrchestrator] Despawn started

[INFO] [WorldLifecycleOrchestrator] Despawn service started: PlayerSpawnService

[VERBOSE] [PlayerSpawnService] DespawnAsync iniciado (scene=GameplayScene). (@ 23,50s)

[VERBOSE] [ActorRegistry] Ator removido: A_75bb36ca_1_Player_NewScriptsClone. (@ 23,50s)

[INFO] [PlayerSpawnService] Actor despawned: A_75bb36ca_1_Player_NewScriptsClone (root=WorldRoot, scene=GameplayScene)

[INFO] [PlayerSpawnService] Registry count: 1

[VERBOSE] [WorldLifecycleOrchestrator] Despawn service duration: PlayerSpawnService => 5ms (@ 23,51s)

[INFO] [WorldLifecycleOrchestrator] Despawn service completed: PlayerSpawnService

[INFO] [WorldLifecycleOrchestrator] Despawn service started: EaterSpawnService

[VERBOSE] [EaterSpawnService] DespawnAsync iniciado (scene=GameplayScene). (@ 23,51s)

[VERBOSE] [ActorRegistry] Ator removido: A_75bb36ca_2_Eater_NewScriptsClone. (@ 23,51s)

[INFO] [EaterSpawnService] Actor despawned: A_75bb36ca_2_Eater_NewScriptsClone (root=WorldRoot, scene=GameplayScene)

[INFO] [EaterSpawnService] Registry count: 0

[VERBOSE] [WorldLifecycleOrchestrator] Despawn service duration: EaterSpawnService => 0ms (@ 23,51s)

[INFO] [WorldLifecycleOrchestrator] Despawn service completed: EaterSpawnService

[VERBOSE] [WorldLifecycleOrchestrator] Despawn duration: 5ms (@ 23,51s)

[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'After Despawn': 0

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn step started (hooks=1) (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn started: SceneLifecycleHookLoggerA (@ 23,51s)

[VERBOSE] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnAfterDespawnAsync (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn duration: SceneLifecycleHookLoggerA => 0ms (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn completed: SceneLifecycleHookLoggerA (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn step duration: 0ms (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn step completed (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn step started (hooks=1) (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn started: SceneLifecycleHookLoggerA (@ 23,51s)

[VERBOSE] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnBeforeSpawnAsync (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn duration: SceneLifecycleHookLoggerA => 0ms (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn completed: SceneLifecycleHookLoggerA (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn step duration: 0ms (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn step completed (@ 23,51s)

[INFO] [WorldLifecycleOrchestrator] Spawn started

[INFO] [WorldLifecycleOrchestrator] Spawn service started: PlayerSpawnService

[VERBOSE] [PlayerSpawnService] SpawnAsync iniciado (scene=GameplayScene). (@ 23,51s)

[VERBOSE] [GlobalServiceRegistry] Serviço IUniqueIdFactory encontrado no escopo global (tipo registrado: IUniqueIdFactory). (@ 23,51s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 23,51s)

[VERBOSE] [GlobalServiceRegistry] Serviço IStateDependentService encontrado no escopo global (tipo registrado: IStateDependentService). (@ 23,51s)

[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=False, scene='GameplayScene', actor='A_75bb36ca_3_Player_NewScriptsClone'. (@ 23,51s)

[VERBOSE] [ActorRegistry] Ator registrado: A_75bb36ca_3_Player_NewScriptsClone. (@ 23,51s)

[INFO] [PlayerSpawnService] Actor spawned: A_75bb36ca_3_Player_NewScriptsClone (prefab=Player_NewScripts, instance=Player_NewScripts, root=WorldRoot, scene=GameplayScene)

[INFO] [PlayerSpawnService] Registry count: 1

[VERBOSE] [WorldLifecycleOrchestrator] Spawn service duration: PlayerSpawnService => 2ms (@ 23,51s)

[INFO] [WorldLifecycleOrchestrator] Spawn service completed: PlayerSpawnService

[INFO] [WorldLifecycleOrchestrator] Spawn service started: EaterSpawnService

[VERBOSE] [EaterSpawnService] SpawnAsync iniciado (scene=GameplayScene). (@ 23,51s)

[VERBOSE] [ActorRegistry] Ator registrado: A_75bb36ca_4_Eater_NewScriptsClone. (@ 23,51s)

[INFO] [EaterSpawnService] Actor spawned: A_75bb36ca_4_Eater_NewScriptsClone (prefab=Eater_NewScripts, instance=Eater_NewScripts, root=WorldRoot, scene=GameplayScene)

[INFO] [EaterSpawnService] Registry count: 2

[VERBOSE] [WorldLifecycleOrchestrator] Spawn service duration: EaterSpawnService => 0ms (@ 23,51s)

[INFO] [WorldLifecycleOrchestrator] Spawn service completed: EaterSpawnService

[VERBOSE] [WorldLifecycleOrchestrator] Spawn duration: 2ms (@ 23,51s)

[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'After Spawn': 2

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor hooks step started (actors=2) (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor started: A_75bb36ca_3_Player_NewScriptsClone (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor duration: A_75bb36ca_3_Player_NewScriptsClone => 0ms (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor completed: A_75bb36ca_3_Player_NewScriptsClone (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor started: A_75bb36ca_4_Eater_NewScriptsClone (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor duration: A_75bb36ca_4_Eater_NewScriptsClone => 0ms (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor completed: A_75bb36ca_4_Eater_NewScriptsClone (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor hooks step duration: 0ms (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn step started (hooks=1) (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn started: SceneLifecycleHookLoggerA (@ 23,51s)

[INFO] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnAfterSpawnAsync

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn duration: SceneLifecycleHookLoggerA => 0ms (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn completed: SceneLifecycleHookLoggerA (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn step duration: 0ms (@ 23,51s)

[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn step completed (@ 23,51s)

[VERBOSE] [SimulationGateService] [Gate] Release token='WorldLifecycle.WorldReset'. Active=1. IsOpen=False (@ 23,51s)

[INFO] [WorldLifecycleOrchestrator] Gate Released

[INFO] [WorldLifecycleOrchestrator] World Reset Completed

[VERBOSE] [WorldLifecycleOrchestrator] Reset duration: 10ms (@ 23,51s)

[INFO] [WorldLifecycleController] Reset concluído. reason='SceneFlow/ScenesReady', scene='GameplayScene'.

<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] ResetWorld concluído (ScenesReady). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', targetScene='GameplayScene'. (@ 23,51s)</color>

<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'. (@ 23,51s)</color>

[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='SceneFlow/ScenesReady'. (@ 23,51s)

[VERBOSE] [SceneFlowLoadingService] [Loading] ScenesReady → Update pending. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 23,51s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='ScenesReady'. (@ 23,51s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 23,51s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_scenes_ready'. (@ 23,51s)

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] ScenesReady id=3 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.</color>

[VERBOSE] [SceneTransitionService] [SceneFlow] Aguardando completion gate antes do FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 23,51s)

[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] Já concluído (cached). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 23,51s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 23,51s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='BeforeFadeOut'. (@ 23,51s)

[VERBOSE] [SceneFlowLoadingService] [Loading] BeforeFadeOut → Hide. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 23,51s)

[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=0 (dur=0,5) (@ 23,51s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] Movement blocked by IStateDependentService. (@ 23,52s)

[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=0 (@ 24,00s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='Completed'. (@ 24,00s)

[VERBOSE] [SceneFlowLoadingService] [Loading] Completed → Safety hide. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 24,00s)

[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 24,00s)

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Modo 'Gameplay' ja ativo. Reaplicando (SceneFlow/Completed:Gameplay). (@ 24,00s)</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (SceneFlow/Completed:Gameplay). (@ 24,00s)</color>

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [OBS][InputMode] Applied mode='Gameplay' map='Gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' scene='GameplayScene' profile='gameplay' reason='SceneFlow/Completed:Gameplay'. (@ 24,00s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 24,00s)

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] Estado=Boot -> bypass dedupe (Restart/Boot cycle). (@ 24,00s)</color>

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] SceneFlow/Completed:Gameplay -> sincronizando GameLoop. (@ 24,00s)</color>

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] Estado=Boot -> RequestReady() para habilitar IntroStage (Restart/Boot cycle). (@ 24,00s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameplaySceneClassifier encontrado no escopo global (tipo registrado: IGameplaySceneClassifier). (@ 24,01s)

[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageCoordinator encontrado no escopo global (tipo registrado: IIntroStageCoordinator). (@ 24,01s)

[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStagePolicyResolver encontrado no escopo global (tipo registrado: IIntroStagePolicyResolver). (@ 24,01s)

[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageStep encontrado no escopo global (tipo registrado: IIntroStageStep). (@ 24,01s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 24,01s)

<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageStarted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageControlService encontrado no escopo global (tipo registrado: IIntroStageControlService). (@ 24,01s)

[VERBOSE] [SimulationGateService] [Gate] Acquire token='sim.gameplay'. Active=2. IsOpen=False (@ 24,01s)

<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>

<color=#A8DEED>[INFO] [IntroStageCoordinator] [IntroStage] IntroStage ativa: simulação gameplay bloqueada; aguardando confirmação (UI).</color>

<color=#A8DEED>[VERBOSE] [IntroStageCoordinator] [QA][IntroStage] ContextMenu/MenuItem disponíveis para Complete/Skip em Editor/Dev. (@ 24,01s)</color>

<color=#A8DEED>[INFO] [ConfirmToStartIntroStageStep] [OBS][InputMode] Apply mode='FrontendMenu' map='UI' phase='IntroStage' reason='IntroStage/ConfirmToStart' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' scene='GameplayScene' profile='gameplay'.</color>

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'FrontendMenu' (IntroStage/ConfirmToStart).</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'UI' em 1/1 PlayerInput(s) (IntroStage/ConfirmToStart). (@ 24,01s)</color>

[VERBOSE] [SimulationGateService] [Gate] Release token='flow.scene_transition'. Active=1. IsOpen=False (@ 24,01s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=True. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 24,01s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=False, activeTokens=1, reason='scene_transition_completed'. (@ 24,01s)

<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted id=3 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.</color>

[VERBOSE] [GameLoopService] [GameLoop] EXIT: Boot (@ 24,01s)

[VERBOSE] [GameLoopService] [GameLoop] ENTER: Ready (active=False) (@ 24,01s)

[INFO] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] GUI exibido.

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: GateClosed (gateOpen=False, gameplayReady=True, paused=False, serviceState=Ready, gameLoopState='Ready', activeTokens=1). (@ 24,01s)

[VERBOSE] [GameLoopService] [GameLoop] EXIT: Ready (@ 24,02s)

[VERBOSE] [GameLoopService] [GameLoop] ENTER: IntroStage (active=False) (@ 24,02s)

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: GateClosed (gateOpen=False, gameplayReady=True, paused=False, serviceState=Ready, gameLoopState='IntroStage', activeTokens=1). (@ 24,02s)

[INFO] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] Botão Concluir IntroStage clicado.

[INFO] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] Solicitando CompleteIntroStage reason='IntroStage/UIConfirm'.

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 28,39s)

<color=#A8DEED>[INFO] [IntroStageControlService] [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' skip=false decision='applied' state='IntroStage' isActive=true signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>

[INFO] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] GUI oculto.

<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' result='completed' profile='gameplay' target='GameplayScene'.</color>

<color=#A8DEED>[VERBOSE] [IntroStageCoordinator] [IntroStage] Solicitando RequestStart após conclusão explícita da IntroStage. (@ 28,39s)</color>

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: NotPlaying (gateOpen=True, gameplayReady=True, paused=False, serviceState=Ready, gameLoopState='IntroStage', activeTokens=0). (@ 28,39s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=True, activeTokens=0, reason='gate_opened'. (@ 28,39s)

[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=True, scene='GameplayScene', actor='A_75bb36ca_3_Player_NewScriptsClone'. (@ 28,39s)

[VERBOSE] [SimulationGateService] [Gate] Release token='sim.gameplay'. Active=0. IsOpen=True (@ 28,39s)

<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationUnblocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>

[VERBOSE] [GameLoopService] [GameLoop] EXIT: IntroStage (@ 28,40s)

[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 28,40s)

[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 28,40s)

[VERBOSE] [GlobalServiceRegistry] Serviço ISceneFlowSignatureCache encontrado no escopo global (tipo registrado: ISceneFlowSignatureCache). (@ 28,40s)

<color=#A8DEED>[INFO] [GameLoopService] [OBS][InputMode] Apply mode='Gameplay' map='Player' phase='Playing' reason='GameLoop/Playing' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' scene='GameplayScene' profile='gameplay' frame=4941.</color>

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (GameLoop/Playing).</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (GameLoop/Playing). (@ 28,40s)</color>

[VERBOSE] [GameRunStatusService] [GameLoop] GameRunStatusService.Clear() chamado. Resultado resetado para Unknown. (@ 28,40s)

[VERBOSE] [GameRunStatusService] [GameLoop] GameRunStatusService: nova run iniciada (state=Playing). Resultado anterior resetado. (@ 28,40s)

[VERBOSE] [GameRunOutcomeService] [GameLoop] GameRunStartedEvent observado -> rearmando GameRunOutcomeService. state=Playing (@ 28,40s)

<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] GameRunStartedEvent recebido. Ocultando overlay. (@ 28,40s)</color>

<color=#A8DEED>[VERBOSE] [GameplayEndConditionsController] [GameplayEndConditionsController] State reset. reason='GameRunStartedEvent', startTime=23,570. (@ 28,40s)</color>

<color=#A8DEED>[VERBOSE] [GameplayEndConditionsController] [GameplayEndConditionsController] Rearmed on GameRunStartedEvent. (@ 28,40s)</color>

[VERBOSE] [GameLoopService] [GameLoop] Activity: True (@ 28,40s)

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' liberada (gateOpen=True, gameplayReady=True, paused=False, serviceState=Playing, gameLoopState='Playing', activeTokens=0). (@ 28,40s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (0.93, 0.00, 0.36) (scene='GameplayScene'). (@ 28,40s)

[VERBOSE] [GameRunEndRequestService] RequestEnd(Defeat, reason='Gameplay/DevManualDefeat') (@ 28,86s)

[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Defeat, Reason='Gameplay/DevManualDefeat'.

[INFO] [GameRunStatusService] [GameLoop] GameRunStatus atualizado. Outcome=Defeat, Reason='Gameplay/DevManualDefeat'.

[VERBOSE] [GameRunStatusService] [GameLoop] GameRunEndedEvent (Outcome=Defeat) -> PostGame sem PauseOverlay (pausa suprimida). (@ 28,86s)

<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] GameRunEndedEvent recebido. Exibindo overlay. (@ 28,86s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameRunStatusService encontrado no escopo global (tipo registrado: IGameRunStatusService). (@ 28,86s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (0.77, 0.00, 0.64) (scene='GameplayScene'). (@ 30,40s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (-0.14, 0.00, -0.99) (scene='GameplayScene'). (@ 32,40s)

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (0.60, 0.00, 0.80) (scene='GameplayScene'). (@ 34,40s)

[VERBOSE] [GamePauseGateBridge] [PauseBridge] ExitToMenu recebido -> liberando gate Pause (se adquirido por esta bridge). reason='PostGame/ExitToMenu'. (@ 34,74s)

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 34,74s)

[VERBOSE] [GameLoopEventInputBridge] [GameLoop] ExitToMenu recebido -> RequestReady (não voltar para Playing). reason='PostGame/ExitToMenu'. (@ 34,74s)

[VERBOSE] [GlobalServiceRegistry] Serviço IGameNavigationService encontrado no escopo global (tipo registrado: IGameNavigationService). (@ 34,74s)

<color=#A8DEED>[INFO] [ExitToMenuNavigationBridge] [Navigation] ExitToMenu recebido -> RequestMenuAsync. routeId='to-menu', reason='PostGame/ExitToMenu'.</color>

<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-menu', reason='PostGame/ExitToMenu', Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend'.</color>

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=4 signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend' requestedBy='n/a' Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend'</color>

[VERBOSE] [SceneFlowLoadingService] [Loading] Started → Ensure only (Show após FadeIn). signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 34,74s)

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] SceneFlow/Started -> reset dedupe. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 34,74s)</color>

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: GateClosed (gateOpen=False, gameplayReady=False, paused=False, serviceState=Ready, gameLoopState='Playing', activeTokens=1). (@ 34,74s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='gate_closed'. (@ 34,74s)

[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=False, scene='GameplayScene', actor='A_75bb36ca_3_Player_NewScriptsClone'. (@ 34,74s)

[VERBOSE] [SimulationGateService] [Gate] Acquire token='flow.scene_transition'. Active=1. IsOpen=False (@ 34,74s)

[VERBOSE] [GameReadinessService] [Readiness] SimulationGate adquirido com token='flow.scene_transition'. Active=1. IsOpen=False (@ 34,74s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend' (@ 34,74s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_started'. (@ 34,74s)

[VERBOSE] [NewScriptsSceneTransitionProfileResolver] [SceneFlow] Profile resolvido: name='frontend', path='SceneFlow/Profiles/frontend', type='_ImmersiveGames.NewScripts.Infrastructure.SceneFlow.NewScriptsSceneTransitionProfile'. (@ 34,74s)

[VERBOSE] [NewScriptsSceneFlowFadeAdapter] [SceneFlow] Profile 'frontend' aplicado (path='SceneFlow/Profiles/frontend'): fadeIn=0,5, fadeOut=0,5. (@ 34,74s)

[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=1 (dur=0,5) (@ 34,74s)

[INFO] [PostGameOverlayController] [PostGame] ExitToMenu solicitado via overlay. reason='PostGame/ExitToMenu'.

[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] Movement blocked by IStateDependentService. (@ 34,75s)

[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=1 (@ 35,24s)

[VERBOSE] [SceneFlowLoadingService] [Loading] FadeInCompleted → Show. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 35,24s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', phase='AfterFadeIn'. (@ 35,24s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Carregando cena 'MenuScene' (Additive)... (@ 35,24s)

<color=#A8DEED>[VERBOSE] [FrontendButtonBinderBase] [FrontendButton] Click-guard armado por 0,250s (label='OnEnable/Guard', go='Quit_Btn', btn='Quit_Btn'). (@ 35,26s)</color>

<color=#A8DEED>[VERBOSE] [FrontendButtonBinderBase] [FrontendButton] Click-guard armado por 0,250s (label='OnEnable/Guard', go='HowToPlay_Btn', btn='HowToPlay_Btn'). (@ 35,26s)</color>

<color=#A8DEED>[VERBOSE] [FrontendButtonBinderBase] [FrontendButton] Click-guard armado por 0,250s (label='OnEnable/Guard', go='Options_Btn', btn='Options_Btn'). (@ 35,26s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameNavigationService encontrado no escopo global (tipo registrado: IGameNavigationService). (@ 35,26s)

<color=#A8DEED>[VERBOSE] [FrontendButtonBinderBase] [FrontendButton] Click-guard armado por 0,250s (label='OnEnable/Guard', go='Play_Btn', btn='Play_Btn'). (@ 35,26s)</color>

<color=#A8DEED>[VERBOSE] [FrontendPanelsController] [FrontendPanels] Panel='main' (reason='Awake/Initial'). (@ 35,26s)</color>

<color=#A8DEED>[VERBOSE] [FrontendPanelsController] [FrontendPanels] EventSystem selected='Play_Btn' (panel='main'). (@ 35,26s)</color>

[VERBOSE] [ServiceRegistry] Dicionário obtido do pool para serviços. (@ 35,26s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço INewSceneScopeMarker registrado para a cena MenuScene. (@ 35,26s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IWorldSpawnContext registrado para a cena MenuScene. (@ 35,26s)</color>

[INFO] [NewSceneBootstrapper] WorldRoot ready: MenuScene/WorldRoot

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IActorRegistry registrado para a cena MenuScene. (@ 35,26s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IWorldSpawnServiceRegistry registrado para a cena MenuScene. (@ 35,26s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IGameplayResetTargetClassifier registrado para a cena MenuScene. (@ 35,26s)</color>

[VERBOSE] [NewSceneBootstrapper] IGameplayResetTargetClassifier registrado para a cena 'MenuScene'. (@ 35,26s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IGameplayResetOrchestrator registrado para a cena MenuScene. (@ 35,26s)</color>

[VERBOSE] [NewSceneBootstrapper] IGameplayResetOrchestrator registrado para a cena 'MenuScene'. (@ 35,26s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço WorldLifecycleHookRegistry registrado para a cena MenuScene. (@ 35,26s)</color>

[VERBOSE] [NewSceneBootstrapper] WorldLifecycleHookRegistry registrado para a cena 'MenuScene'. (@ 35,26s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Serviço IResetScopeParticipant registrado para a cena MenuScene. (@ 35,27s)</color>

[VERBOSE] [NewSceneBootstrapper] PlayersResetParticipant registrado para a cena 'MenuScene'. (@ 35,27s)

[VERBOSE] [NewSceneBootstrapper] Hook de cena registrado: SceneLifecycleHookLoggerA (@ 35,27s)

[VERBOSE] [NewSceneBootstrapper] WorldDefinition não atribuída (scene='MenuScene'). Isto é permitido em cenas sem spawn (ex.: Ready). Serviços de spawn não serão registrados. (@ 35,27s)

[INFO] [NewSceneBootstrapper] Spawn services registered from definition: 0

[INFO] [NewSceneBootstrapper] Scene scope created: MenuScene

[VERBOSE] [SceneTransitionService] [SceneFlow] Cena 'UIGlobalScene' já está carregada. Pulando load. (@ 35,27s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Cena ativa definida para 'MenuScene'. (@ 35,29s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Descarregando cena 'GameplayScene'... (@ 35,29s)

[VERBOSE] [ServiceRegistry] Dicionário retornado ao pool. Tamanho do pool: 1. (@ 35,30s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Removidos 8 serviços para a cena GameplayScene. (@ 35,30s)</color>

[INFO] [NewSceneBootstrapper] Scene scope cleared: GameplayScene

[INFO] [WorldLifecycleController] Limpando WorldLifecycleHookRegistry na destruição do controller. scene='GameplayScene', hooksCount='0'

[INFO] [WorldLifecycleController] Limpando IWorldSpawnServiceRegistry na destruição do controller. scene='GameplayScene', servicesCount='0'

<color=#A8DEED>[VERBOSE] [CameraResolverService] Camera unregistered for playerId=0: Main Camera. (@ 35,30s)</color>

<color=#A8DEED>[VERBOSE] [CameraResolverService] Default camera updated to null (playerId=0). (@ 35,30s)</color>

<color=#4CAF50>[VERBOSE] [SceneServiceCleaner] Cena GameplayScene descarregada, serviços limpos. (@ 35,31s)</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' sourceSignature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 35,31s)</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] ResetWorld SKIP (profile != gameplay). signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', profile='frontend', targetScene='MenuScene'. (@ 35,31s)</color>

<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 35,31s)</color>

[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 35,31s)

[VERBOSE] [SceneFlowLoadingService] [Loading] ScenesReady → Update pending. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 35,31s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', phase='ScenesReady'. (@ 35,31s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context=Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend' (@ 35,31s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_scenes_ready'. (@ 35,31s)

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] ScenesReady id=4 signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend'.</color>

[VERBOSE] [SceneTransitionService] [SceneFlow] Aguardando completion gate antes do FadeOut. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 35,31s)

[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] Já concluído (cached). signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 35,31s)

[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 35,31s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', phase='BeforeFadeOut'. (@ 35,31s)

[VERBOSE] [SceneFlowLoadingService] [Loading] BeforeFadeOut → Hide. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 35,31s)

[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=0 (dur=0,5) (@ 35,31s)

[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=0 (@ 35,80s)

[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', phase='Completed'. (@ 35,80s)

[VERBOSE] [SceneFlowLoadingService] [Loading] Completed → Safety hide. signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'. (@ 35,80s)

[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 35,80s)

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'FrontendMenu' (SceneFlow/Completed:Frontend).</color>

<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Nenhum PlayerInput ativo encontrado ao aplicar modo 'FrontendMenu'. Isto é esperado em Menu/Frontend. Em Gameplay, verifique se o Player foi spawnado. (@ 35,80s)</color>

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [OBS][InputMode] Applied mode='FrontendMenu' map='UI' signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' scene='MenuScene' profile='frontend' reason='SceneFlow/Completed:Frontend'. (@ 35,80s)</color>

[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 35,80s)

<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] Frontend completed com estado ativo ('Playing'). Solicitando RequestReady() para garantir menu inativo. (@ 35,80s)</color>

[VERBOSE] [StateDependentService] [StateDependent] Action 'Move' bloqueada: GameplayNotReady (gateOpen=True, gameplayReady=False, paused=False, serviceState=Ready, gameLoopState='Playing', activeTokens=0). (@ 35,80s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=True, activeTokens=0, reason='gate_opened'. (@ 35,80s)

[VERBOSE] [SimulationGateService] [Gate] Release token='flow.scene_transition'. Active=0. IsOpen=True (@ 35,80s)

[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=False. Context=Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend' (@ 35,80s)

[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=True, activeTokens=0, reason='scene_transition_completed'. (@ 35,80s)

<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted id=4 signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend'.</color>

<color=#4CAF50>[VERBOSE] [SceneServiceCleaner] SceneServiceCleaner finalizado. (@ 38,49s)</color>

<color=#4CAF50>[VERBOSE] [ObjectServiceRegistry] Removidos 0 serviços de todos os objetos. (@ 38,49s)</color>

[VERBOSE] [ServiceRegistry] Dicionário retornado ao pool. Tamanho do pool: 2. (@ 38,49s)

<color=#4CAF50>[VERBOSE] [SceneServiceRegistry] Removidos 8 serviços de todas as cenas. (@ 38,49s)</color>

[VERBOSE] [GamePauseGateBridge] [PauseBridge] Release ignorado (Dispose) — sem handle ativo (ownership inexistente). IsOpen=True Active=0 (@ 38,49s)

[VERBOSE] [GameRunOutcomeEventInputBridge] GameRunOutcomeEventInputBridge disposed. (@ 38,49s)

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Removidos 40 serviços do escopo global. (@ 38,49s)</color>

<color=#4CAF50>[VERBOSE] [DependencyManager] Serviços limpos no fechamento do jogo. (@ 38,49s)</color>

<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] Bindings de GameRunEnded/GameRunStarted removidos. (@ 0,00s)</color>

[INFO] [NewSceneBootstrapper] Scene scope cleared: MenuScene
