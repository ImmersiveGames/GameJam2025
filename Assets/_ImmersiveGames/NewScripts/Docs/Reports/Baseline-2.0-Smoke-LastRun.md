# Baseline 2.0 — Smoke Run (Editor PlayMode)

- Result: **FAIL**
- Duration: `7,09s`
- Mode: `MANUAL_ONLY`
- Last signature seen: `p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene`
- Last profile seen: `gameplay`

## Failure reason

- Falha ao publicar GamePauseCommandEvent: Não foi possível localizar método de publish no EventBus para 'GamePauseCommandEvent'.
- Categoria: `pause publish`

## Failure log window (tail)

```
[VERBOSE] [GlobalServiceRegistry] Serviço IUniqueIdFactory encontrado no escopo global (tipo registrado: IUniqueIdFactory). (@ 10,66s)
<color=#FFD54F>[DebugUtility] ⚠️ Chamada repetida no frame 482: [GlobalServiceRegistry] Serviço IStateDependentService encontrado no escopo global (tipo registrado: IStateDependentService).</color>
[VERBOSE] [GlobalServiceRegistry] Serviço IStateDependentService encontrado no escopo global (tipo registrado: IStateDependentService). (@ 10,66s)
[VERBOSE] [ActorRegistry] Ator registrado: A_e8dba75b_2_Eater_NewScriptsClone. (@ 10,67s)
[INFO] [EaterSpawnService] Actor spawned: A_e8dba75b_2_Eater_NewScriptsClone (prefab=Eater_NewScripts, instance=Eater_NewScripts, root=WorldRoot, scene=GameplayScene)
[INFO] [EaterSpawnService] Registry count: 2
[VERBOSE] [WorldLifecycleOrchestrator] Spawn service duration: EaterSpawnService => 1ms (@ 10,67s)
[INFO] [WorldLifecycleOrchestrator] Spawn service completed: EaterSpawnService
[VERBOSE] [WorldLifecycleOrchestrator] Spawn duration: 28ms (@ 10,67s)
[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'After Spawn': 2
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor hooks phase started (actors=2) (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor started: A_e8dba75b_1_Player_NewScriptsClone (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor duration: A_e8dba75b_1_Player_NewScriptsClone => 0ms (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor completed: A_e8dba75b_1_Player_NewScriptsClone (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor started: A_e8dba75b_2_Eater_NewScriptsClone (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor duration: A_e8dba75b_2_Eater_NewScriptsClone => 0ms (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor completed: A_e8dba75b_2_Eater_NewScriptsClone (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterActorSpawn actor hooks phase duration: 1ms (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn phase started (hooks=1) (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn execution order: SceneLifecycleHookLoggerA(order=10000) (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn started: SceneLifecycleHookLoggerA (@ 10,67s)
[INFO] [SceneLifecycleHookLoggerA] [QA] SceneLifecycleHookLoggerA -> OnAfterSpawnAsync
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn duration: SceneLifecycleHookLoggerA => 0ms (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn completed: SceneLifecycleHookLoggerA (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn phase duration: 0ms (@ 10,67s)
[VERBOSE] [WorldLifecycleOrchestrator] OnAfterSpawn phase completed (@ 10,67s)
[VERBOSE] [SimulationGateService] [Gate] Release token='WorldLifecycle.WorldReset'. Active=1. IsOpen=False (@ 10,67s)
[INFO] [WorldLifecycleOrchestrator] Gate Released
[INFO] [WorldLifecycleOrchestrator] World Reset Completed
[VERBOSE] [WorldLifecycleOrchestrator] Reset duration: 59ms (@ 10,67s)
[INFO] [WorldLifecycleController] Reset concluído. reason='ScenesReady/GameplayScene', scene='GameplayScene'.
[VERBOSE] [WorldLifecycleController] Reset enfileirado (posição=1). label='WorldReset(reason='ScenesReady/GameplayScene')', scene='GameplayScene'. (@ 10,67s)
<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 10,67s)</color>
[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 10,67s)
[VERBOSE] [SceneFlowLoadingService] [Loading] ScenesReady → Update pending. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,67s)
[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Show aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='ScenesReady'. (@ 10,67s)
[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 10,67s)
[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=False, activeTokens=1, reason='scene_transition_scenes_ready'. (@ 10,67s)
[VERBOSE] [SceneTransitionService] [SceneFlow] Aguardando completion gate antes do FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,67s)
[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] Já concluído (cached). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,67s)
[VERBOSE] [SceneTransitionService] [SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,67s)
[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='BeforeFadeOut'. (@ 10,67s)
[VERBOSE] [SceneFlowLoadingService] [Loading] BeforeFadeOut → Hide. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 10,67s)
[VERBOSE] [NewScriptsFadeController] [Fade] Iniciando Fade para alpha=0 (dur=0,5) (@ 10,67s)
[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] Movement blocked by IStateDependentService. (@ 10,71s)
[VERBOSE] [NewScriptsFadeController] [Fade] Fade concluído para alpha=0 (@ 11,09s)
[VERBOSE] [NewScriptsLoadingHudService] [LoadingHUD] Hide aplicado. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', phase='Completed'. (@ 11,09s)
[VERBOSE] [SceneFlowLoadingService] [Loading] Completed → Safety hide. signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'. (@ 11,09s)
[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 11,09s)
<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (SceneFlow/Completed:Gameplay).</color>
<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (SceneFlow/Completed:Gameplay). (@ 11,09s)</color>
[VERBOSE] [GlobalServiceRegistry] Serviço IGameLoopService encontrado no escopo global (tipo registrado: IGameLoopService). (@ 11,09s)
<color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputModeSceneFlowBridge] [GameLoop] SceneFlow/Completed:Gameplay -> sincronizando GameLoop. (@ 11,09s)</color>
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' bloqueada: GameplayNotReady (gateOpen=True, gameplayReady=False, paused=False, serviceState=Ready, gameLoopState='Ready', activeTokens=0). (@ 11,09s)
[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=False, gateOpen=True, activeTokens=0, reason='gate_opened'. (@ 11,10s)
[VERBOSE] [NewPlayerMovementController] [Movement][Gate] GateChanged: open=True, scene='GameplayScene'. (@ 11,10s)
[VERBOSE] [SimulationGateService] [Gate] Release token='flow.scene_transition'. Active=0. IsOpen=True (@ 11,10s)
[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=True. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 11,10s)
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' bloqueada: NotPlaying (gateOpen=True, gameplayReady=True, paused=False, serviceState=Ready, gameLoopState='Ready', activeTokens=0). (@ 11,10s)
[VERBOSE] [GameReadinessService] [Readiness] Snapshot publicado. gameplayReady=True, gateOpen=True, activeTokens=0, reason='scene_transition_completed'. (@ 11,10s)
<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] Transição concluída com sucesso.</color>
[VERBOSE] [NewPlayerMovementController] [Movement][StateDependent] Movimento bloqueado por IStateDependentService. (@ 11,10s)
[VERBOSE] [GameLoopService] [GameLoop] EXIT: Ready (@ 11,11s)
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 11,11s)
[VERBOSE] [GameRunStatusService] [GameLoop] GameRunStartedEvent inicial observado (state=Playing). (@ 11,11s)
[VERBOSE] [GameRunOutcomeService] [GameLoop] GameRunStartedEvent observado -> rearmando GameRunOutcomeService. state=Playing (@ 11,11s)
<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] GameRunStartedEvent recebido. Ocultando overlay. (@ 11,11s)</color>
[VERBOSE] [GlobalServiceRegistry] Serviço IInputModeService encontrado no escopo global (tipo registrado: IInputModeService). (@ 11,11s)
[VERBOSE] [DependencyInjector] Injetando IInputModeService do escopo global para PostGameOverlayController. (@ 11,11s)
[VERBOSE] [DependencyInjector] Injeção bem-sucedida: IInputModeService -> PostGameOverlayController (implementação: InputModeService) (@ 11,11s)
[VERBOSE] [GlobalServiceRegistry] Serviço ISimulationGateService encontrado no escopo global (tipo registrado: ISimulationGateService). (@ 11,11s)
[VERBOSE] [DependencyInjector] Injetando ISimulationGateService do escopo global para PostGameOverlayController. (@ 11,11s)
[VERBOSE] [DependencyInjector] Injeção bem-sucedida: ISimulationGateService -> PostGameOverlayController (implementação: SimulationGateService) (@ 11,11s)
<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Modo 'Gameplay' ja ativo. Reaplicando (PostGame/RunStarted). (@ 11,11s)</color>
<color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (PostGame/RunStarted). (@ 11,11s)</color>
[VERBOSE] [GameLoopService] [GameLoop] Activity: True (@ 11,11s)
[VERBOSE] [NewScriptsStateDependentService] [StateDependent] Action 'Move' liberada (gateOpen=True, gameplayReady=True, paused=False, serviceState=Playing, gameLoopState='Playing', activeTokens=0). (@ 11,11s)
[VERBOSE] [NewEaterRandomMovementController] [EaterMovement] New direction: (0.63, 0.00, 0.77) (scene='GameplayScene'). (@ 11,11s)
[Baseline2Smoke] >>> B3) Gameplay stable (Playing + input)
[Baseline2Smoke] >>> C1) Pause/Resume

```

## Evidências

- Menu TransitionCompleted (flag): `True`
- GameLoop Ready (log): `True`
- GameLoop Playing (log): `True`
- Nav log to-gameplay observado: `True`
- Gameplay transition started observado: `True`
- Gameplay transition completed observado: `True`
- Reset completed observado: `True`
- Spawn registry (=2) observado: `True`
- Actor spawned observado: `True`
- Move liberada observado: `True`
- IGameNavigationService resolvido: `<ignored (manual)>`

## Token balance (Acquire vs Release)

- flow.scene_transition: `2` vs `2`
- WorldLifecycle.WorldReset: `1` vs `1`
- state.pause: `0` vs `0`
- state.postgame: `0` vs `0`

## Artifacts

- Raw log: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.log`
- Report: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.md`

