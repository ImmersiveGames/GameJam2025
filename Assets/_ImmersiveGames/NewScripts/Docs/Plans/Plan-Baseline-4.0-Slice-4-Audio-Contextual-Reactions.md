# Plan - Baseline 4.0 Slice 4

Subordinate to `ADR-0043`, `ADR-0044` and the baseline blueprint.

Scope:
- `NewScripts` only
- `Docs/Plans` only
- no legacy `Scripts`
- no implementation here
- no reopening `Slice 1`, `Slice 2` or `Slice 3`
- `Save` is out of scope

## 1. Executive Summary

Operational goal of Slice 4: prove `Navigation primary dispatch -> Audio contextual reactions` as a reactive domain.

Status: closed as documented evidence.

Validated runtime backbone:
- `BGM` contextual by route / transition.
- `BGM` contextual by local level swap.
- `RestartFromFirstLevel` correcting the initial level cue.
- transversal ducking by `Pause`.
- entity semantic rail.

Validated ownership:
- `Audio` is the real owner of contextual reactions.
- `Navigation` is the primary dispatch.
- `SceneFlow` is the technical rail.
- `GameLoop` is the upstream source of pause / run state.
- temporary bridges stay bridges, not owners.

Validated runtime evidence:
- `Level1 -> Level2` with real cue change.
- `Level2 -> Level1` with real cue change.
- `RestartFromFirstLevel` from `Level2` correcting to `Level1`.
- same-level reset with legitimate no-op.
- `exit-to-menu` preserving frontend cue.
- early ducking in `PauseWillEnter` / `PauseWillExit` with reconciliation in `PauseStateChanged`.
- entity semantic valid via `semantic_map`.
- entity semantic missing with no-op by `missing_mapping`.
- entity semantic direct with `source='direct'`.

Out of scope:
- `Save`
- wide refactors
- mass renames
- any new architecture outside the blueprint
- reopening the validated BGM bridge issue
- reopening the validated swap-local evidence
- opening Slice 5

## 2. Slice Backbone

### Canonical names

- `Audio`
- `Navigation`
- `SceneFlow`
- `Pause`

### Temporary names / bridges

- `NavigationLevelRouteBgmBridge`
- `AudioPauseDuckingBridge`
- `AudioEntitySemanticService`
- `EntityAudioSemanticMapAsset`
- `AudioRuntimeComposer`
- `SceneTransitionStartedEvent`
- `SceneTransitionBeforeFadeOutEvent`
- `LevelSwapLocalAppliedEvent`
- `PauseWillEnterEvent`
- `PauseWillExitEvent`
- `PauseStateChangedEvent`

### Expected validated backbone

- `Navigation` remains the observable source of route changes.
- `SceneFlow` remains the technical transition rail.
- `Audio` reacts to context and does not define `Gameplay`, `PostRunMenu` or `RunResult`.
- `Pause` remains the transversal state with its own ducking.
- `EntityAudioSemanticMapAsset` must not keep carrying non-entity semantics in the final slice.

### Runtime order target

1. `Navigation` resolves and dispatches the route/intent.
2. `SceneFlow` emits the technical transition lifecycle.
3. `Audio` applies the contextual response.
4. `Pause` applies or removes ducking when the transversal state changes.
5. Runtime logs the audio reaction without taking ownership of `Gameplay`, `PostRunMenu` or `RunResult`.

### Module owners

| Module | Role in slice |
|---|---|
| `Audio` | real owner of contextual reactions, BGM, ducking and semantic routing |
| `Navigation` | owner of the primary dispatch that feeds context |
| `SceneFlow` | technical transition rail |
| `GameLoop` | only upstream source of `Playing` / `Pause` / run end |
| `PostGame` | only upstream source of post-run visual state and consolidated result |
| `LevelFlow` | only upstream source of level/route context |
| `Frontend/UI` | only intent and command emitter |

## 3. Reuse Map

### Navigation / SceneFlow

