# Baseline 2.2 — Evidência (2026-02-03)

Este snapshot consolida a evidência canônica do Baseline 2.2 usando o log bruto `Baseline-2.2-Smoke-LastRun.log`.

## Resultado

- **Status:** PASS (cenários A–F)
- **Perfis exercitados:** `startup` → `gameplay` → `frontend`
- **Pontos validados:** SceneFlow, WorldLifecycle (reset determinístico + spawn Player/Eater), IntroStage + gate `sim.gameplay`, ContentSwap in-place (QA), Pause/Resume (`state.pause`), PostGame (Victory/Defeat), Restart e ExitToMenu.

## Resumo de cobertura

### A) Boot → Menu (startup) — reset SKIP

Âncoras:
- `[SceneFlow] TransitionStarted id=1 signature='p:startup`
- `[OBS][WorldLifecycle] ResetCompleted signature='p:startup` + `reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'`
- `[SceneFlow] TransitionCompleted id=1 signature='p:startup`

### B) Menu → Gameplay (profile=gameplay) — reset + spawn

Âncoras:
- `[SceneFlow] TransitionStarted id=2 signature='p:gameplay`
- `[OBS][WorldLifecycle] ResetRequested signature='p:gameplay` + `reason='SceneFlow/ScenesReady'`
- `Actor spawned:` (Player/Eater) + `Registry count: 2`
- `[OBS][WorldLifecycle] ResetCompleted signature='p:gameplay` + `reason='SceneFlow/ScenesReady'`

### C) IntroStage — gate `sim.gameplay` + UIConfirm → Playing

Âncoras:
- `[OBS][IntroStageController] IntroStageStarted`
- `[Gate] Acquire token='sim.gameplay'`
- `[OBS][IntroStageController] CompleteIntroStage received reason='IntroStageController/UIConfirm'`
- `[Gate] Release token='sim.gameplay'`
- `[GameLoop] ENTER: Playing`

### D) ContentSwap in-place (QA)

Âncoras:
- `[QA][ContentSwap] G01 start contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'`
- `[OBS][ContentSwap] ContentSwapRequested ... contentId='content.2'`
- `[ContentSwapContext] ContentSwapCommitted ... current='content.2'`

### E) Pause/Resume — gate `state.pause` + InputMode

Âncoras:
- `[Gate] Acquire token='state.pause'`
- `[InputMode] Modo alterado para 'PauseOverlay'`
- `[Gate] Release token='state.pause'`
- `[InputMode] Modo alterado para 'Gameplay' (PauseOverlay/Hide)`

### F) PostGame — Victory + Restart + Defeat + ExitToMenu

Âncoras:
- `RequestEnd(Victory, reason='Gameplay/DevManualVictory')`
- `[OBS][PostGame] PostGameEntered ... outcome='Victory'`
- `RestartRequested -> RequestReset ... reason='PostGame/Restart'`
- `[OBS][PostGame] PostGameExited ... reason='Restart'`
- `RequestEnd(Defeat, reason='Gameplay/DevManualDefeat')`
- `[OBS][PostGame] PostGameEntered ... outcome='Defeat'`
- `ExitToMenu recebido -> RequestMenuAsync ... reason='PostGame/ExitToMenu'`
- `[SceneFlow] TransitionStarted id=4 signature='p:frontend`
- `[OBS][WorldLifecycle] ResetCompleted signature='p:frontend` + `reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'`
- `[OBS][PostGame] PostGameExited ... reason='ExitToMenu'`

---

## Trecho canônico (mínimo)

> Trecho mínimo e ordenado para auditoria. Omissões usam `[...trecho omitido...]`.

