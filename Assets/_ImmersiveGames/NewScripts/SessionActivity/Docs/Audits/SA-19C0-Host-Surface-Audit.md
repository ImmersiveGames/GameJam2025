# SA-19C0 — Host Surface Audit

**Date:** 2026-06-15  
**Status:** CLOSED / Completed / No runtime changes  
**Scope:** `SessionActivity/Pipeline/SessionActivityHost.cs` after `SA-19B3-A1` functional PASS.

## Decision

`SessionActivityHost` is not a lifecycle owner. It should remain a Unity `MonoBehaviour` boundary for composition, inspector-held authoring references, Operational route-exit/visual-readiness boundary registration, and QA/debug integration only.

Proceed with a grouped `SA-19C1` implementation, but do not touch `ActivityExitActorTeardownStage`, retained participant lookup, ObjectSetup, reset policy, command/projectile, or SessionOperational.

## Current surface

`SessionActivityHost.cs` currently has approximately 536 lines and mixes:

- Unity lifecycle (`Awake`, `Start`, `OnDisable`, `Update`)
- composition bootstrap (`GetOrCreateCompositionInstaller`, `BindComposition`)
- Operational boundaries (`ISessionActivityRouteExitTeardownBoundary`, `ISessionActivityVisualReadinessBoundary`)
- QA command forwarding (`CompleteCurrentActivity`, `RestartCurrentActivity`, reset/snapshot/attribute QA)
- debug observation loop (`Update`, `StateObservedChanged`, `BuildStateObservationToken`)
- read-only debug surface (`State`, `Catalog`, `Pipeline`, `GateState`, stage/current rail helpers)
- fatal guard for scene unload without canonical route-exit/deactivation

## Matrix

| File / member | Current responsibility | Correct owner | Problem | Severity | Recommended action | Risk | Evidence |
|---|---|---|---|---:|---|---:|---|
| `SessionActivityHost.Awake` | Build runtime catalog and ask installer to compose | Host as Unity bootstrap boundary | Acceptable; Host owns serialized refs, not lifecycle | Low | Keep | Low | Requires `activityCatalog`; calls installer |
| `SessionActivityHost.Start` / `autoStart` | Blocks non-canonical autostart | Host config guard | Acceptable as guard; no activity lifecycle execution | Low | Keep, optionally move message constants later | Low | Throws if runtime autoStart enabled |
| `SessionActivityHost.OnDisable` | Decides whether scene unload/route-exit is canonical | `SessionActivityPipeline` should classify lifecycle validity; Host only invokes guard | Host directly reads stages/rail and decides fatal lifecycle condition | High | Move decision to pipeline method such as `ValidateHostDisableOrThrow(...)`; Host just delegates | Medium | Reads `CurrentStage`, `ActiveRailKind`, definition |
| `ISessionActivityRouteExitTeardownBoundary` methods | Operational route-exit boundary delegates | Host boundary delegating to pipeline | Correct shape | Low | Keep | Low | `RequestRouteExitTeardown`, `AwaitRouteExitTeardownAsync` delegate |
| `ISessionActivityVisualReadinessBoundary` method | Operational visual readiness boundary delegates | Host boundary delegating to pipeline | Correct shape | Low | Keep | Low | `AwaitVisualReadinessAsync` delegates |
| `Pipeline` public property | Exposes full macro pipeline to debug/UI | Debug surface should be narrow/read-only | Leaks full pipeline from Host; debug panel reads internals through it | High | Replace with narrow read-only helpers needed by debug panel | Medium | Debug panel reads `host.Pipeline.GetCurrentActivityContentLoadedSet()` and `host.Pipeline.EntryPipeline.GetCurrentActivitySetupInventory()` |
| `State`, `Catalog`, `GateState`, `CurrentStage`, `HasPendingOperation` | Read-only debug/QA surface | Host debug/read-only boundary | Acceptable if kept read-only | Medium | Keep for C1; avoid adding write APIs | Low | Debug panel uses many reads |
| `Update` + `StateObservedChanged` | Poll state for QA/debug refresh | Debug/QA observation surface, not lifecycle | Acceptable but too much host-local debug logic | Medium | Keep for now or move to debug panel in later C2; not first C1 risk | Medium | Polling only emits event/log |
| Lifecycle QA methods (`CompleteCurrentActivity`, `CompleteActivationWindow`, `CompleteDeactivationWindow`, `ContinueToNextActivity`, `RestartCurrentActivity`, `ResetSession`, pause/resume) | UI/debug command forwarding | QA command surface or Host as QA boundary delegating to pipeline | Delegating is acceptable; Host still large | Medium | For C1, group into a concrete internal `SessionActivityHostQaCommandSurface` or keep if avoiding UI churn | Medium | Methods call `_pipeline.*` and `LogResult` |
| Removed navigation shortcuts (`GoToNextActivity`, `GoToPreviousActivity`, `GoToActivity`, `DebugStartActivity`) | Explicitly block bypasses | Host contract guard | Correct as defensive guard while UI may call old buttons | Low | Keep until debug panel no longer references; document as blocked compatibility | Low | Throws `[FATAL][Contract]` |
| Attribute/reset/snapshot QA methods | Test tooling calls pipeline QA helpers | QA tooling, not Host core | Host owns too much QA-specific code | Medium | Move to dedicated QA command surface in C1 if call sites can be contained | Medium | `QaApplyActorAttributeCommand`, `QaResetCurrentPlayerActor`, object reset, snapshot capture |
| `BuildHostBanner`, `BuildStateObservationToken`, `GetNextExpectedQaAction` | Debug formatting | Debug panel / debug view | Host formatting is not lifecycle owner, but bloats Host | Low/Medium | Defer unless C1 extracts debug surface; not blocker | Low | Pure formatting |
| `FormatActivitySetupInventory` | Reads `EntryPipeline` through full pipeline | ActivityEntry/debug surface | Leaks EntryPipeline through Host | Medium | Replace with pipeline helper or remove if unused | Low | Method appears local/unused candidate |

