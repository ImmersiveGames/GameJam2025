# Plan - Baseline 4.0 Reorganization

Status: Draft
Date: 2026-03-28
Note: auxiliary execution support only. Subordinate to [Blueprint-Baseline-4.0-Ideal-Architecture.md](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md) and [ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md).

## 1. Executive Summary

O Baseline 4.0 deve consolidar a separacao entre:

- `GameLoop` como estado de fluxo, resultado terminal e pausas.
- `PostGame` como ownership do pos-run, projecao do resultado e contexto visual local.
- `LevelFlow` como ownership da selecao, restart e acoes pos-level.
- `Navigation` como resolucao de intent para route/dispatch.
- `Audio` como dominio standalone de playback e precedencia contextual propria.
- `SceneFlow` como pipeline tecnico de transicao.
- `Frontend/UI` como contexto visual local e emissor de intents, nao como owner de dominio.

O plano abaixo transforma as auditorias em backlog e sequenciamento. Ele nao reabre catalogacao conceitual; usa o que ja foi validado para mover ownership, reduzir duplicacao e separar bridges de dominio.

## 2. Target Architecture by Module

| Module | Final role | Must stay | Must leave | Bridge / adapter | Wrong naming to normalize | Real structural refactor |
|---|---|---|---|---|---|---|
| `GameLoop` | Canonical flow state machine of gameplay | `Boot`, `Ready`, `Playing`, `Paused`, lifecycle signals, run start/end, activity telemetry | Post-run visual ownership, menu overlay ownership, route dispatch policy | `GameRunEndedEventBridge`, scene-flow sync, pause input bridge | `PostPlay` should not be treated as domain phase | Keep domain state; push post-run ownership out of the core loop |
| `PostGame` | Canonical post-run ownership and presentation seam | `PostStage`, post-run ownership service, result projection, overlay, post-run UI state | Primary route resolution, gameplay start policy, flow state machine | `GameRunEndedEventBridge`, `PostGameEntered/Exited`, presenter registry/scope resolver | `PostGame` name can keep legacy surface, but not as macro context | Separate ownership from visual projection and from navigation dispatch |
| `LevelFlow` | Content-local lifecycle and post-level execution | level selection, restart context, next-level/reset/exit actions, level hooks | result ownership, post-run UI ownership, pause ownership | `PostLevelActionsService`, restart snapshot bridge | `Restart` and `ExitToMenu` are intents, not level state | Collapse duplicated restart paths and keep one semantic entry per action |
| `Navigation` | Intent resolution and dispatch | intent catalog, route/style resolution, transition dispatch | result ownership, post-run ownership, pause ownership | intent-to-route dispatch, optional integration bridges | `Restart` / `ExitToMenu` are intents, not navigation-owned outcomes | Reduce to pure `intent -> route/style -> dispatch` |
| `Audio` | Standalone playback domain | global audio, entity audio, cue/profile rules, BGM precedence | navigation ownership, post-run ownership, scene-flow policy | contextual audio bridges from consumers | `EntityAudioSemanticMapAsset` should not carry non-entity scope | Split context resolution from consumer domains and remove cross-module ownership |
| `SceneFlow` | Technical transition pipeline | loading, fade, scene transition ordering, readiness gating | domain semantics for gameplay/post-run/result | transition completion sync, readiness hooks | route/context terms used as technical labels only | Keep technical pipeline; remove semantic drift from route consumers |
| `Frontend/UI` | Local visual contexts and intent emitters | menus, panels, overlays, button binders | domain ownership, route ownership, result ownership | input/action binders, panel controllers | `PauseMenu` / `PostRunMenu` are visual contexts, not macro contexts | Keep as presenter layer that emits intents only |

## 3. Concrete Backlog

### Semantic renames / normalization

- Treat `PostPlay` as technical retention state only.
- Normalize logs/docs that say `PostGame` when they mean `PostPlay` technical retention.
- Normalize `Restart` and `ExitToMenu` as derived intents across `PostGame`, `LevelFlow` and `Navigation`.
- Normalize `PostRunMenu` semantics in docs and UI naming, even if the code uses `PostGameOverlayController`.

### Ownership changes

- Move post-run visual ownership out of `GameLoop` core, keeping only the state handoff.
- Keep `PostGameOwnershipService` as owner of post-run gate/input, not as result owner.
- Keep `PostGameResultService` as projection only, not as decision maker.
- Keep `LevelFlow` as owner of `Restart` execution and `ExitToMenu` execution when the action is level/post-level driven.
- Keep `Navigation` as owner of intent dispatch only, not of post-run semantics.

### Bridge extraction / movement

- Isolate `GameRunEndedEventBridge` as the single handoff bridge from `GameLoop` into `PostGame`.
- Keep `PostGameEnteredEvent` and `PostGameExitedEvent` as bridge events, not as domain states.
- Keep `GameLoopSceneFlowSyncCoordinator` as technical sync only, with no semantic growth.
- Keep audio context resolution in explicit bridges instead of in `Navigation` core.

### Contract simplification

