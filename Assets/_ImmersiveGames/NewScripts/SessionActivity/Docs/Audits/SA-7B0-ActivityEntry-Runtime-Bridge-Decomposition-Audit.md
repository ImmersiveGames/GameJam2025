# SA-7B0 — ActivityEntry Runtime Bridge Decomposition Audit

## Status

Implemented as a boundary decomposition cut. No lifecycle behavior change.

## Goal

Reduce the generic `IActivityEntryRuntimeBridge` surface used by `ActivityEntryPipeline` by splitting its responsibilities into smaller runtime bridge contracts.

## Owner decision

- `SessionActivityPipeline` remains macro lifecycle owner.
- `ActivityEntryPipeline` remains setup/readiness owner.
- Runtime state/fact/content operations are bridge primitives, not lifecycle ownership.

## Contract split introduced

`IActivityEntryRuntimeBridge` is now composed from smaller contracts:

- `IActivityEntryIdentityRuntimeBridge`
- `IActivityEntryFactRuntimeBridge`
- `IActivityEntryContentRuntimeBridge`
- `IActivityEntryLogRuntimeBridge`
- `IActivityEntryPreparationRuntimeBridge`

The old aggregate interface remains only as a transitional composition type.

## ActivityEntryPipeline change

`ActivityEntryPipeline` now stores and uses the smaller bridge fields directly for normalized code paths:

- `_identityBridge`
- `_factBridge`
- `_contentBridge`
- `_logBridge`
- `_preparationBridge`

A transitional `_runtimeBridge` remains only for stages that still accept the aggregate bridge.

## Remaining bridge debt

The following stage families still receive the aggregate `IActivityEntryRuntimeBridge` and should be decomposed in later cuts:

- `ActivityEntryObjectSetupStages`
- `ActivityEntryActorInventoryStage`
- `ActivityEntryParticipantBindingStage`
- `ActivityEntryActorPresentationStage`
- `ActivityEntryActorAttributeStage`
- `ActivityEntryActorParticipationStage`
- `ActivityEntryPlayerInputBindingStage`
- `ActivityEntryPermissionTargetPreparationStage`
- `ActivityEntryMovementBindingStage`
- `ActivityEntryCameraBindingStage`
- exit/release/snapshot stages that still share the same bridge contract

## Accepted residue

`SessionActivityPipeline` still implements the aggregate `IActivityEntryRuntimeBridge` because the stage migration is not finished in this cut. This is not final architecture.

## Next recommended cut

`SA-7B1 — ActivityEntry stage bridge parameter split`

Target the stages one group at a time, starting with content/object setup or participant binding, and replace aggregate bridge parameters with the minimum domain bridge interfaces.

## Non-goals

- No ActivationWindow changes.
- No DeactivationWindow changes.
- No Restart changes.
- No RouteExit changes.
- No new pipeline.
- No compat alias or silent fallback.
