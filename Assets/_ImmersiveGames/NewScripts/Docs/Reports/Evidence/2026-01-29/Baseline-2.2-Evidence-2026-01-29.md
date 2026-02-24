# Baseline 2.2 — Evidence (2026-01-29)

This snapshot is the **canonical** Baseline 2.2 evidence as of **2026-01-29** (America/Sao_Paulo).

It captures the end-to-end “happy path” plus key **gates**, **reset determinism**, **IntroStage unblock**, **ContentSwap in-place**, **Pause/Resume**, and **PostGame flows** (Victory/Defeat, Restart, ExitToMenu), using logs as the source of truth.

---

## Summary of coverage

### A) Boot → Menu (startup) — **SKIP** reset on startup/frontend
Evidence:
- NewScripts global bootstrap completes.
- SceneFlow transitions to Menu with `profile='startup'`.
- WorldLifecycle reset is **SKIP** for startup.

Key log anchors (exact substrings):
- `[VERBOSE] [GlobalBootstrap] NewScripts logging configured.`
- `[SceneFlow] TransitionStarted id=1 signature='p:startup` (target Menu)
- `[WorldLifecycle] ResetCompleted signature='p:startup` and `reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'`
- `[SceneFlow] TransitionCompleted id=1 signature='p:startup` (end of startup transition)

### B) Menu → Gameplay — gameplay **RESET + spawn**
Evidence:
- Transition to Gameplay with `profile='gameplay'`.
- `ResetRequested` is emitted by `WorldLifecycleSceneFlowResetDriver` with canonical reason `SceneFlow/ScenesReady`.
- Reset produces Player + Eater, with registry count reaching **2**.

Key log anchors (exact substrings):
- `[SceneFlow] TransitionStarted id=2 signature='p:gameplay` (target Gameplay)
- `[WorldLifecycle] ResetRequested signature='p:gameplay` and `reason='SceneFlow/ScenesReady'`
- `Reset iniciado. reason='SceneFlow/ScenesReady', scene='GameplayScene'.`
- `Actor spawned:` (Player_NewScripts)
- `Actor spawned:` (Eater_NewScripts)
- `Registry count: 2`
- `[WorldLifecycle] ResetCompleted signature='p:gameplay` and `reason='SceneFlow/ScenesReady'`

### C) IntroStage — simulation gate + UI confirm → Playing
Evidence:
- IntroStage blocks `sim.gameplay` on entry.
- IntroStage completes via UI confirm with canonical reason `IntroStage/UIConfirm`.
- Simulation is unblocked and GameLoop enters Playing.

Key log anchors (exact substrings):
- `[OBS][IntroStage] IntroStageStarted` and `reason='SceneFlow/Completed'`
- `[Gate] Acquire token='sim.gameplay'`
- `[OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm'`
- `[Gate] Release token='sim.gameplay'`
- `[GameLoop] ENTER: Playing`

### D) ContentSwap (in-place) — QA G01
Evidence:
- In-place content swap occurs without a visual transition.
- Canonical reason is present.

Key log anchors (exact substrings):
- `[QA][ContentSwap] G01 start contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'`
- `[ContentSwapContext] ContentSwapCommitted prev='<none>' current='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'`

### E) Pause / Resume — gate + InputMode
Evidence:
- Pause enters with gate token `state.pause`.
- InputMode switches to PauseOverlay.
- Resume releases the same token and restores Gameplay input mode.

Key log anchors (exact substrings):
- `[Gate] Acquire token='state.pause'`
- `[InputMode] Modo alterado para 'PauseOverlay' (PauseOverlay/Show).`
- `[Gate] Release token='state.pause'`
- `[InputMode] Modo alterado para 'Gameplay' (PauseOverlay/Hide).`

### F) PostGame — Victory + Defeat, Restart, ExitToMenu
Evidence:
- PostGame triggers on both Victory and Defeat.
- Restart triggers deterministic Boot cycle and reset.
- ExitToMenu returns to Menu with `profile='frontend'` and **SKIP** reset.

