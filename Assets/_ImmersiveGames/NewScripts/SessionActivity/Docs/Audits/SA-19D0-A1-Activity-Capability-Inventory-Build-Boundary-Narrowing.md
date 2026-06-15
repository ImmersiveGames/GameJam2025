# SA-19D0-A1 — Activity Capability Inventory Build Boundary Narrowing

Status: Applied / Pending compile + smoke
Date: 2026-06-15

## Decision

`ActivityCapabilityInventoryCoordinator` was removed from the active runtime path as a coordinator-shaped component.

The retained behavior is now named as a deterministic entry build boundary:

- `ActivityEntryCapabilityInventoryBuildStage`

This component builds the capability inventory for the current entry and emits build outputs that are later consumed by entry stages. It is not a lifecycle owner.

## Ownership

| Concern | Owner |
|---|---|
| Entry order | `ActivityEntryPipeline` |
| Capability inventory build | `ActivityEntryCapabilityInventoryBuildStage` |
| Inventory preview fact/snapshot | `ActivityEntryCapabilityInventoryPreviewStage` |
| Inventory state storage | `ActivityEntryInventoryRuntimeState` |
| Reset participation | `ActivityEntryParticipantResetStage` |
| Object reset/release/snapshot | Dedicated object stages |
| Presentation/attribute/camera/permission consumption | Dedicated entry binding/setup stages |

## Anti-displacement notes

- No lifecycle was moved to the build stage.
- No scanner became a lifecycle owner.
- No registry became a source of truth.
- `PendingOperationRunner` was not changed because the audit classified it as infrastructure with low gain for this phase.
- Composition wiring was not changed for aesthetics.

## Expected smoke

Minimum:

- clean compile;
- `RestartCurrentActivity`;
- `Activity01ToActivity02`;
- `RouteExitBackToMenu`.

Expected markers:

- no `error CS`;
- no `FATAL`;
- no `Exception`;
- no `route_transition_failed`;
- no `checkpointStatus='Failed'`;
- `ActivityEntryCapabilityInventoryPreviewObserved` still appears;
- reset, retained participant, content release and route exit checkpoints remain green.

## Closure rule

Close this cut if compile is clean and the canonical smoke remains equivalent to the previous `SA-19C2` baseline.
