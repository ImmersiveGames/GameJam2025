# SF-1.3b.2a - SceneFlow Signature/Dedupe Consolidation Audit v2 (CODE, behavior-preserving)

Date: 2026-03-06
Source of truth: local workspace files.

## Scope
- `Modules/SceneFlow/Runtime/SceneFlowSameFrameDedupe.cs` (new)
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` (boundary comments only)
- `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs` (boundary comments only)

## Criteria and status
- [OK] Consumer same-frame idempotency consolidated in shared helper.
- [OK] LoadingHudService uses shared helper and preserves `[OBS][Loading] LoadingHudEnsure dedupe_same_frame ...`.
- [OK] SceneFlowInputModeBridge uses shared helper and preserves `[OBS][GRS] InputModeBridge dedupe ...`.
- [OK] TransitionService remains owner of request dedupe (commented boundary only; no logic changes in this step).
- [OK] SceneFlowSignatureCache remains read-model only (commented boundary only; no logic changes in this step).
- [OK] Public contracts/payloads unchanged (interface/events declarations preserved by static evidence below).

## Mandatory static evidence (rg)

### Helper usage in consumers
```text
rg -n "class\s+SceneFlowSameFrameDedupe|ShouldDedupe\(" Modules/SceneFlow/Runtime/SceneFlowSameFrameDedupe.cs Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs Modules/InputModes/Interop/SceneFlowInputModeBridge.cs
```
Relevant lines:
- `Modules/SceneFlow/Runtime/SceneFlowSameFrameDedupe.cs:3` class definition
- `Modules/SceneFlow/Runtime/SceneFlowSameFrameDedupe.cs:9` method definition
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:63` helper call
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:62` helper call

### Logs preserved in altered points
```text
rg -n "\[OBS\]\[Loading\] LoadingHudEnsure dedupe_same_frame|\[OBS\]\[GRS\] InputModeBridge dedupe" Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs Modules/InputModes/Interop/SceneFlowInputModeBridge.cs
```
Relevant lines:
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:70`
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:69`

### Contracts/payload declarations preserved
```text
rg -n "interface\s+ISceneFlowSignatureCache|readonly struct SceneTransitionStartedEvent|readonly struct SceneTransitionCompletedEvent" Modules/SceneFlow/Runtime/ISceneFlowSignatureCache.cs Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs
```
Relevant lines:
- `Modules/SceneFlow/Runtime/ISceneFlowSignatureCache.cs:6`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs:170`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs:174`

## Behavior-preserving note
- This step only consolidates consumer-side same-frame idempotency helper usage.
- Request dedupe ownership remains in `SceneTransitionService`.
- `SceneFlowSignatureCache` remains a read-model.
- No pipeline order/callsites/contracts changed.

## Runtime trigger note
- Runtime logs for new/adjusted dedupe paths are conditional.
- If not observed in a given run, record as:
  - `NOT TRIGGERED by this run (requires duplicate same-frame or fallback path). Static evidence confirms presence in code.`

## Manual checklist (Editor/Dev Build)
- Run smoke A-E.
- Add rapid repeated click/trigger around transition/loading to force same-frame attempts.
- Capture `lastlog.log` and attach to audit evidence.