| Current piece | Decision | Note |
|---|---|---|
| `Modules/Navigation/GameNavigationService.cs` | Keep | observable primary dispatch |
| `Modules/Navigation/GameNavigationCatalogAsset.cs` | Keep | canonical route/intent resolution |
| `Modules/Navigation/GameNavigationIntents.cs` | Keep | canonical intents feeding audio by context |
| `Modules/Navigation/Bootstrap/NavigationBootstrap.cs` | Keep | dispatch composition |
| `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | Keep | technical transport |
| `Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs` | Keep | technical lifecycle for audio reactions |

### Audio contextual

| Current piece | Decision | Note |
|---|---|---|
| `Modules/Audio/Bootstrap/AudioRuntimeComposer.cs` | Keep with reshape | composition point for audio domain |
| `Modules/Audio/Runtime/AudioBgmService.cs` | Keep | owner of contextual BGM |
| `Modules/Audio/Runtime/AudioPauseDuckingBridge.cs` | Temporary bridge | pause/transversal ducking |
| `Modules/Audio/Interop/NavigationLevelRouteBgmBridge.cs` | Temporary bridge | BGM reaction by route/level/transition |
| `Modules/Audio/Runtime/AudioEntitySemanticService.cs` | Keep with reshape | entity semantic routing |
| `Modules/Audio/Config/EntityAudioSemanticMapAsset.cs` | Keep with reshape | entity-purpose mapping only |

### Slice 3 inherited context

| Current piece | Decision | Note |
|---|---|---|
| `Modules/GameLoop/Interop/ExitToMenuCoordinator.cs` | Inherited follow-up, not a blocker | no active bridge issue in this plan |
| `Modules/LevelFlow/Runtime/PostLevelActionsService.cs` | Keep as upstream bridge | observable source of downstream intents |

## 4. Minimal Hooks

Slice 4 needs, at minimum, these existing or consolidated hooks:

| Hook/event | Role |
|---|---|
| `SceneTransitionStartedEvent` | technical route trigger for BGM |
| `SceneTransitionBeforeFadeOutEvent` | final transition confirmation for audio |
| `LevelSwapLocalAppliedEvent` | BGM update on local level swap |
| `PauseWillEnterEvent` | early ducking |
| `PauseWillExitEvent` | ducking release |
| `PauseStateChangedEvent` | effective pause fallback |

Rule:
- do not create a new event if the current lifecycle already covers the reaction
- if a new contract is strictly needed, it must be owned by Audio and not duplicated by consumers
- no `Save`
- no reopening earlier slices

## 5. Short Phases

### Phase 0 - freeze the rail

- freeze `Navigation primary dispatch -> Audio contextual reactions`
- declare `Audio`, `Navigation`, `SceneFlow` and `Pause` as canonical names
- mark `NavigationLevelRouteBgmBridge` and `AudioPauseDuckingBridge` as temporary bridges
- state that `Audio` reacts to context and does not define `Gameplay`, `PostRunMenu` or `RunResult`
- record inherited `ExitToMenu` context as non-blocking
- status: completed

### Phase 1 - contextual BGM

- consolidate BGM reaction by route transition and local level swap
- keep `NavigationLevelRouteBgmBridge` as temporary bridge
- guarantee cue apply/correct logs by route without ownership in `Navigation`
- status: completed

### Phase 2 - transversal ducking

- consolidate `Pause` as a transversal audio state
- keep `AudioPauseDuckingBridge` as temporary bridge
- keep pause transitions unambiguous for `GameLoop`
- status: completed

### Phase 3 - entity semantics

- consolidate `AudioEntitySemanticService` as the entity semantic map router
- keep `EntityAudioSemanticMapAsset` scoped to entity semantics only
- avoid entity audio becoming a global menu/route semantic channel
- contextual BGM by route/level, local swap with real cue change and restart correcting the initial level cue are already closed as validated evidence
- runtime evidence for entity semantics is exercised through the existing QA harness `AudioEntitySemanticQaSceneHarness` via `Play Purpose` context menus
- status: completed

### Phase 4 - rail validation

- connect logs from `Navigation`, `SceneFlow`, `Audio` and `Pause`
- validate that audio reacts to context, not the other way around
- validate that `GameLoop`, `PostGame` and `Frontend/UI` did not gain audio ownership
- status: completed

## 6. Non-blocking observations / follow-ups

- BGM contextual evidence is already closed:
  - local swap `Level1 <-> Level2` with real cue change
  - restart from `Level2` correcting to `Level1` cue
  - same-level reset with no-op
  - exit-to-menu preserving frontend cue
- Entity semantic rail evidence is now validated through the QA harness path.
- `LevelSwapLocalAppliedEvent` remains connected to the contextual BGM rail, with no active evidence gap.
- There are no active follow-ups in this plan.
- Slice 5 is opened separately as `SceneFlow` technical rail work, not as a continuation of Slice 4.

## 7. Acceptance Criteria

Slice 4 is accepted only if:

- `Audio contextual reactions` respond to consolidated context without defining the rail
- `Navigation` remains the observable primary dispatch
- `SceneFlow` stays technical
- `Pause` applies ducking without ownership reentrancy
- `GameLoop`, `PostGame` and `Frontend/UI` do not become audio owners
- `EntityAudioSemanticMapAsset` does not keep carrying non-entity semantics
- Phase 1 stays limited to contextual BGM, without mixing ducking or entity semantics
- `NavigationLevelRouteBgmBridge` does not duplicate or reapply cue in the same effective context
- validated runtime stays coherent with slices 1, 2 and 3
- inherited `ExitToMenu` context stays only annotated, non-blocking
- the documented Slice 4 evidence remains closed
- Slice 5 is handled in a separate plan and does not reopen the validated audio rails

## 8. Inherited / Non-blocking

- In the validated Slice 3 runtime, `ExitToMenu` appeared via `PostLevelActionsService -> Navigation`.
- `ExitToMenuCoordinator` remained registered as a temporary bridge, but its primary bridge role was not proven in the validated flow.
- This stays as inherited context for Slice 4, without blocking and without resolving the canonical `ExitToMenu` bridge here.
- No additional follow-up is active for Slice 4.
