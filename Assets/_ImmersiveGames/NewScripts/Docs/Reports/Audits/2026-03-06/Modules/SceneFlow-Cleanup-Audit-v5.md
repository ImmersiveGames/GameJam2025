# SF-1.3b.1 - Inline fallback adapters in SceneTransitionService (behavior-preserving)

Date: 2026-03-06
Source of truth: local workspace files.

## What was changed
- Inlined fallback implementations into `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` as private nested classes:
  - `NoFadeAdapter : ISceneFlowFadeAdapter`
  - `NoOpTransitionCompletionGate : ISceneTransitionCompletionGate`
- Kept constructor fallback instantiation unchanged:
  - `_fadeAdapter = fadeAdapter ?? new NoFadeAdapter();`
  - `_completionGate = completionGate ?? new NoOpTransitionCompletionGate();`
- Removed external adapter files:
  - `Modules/SceneFlow/Transition/Adapters/NoFadeAdapter.cs` (+ `.meta`)
  - `Modules/SceneFlow/Transition/Adapters/NoOpTransitionCompletionGate.cs` (+ `.meta`)

## Pre-check evidence (mandatory)
Command:
```text
rg -n "NoFadeAdapter|NoOpTransitionCompletionGate" Modules Infrastructure -g "*.cs"
```
Result (before change):
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:57`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:58`
- `Modules/SceneFlow/Transition/Adapters/NoOpTransitionCompletionGate.cs:12`
- `Modules/SceneFlow/Transition/Adapters/NoFadeAdapter.cs:9`

Decision gate:
- No references outside allowed files. Proceeded.

## Post-change evidence (mandatory)

### 1) Remaining code references
Command:
```text
rg -n "NoFadeAdapter|NoOpTransitionCompletionGate" Modules Infrastructure -g "*.cs"
```
Result (after change):
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:56`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:57`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:790`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:801`

Expected condition satisfied: references only in `SceneTransitionService.cs`.

### 2) Asset scan by name
Command:
```text
rg -n "NoFadeAdapter|NoOpTransitionCompletionGate" -g "*.unity" -g "*.prefab" -g "*.asset" .
```
Result:
- no matches

### 3) Asset scan by old GUIDs
(removed files had GUIDs from previous audit inventory)
Command:
```text
rg -n "33579b530a9e43b0b207ff226d25bc01|842e849dea7d4e5c96ed95b5dd206cc9" -g "*.unity" -g "*.prefab" -g "*.asset" .
```
Result:
- no matches

### 4) Anchor logs textual check
Command:
```text
rg -n "\[SceneFlow\] TransitionStarted|\[SceneFlow\] ScenesReady|\[SceneFlow\] TransitionCompleted|\[OBS\]\[SceneFlow\] CompletionGateFallback" Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs
```
Result:
- anchors present (`TransitionStarted`, `ScenesReady`, `TransitionCompleted`, `CompletionGateFallback`).

## Behavior-preserving statement
- No public interfaces/contracts were changed.
- No intended change to transition timeline/order or fallback policy.
- Existing anchor logs were preserved.
- Smoke A-E should be rerun in Editor/Development Build for runtime confirmation.