Key log anchors (exact substrings):
- `RequestEnd(Victory, reason='Gameplay/DevManualVictory')`
- `[PostGame] GameRunEndedEvent recebido. Exibindo overlay.`
- `RestartRequested -> RequestReset` and `reason='PostGame/Restart'`
- `[GameLoop] ENTER: Boot` and `Restart->Boot confirmado`
- `RequestEnd(Defeat, reason='Gameplay/DevManualDefeat')`
- `ExitToMenu recebido -> RequestMenuAsync` and `reason='PostGame/ExitToMenu'`
- `[SceneFlow] TransitionStarted id=4 signature='p:frontend` (target Menu)
- `[WorldLifecycle] ResetCompleted signature='p:frontend` and `reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'`

---

## Canonical log excerpt

> This excerpt is intentionally **minimal but sufficient** to validate the Baseline 2.2 matrix A–F via grep-able anchors.

```text
[VERBOSE] [GlobalBootstrap] NewScripts logging configured. (@ 3,26s)
<color=#00BCD4>[VERBOSE] [SceneServiceCleaner] SceneServiceCleaner inicializado. (@ 3,26s)</color>
<color=#00BCD4>[VERBOSE] [DependencyManager] DependencyManager inicializado (DontDestroyOnLoad). (@ 3,26s)</color>
<color=#A8DEED>[VERBOSE] [GlobalBootstrap] [EventBus] EventBus inicializado (GameLoop + SceneFlow + WorldLifecycle). (@ 3,26s)</color>

[VERBOSE] [GameLoopService] [GameLoop] ENTER: Boot (active=False) (@ 3,27s)
[VERBOSE] [GameLoopService] [GameLoop] Initialized. (@ 3,27s)

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] Driver registrado: SceneFlow ScenesReady -> ResetWorld -> ResetCompleted. reason='SceneFlow/ScenesReady'. (@ 3,28s)</color>
<color=#A8DEED>[VERBOSE] [GameNavigationService] [Navigation] GameNavigationService inicializado. Rotas: to-menu, to-gameplay (@ 3,28s)</color>
[INFO] [GameLoopSceneFlowCoordinator] [GameLoopSceneFlow] Coordinator registrado. StartPlan: Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'.
<color=#4CAF50>[INFO] [GlobalBootstrap] ✅ NewScripts global infrastructure initialized (Commit 1 minimal).</color>

<color=#A8DEED>[INFO] [GameStartRequestProductionBootstrapper] [Production][StartRequest] Start solicitado (GameStartRequestedEvent).</color>
[INFO] [GameLoopSceneFlowCoordinator] [GameLoopSceneFlow] Start REQUEST recebido. Disparando transição de cenas...

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup' requestedBy='n/a' Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'</color>
[VERBOSE] [SimulationGateService] [Gate] Acquire token='flow.scene_transition'. Active=1. IsOpen=False (@ 3,66s)

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] ScenesReady id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' sourceSignature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,50s)</color>
<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,50s)</color>
[VERBOSE] [WorldLifecycleResetCompletionGate] [SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,50s)

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'FrontendMenu' (SceneFlow/Completed:Frontend).</color>
[VERBOSE] [SimulationGateService] [Gate] Release token='flow.scene_transition'. Active=0. IsOpen=True (@ 5,98s)
<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.</color>
[VERBOSE] [GameLoopService] [GameLoop] EXIT: Boot (@ 5,99s)
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Ready (active=False) (@ 5,99s)

<color=#A8DEED>[VERBOSE] [MenuPlayButtonBinder] [Navigation] Play solicitado. reason='Menu/PlayButton'. (@ 12,62s)</color>
<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='Menu/PlayButton', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>

<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' requestedBy='n/a' Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'</color>
[VERBOSE] [SimulationGateService] [Gate] Acquire token='flow.scene_transition'. Active=1. IsOpen=False (@ 12,62s)

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' sourceSignature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'. (@ 13,21s)</color>
[INFO] [WorldLifecycleController] Reset iniciado. reason='SceneFlow/ScenesReady', scene='GameplayScene'.
[VERBOSE] [SimulationGateService] [Gate] Acquire token='WorldLifecycle.WorldReset'. Active=2. IsOpen=False (@ 13,21s)

[INFO] [PlayerSpawnService] Actor spawned: A_a2f79d81_1_Player_NewScriptsClone (prefab=Player_NewScripts, instance=Player_NewScripts, root=WorldRoot, scene=GameplayScene)
[INFO] [PlayerSpawnService] Registry count: 1
[INFO] [EaterSpawnService] Actor spawned: A_a2f79d81_2_Eater_NewScriptsClone (prefab=Eater_NewScripts, instance=Eater_NewScripts, root=WorldRoot, scene=GameplayScene)
[INFO] [EaterSpawnService] Registry count: 2
[INFO] [WorldLifecycleController] Reset concluído. reason='SceneFlow/ScenesReady', scene='GameplayScene'.

<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'. (@ 13,25s)</color>
<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] ScenesReady id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.</color>

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (SceneFlow/Completed:Gameplay).</color>
<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageStarted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
[VERBOSE] [SimulationGateService] [Gate] Acquire token='sim.gameplay'. Active=2. IsOpen=False (@ 13,70s)
<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>

<color=#A8DEED>[INFO] [IntroStageControlService] [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' skip=false decision='applied' state='IntroStage' isActive=true signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' result='completed' profile='gameplay' target='GameplayScene'.</color>
[VERBOSE] [SimulationGateService] [Gate] Release token='sim.gameplay'. Active=0. IsOpen=True (@ 15,65s)
<color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationUnblocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 15,65s)

<color=#A8DEED>[INFO] [ContentSwapQaContextMenu] [QA][ContentSwap] G01 start contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals' (fade=False, hud=False).</color>
<color=#A8DEED>[INFO] [ContentSwapChangeServiceInPlaceOnly] [OBS][ContentSwap] ContentSwapRequested event=content_swap_inplace mode=InPlace contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'</color>
[INFO] [ContentSwapContextService] [ContentSwapContext] ContentSwapCommitted prev='<none>' current='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'
<color=#4CAF50>[INFO] [ContentSwapQaContextMenu] [QA][ContentSwap] G01 done contentId='content.2'.</color>

[VERBOSE] [SimulationGateService] [Gate] Acquire token='state.pause'. Active=1. IsOpen=False (@ 104,93s)
<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'PauseOverlay' (PauseOverlay/Show).</color>
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Paused (active=False) (@ 104,94s)

[VERBOSE] [SimulationGateService] [Gate] Release token='state.pause'. Active=0. IsOpen=True (@ 105,59s)
<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (PauseOverlay/Hide).</color>
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 105,59s)

[VERBOSE] [GameRunEndRequestService] RequestEnd(Victory, reason='Gameplay/DevManualVictory') (@ 106,65s)
[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Victory, Reason='Gameplay/DevManualVictory'.
<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] GameRunEndedEvent recebido. Exibindo overlay. (@ 106,65s)</color>

<color=#A8DEED>[INFO] [GameLoopEventInputBridge] [GameLoop] RestartRequested -> RequestReset (expect Boot cycle). reason='PostGame/Restart'.</color>
<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='PostGame/Restart', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.</color>
<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=3 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' requestedBy='n/a' Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'</color>
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Boot (active=False) (@ 107,55s)
<color=#A8DEED>[INFO] [GameLoopService] [GameLoop] Restart->Boot confirmado (reinício determinístico).</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' sourceSignature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'. (@ 108,05s)</color>
[INFO] [WorldLifecycleController] Reset iniciado. reason='SceneFlow/ScenesReady', scene='GameplayScene'.
<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'. (@ 108,06s)</color>

<color=#A8DEED>[INFO] [IntroStageControlService] [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' skip=false decision='applied' state='IntroStage' isActive=true signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True) (@ 110,82s)

[VERBOSE] [GameRunEndRequestService] RequestEnd(Defeat, reason='Gameplay/DevManualDefeat') (@ 111,99s)
[INFO] [GameRunOutcomeService] [GameLoop] Publicando GameRunEndedEvent. Outcome=Defeat, Reason='Gameplay/DevManualDefeat'.
<color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] GameRunEndedEvent recebido. Exibindo overlay. (@ 111,99s)</color>

<color=#A8DEED>[INFO] [ExitToMenuNavigationBridge] [Navigation] ExitToMenu recebido -> RequestMenuAsync. routeId='to-menu', reason='PostGame/ExitToMenu'.</color>
<color=#A8DEED>[INFO] [GameNavigationService] [Navigation] NavigateAsync -> routeId='to-menu', reason='PostGame/ExitToMenu', Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend'.</color>
<color=#A8DEED>[INFO] [SceneTransitionService] [SceneFlow] TransitionStarted id=4 signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend' requestedBy='n/a' Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend'</color>

<color=#A8DEED>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' sourceSignature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 113,57s)</color>
<color=#4CAF50>[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend' target='MenuScene' reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 113,57s)</color>

<color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'FrontendMenu' (SceneFlow/Completed:Frontend).</color>
<color=#4CAF50>[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted id=4 signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene' profile='frontend'.</color>

<color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Removidos 38 serviços do escopo global. (@ 115,81s)</color>
<color=#4CAF50>[VERBOSE] [DependencyManager] Serviços limpos no fechamento do jogo. (@ 115,81s)</color>
```

