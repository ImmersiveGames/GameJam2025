# SceneFlow Cleanup Audit v3 (SF-1.2b hardening mínimo)

Date: 2026-03-06
Scope: `Modules/SceneFlow/**`, `Infrastructure/Composition/**` (callsites), docs de auditoria.
Goal: behavior-preserving hardening for ensure dedupe, transition dedupe safety, and completion gate fallback observability.

## Changes applied (.cs)
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`

## What changed
1) LoadingHud ensure same-frame dedupe (safe)
- Local guard added in `LoadingHudService.EnsureLoadedAsync` using `signature + Time.frameCount`.
- New [OBS] log on dedupe hit:
  - `[OBS][Loading] LoadingHudEnsure dedupe_same_frame signature='...' frame=...`
- Main ensure path preserved (`EnsureLoadedInternalAsync` still runs), only duplicate ensure log emission in same frame is deduped.

2) Transition dedupe by signature hardened (no post-complete drop)
- Removed legacy dedupe-by-time-window behavior.
- New rules in `SceneTransitionService`:
  - dedupe only same `signature` in same frame (`ShouldDedupeSameFrame`), verbose log.
  - coalesce only when same `signature` is already in-flight (`IsInFlightSameSignature`), OBS log.
  - same `signature` after completed is accepted, OBS log.
- New [OBS] logs:
  - `[OBS][SceneFlow] TransitionRequestCoalesced reason='in_flight_same_signature' signature='...'`
  - `[OBS][SceneFlow] TransitionRequestAccepted reason='completed_allows_same_signature' signature='...'`

3) Completion gate fallback observability
- Policy unchanged: non-H1 completion gate exception still proceeds to FadeOut (best-effort).
- Added explicit [OBS] fallback log:
  - `[OBS][SceneFlow] CompletionGateFallback applied='true' reason='<timeout|abort|exception>' signature='...'`
- Added helper `ResolveCompletionGateFallbackReason(Exception ex)`.
- Transition cleanup remains in `finally` (`_transitionInProgress` reset + `_transitionGate.Release()`), now also clears `_inFlightSignature`.

## Required static evidence

### PASSO 0 (problem location + ensure callsites)
Command:
```powershell
rg -n "LoadingHudEnsure|EnsureLoaded|Ensure\(" Modules/SceneFlow Infrastructure/Composition
```
Relevant output:
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:54` (`EnsureLoadedAsync`)
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:66` (`[OBS][Loading] ... dedupe_same_frame`)
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:75` (`[LoadingHudEnsure] ...`)
- `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs:228` and `:269` (double ensure callsites)

### Transition ownership + dedupe/fallback anchors
Command:
```powershell
rg -n "ShouldDedupeSameFrame|IsInFlightSameSignature|TransitionRequestCoalesced|TransitionRequestAccepted|CompletionGateFallback|AwaitCompletionGateAsync|TransitionStarted|ScenesReady|BeforeFadeOut|TransitionCompleted" Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs
```
Relevant output:
- `ShouldDedupeSameFrame`: line ~355
- `IsInFlightSameSignature`: line ~372
- `TransitionRequestCoalesced`: line ~114
- `TransitionRequestAccepted`: line ~141
- `AwaitCompletionGateAsync`: line ~392
- `CompletionGateFallback`: line ~419
- Pipeline anchors still present: `TransitionStarted`, `ScenesReady`, `BeforeFadeOut`, `TransitionCompleted`.

## Behavior-preserving guarantees
- No public contract changes (`interfaces/events/payloads`) in SceneFlow.
- No pipeline order changes in transition flow.
- No composition pipeline stage order change.
- Hardening is local and defensive with added observability.

## Manual validation checklist (Editor)
- [ ] A) Boot -> Menu -> Gameplay (Play)
- [ ] B) NextLevel
- [ ] C) PostGame/Restart
- [ ] D) PostGame/Restart novamente (logo em seguida)
- [ ] E) RestartCurrentLevelLocal -> PostGame/Restart

Expected checks:
- [ ] warning `Chamada repetida no frame ... [LoadingHudEnsure]` não reaparece.
- [ ] nenhuma transição legítima é dropada por signature após `Completed`.
- [ ] logs âncora continuam presentes: `TransitionStarted`, `ScenesReady`, `BeforeFadeOut`, `TransitionCompleted`.

## Residual risk
- Same-frame dedupe may hide intentionally duplicated requests that are exactly same signature in same frame; mitigated by OBS logs.
- Concurrent different-signature requests are still rejected while one transition is in-flight (existing policy preserved).
