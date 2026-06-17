# SA-19C2-AUDIT — Route-Exit Teardown Ownership Lock Audit

**Date:** 2026-06-15  
**Status:** `CLOSED / Completed / No runtime changes`  
**Scope:** audit only. No runtime implementation.

## Context

This audit follows:

```text
SA-19B2 teardown residual — PAUSED / sufficient to unblock plan
SA-19B3-A1 — CLOSED / PASS
SA-19C0 — CLOSED / Completed / No runtime changes
SA-19C1 — CLOSED / PASS funcional canônico / OnDisable unexercised
```

The purpose is to verify whether route-exit teardown ownership is locked after Host thinning.

## Files audited

```text
SessionActivity/Pipeline/SessionActivityHost.cs
SessionActivity/Pipeline/SessionActivityPipeline.cs
SessionActivity/Contracts/ISessionActivityRouteExitTeardownBoundary.cs
SessionOperational/Adapters/SessionActivityOperationalRouteHandoffExitAdapter.cs
SessionOperational/Pipeline/OperationalHandoffExitStage.cs
SessionOperational/Pipeline/SessionOperationalPipeline.cs
SessionOperational/Runtime/SessionOperationalRuntimeComposer.cs
```

## Mandatory anti-displacement answers

| Question | Answer |
|---|---|
| Qual pipeline é dono desta decisão? | `SessionActivityPipeline` owns SessionActivity route-exit teardown ordering/lifecycle. `SessionOperationalPipeline` owns route transition order and may request previous route handoff exit through a boundary. |
| Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data? | This is a boundary/ownership audit. The relevant shape is `ISessionActivityRouteExitTeardownBoundary` + `OperationalHandoffExitStage` + `SessionActivityPipeline`. |
| Isso é comportamento final ou bridge transitória? | Host delegation is final enough. The current raw-stage preflight surface on `ISessionActivityRouteExitTeardownBoundary` remains transitional. |
| Essa compatibilidade ainda é necessária? | Some boundary access is necessary; raw lifecycle state exposure is not necessarily necessary and should be reduced. |
| O erro está no sintoma ou na fronteira arquitetural errada? | Boundary leak: Operational preflight currently interprets SessionActivity stage/rail details. |
| Existe owner duplicado para o mesmo lifecycle? | No duplicate teardown executor. But preflight classification is partially outside the SessionActivity owner. |
| A extração remove owner duplicado ou apenas desloca responsabilidade? | The next extraction should move preflight classification into SessionActivity boundary/pipeline, not create a new owner. |
| O novo componente tem nome concreto e fronteira estável? | If implemented, prefer a concrete `SessionActivityRouteExitTeardownPreflightResult` contract; no generic manager/coordinator. |
| O novo componente pode ser descrito sem manager/coordinator/processor genérico? | Yes. It is a route-exit teardown preflight boundary method/result. |
| O smoke/log comprova comportamento, mas a matriz comprova ownership? | Smoke proves behavior. This audit proves remaining ownership gap is preflight classification, not Host lifecycle. |

## Findings

### Finding 1 — Host is now a thin route-exit delegator

`SessionActivityHost` implements `ISessionActivityRouteExitTeardownBoundary`, but the route-exit methods only call `EnsurePipeline()` and delegate to the pipeline:

```text
RequestRouteExitTeardown(...) -> _pipeline.RequestRouteExitTeardown(...)
AwaitRouteExitTeardownAsync(...) -> _pipeline.AwaitRouteExitTeardownAsync(...)
ResetSessionAfterRouteExit(...) -> _pipeline.ResetSessionAfterRouteExit(...)
```

`OnDisable` also delegates lifecycle validation to `SessionActivityPipeline.ValidateHostDisableOrFail(...)`.

**Severity:** Low  
**Decision:** Accept. No action in Host for route-exit ownership.

### Finding 2 — SessionActivityPipeline owns route-exit teardown order

`SessionActivityPipeline` owns:

```text
CloseForRouteExit(...)
EmitCloseForRouteExit(...)
ActivityExitOrderingPolicy.ForScenario(...)
_routeExitActorTeardownCompleted
ExecuteActivityExitActorTeardown(...)
FinalizeDeactivationForRouteExit(...)
CompleteRouteExitClosure(...)
CompletePendingRouteExitTeardownIfAny(...)
```

The pipeline decides the ordering between activity running, deactivation window, actor teardown, content release and `ClosedForRouteExit`.

**Severity:** Low  
**Decision:** Accept. This satisfies the core of C2.

### Finding 3 — Operational calls route-exit through the correct port chain

The active chain is:

```text
SessionOperationalPipeline
-> OperationalHandoffExitStage
-> IOperationalRouteHandoffExitPort
-> SessionActivityOperationalRouteHandoffExitAdapter
-> ISessionActivityRouteExitTeardownBoundary.AwaitRouteExitTeardownAsync(...)
-> SessionActivityHost
-> SessionActivityPipeline
```

Operational does not execute Activity teardown directly.

