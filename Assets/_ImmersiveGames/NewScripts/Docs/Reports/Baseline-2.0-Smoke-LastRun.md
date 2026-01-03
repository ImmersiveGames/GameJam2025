# Baseline 2.0 Smoke - Última execução

- Resultado: **PASS**
- Timestamp: 2026-01-03 00:09:45
- Mode: MANUAL_ONLY

## Etapas
- [PASS] Resolve services — IGameCommands=OK, IGameNavigationService=OK
- [PASS] Menu stable — Evidência via log: Release token='flow.scene_transition'. Active=0. IsOpen=True
- [PASS] Gameplay transition started — Evidência via log: Profile='gameplay'
- [PASS] Gameplay Playing — Evidência via log: ENTER: Playing
- [PASS] Gameplay stable — Playing já observado.
- [PASS] Pause — Evidência via log: ENTER: Paused
- [PASS] Resume — Evidência via log: ENTER: Playing

## Janela final de logs
```
[INFO] [WorldLifecycleOrchestrator] Despawn started
[INFO] [WorldLifecycleOrchestrator] Despawn service started: PlayerSpawnService
[VERBOSE] [PlayerSpawnService] DespawnAsync iniciado (scene=GameplayScene). (@ 10,29s)
<color=cyan>[VERBOSE] [PlayerSpawnService] Despawn ignorado (no actor). (@ 10,29s)</color>
[VERBOSE] [WorldLifecycleOrchestrator] Despawn service duration: PlayerSpawnService => 0ms (@ 10,29s)
[INFO] [WorldLifecycleOrchestrator] Despawn service completed: PlayerSpawnService
[INFO] [WorldLifecycleOrchestrator] Despawn service started: EaterSpawnService
[VERBOSE] [EaterSpawnService] DespawnAsync iniciado (scene=GameplayScene). (@ 10,29s)
<color=cyan>[VERBOSE] [EaterSpawnService] Despawn ignorado (no actor). (@ 10,29s)</color>
[VERBOSE] [WorldLifecycleOrchestrator] Despawn service duration: EaterSpawnService => 0ms (@ 10,29s)
[INFO] [WorldLifecycleOrchestrator] Despawn service completed: EaterSpawnService
[VERBOSE] [WorldLifecycleOrchestrator] Despawn duration: 1ms (@ 10,29s)
[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'After Despawn': 0
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn phase started (hooks=1) (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn started: SceneLifecycleHookLoggerA (@ 10,29s)
[VERBOSE] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnAfterDespawnAsync (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn duration: SceneLifecycleHookLoggerA => 0ms (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn completed: SceneLifecycleHookLoggerA (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn phase duration: 0ms (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterDespawn phase completed (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn phase started (hooks=1) (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn started: SceneLifecycleHookLoggerA (@ 10,29s)
[VERBOSE] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnBeforeSpawnAsync (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn duration: SceneLifecycleHookLoggerA => 0ms (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn completed: SceneLifecycleHookLoggerA (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn phase duration: 0ms (@ 10,29s)
[VERBOSE] [WorldLifecycleOrchestrator] OnBeforeSpawn phase completed (@ 10,29s)
[INFO] [WorldLifecycleOrchestrator] Spawn started
[INFO] [WorldLifecycleOrchestrator] Spawn service started: PlayerSpawnService
[VERBOSE] [PlayerSpawnService] SpawnAsync iniciado (scene=GameplayScene). (@ 10,29s)
[VERBOSE] [GlobalServiceRegistry] Serviço IUniqueIdFactory encontrado no escopo global (tipo registrado: IUniqueIdFactory). (@ 10,32s)
[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 10,32s)
[VERBOSE] [GlobalServiceRegistry] Serviço IStateDependentService encontrado no escopo global (tipo registrado: IStateDependentService). (@ 10,32s)
[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=False, scene='GameplayScene'. (@ 10,32s)
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' bloqueada: GateClosed (gateOpen=False, gameplayReady=False, paused=False, serviceState=Ready, gameLoopState='Ready', activeTokens=2). (@ 10,32s)
[VERBOSE] [ActorRegistry] Ator registrado: A_8a6506f3_1_Player_NewScriptsClone. (@ 10,32s)
[INFO] [PlayerSpawnService] Actor spawned: A_8a6506f3_1_Player_NewScriptsClone (prefab=Player_NewScripts, instance=Player_NewScripts, root=WorldRoot, scene=GameplayScene)
[INFO] [PlayerSpawnService] Registry count: 1
[VERBOSE] [WorldLifecycleOrchestrator] Spawn service duration: PlayerSpawnService => 28ms (@ 10,32s)
[INFO] [WorldLifecycleOrchestrator] Spawn service completed: PlayerSpawnService
[INFO] [WorldLifecycleOrchestrator] Spawn service started: EaterSpawnService
[VERBOSE] [EaterSpawnService] SpawnAsync iniciado (scene=GameplayScene). (@ 10,32s)
<color=#FFD54F>[DebugUtility] ⚠️ Chamada repetida no frame 436: [GlobalServiceRegistry] Serviço IUniqueIdFactory encontrado no escopo global (tipo registrado: IUniqueIdFactory).</color>
[VERBOSE] [GlobalServiceRegistry] Serviço IUniqueIdFactory encontrado no escopo global (tipo registrado: IUniqueIdFactory). (@ 10,32s)
<color=#FFD54F>[DebugUtility] ⚠️ Chamada repetida no frame 436: [GlobalServiceRegistry] Serviço IStateDependentService encontrado no escopo global (tipo registrado: IStateDependentService).</color>
[VERBOSE] [GlobalServiceRegistry] Serviço IStateDependentService encontrado no escopo global (tipo registrado: IStateDependentService). (@ 10,32s)
[VERBOSE] [ActorRegistry] Ator registrado: A_8a6506f3_2_Eater_NewScriptsClone. (@ 10,32s)
[INFO] [EaterSpawnService] Actor spawned: A_8a6506f3_2_Eater_NewScriptsClone (prefab=Eater_NewScripts, instance=Eater_NewScripts, root=WorldRoot, scene=GameplayScene)
[INFO] [EaterSpawnService] Registry count: 2
[VERBOSE] [WorldLifecycleOrchestrator] Spawn service duration: EaterSpawnService => 1ms (@ 10,32s)
[INFO] [WorldLifecycleOrchestrator] Spawn service completed: EaterSpawnService
[VERBOSE] [WorldLifecycleOrchestrator] Spawn duration: 29ms (@ 10,32s)
[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'After Spawn': 2
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor hooks phase started (actors=2) (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor started: A_8a6506f3_1_Player_NewScriptsClone (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor duration: A_8a6506f3_1_Player_NewScriptsClone => 0ms (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor completed: A_8a6506f3_1_Player_NewScriptsClone (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor started: A_8a6506f3_2_Eater_NewScriptsClone (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor duration: A_8a6506f3_2_Eater_NewScriptsClone => 0ms (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor completed: A_8a6506f3_2_Eater_NewScriptsClone (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor hooks phase duration: 1ms (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn phase started (hooks=1) (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn started: SceneLifecycleHookLoggerA (@ 10,33s)
[INFO] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnAfterSpawnAsync
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn duration: SceneLifecycleHookLoggerA => 0ms (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn completed: SceneLifecycleHookLoggerA (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn phase duration: 0ms (@ 10,33s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn phase completed (@ 10,33s)
[VERBOSE] [SimulationGateService] [Gate] Release token='WorldLifecycle.WorldReset'. Active=1. IsOpen=False (@ 10,33s)
[INFO] [WorldLifecycleOrchestrator] Gate Released
[INFO] [WorldLifecycleOrchestrator] World Reset Completed
[VERBOSE] [WorldLifecycleOrchestrator] Reset duration: 63ms (@ 10,33s)
[INFO] [WorldLifecycleController] Reset concluído. reason='ScenesReady/GameplayScene', scene='GameplayScene'.
[VERBOSE] [WorldLifecycleController] Reset enfileirado (posição=1). label='WorldReset(reason='ScenesReady/GameplayScene')', scene='GameplayScene'. (@ 10,33s)
<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 10,33s)</color>
[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 10,33s)
[VERBOSE] [SceneFlowLoadingService] [Loading] ScenesReady → Update pending. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,33s)
[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='ScenesReady'. (@ 10,33s)
[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 10,33s)
[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_scenes_ready'. (@ 10,33s)
[VERBOSE] [SceneTransitionService] [SceneFlow] Aguardando completion gate antes do FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,33s)
[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] Já concluído (cached). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,33s)
[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,33s)
[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='BeforeFadeOut'. (@ 10,33s)
[VERBOSE] [SceneFlowLoadingService] [Loading] BeforeFadeOut → Hide. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,33s)
[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=0 (dur=0,5) (@ 10,33s)
[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] Movement blocked by IStateDependentService. (@ 10,37s)
[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=0 (@ 10,76s)
[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='Completed'. (@ 10,76s)
[VERBOSE] [SceneFlowLoadingService] [Loading] Completed → Safety hide. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,76s)
[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 10,76s)
<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (SceneFlow/Completed:Gameplay).</color>
<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (SceneFlow/Completed:Gameplay). (@ 10,76s)</color>
[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 10,76s)
<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] SceneFlow/Completed:Gameplay -> sincronizando GameLoop. (@ 10,76s)</color>
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' bloqueada: GameplayNotReady (gateOpen=True, gameplayReady=False, paused=False, serviceState=Ready, gameLoopState='Ready', activeTokens=0). (@ 10,76s)
[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=True, activeTokens=0, reason='gate_opened'. (@ 10,76s)
[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=True, scene='GameplayScene'. (@ 10,76s)
[VERBOSE] [SimulationGateService] [Gate] Release token='flow.scene_transition'. Active=0. IsOpen=True (@ 10,76s)
[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=True. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 10,76s)
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' bloqueada: NotPlaying (gateOpen=True, gameplayReady=True, paused=False, serviceState=Ready, gameLoopState='Ready', activeTokens=0). (@ 10,76s)
[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=True, activeTokens=0, reason='scene_transition_completed'. (@ 10,76s)
<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] Transição concluída com sucesso.</color>
[VERBOSE] [NewPlayerMovementController] [Movement][StateDependent] Movimento bloqueado por IStateDependentService. (@ 10,77s)
[VERBOSE] [GameLoopService] [GameLoop] EXIT: Ready (@ 10,77s)
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 10,77s)
[VERBOSE] [GameRunStatusService] [GameLoop] GameRunStartedEvent inicial observado (state=Playing). (@ 10,77s)
[VERBOSE] [GameRunOutcomeService] [GameLoop] GameRunStartedEvent observado -> rearmando GameRunOutcomeService. state=Playing (@ 10,77s)
<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] GameRunStartedEvent recebido. Ocultando overlay. (@ 10,77s)</color>
[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 10,77s)
[VERBOSE] [DependencyInjector] Injetando IInputModeService do escopo global para PostGameOverlayController. (@ 10,77s)
[VERBOSE] [DependencyInjector] Injeção bem-sucedida: IInputModeService -> PostGameOverlayController (implementação: InputModeService) (@ 10,77s)
[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 10,77s)
[VERBOSE] [DependencyInjector] Injetando ISimulationGateService do escopo global para PostGameOverlayController. (@ 10,77s)
[VERBOSE] [DependencyInjector] Injeção bem-sucedida: ISimulationGateService -> PostGameOverlayController (implementação: SimulationGateService) (@ 10,77s)
<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Modo 'Gameplay' ja ativo. Reaplicando (PostGame/RunStarted). (@ 10,77s)</color>
<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (PostGame/RunStarted). (@ 10,77s)</color>
[VERBOSE] [GameLoopService] [GameLoop] Activity: True (@ 10,77s)
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' liberada (gateOpen=True, gameplayReady=True, paused=False, serviceState=Playing, gameLoopState='Playing', activeTokens=0). (@ 10,77s)
[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (-0.48, 0.00, -0.87) (scene='GameplayScene'). (@ 10,77s)
[Baseline2Smoke] PASS: Gameplay Playing: Evidência via log: ENTER: Playing
[Baseline2Smoke] >>> B3) Gameplay stable (Playing + input)
[Baseline2Smoke] PASS: Gameplay stable: Playing já observado.
[Baseline2Smoke] >>> C1) Pause/Resume
[INFO] [GameCommands] [GameCommands] RequestPause reason='baseline2'
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' bloqueada: Paused (gateOpen=False, gameplayReady=True, paused=True, serviceState=Paused, gameLoopState='Playing', activeTokens=1). (@ 10,78s)
[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=False, activeTokens=1, reason='gate_closed'. (@ 10,78s)
[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=False, scene='GameplayScene'. (@ 10,78s)
[VERBOSE] [SimulationGateService] [Gate] Acquire token='state.pause'. Active=1. IsOpen=False (@ 10,78s)
[VERBOSE] [GamePauseGateBridge] [PauseBridge] Gate adquirido com token='state.pause'. IsOpen=False Active=1 (@ 10,78s)
[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 10,78s)
<color=#A8DEED>[VERBOSE] [PauseOverlayController] [PauseOverlay] Overlay ativado. (@ 10,78s)</color>
<color=#A8DEED>[VERBOSE] [PauseOverlayController] [PauseOverlay] ShowLocal (reason='PauseCommand'). (@ 10,78s)</color>
<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'PauseOverlay' (PauseOverlay/Show).</color>
<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'UI' em 1/1 PlayerInput(s) (PauseOverlay/Show). (@ 10,78s)</color>
[VERBOSE] [GameLoopService] [GameLoop] EXIT: Playing (@ 10,81s)
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Paused (active=False) (@ 10,81s)
[VERBOSE] [GameLoopService] [GameLoop] Activity: False (@ 10,81s)
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' bloqueada: Paused (gateOpen=False, gameplayReady=True, paused=True, serviceState=Paused, gameLoopState='Paused', activeTokens=1). (@ 10,81s)
[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] Movement blocked by IStateDependentService. (@ 10,81s)
[Baseline2Smoke] PASS: Pause: Evidência via log: ENTER: Paused
[INFO] [GameCommands] [GameCommands] RequestResume reason='baseline2'
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' bloqueada: NotPlaying (gateOpen=True, gameplayReady=True, paused=False, serviceState=Paused, gameLoopState='Paused', activeTokens=0). (@ 10,81s)
[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=True, activeTokens=0, reason='gate_opened'. (@ 10,81s)
[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=True, scene='GameplayScene'. (@ 10,81s)
[VERBOSE] [SimulationGateService] [Gate] Release token='state.pause'. Active=0. IsOpen=True (@ 10,81s)
[VERBOSE] [GamePauseGateBridge] [PauseBridge] Gate liberado (GameResumeRequestedEvent) token='state.pause'. IsOpen=True Active=0 (@ 10,81s)
[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 10,81s)
<color=#A8DEED>[VERBOSE] [PauseOverlayController] [PauseOverlay] Overlay desativado. (@ 10,81s)</color>
<color=#A8DEED>[VERBOSE] [PauseOverlayController] [PauseOverlay] HideLocal (reason='ResumeRequested'). (@ 10,81s)</color>
<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (PauseOverlay/Hide).</color>
<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (PauseOverlay/Hide). (@ 10,81s)</color>
[VERBOSE] [GameLoopService] [GameLoop] EXIT: Paused (@ 10,82s)
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 10,82s)
[VERBOSE] [GameLoopService] [GameLoop] Activity: True (@ 10,82s)
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' liberada (gateOpen=True, gameplayReady=True, paused=False, serviceState=Playing, gameLoopState='Playing', activeTokens=0). (@ 10,82s)
[Baseline2Smoke] PASS: Resume: Evidência via log: ENTER: Playing
```