---

## Notes

- This evidence is intended to be **link-stable** via `Docs/Reports/Evidence/LATEST.md`.
- If future changes alter log signatures or reasons, create a new dated snapshot folder and update `LATEST.md` accordingly.

---

## Apêndice — Arquivos mesclados (consolidação: 1 evidência/dia)

### Fonte mesclada: `Docs/Reports/Evidence/2026-01-29/ADR-0016-Evidence-2026-01-29.md`

# ADR-0016 — Evidência (2026-01-29)

Este snapshot valida o **fechamento do ADR-0016** (ContentSwap integrado ao WorldLifecycle), usando o **Baseline 2.2** como fonte canônica.

## Fonte canônica

- `Baseline-2.2-Evidence-2026-01-29.md` (seção **D**)

## Âncoras observáveis (Console)

As seguintes assinaturas confirmam que o ContentSwap **in-place** foi disparado e registrado com `contentId` e `reason` canônicos:

- `[QA][ContentSwap] ContentSwap triggered mode='InPlace' contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'.`

## Interpretação

- O ContentSwap **não** exige transição de cena (in-place), mas mantém o contrato de observabilidade:
  - `reason` com prefixo `QA/ContentSwap/...`
  - registro explícito de `contentId`

### Fonte mesclada: `Docs/Reports/Evidence/2026-01-29/Verifications/ADR-0016-Verification-2026-01-29.md`

# ADR-0016 — Verification (2026-01-29)

Checklist de verificação para o fechamento do **ADR-0016**.

## Pré-condições

- Execução do Baseline 2.2 (snapshot 2026-01-29).

## Verificações (PASS esperado)

1) **G01 / ContentSwap in-place observado**
- Deve existir no log:
  - `[QA][ContentSwap] ContentSwap triggered mode='InPlace' contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'.`

2) **Sem dependência de transição de cena**
- Não é necessário `SceneTransitionStarted` para o ContentSwap in-place (pode ocorrer dentro da mesma gameplay).

## Referências

- `../Baseline-2.2-Evidence-2026-01-29.md` (seção D)
- `../ADR-0016-Evidence-2026-01-29.md`

