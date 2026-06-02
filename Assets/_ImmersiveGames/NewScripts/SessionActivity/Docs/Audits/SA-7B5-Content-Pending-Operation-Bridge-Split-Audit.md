# SA-7B5 — Content pending operation bridge split

## Status

Implemented as a small ownership/contract split.

## Goal

Reduce `IActivityEntryContentRuntimeBridge` by separating two different responsibilities that were grouped in the same bridge:

1. current `ActivityContentLoadedSet` state store;
2. pending operation creation/registration/dispatch for additive content scene load.

This cut does not change activity lifecycle, route-exit ordering, activation/deactivation windows, movement, camera, actor setup, actor participation, object setup or content load behavior.

## Architectural answers

### Which pipeline owns this decision?

`ActivityEntryPipeline` owns activity entry content-load orchestration.

`SessionActivityPipeline` remains the temporary host of inherited runtime state and pending operation runner callbacks until those can be extracted into concrete stores/adapters.

### Is this stage, policy, command, fact, adapter, endpoint, snapshot or authoring data?

- `ActivityEntryContentLoadedSetRuntimeBridge`: transitional runtime-state store bridge.
- `ActivityEntryContentPendingOperationRuntimeBridge`: transitional pending operation dispatch bridge.
- `ActivityContentSceneLoadCommand`: command.
- `ActivityContentLoadedSet`: runtime-state/fact payload.
- `ActivityContentReleaseRuntimeState`: runtime-state owner for loaded-set retention/release information.

### Is this final behavior or transitional bridge?

Transitional bridge reduction. It is not a final endpoint/adapter shape.

The final shape should move pending operation dispatch to a content load dispatch stage/adapter and keep loaded set persistence inside the appropriate runtime state/store.

### Is compatibility still required?

No product compatibility is required.

The aggregate `IActivityEntryContentRuntimeBridge` remains only as temporary internal composition while `SessionActivityPipeline` still hosts legacy runtime state and runner wiring.

### Is the error in the symptom or in the architectural boundary?

Boundary issue. The previous bridge mixed state-store ownership with pending operation dispatch ownership.

### Is there duplicated owner for the same lifecycle?

Partially reduced. `ActivityEntryPipeline` remains the owner of content-load order; `SessionActivityPipeline` still hosts the technical runner and state mutation as bridge implementation.

## Changes

### Contracts

Created:

```csharp
IActivityEntryContentLoadedSetRuntimeBridge
IActivityEntryContentPendingOperationRuntimeBridge
```

Kept as temporary aggregate:

```csharp
IActivityEntryContentRuntimeBridge :
    IActivityEntryContentLoadedSetRuntimeBridge,
    IActivityEntryContentPendingOperationRuntimeBridge
```

### ActivityEntryPipeline

Replaced the single `_contentBridge` field with:

```csharp
_contentLoadedSetBridge
_contentPendingOperationBridge
```

This makes the orchestration code explicit:

- loaded-set clear/store goes through `_contentLoadedSetBridge`;
- pending operation build/set/run goes through `_contentPendingOperationBridge`.

### SessionActivityPipeline

Explicit implementations were moved from the aggregate interface to the smaller interfaces:

```csharp
IActivityEntryContentLoadedSetRuntimeBridge.SetCurrentActivityContentLoadedSet
IActivityEntryContentLoadedSetRuntimeBridge.ClearCurrentActivityContentLoadedSet
IActivityEntryContentPendingOperationRuntimeBridge.BuildActivityContentPendingOperation
IActivityEntryContentPendingOperationRuntimeBridge.SetPendingOperation
IActivityEntryContentPendingOperationRuntimeBridge.RunActivityContentOperation
```

Loaded-set runtime-state logs now use:

```text
source='ActivityEntryContentLoadedSetStore'
```

instead of:

```text
source='ActivityEntryContentRuntimeBridge'
```

## Expected smoke evidence

Required:

```text
sem erro CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido

ActivityEntryPipelineStarted
contentPendingOperationSplit='loaded_set_store_pending_operation_dispatch'

source='ActivityEntryContentRuntimeBridge' deve ser 0
source='ActivityEntryContentLoadedSetStore'

ActivityEntryContentLoadStarted
ActivityEntryContentLoadCompleted
ActivityEntrySetupReadinessStarted
ActivityEntrySetupReadinessCompleted
ActivityParticipantCommandPlanReady
ActivityParticipantBindApplied
ActivityParticipantPlacementApplied
ActivityParticipantResetApplied

RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

## Residual debt

`IActivityEntryContentRuntimeBridge` still exists as an aggregate bridge.

Next recommended cut:

```text
SA-7B6 — ActivityContent pending operation dispatch stage/adapter extraction
```

Recommended scope:

1. create a dedicated content load pending operation dispatcher/stage adapter;
2. remove pending operation runner calls from the `SessionActivityPipeline` bridge implementation;
3. keep `SessionActivityPipeline` as macro lifecycle owner only;
4. do not touch ActivationWindow, DeactivationWindow, Restart, RouteExit, Movement or Camera.
