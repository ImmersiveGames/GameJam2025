# SA-19B0 — ActivityEntry Runtime Bridge Surface Freeze (SA-19 Normalization Wave)

**Date:** 2026-06-14  
**Part of:** Phase 1 of `Docs/Plans/SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md`  
**Related to:** SA-7B0, SA-7B1, SA-13, and the 2026-06-14 Ownership Canonization Audit

## Objective

Explicitly freeze the current aggregated bridge surface as **transitional only**.

Produce a clear inventory and a target matrix so that subsequent cuts (especially SA-19B1 and the stage-by-stage narrowing in SA-19B2) have a defined "from → to" and do not accidentally grow the aggregate.

## Current State (as of 2026-06-14)

### The Aggregate
- Primary: `IActivityEntryRuntimeBridge` (defined in `Contracts/ActivityEntryPipelineContracts.cs`)
- It composes (at minimum):
  - `IActivityEntryIdentityRuntimeBridge`
  - `IActivityEntryFactRuntimeBridge`
  - `IActivityEntryContentPendingOperationRuntimeBridge`
  - `IActivityEntryPreparationRuntimeBridge`
- Additional specific bridges that stages also receive (some stages receive both the aggregate + specific ones):
  - `IActivityEntryActorPresentationRuntimeBridge`
  - `IActivityEntryActorParticipationRuntimeBridge`
  - `IActivityEntryPermissionTargetRuntimeBridge`
  - `IActivityEntryMovementBindingRuntimeBridge`
  - `IActivityEntryCameraBindingRuntimeBridge`

### The "Ponte Transitória" Marker
Location: `SessionActivity/Pipeline/ActivityEntryPipeline.cs` (around line 41-43):

```csharp
// Ponte transitória SA-7B0: mantida apenas para stages ainda não migrados
// para bridges menores. O ActivityEntryPipeline usa os campos de domínio abaixo
// nos pontos já normalizados deste corte.
private readonly IActivityEntryRuntimeBridge _runtimeBridge;
```

### The BindEntryPipeline Seam
- `SessionActivityPipeline` has `internal void BindEntryPipeline(ActivityEntryPipeline activityEntryPipeline)` (simple guard + assignment to private field).
- `SessionActivityCompositionInstaller.cs:92`:
  ```csharp
  _pipeline.BindEntryPipeline(activityEntryPipeline);
  ```
- The installer creates the macro pipeline first, then the entry pipeline by passing `_pipeline` (as the big bridge) **many times** + other internal "Entry*" properties, then binds it back.
- The macro pipeline exposes `public ActivityEntryPipeline EntryPipeline { get; }` (used internally for some delegation).

### Who Still Receives the Full Aggregate (`IActivityEntryRuntimeBridge`)
From current code:
- Many stages in `Pipeline/Stages/` still take `IActivityEntryRuntimeBridge endpoint` as primary parameter (examples: `ActivityEntryActorAttributeStage`, `ActivityEntryActorCommandBindingStage`, `ActivityEntryPlayerInputBindingStage`, `ActivityEntryParticipantResetStage`, `ActivityContentSceneUnload*`, `ActivityObject*` stages, `ActivityEntryObjectSetupStages`, `ActivityExitActorTeardownStage`, etc.).
- Some stages have already been partially migrated and receive specific sub-bridges (e.g. `ActivityEntryActorPresentationStage`, `ActivityEntryMovementBindingStage`, `ActivityGateBindingStage`, `ActivityEntryActorInventoryStage`).
- `ActivityEntryPipeline` itself still stores the aggregate and several specific sub-bridges.

### SessionActivityPipeline Implements
From class declaration:
```csharp
public sealed class SessionActivityPipeline : ...,
    IActivityEntryRuntimeBridge,
    IActivityEntryActorPresentationRuntimeBridge,
    IActivityEntryActorParticipationRuntimeBridge,
    IActivityEntryPermissionTargetRuntimeBridge,
    IActivityEntryMovementBindingRuntimeBridge,
    IActivityEntryCameraBindingRuntimeBridge,
    IActivityExitActorTeardownRuntimeBridge
```

Plus several other interfaces for handoff, pending ops, and snapshot provider.

## Target Direction (Frozen for SA-19)

The current aggregate `IActivityEntryRuntimeBridge` + the `BindEntryPipeline` seam are declared **transitional surfaces**.

**Goal of the SA-19 wave:**
- `ActivityEntryPipeline` and individual stages should depend on the smallest possible contracts they actually need.
- The macro `SessionActivityPipeline` should implement only the bridges that genuinely belong at the macro + handoff + route-exit level.
- Construction of `ActivityEntryPipeline` should not require passing the macro pipeline as a giant "everything" object, and the `Bind` indirection should disappear.

### Proposed Mapping (initial, to be refined in SA-19B2 batches)

| Current Heavy Usage                  | Recommended Narrow Target                          | Priority for SA-19B2 |
|--------------------------------------|----------------------------------------------------|----------------------|
| Full `IActivityEntryRuntimeBridge`   | Split per major phase (Content, Preparation, Identity/Fact, etc.) | High |
| `IActivityEntryRuntimeBridge` in object setup stages | `IActivityEntryFactRuntimeBridge` + inventory contracts + specific setup ports | High |
| Actor presentation / participation   | Keep or further narrow the specific `*Presentation*` / `*Participation*` bridges | Medium |
| Camera / Movement / Permission binding | Keep the specific binding bridges (already partially done) | Low (already better) |
| `BindEntryPipeline` seam             | Remove completely. EntryPipeline either created with narrow deps by the installer/composer or owned/created by the macro pipeline itself | Highest for SA-19B1 |

## Acceptance Criteria for This Freeze (SA-19B0)

- This document exists and is referenced from the main 2026-06-14 audit and the SA-19 plan.
- The "ponte transitória SA-7B0" comment and the `BindEntryPipeline` seam are now formally marked as transitional in docs.
- **Real removal update (same day)**: The method named `BindEntryPipeline` has been completely deleted from `SessionActivityPipeline.cs`. A transitional `AttachEntryPipeline` was introduced only as a temporary bridge during the SA-19B1 cut. This fulfills the "remoção real" of the old seam name and mechanism.

- **Batches progress (expanded)**:
  - Batches 1-3 (earlier): PlayerInputBinding, ActorAttribute, ActorCommandBinding.
  - Batches 4-7 (this pass):
    - `ActivityEntryParticipantResetStage` (Fact bridge).
    - `ActivityEntryActorPresentationStage`.
    - `ActivityEntryMovementBindingStage`.
    - `ActivityEntryCameraBindingStage`.
  - All now use narrow identity + fact bridges (plus their domain-specific bridges where already present).
  - Broad audit of remaining aggregate usages completed (see list in this document + main audit). Complex areas (ExitTeardown, Content unload/release, ObjectSetup composite, full ParticipantBinding, Gate) deferred to targeted sub-batches.
- A clear "current → target" direction is published so future cuts have a north star.
- No new usage of the full aggregate `IActivityEntryRuntimeBridge` is introduced after this date without explicit justification in an SA-19 audit note.

## Next

- SA-19B1: Obsolete and begin removal of the `BindEntryPipeline` seam.
- SA-19B2: Batches of stage migration away from the aggregate (start with lowest-risk stages).

All future work in this area must answer the anti-deslocamento questions before touching bridge signatures or the wiring in the CompositionInstaller.

---

**End of SA-19B0 freeze audit.**