# SF-1.2b Completeness Check (workspace local)

Date: 2026-03-06
Scope checked:
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`

Source of truth:
- Workspace local files only.

## Criteria

### 1) LoadingHudService guard (signature + frame)
- Status: OK
- Required:
  - guard by `signature + Time.frameCount` in `EnsureLoadedAsync`
  - log `[OBS][Loading] LoadingHudEnsure dedupe_same_frame ...`
  - preserve `EnsureLoadedInternalAsync` flow

Evidence (`rg`):
```text
Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:61 int currentFrame = Time.frameCount;
Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:66 [OBS][Loading] LoadingHudEnsure dedupe_same_frame ...
Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:54 EnsureLoadedAsync(...)
Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:95 return EnsureLoadedInternalAsync(signature);
Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:179 private async Task EnsureLoadedInternalAsync(...)
```

### 2) SceneTransitionService dedupe hardening
- Status: OK
- Required:
  - no legacy time-window dedupe (`DuplicateSignatureWindowMs` etc.)
  - same-frame dedupe + in-flight coalesce + accept after completed
  - `_inFlightSignature` cleanup in `finally`
  - completion gate fallback OBS log

Evidence (`rg`):
```text
(no matches for legacy time-window dedupe symbols)

Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:104 ShouldDedupeSameFrame(...)
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:111 IsInFlightSameSignature(...)
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:114 [OBS][SceneFlow] TransitionRequestCoalesced ...
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:141 [OBS][SceneFlow] TransitionRequestAccepted ...
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:200 finally
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:202 _inFlightSignature = string.Empty;
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:204 _transitionGate.Release();
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:419 [OBS][SceneFlow] CompletionGateFallback applied='true' ...
```

## Runtime log note
- NOT TRIGGERED by this run (requires duplicate same-frame or fallback path).
- Static evidence confirms presence in code.

## Result
- SF-1.2b completeness: PASS (static).
- Code patch reapplied: not required.
