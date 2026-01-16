# Baseline 2.1 — Contract-driven Verification (Last Run)

- Date (local): 2026-01-16 16:18:38
- Status: **Fail**
- Log lines: 999

## Inputs (paths)
- Contract: `C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Observability-Contract.md`
- Log: `C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Baseline-2.1-Smoke-LastRun.log`
- Output: `C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Baseline-2.1-ContractVerification-LastRun.md`

## Diagnostics

- Missing evidence `profile=gameplay` (domain SceneFlow). No close match found.
- Missing evidence `SceneFlow/ScenesReady` (domain SceneFlow). Closest match: line 15: <color=#A8DEED>[VERBOSE] [GlobalBootstrap] [EventBus] EventBus inicializado (GameLoop + SceneFlow + WorldLifecycle). (@ 3,25s)</color>
- Missing evidence `ScenesReady/<scene>` (domain WorldLifecycle). Closest match: line 67: <color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent. (@ 3,26s)</color>
- Missing evidence `ProductionTrigger/<source>` (domain WorldLifecycle). Closest match: line 527: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset REQUESTED. signature='directReset:scene=GameplayScene;src=Gameplay/HotkeyR;seq=1;salt=8a605393', reason='ProductionTrigger/Gameplay/HotkeyR', source='Gameplay/HotkeyR', scene='GameplayScene'.</color>
- Missing evidence `Skipped_StartupOrFrontend:profile=<profile>;scene=<scene>` (domain WorldLifecycle). Closest match: line 243: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [OBS][Phase] ResetRequested sourceSignature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene' profile='startup' target='MenuScene'.</color>
- Missing evidence `Failed_NoController:<scene>` (domain WorldLifecycle). No close match found.
- Missing evidence `ENTER: PostGame` (domain GameLoop). Closest match: line 28: [VERBOSE] [GameLoopService] [GameLoop] ENTER: Boot (active=False) (@ 3,26s)
- Missing evidence `SceneFlow/Completed:*` (domain InputMode). Closest match: line 15: <color=#A8DEED>[VERBOSE] [GlobalBootstrap] [EventBus] EventBus inicializado (GameLoop + SceneFlow + WorldLifecycle). (@ 3,25s)</color>
- Missing evidence `IntroStage/*` (domain InputMode). Closest match: line 34: <color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageCoordinator registrado no escopo global. (@ 3,26s)</color>
- Missing evidence `GameLoop/*` (domain InputMode). Closest match: line 15: <color=#A8DEED>[VERBOSE] [GlobalBootstrap] [EventBus] EventBus inicializado (GameLoop + SceneFlow + WorldLifecycle). (@ 3,25s)</color>
- Missing evidence `PostGame/*` (domain InputMode). Closest match: line 226: <color=#A8DEED>[VERBOSE] [PostGameOverlayController] [PostGame] Bindings de GameRunEnded/GameRunStarted registrados. (@ 5,38s)</color>
- Missing evidence `PhaseChange/In-Place` (domain PhaseChange). Closest match: line 123: <color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IPhaseChangeService registrado no escopo global. (@ 3,28s)</color>
- Missing evidence `PhaseChange/SceneTransition` (domain PhaseChange). Closest match: line 123: <color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IPhaseChangeService registrado no escopo global. (@ 3,28s)</color>
- Missing evidence `PhaseChange/In-Place/<phaseId>` (domain PhaseChange). Closest match: line 123: <color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IPhaseChangeService registrado no escopo global. (@ 3,28s)</color>
- Missing evidence `PhaseChange/SceneTransition/<phaseId>` (domain PhaseChange). Closest match: line 123: <color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IPhaseChangeService registrado no escopo global. (@ 3,28s)</color>
- Missing evidence `phaseId` (domain PhaseChange). No close match found.
- Missing evidence `IntroStage/NoContent` (domain PhaseChange). Closest match: line 34: <color=#4CAF50>[VERBOSE] [GlobalServiceRegistry] Serviço IIntroStageCoordinator registrado no escopo global. (@ 3,26s)</color>
- Missing evidence `ScenesReady/<scene>` (domain PhaseChange). Closest match: line 67: <color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent. (@ 3,26s)</color>
- Missing evidence `ProductionTrigger/<source>` (domain PhaseChange). Closest match: line 527: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset REQUESTED. signature='directReset:scene=GameplayScene;src=Gameplay/HotkeyR;seq=1;salt=8a605393', reason='ProductionTrigger/Gameplay/HotkeyR', source='Gameplay/HotkeyR', scene='GameplayScene'.</color>
- Missing evidence `Skipped_StartupOrFrontend:profile=<...>;scene=<...>` (domain PhaseChange). Closest match: line 243: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [OBS][Phase] ResetRequested sourceSignature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene' profile='startup' target='MenuScene'.</color>
- Missing evidence `Failed_NoController:<scene>` (domain PhaseChange). No close match found.
- Missing evidence `Docs/Reports/Baseline-2.0-Smoke-LastRun.log` (domain PhaseChange). Closest match: line 2: [Baseline21Smoke] Output: C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Baseline-2.1-Smoke-LastRun.log
- Missing evidence `Docs/WORLD_LIFECYCLE.md` (domain PhaseChange). Closest match: line 2: [Baseline21Smoke] Output: C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Baseline-2.1-Smoke-LastRun.log
- Missing evidence `Docs/ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md` (domain PhaseChange). Closest match: line 2: [Baseline21Smoke] Output: C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Baseline-2.1-Smoke-LastRun.log
- Missing evidence `Docs/Reports/QA-IntroStage-Smoke.md` (domain PhaseChange). Closest match: line 2: [Baseline21Smoke] Output: C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Baseline-2.1-Smoke-LastRun.log
- Missing evidence `Skipped_StartupOrFrontend:profile=...;scene=...` (domain PhaseChange). Closest match: line 243: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [OBS][Phase] ResetRequested sourceSignature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene' profile='startup' target='MenuScene'.</color>
- Missing evidence `[WorldLifecycle] Reset REQUESTED. signature='directReset:scene=GameplayScene;src=qa_marco0_reset;seq=4;salt=b3a0e296', reason='ProductionTrigger/qa_marco0_reset', source='qa_marco0_reset', scene='GameplayScene'.` (domain PhaseChange). Closest match: line 67: <color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent. (@ 3,26s)</color>
- Missing evidence `[WorldLifecycle] WorldLifecycleController não encontrado na cena 'MenuScene'. Reset abortado.` (domain PhaseChange). Closest match: line 67: <color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent. (@ 3,26s)</color>
- Missing evidence `Emitting WorldLifecycleResetCompletedEvent. ... reason='Failed_NoController:MenuScene'.` (domain PhaseChange). Closest match: line 246: <color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='startup', signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,47s)</color>