- Reduce duplicated result surfaces between `GameLoop` and `PostGame`.
- Keep one canonical definition of `Restart` execution path.
- Keep one canonical definition of `ExitToMenu` execution path.
- Reduce `PostStage` to a technical stage contract, not a reusable domain phase.
- Reduce `EntityAudioSemanticMapAsset` to entity-only semantic scope, or split it if non-entity mappings remain.

### Duplications to remove or clarify later

- `GameRunResultSnapshotService` vs `PostGameResultService`.
- Direct `ExitToMenu` dispatch from `PostGameOverlayController` vs indirect `GameLoop`-mediated exit.
- Post-run gate/input handling split between `PostGameOwnershipService` and overlay controller.
- Contextual audio precedence duplicated between navigation-facing config and audio-facing resolution.

## 4. Execution Phases

### Phase 1 - Semantic Freeze

- Objective: freeze the conceptual boundary before moving code.
- Modules touched: documentation only.
- Expected changes: update ADR-linked plan, map target ownership, define invariants.
- Risks: over-documenting without action.
- Acceptance: every candidate change has a target owner and a non-regression rule.
- Do not mix: code edits, renames, bridge movement.

### Phase 2 - Core Boundary Cleanup

- Objective: isolate `GameLoop`, `PostGame`, `LevelFlow`, `Navigation`.
- Modules touched: `GameLoop`, `PostGame`, `LevelFlow`, `Navigation`.
- Expected changes: move ownership clarifications, collapse duplicated intent paths, keep bridges thin.
- Risks: breaking restart/exit handoffs or post-run visibility.
- Acceptance: `Playing`, run end, post-run overlay, restart and exit-to-menu still behave the same.
- Do not mix: audio refactor, scene-flow refactor.

### Phase 3 - Audio Boundary Cleanup

- Objective: remove semantic ownership from `Navigation` and keep audio standalone.
- Modules touched: `Audio`, `Navigation`, consumers with audio bridges.
- Expected changes: contextual BGM precedence becomes audio-owned; `EntityAudioSemanticMapAsset` is reduced or split.
- Risks: audio regressions on menu/gameplay/post-run transitions.
- Acceptance: same observable BGM and entity audio behavior, with cleaner ownership.
- Do not mix: post-run ownership changes, scene-flow pipeline changes.

### Phase 4 - SceneFlow and Runtime Integration Hardening

- Objective: keep `SceneFlow` as technical pipeline only.
- Modules touched: `SceneFlow`, `GameLoop`, `LevelFlow`.
- Expected changes: remove semantic drift from transition sync and route labels.
- Risks: loading/fade regressions and race conditions on start/restart.
- Acceptance: scene transitions, readiness, and gameplay start sync remain stable.
- Do not mix: UI redesign, audio catalog work.

### Phase 5 - UI / Presenter Cleanup

- Objective: align `Frontend/UI` with local visual context semantics.
- Modules touched: `Frontend/UI`, `PostGame`, `GameLoop`.
- Expected changes: presenter/binder boundaries become intent emitters only; overlays remain visual contexts.
- Risks: broken button wiring or focus gating.
- Acceptance: UI keeps emitting the same actions, while ownership remains elsewhere.
- Do not mix: domain ownership changes or route catalog changes.

## 5. Non-Regressions by Phase

| Phase | Must keep working | Evidence to validate | Critical flows that cannot break |
|---|---|---|---|
| 1 | none at runtime, only doc traceability | plan references, ownership matrix, backlog trace | baseline alignment and scope discipline |
| 2 | gameplay start, run end, pause/resume, post-run overlay, restart, exit-to-menu | logs for `GameRunStartedEvent`, `GameRunEndedEvent`, `PostGameEntered/Exited`, restart/exit intents | `Playing`, `Victory/Defeat`, `Restart`, `ExitToMenu`, `Pause` |
| 3 | BGM, UI audio, entity audio cues, contextual precedence | before/after audio logs and harness output | menu, gameplay, post-run, pause audio continuity |
| 4 | scene transition ordering, readiness gating, level start/restart sync | transition logs and sync events | startup, gameplay entry, restart loop, menu return |
| 5 | button actions, overlay visibility, focus and gate blocking | UI logs and overlay/gate logs | pause menu, post-run menu, restart, exit-to-menu |

## 6. Open Gaps

| Gap | Why it matters | Plan impact |
|---|---|---|
| Final post-run exit path standardization | `ExitToMenu` still has two semantic exits: direct navigation in `PostGame` and loop-mediated exit in pause | needed before code movement in Phase 2 or 5 |
| Final audio precedence contract shape | audio has already been audited, but the exact ownership split between config, bridge and runtime must stay stable during implementation | needed before Phase 3 starts |
| Result projection duplication | `GameLoop` and `PostGame` both project result snapshots | should be clarified in Phase 2 before any collapse |

## 7. Final Verdict

Ready for planned implementation.

The plan is actionable because the major ownership lines are already known from the audits. The remaining ambiguity is not blocking, but it must be handled explicitly in Phase 2 and Phase 3:

- `ExitToMenu` must be normalized to one canonical execution path.
- `GameRunResultSnapshotService` and `PostGameResultService` must be treated as separate roles until a single projection strategy is chosen.
- audio precedence must stay behavior-preserving while ownership moves away from `Navigation`.