## Anti-deslocamento answers

1. **Pipeline owner:** `SessionActivityPipeline` owns macro lifecycle/order; `ActivityEntryPipeline` owns entry lifecycle. Host owns only Unity boundary/delegation.
2. **Category:** This is boundary-surface audit; no runtime stage/policy/adapter change in C0.
3. **Final or bridge:** Current Host has final boundary roles plus QA/debug bridge roles. QA/debug bridge is allowed only if explicit and narrow.
4. **Compatibility needed:** Full `Pipeline` property compatibility is not architecturally needed; debug panel dependence should be narrowed.
5. **Symptom or boundary:** Boundary issue: Host exposes and decides too much beyond MonoBehaviour/boundary duties.
6. **Duplicate lifecycle owner:** Yes, partially: `OnDisable` makes lifecycle classification that belongs in pipeline.
7. **Extraction value:** C1 must remove ownership leakage, not just move methods to another generic helper.
8. **Concrete stable name:** Recommended names: `SessionActivityHostQaCommandSurface` for QA command delegation; pipeline guard method for disable validation; narrow debug helpers instead of exposing full pipeline.
9. **No generic manager/coordinator:** Do not create HostManager/HostCoordinator/HostProcessor.
10. **Smoke vs ownership:** Current smoke proves behavior. This audit proves the next ownership gap is Host surface, not more teardown decomposition.

## Recommended grouped C1

**SA-19C1 — Make Host Thin Delegator / First Batch**

Implement in one grouped cut:

1. Move `OnDisable` decision to `SessionActivityPipeline.ValidateHostDisableOrThrow(source, reason)` or equivalent concrete pipeline method. Host keeps only Unity lifecycle callback and delegates.
2. Remove or narrow public `SessionActivityHost.Pipeline` exposure. Replace debug panel needs with explicit read-only methods/properties:
   - current content loaded set summary;
   - current activity setup inventory summary/accessor if still needed;
   - avoid exposing `EntryPipeline`.
3. Extract QA command forwarding into a concrete internal surface if it reduces Host materially without breaking Unity serialized UI references:
   - `SessionActivityHostQaCommandSurface` may be a private/internal helper owned by Host, not a lifecycle owner;
   - Host public methods can remain wrappers for UI buttons, but logic/logging should move out.
4. Keep Operational boundary methods on Host.
5. Keep `Awake`, serialized config, and installer composition on Host.

## Do not do in C1

- Do not touch `ActivityExitActorTeardownStage`.
- Do not reopen `SA-19B2` or `SA-19B3`.
- Do not change ObjectSetup, retained participant lookup, reset policy, command/projectile, camera, movement, or content release.
- Do not create manager/coordinator/processor.
- Do not move lifecycle to Host helper/QA helper.
- Do not change event names intentionally.

## Acceptance for C1

- `SessionActivityHost` visibly smaller or visibly thinner.
- Host no longer classifies route-exit/unload lifecycle by itself.
- Full pipeline is no longer exposed unless a documented temporary debug-only exception remains.
- Operational boundary behavior preserved.
- Smoke baseline required:
  - `RestartCurrentActivity`
  - `Activity01ToActivity02`
  - `RouteExitBackToMenu`
  - no `error CS`, `FATAL`, `Exception`, `route_transition_failed`, `checkpointStatus='Failed'`
  - no foreign/stale/fallback regression.

## Status update

```text
SA-19B2 teardown residual — PAUSED / sufficient to unblock plan
SA-19B3-A1 — CLOSED / PASS
SA-19B3 — SATISFIED FOR CURRENT CHECKPOINT
SA-19C0 — CLOSED / Completed / No runtime changes
Next — SA-19C1 grouped implementation
```