## Domain Results

### SceneFlow — **Fail**

**Evidence found**
- `SceneTransitionStartedEvent`
- `signature`
- `profile`
- `Load`
- `Unload`
- `Active`
- `flow.scene_transition`
- `SceneTransitionScenesReadyEvent`
- `SceneTransitionCompletedEvent`
- `SceneFlow/Started`
- `SceneFlow/Completed`
- `SceneFlow/Completed:Gameplay`

**Evidence missing**
- `profile=gameplay`
- `SceneFlow/ScenesReady`

### WorldLifecycle — **Fail**

**Evidence found**
- `ResetRequested`
- `sourceSignature`
- `reason`
- `profile`
- `target`
- `WorldLifecycleResetCompletedEvent`
- `signature`

**Evidence missing**
- `ScenesReady/<scene>`
- `ProductionTrigger/<source>`
- `Skipped_StartupOrFrontend:profile=<profile>;scene=<scene>`
- `Failed_NoController:<scene>`

### GameLoop — **Fail**

**Evidence found**
- `Ready`
- `ENTER: Ready`
- `active=False`
- `IntroStage`
- `ENTER: IntroStage`
- `[OBS][IntroStage]`
- `Playing`
- `ENTER: Playing`
- `GameRunStartedEvent`
- `PostGame`

**Evidence missing**
- `ENTER: PostGame`

### InputMode — **Fail**

**Evidence found**
- `mode`
- `map`
- `reason`
- `signature`
- `scene`
- `profile`
- `SceneFlow/Completed:Frontend`
- `SceneFlow/Completed:Gameplay`
- `IntroStage/ConfirmToStart`
- `GameLoop/Playing`
- `PostGame/RunStarted`

**Evidence missing**
- `SceneFlow/Completed:*`
- `IntroStage/*`
- `GameLoop/*`
- `PostGame/*`

### PhaseChange — **Fail**

**Evidence found**
- `IntroStage/UIConfirm`
- `signature`
- `SceneFlow/Completed`
- `[WorldLifecycle] Reset SKIPPED (startup/frontend). why='profile', profile='startup', activeScene='MenuScene', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'.`
- `[WorldLifecycle] Reset REQUESTED. reason='ScenesReady/GameplayScene', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', profile='gameplay'.`
- `Solicitando CompleteIntroStage reason='IntroStage/UIConfirm'.`

**Evidence missing**
- `PhaseChange/In-Place`
- `PhaseChange/SceneTransition`
- `PhaseChange/In-Place/<phaseId>`
- `PhaseChange/SceneTransition/<phaseId>`
- `phaseId`
- `IntroStage/NoContent`
- `ScenesReady/<scene>`
- `ProductionTrigger/<source>`
- `Skipped_StartupOrFrontend:profile=<...>;scene=<...>`
- `Failed_NoController:<scene>`
- `Docs/Reports/Baseline-2.0-Smoke-LastRun.log`
- `Docs/WORLD_LIFECYCLE.md`
- `Docs/ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md`
- `Docs/Reports/QA-IntroStage-Smoke.md`
- `Skipped_StartupOrFrontend:profile=...;scene=...`
- `[WorldLifecycle] Reset REQUESTED. signature='directReset:scene=GameplayScene;src=qa_marco0_reset;seq=4;salt=b3a0e296', reason='ProductionTrigger/qa_marco0_reset', source='qa_marco0_reset', scene='GameplayScene'.`
- `[WorldLifecycle] WorldLifecycleController não encontrado na cena 'MenuScene'. Reset abortado.`
- `Emitting WorldLifecycleResetCompletedEvent. ... reason='Failed_NoController:MenuScene'.`

## Invariants

- **Pass** — SceneFlow: ScenesReady before Completed
  - ScenesReady appears before Completed.
- **Pass** — WorldLifecycle: ResetCompleted emitted
  - ResetCompleted evidence found.

## Summary

Status=Fail | Domains=5 | Pass=0 | Fail=5 | Inconclusive=0 | LogLines=999 | Note='Contract-driven verification completed.'
