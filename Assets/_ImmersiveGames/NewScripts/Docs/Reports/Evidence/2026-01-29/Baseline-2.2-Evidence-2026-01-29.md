# Baseline 2.2 — Evidence (2026-01-29)

This snapshot is the **canonical** Baseline 2.2 evidence as of **2026-01-29** (America/Sao_Paulo).

It captures the end-to-end “happy path” plus key **gates**, **reset determinism**, **IntroStage unblock**, **ContentSwap in-place**, **Pause/Resume**, and **PostGame flows** (Victory/Defeat, Restart, ExitToMenu), using logs as source of truth.

---

## Summary of coverage

### A) Boot → Menu (startup) — frontend **SKIP reset**
Evidence:
- NewScripts global bootstrap completes.
- SceneFlow transitions to Menu with `profile='startup'`.
- ResetCompleted is **SKIP** in frontend.

Key log anchors:
- `[VERBOSE] [GlobalBootstrap] NewScripts logging configured.`
- `[OBS][SceneFlow] TransitionStarted ... profile='startup' target='MenuScene'.`
- `[OBS][WorldLifecycle] ResetCompleted ... result='SKIP' profile='frontend'.`

### B) Menu → Gameplay — gameplay **RESET + spawn**
Evidence:
- Transition to Gameplay with `profile='gameplay'`.
- `ResetWorld` is triggered by `WorldLifecycleSceneFlowResetDriver` with canonical reason.
- `ResetCompleted` occurs before gameplay starts.
- Spawn pipeline yields expected actors (Player + Eater), and registry count matches.

Key log anchors:
- `[OBS][SceneFlow] TransitionStarted ... profile='gameplay' target='GameplayScene'.`
- `[OBS][WorldLifecycle] ResetWorld ... reason='SceneFlow/ScenesReady'.`
- `[OBS][WorldLifecycle] ResetCompleted ... profile='gameplay'.`
- `[OBS][Spawn] ActorRegistryChanged count=2 ... (Player, Eater).`

### C) IntroStage — simulation gate + UI confirm → Playing
Evidence:
- IntroStage blocks `sim.gameplay` on entry.
- IntroStage completes via UI confirm with canonical reason.
- Simulation is unblocked and GameLoop enters Playing.

Key log anchors:
- `[OBS][IntroStage] IntroStageStarted ... reason='SceneFlow/Completed'.`
- `[OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' ...`
- `[OBS][IntroStage] IntroStageCompleted ... reason='IntroStage/UIConfirm'.`
- `[OBS][Gate] GateTokenReleased token='sim.gameplay' reason='IntroStage/UIConfirm'.`
- `[OBS][GameLoop] PhaseEntered phase='Playing' reason='IntroStage/UIConfirm'.`

### D) ContentSwap (in-place) — QA G01
Evidence:
- In-place content swap occurs without a visual transition.
- Canonical reason is present.

Key log anchors:
- `[QA][ContentSwap] InPlaceSwap contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'.`

### E) Pause / Resume — gate + InputMode
Evidence:
- Pause enters with gate token `state.pause`.
- InputMode switches to PauseOverlay.
- Resume releases the same token and restores prior input mode.

Key log anchors:
- `[OBS][Gate] GateTokenAcquired token='state.pause' reason='Pause/Enter'.`
- `[OBS][InputMode] Applied mode='PauseOverlay' reason='Pause/Enter'.`
- `[OBS][Gate] GateTokenReleased token='state.pause' reason='Pause/Exit'.`
- `[OBS][InputMode] Applied mode='Gameplay' reason='Pause/Exit'.`

### F) PostGame — Victory + Defeat (idempotent), Restart, ExitToMenu
Evidence:
- PostGame triggers on both Victory and Defeat.
- Overlay behavior is idempotent (no duplicate acquisition / no leaks).
- Restart triggers deterministic reset and re-enters gameplay flow.
- ExitToMenu returns to frontend and **SKIP** reset in frontend.

Key log anchors:
- `[OBS][PostGame] Entered outcome='Victory' ...`
- `[OBS][PostGame] Entered outcome='Defeat' ...`
- `[OBS][PostGame] RestartRequested reason='PostGame/Restart'.`
- `[OBS][SceneFlow] TransitionStarted ... target='Boot' reason='PostGame/Restart'.`
- `[OBS][WorldLifecycle] ResetCompleted ... profile='gameplay'.`
- `[OBS][PostGame] ExitToMenuRequested reason='PostGame/ExitToMenu'.`
- `[OBS][SceneFlow] TransitionStarted ... target='MenuScene' profile='frontend' reason='PostGame/ExitToMenu'.`
- `[OBS][WorldLifecycle] ResetCompleted ... result='SKIP' profile='frontend'.`

---

## Notes

- This evidence is intended to be **link-stable** via `Docs/Reports/Evidence/LATEST.md`.
- If future changes alter log signatures or reasons, create a new dated snapshot folder and update `LATEST.md` accordingly.
