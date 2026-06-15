# ACTOR-POOL-LIFETIME-1A — Mandatory Spawned Release By Owner

Status: Applied / Pending compile + smoke  
Date: 2026-06-15

## Objective

Guarantee that runtime-spawned pooled objects tracked by an Actor owner are returned to their origin pool when the owner is released.

This cut does not introduce owner-scoped pools and does not dematerialize pool hosts. It only closes the obvious invariant:

```text
A spawned runtime object must not survive the release of its owner.
```

## Ownership

| Area | Owner |
|---|---|
| Lifecycle trigger | `SessionActivityPipeline` / existing actor lifetime stages |
| Actor release contribution execution | `ActorReleaseContributionStage` |
| Spawned object tracking and pool return | `ActorProjectileSpawnRuntimeTracker` |
| Technical pool operation | `IPoolService.Return(...)` |

## Changes

- `ActorProjectileSpawnRuntimeTracker` now also provides an actor release contribution.
- `ActorReleaseContributionStage` executes actor release contributions before owner destruction/lifetime release is completed.
- `ActivityExitActorTeardownStage` calls release contributions for actors whose lifetime decision is `Release`.
- `SessionActivityActorRuntimeReleaseStage` calls release contributions before destroying session/route indexed actors.

## Non-goals

- No pool ownership model.
- No `GlobalShared` vs `OwnerScoped` authoring yet.
- No pool host dematerialization.
- No SessionActivity refactor beyond invoking the release contribution at existing release boundaries.
- No new manager/coordinator/processor.

## Expected smoke evidence

```text
ActorProjectileSpawnedRuntimeObjectsReleased
trigger='owner_release'
releaseMandatory='true'
returnedCount > 0 when spawned objects exist
trackedCountAfter='0'
ActorReleaseContributionsCompleted
RouteExitBackToMenu Passed
```

Fallback clean path with no active spawned objects is acceptable:

```text
ActorProjectileSpawnedRuntimeObjectsReleaseSkipped reason='no_tracked_runtime_objects'
```

## Acceptance

- No `FATAL`.
- No `Exception`.
- No `route_transition_failed`.
- No `checkpointStatus='Failed'`.
- Existing reset behavior remains intact.
- Runtime-spawned objects are returned before owner destruction when tracked objects exist.