**Severity:** Low  
**Decision:** Accept.

### Finding 4 — Residual ownership leak: OperationalHandoffExitStage interprets SessionActivity lifecycle state

`OperationalHandoffExitStage.EvaluatePreflight(...)` reads:

```text
boundary.HasPendingOperation
boundary.CurrentStage
boundary.CurrentRailKind
```

and locally classifies:

```text
handoff_exit_pending_operation_active
handoff_exit_activation_not_completed
handoff_exit_target_transition_in_progress
```

with local helpers:

```text
IsActivationWindowStage(...)
IsDeactivationWindowStage(...)
```

This means Operational stage knows SessionActivity lifecycle stages and applies preflight policy that belongs closer to `SessionActivityPipeline` / `ISessionActivityRouteExitTeardownBoundary`.

**Severity:** Medium  
**Decision:** Action recommended. This is the remaining C2 ownership gap.

### Finding 5 — Boundary exposes raw state for preflight

`ISessionActivityRouteExitTeardownBoundary` exposes:

```text
CurrentRailKind
CurrentStage
HasPendingOperation
```

This was useful during transition, but it enables the Operational stage to interpret SessionActivity state. A stricter boundary would expose a preflight result instead of raw lifecycle state.

**Severity:** Medium  
**Decision:** Action recommended as part of the same grouped C2 implementation.

## Matrix

| File/class/method | Current responsibility | Correct owner | Problem | Severity | Action |
|---|---|---|---|---:|---|
| `SessionActivityHost.RequestRouteExitTeardown` | Delegate boundary call | Host as boundary | None | Low | Keep |
| `SessionActivityHost.AwaitRouteExitTeardownAsync` | Delegate boundary async call | Host as boundary | None | Low | Keep |
| `SessionActivityHost.ResetSessionAfterRouteExit` | Delegate reset after route-exit | Host as boundary | None | Low | Keep |
| `SessionActivityHost.OnDisable` | Delegates validation to pipeline | Pipeline owns decision | Not smoke-exercised, but ownership correct | Low | Keep |
| `SessionActivityPipeline.CloseForRouteExit` | Starts canonical route-exit closure | `SessionActivityPipeline` | None | Low | Keep |
| `SessionActivityPipeline.EmitCloseForRouteExit` | Decides route-exit ordering | `SessionActivityPipeline` + `ActivityExitOrderingPolicy` | Large, but canonical owner | Low | Keep |
| `SessionActivityPipeline.CompleteRouteExitClosure` | Marks `ClosedForRouteExit`, completes pending await | `SessionActivityPipeline` | None | Low | Keep |
| `OperationalHandoffExitStage.EvaluatePreflight` | Reads SessionActivity raw state and classifies route-exit readiness | `SessionActivityPipeline` / boundary preflight result | Ownership leak | Medium | Move classification into SessionActivity boundary/pipeline |
| `ISessionActivityRouteExitTeardownBoundary.CurrentStage/CurrentRailKind/HasPendingOperation` | Exposes raw lifecycle state to Operational | Boundary should expose result, not raw policy inputs | Enables leak | Medium | Replace/narrow after preflight contract exists |

## Recommended grouped implementation

```text
SA-19C2-A1 — Route-Exit Teardown Preflight Ownership Lock
```

### Scope

1. Add a typed SessionActivity route-exit teardown preflight contract/result.
2. Add a boundary method such as:

```text
EvaluateRouteExitTeardownPreflight(sessionStateId, source, reason)
```

3. Implement the classification inside `SessionActivityPipeline`.
4. Make `SessionActivityHost` delegate this method.
5. Update `OperationalHandoffExitStage.EvaluatePreflight(...)` to consume the result instead of reading raw `CurrentStage`, `CurrentRailKind`, `HasPendingOperation`.
6. Keep `AwaitRouteExitTeardownAsync(...)` and `RequestRouteExitTeardown(...)` unchanged.
7. Remove or mark raw state properties as transitional only if no longer used.

### Out of scope

```text
Do not change route-exit teardown execution.
Do not change ActivityExitActorTeardownStage.
Do not reopen SA-19B2.
Do not reopen SA-19B3.
Do not touch reset policy.
Do not touch Actor/Command/Projectile.
Do not touch movement/camera/content release.
Do not create manager/coordinator/processor.
```

## Decision

```text
SA-19C2-AUDIT — CLOSED / Completed / No runtime changes
SA-19C2 — NOT CLOSED YET
Next recommended — SA-19C2-A1 grouped implementation
```

## Acceptance for SA-19C2-A1

```text
Compile clean.
Smoke can be partial if RouteExitBackToMenu is available.
Required evidence:
- no error CS
- no FATAL
- no Exception
- no route_transition_failed
- no checkpointStatus='Failed'
- RouteExitBackToMenu Passed when executable
- OperationalHandoffExitStage no longer classifies SessionActivity stage/rail directly
- SessionActivityPipeline or boundary result is visible as owner of route-exit preflight classification
```
