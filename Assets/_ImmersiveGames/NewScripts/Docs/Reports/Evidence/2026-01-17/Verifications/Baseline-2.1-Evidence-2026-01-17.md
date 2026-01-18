# Baseline 2.1 — Evidence — 2026-01-17

## Status

**PASS (evidência consolidada via log fornecido no chat)**

## Contexto do run

- Perfil startup faz transição para Menu com **reset skip** (profile != gameplay).
- Menu → Gameplay dispara SceneFlow (fade + loading), executa **WorldLifecycle Reset** em GameplayScene e spawna Player + Eater.
- IntroStage bloqueia simulação (`sim.gameplay`) até confirmação via UI (`IntroStage/UIConfirm`), depois libera e entra em Playing.
- Run termina em Victory e Defeat via comandos Dev; PostGame overlay aciona Restart e ExitToMenu; transições e gates coerentes.

## Invariantes observadas (âncoras)

### INV-01 — SceneTransitionStarted fecha gate (token `flow.scene_transition`)
```log
[VERBOSE] [SimulationGateService] [Gate] Acquire token='flow.scene_transition'. Active=1. IsOpen=False (@ 7,57s)
[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 7,57s)
````

### INV-02 — TransitionCompleted libera gate `flow.scene_transition`

```log
[VERBOSE] [SimulationGateService] [Gate] Release token='flow.scene_transition'. Active=1. IsOpen=False (@ 8,67s)
[VERBOSE] [GameReadinessService] [Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. gameplayReady=True. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 8,67s)
<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.</color>
```

### INV-03 — Em gameplay, ScenesReady dispara ResetWorld e completa

```log
<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] Disparando ResetWorld para 1 controller(s). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', targetScene='GameplayScene'. (@ 8,17s)</color>
[INFO] [WorldLifecycleController] Reset iniciado. reason='SceneFlow/ScenesReady', scene='GameplayScene'.
[INFO] [WorldLifecycleController] Spawn services coletados para a cena 'GameplayScene': 2 (registry total: 2).
[INFO] [WorldLifecycleOrchestrator] World Reset Completed
[INFO] [WorldLifecycleController] Reset concluído. reason='SceneFlow/ScenesReady', scene='GameplayScene'.
<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] ResetWorld concluído (ScenesReady). signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', targetScene='GameplayScene'. (@ 8,22s)</color>
```

### INV-04 — Spawn determinístico: Player + Eater registrados no ActorRegistry

```log
[INFO] [PlayerSpawnService] Actor spawned: A_41edb28e_1_Player_NewScriptsClone (prefab=Player_NewScripts, instance=Player_NewScripts, root=WorldRoot, scene=GameplayScene)
[INFO] [EaterSpawnService] Actor spawned: A_41edb28e_2_Eater_NewScriptsClone (prefab=Eater_NewScripts, instance=Eater_NewScripts, root=WorldRoot, scene=GameplayScene)
[INFO] [WorldLifecycleOrchestrator] ActorRegistry count at 'After Spawn': 2
```

### INV-05 — IntroStage bloqueia gameplay via token `sim.gameplay` até confirmação

```log
<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageStarted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
[VERBOSE] [SimulationGateService] [Gate] Acquire token='sim.gameplay'. Active=2. IsOpen=False (@ 8,66s)
<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
[INFO] [IntroStageRuntimeDebugGui] [IntroStage][RuntimeDebugGui] Solicitando CompleteIntroStage reason='IntroStage/UIConfirm'.
<color=#A8DEED>[INFO] [IntroStageControlService] [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' skip=false signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' result='completed' profile='gameplay' target='GameplayScene'.</color>
[VERBOSE] [SimulationGateService] [Gate] Release token='sim.gameplay'. Active=0. IsOpen=True (@ 9,90s)
<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationUnblocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
```

### INV-06 — GameLoop entra em Playing após IntroStage e publica GameRunStarted

```log
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 9,91s)
[VERBOSE] [GameRunStatusService] [GameLoop] GameRunStartedEvent inicial observado (state=Playing). (@ 9,91s)
```

### INV-07 — PostGame → Restart e PostGame → ExitToMenu executam transições

```log
[VERBOSE] [GameRunEndRequestService] RequestEnd(Victory, reason='Gameplay/DevManualVictory') (@ 11,41s)
[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Victory, Reason='Gameplay/DevManualVictory'.
<color=#A8DEED>[INFO] [GameLoopEventInputBridge] [GameLoop] RestartRequested -> RequestReset (expect Boot cycle). reason='PostGame/Restart'.</color>
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Boot (active=False) (@ 12,55s)
[VERBOSE] [GameRunEndRequestService] RequestEnd(Defeat, reason='Gameplay/DevManualDefeat') (@ 15,59s)
[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Defeat, Reason='Gameplay/DevManualDefeat'.
[VERBOSE] [GameLoopEventInputBridge] [GameLoop] ExitToMenu recebido -> RequestReady (não voltar para Playing). reason='PostGame/ExitToMenu'. (@ 16,38s)
<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-menu', reason='PostGame/ExitToMenu', Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend'.</color>
<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend'.</color>
```

## Artefatos

* Log bruto: **fornecido via chat** (recomendado arquivar como `Reports/Logs/Baseline-2.1-Smoke-2026-01-17.log` e manter este arquivo como ponte canônica).
* Fonte de verdade: linhas acima (âncoras de evidência).

---