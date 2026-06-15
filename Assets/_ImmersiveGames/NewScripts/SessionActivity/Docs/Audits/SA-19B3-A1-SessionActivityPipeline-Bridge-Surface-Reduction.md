# SA-19B3-A1 — SessionActivityPipeline Bridge Surface Reduction Audit + First Batch

**Status:** Applied / Pending compile + smoke  
**Scope:** grouped bridge implementation reduction on `SessionActivityPipeline`.

## Objective

Continue the original SessionActivity canonization plan without returning to endless teardown decomposition.
The previous teardown work was treated only as an unblocker for SA-19B3.

This batch reduces the macro pipeline interface surface by moving entry/detail bridge implementations out of the `SessionActivityPipeline` type declaration and into narrow concrete adapters owned by the composition boundary.

## Anti-displacement answers

| Question | Answer |
|---|---|
| Qual pipeline é dono desta decisão? | `SessionActivityPipeline` remains the macro order/lifecycle owner. `ActivityEntryPipeline` remains entry owner. |
| Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data? | Adapter/boundary surface reduction. No policy or lifecycle moved. |
| Isso é comportamento final ou bridge transitória? | Transitional bridge surface reduction. It does not claim final removal of every entry bridge. |
| Essa compatibilidade ainda é necessária? | The entry bridge contracts still exist for stages that consume them, but the macro pipeline no longer implements them directly. |
| O erro está no sintoma ou na fronteira arquitetural errada? | The boundary was wrong: macro pipeline implemented too many entry/detail runtime bridge contracts. |
| Existe owner duplicado para o mesmo lifecycle? | No new lifecycle owner is introduced. |
| A extração remove owner duplicado ou apenas desloca responsabilidade? | It removes bridge implementation burden from the macro pipeline class declaration; adapters remain technical. |
| O novo componente tem nome concreto e fronteira estável? | Yes, concrete adapter classes per bridge concern. |
| O novo componente pode ser descrito sem manager/coordinator/processor genérico? | Yes. No manager/coordinator/processor introduced. |
| O smoke/log comprova comportamento, mas a matriz comprova ownership? | Pending compile + smoke. Ownership is documented here. |

## Changes

- `SessionActivityPipeline` class declaration reduced to macro-facing interfaces only:
  - `ISessionActivityEntryHandoffReceiver`
  - `ISessionActivityPendingOperationCallback`
  - `ISessionActivitySnapshotPayloadProvider`
- Removed direct implementation from `SessionActivityPipeline` class declaration of:
  - `IActivityEntryRuntimeBridge`
  - `IActivityEntryActorPresentationRuntimeBridge`
  - `IActivityEntryActorParticipationRuntimeBridge`
  - `IActivityEntryPermissionTargetRuntimeBridge`
  - `IActivityEntryMovementBindingRuntimeBridge`
  - `IActivityEntryCameraBindingRuntimeBridge`
  - `IActivityExitActorTeardownRuntimeBridge`
- Added narrow private concrete adapter classes inside `SessionActivityPipeline`:
  - `ActivityEntryRuntimeBridgeAdapter`
  - `ActivityEntryActorPresentationRuntimeBridgeAdapter`
  - `ActivityEntryActorParticipationRuntimeBridgeAdapter`
  - `ActivityEntryPermissionTargetRuntimeBridgeAdapter`
  - `ActivityEntryMovementBindingRuntimeBridgeAdapter`
  - `ActivityEntryCameraBindingRuntimeBridgeAdapter`
  - `ActivityExitActorTeardownRuntimeBridgeAdapter`
- `SessionActivityCompositionInstaller` now passes concrete adapter properties to `ActivityEntryPipeline` instead of passing `_pipeline` as every bridge.
- Internal macro call sites now pass `_entryRuntimeBridge` and `_activityExitActorTeardownRuntimeBridge` where stages require bridge contracts.

## What did not change

- No teardown decomposition resumed.
- No ObjectSetup changes.
- No retained lookup changes.
- No Host changes.
- No lifecycle moved to runtime state, registry, or store.
- No new manager/coordinator/processor.
- No event names intentionally changed.

## Acceptance required

Canonical smoke:

```text
RestartCurrentActivity
Activity01ToActivity02
RouteExitBackToMenu
```

Minimum criteria:

```text
sem error CS
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
ActivityParticipationExitStarted
ActivityParticipationExited
ActivityParticipationExitCompleted
ActorLifetimeDecisionResolved
ActivityRetainedParticipantLookupResolved
ActivityParticipantActorMaterializationRetained
ActivityEntryParticipantBindingCompleted
ActivityEntryParticipantResetCompleted
ActivityParticipantResetAppliedFromInventory
ActivityContentReleaseCompleted
ActorPresentationReleased
ActorAttributeReleased
sem RejectedForeign indevido
sem RejectedStale indevido
```