```text
<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup' requestedBy='n/a' Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'</color>
<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 6,37s)</color>
<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.</color>

[...trecho omitido...]

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' requestedBy='n/a' Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'</color>
<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' sourceSignature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'. (@ 7,81s)</color>
[INFO] [PlayerSpawnService] Actor spawned: A_dc755621_1_Player_NewScriptsClone (prefab=Player_NewScripts, instance=Player_NewScripts, root=WorldRoot, scene=GameplayScene)
[INFO] [PlayerSpawnService] Registry count: 1
[INFO] [EaterSpawnService] Actor spawned: A_dc755621_2_Eater_NewScriptsClone (prefab=Eater_NewScripts, instance=Eater_NewScripts, root=WorldRoot, scene=GameplayScene)
[INFO] [EaterSpawnService] Registry count: 2
<color=#4CAF50>[VERBOSE] [ResetWorldService] [OBS][WorldLifecycle] ResetCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' reason='SceneFlow/ScenesReady'. (@ 7,86s)</color>

[...trecho omitido...]

<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStageController] IntroStageStarted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
[VERBOSE] [SimulationGateService] [Gate] Acquire token='sim.gameplay'. Active=2. IsOpen=False (@ 8,28s)
<color=#A8DEED>[INFO] [IntroStageControlService] [OBS][IntroStageController] CompleteIntroStage received reason='IntroStageController/UIConfirm' skip=false decision='applied' state='IntroStage' isActive=true signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
[VERBOSE] [SimulationGateService] [Gate] Release token='sim.gameplay'. Active=0. IsOpen=True (@ 9,72s)
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 9,73s)

[...trecho omitido...]

<color=#A8DEED>[INFO] [ContentSwapQaContextMenu] [QA][ContentSwap] G01 start contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals' (fade=False, hud=False).</color>
<color=#A8DEED>[INFO] [ContentSwapChangeServiceInPlaceOnly] [OBS][ContentSwap] ContentSwapRequested event=content_swap_inplace mode=InPlace contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'</color>
[INFO] [ContentSwapContextService] [ContentSwapContext] ContentSwapCommitted prev='content.1 | sig.content.1' current='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'

[...trecho omitido...]

[VERBOSE] [SimulationGateService] [Gate] Acquire token='state.pause'. Active=1. IsOpen=False (@ 93,58s)
<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'PauseOverlay' (PauseOverlay/Show).</color>
[VERBOSE] [SimulationGateService] [Gate] Release token='state.pause'. Active=0. IsOpen=True (@ 94,39s)
<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (PauseOverlay/Hide).</color>

[...trecho omitido...]

[VERBOSE] [GameRunEndRequestService] RequestEnd(Victory, reason='Gameplay/DevManualVictory') (@ 95,41s)
<color=#A8DEED>[INFO] [GameLoopService] [OBS][PostGame] PostGameEntered signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' outcome='Victory' reason='Gameplay/DevManualVictory' scene='GameplayScene' profile='gameplay' frame=2781.</color>
<color=#A8DEED>[INFO] [GameLoopEventInputBridge] [GameLoop] RestartRequested -> RequestReset (expect Boot cycle). reason='PostGame/Restart'.</color>
<color=#A8DEED>[INFO] [GameLoopService] [OBS][PostGame] PostGameExited signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' reason='Restart' nextState='Boot' scene='GameplayScene' profile='gameplay' frame=3000.</color>
[VERBOSE] [GameRunEndRequestService] RequestEnd(Defeat, reason='Gameplay/DevManualDefeat') (@ 107,18s)
<color=#A8DEED>[INFO] [GameLoopService] [OBS][PostGame] PostGameEntered signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' outcome='Defeat' reason='Gameplay/DevManualDefeat' scene='GameplayScene' profile='gameplay' frame=4683.</color>
<color=#A8DEED>[INFO] [ExitToMenuNavigationBridge] [Navigation] ExitToMenu recebido -> RequestMenuAsync. routeId='to-menu', reason='PostGame/ExitToMenu'.</color>
<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=4 signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend' requestedBy='n/a' Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend'</color>
<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 108,76s)</color>
<color=#A8DEED>[INFO] [GameLoopService] [OBS][PostGame] PostGameExited signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' reason='ExitToMenu' nextState='Ready' scene='MenuScene' profile='frontend' frame=4891.</color>
```

---

## Referências

- Log bruto: `Baseline-2.2-Smoke-LastRun.log`
- Padrão de evidência: `Docs/Reports/Evidence/README.md`
- Observability contract: `Docs/Standards/Standards.md#observability-contract`
