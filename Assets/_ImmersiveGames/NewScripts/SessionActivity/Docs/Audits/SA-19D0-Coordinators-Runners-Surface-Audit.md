# SA-19D0 — Coordinators/Runners Surface Audit

Status: CLOSED / Completed / No runtime changes  
Date: 2026-06-15  
Scope: Phase 3 — Remove or Narrow Generic Coordinators & Runners

## Question answered

Does Phase 3 provide architectural gain or is it only cleanup?

Answer: mixed.

- `ActivityCapabilityInventoryCoordinator` still provides real architectural gain if narrowed or removed, because it remains in the active entry setup path and hides scanner composition/build orchestration behind a generic coordinator name.
- `UnitySessionActivityPendingOperationRunner` is mostly acceptable as a technical async adapter/runner. It should be documented and maybe renamed only if later evidence shows it owns lifecycle/policy. Current audit does not justify a runtime refactor.

## Files inspected

- `SessionActivity/Capabilities/Inventory/ActivityCapabilityInventoryCoordinator.cs`
- `SessionActivity/Pipeline/ActivityEntryPipeline.cs`
- `SessionActivity/Pipeline/Stages/ActivityEntryObjectSetupStages.cs`
- `SessionActivity/Adapters/UnitySessionActivityPendingOperationRunner.cs`
- `SessionActivity/Contracts/SessionActivityContracts.cs`
- `SessionActivity/Pipeline/SessionActivityPipeline.cs`
- `SessionActivity/Pipeline/Stages/ActivityContentSceneUnloadDispatchStage.cs`
- `SessionActivity/Pipeline/SessionActivityCompositionInstaller.cs`

## Phase 3 plan alignment

The plan defines two checks:

1. `SA-19D0 — Inventory Coordinator`
   - Audit current usage of `ActivityCapabilityInventoryCoordinator`.
   - Choose one canonical shape:
     - A. pure policy object;
     - B. eliminate it and push into explicit stages/runtime state;
     - C. keep as narrow technical helper inside `ActivityEntryPipeline` with explicit justification.

2. `SA-19D1 — Pending Operation Runner`
   - Confirm runner is purely technical dispatch.
   - Confirm it does not decide when operations run or next phase.
   - Move state only if it owns state that belongs to runtime state.

## Findings

### 1. ActivityCapabilityInventoryCoordinator

Current responsibilities:

- Owns object target adaptation through `ActivityObjectCapabilityScanTargetAdapter`.
- Owns concrete scanner registry assembly/order:
  - `ActivityObjectCapabilityScanner`
  - `ActivityCapabilityActorLifecycleScanner`
  - `ActivityCapabilityPermissionScanner`
  - `ActivityCapabilityActorPresentationScanner`
  - `ActivityCapabilityActorAttributeScanner`
  - `ActivityCapabilityCameraTargetScanner`
- Owns `ActivityCapabilityInventoryBuilder` construction.
- Produces `ActivityCapabilityInventoryBuildResult`.
- Also returns side-channel setup/binding contributions:
  - camera binding contributions;
  - attribute setup contributions;
  - presentation setup contributions;
  - permission receiver contributions;
- Computes lifecycle capability summaries for object and actor capabilities.

Current usage:

- Instantiated directly inside `ActivityEntryPipeline`.
- Passed into `ActivityEntryCapabilityInventoryPreviewStage.Execute(...)`.
- Active on every entry setup with discovery or actor targets.

Ownership assessment:

- It is not a lifecycle owner.
- It is not a side-effect adapter.
- It is not pure policy.
- It is not only a passive helper, because it bundles registry composition, target adaptation, inventory build, side-channel contributions, and summaries.

Architectural issue:

The name `Coordinator` hides a real stage-like operation. It also lets `ActivityCapabilityInventory` remain an aggregator for concerns that should be explicit setup/binding feed surfaces. The current shape works functionally, but it weakens the Base 2.0 rule that inventory is passive and stages own deterministic steps.

Severity: Medium.

Recommendation:

Proceed with one grouped implementation, but not a broad cleanup wave:

`SA-19D0-A1 — Activity Capability Inventory Build Boundary Narrowing`

Preferred shape:

- Replace `ActivityCapabilityInventoryCoordinator` with a concrete entry-stage/builder boundary, not a generic coordinator.
- Canonical name options:
  - `ActivityEntryCapabilityInventoryBuildStage`
  - `ActivityCapabilityInventoryBuildStage`
- Keep it stateless.
- Keep scanner order explicit.
- Keep `ActivityEntryPipeline` as order owner.
- Keep `ActivityEntryCapabilityInventoryPreviewStage` as preview/log/runtime-state write owner, or merge build call into that stage if this reduces indirection.
- Do not create a manager/coordinator/processor replacement.
- Do not move setup/binding execution into the inventory builder.

Canonical justification if kept narrowly:

`ActivityCapabilityInventoryBuildStage is a deterministic entry stage that assembles capability scan context and builds a passive inventory snapshot plus setup/binding contribution feeds for later explicit stages; it does not own lifecycle or side-effects.`

### 2. UnitySessionActivityPendingOperationRunner

Current responsibilities:

- Receives already-built `SessionActivityPendingOperation`.
- Dispatches async Unity scene operations through adapters:
  - window load/unload;
  - activity content load;
  - activity content release.
- Calls `ISessionActivityPendingOperationCallback` on completion/failure.

Current callers:

- `SessionActivityPipeline` runs activation/deactivation window operations.
- `ActivityEntryPipeline` runs activity content load.
- `ActivityContentSceneUnloadDispatchStage` runs activity content release.

Ownership assessment:

- The runner does not decide when operations run.
- The runner does not choose next phase.
- The runner does not retain loaded-set state.
- The runner does translate adapter results into callback calls, which is acceptable for an async dispatch adapter.

Architectural issue:

The name `Runner` is generic and suspicious, but current behavior is consistent with infrastructure dispatch. Refactoring it now would be mostly cleanup unless future audit finds hidden state or policy.

Severity: Low.

Recommendation:

Do not implement runtime change for `D1` now. Document it as accepted technical adapter/runner and move on.

Canonical justification:

`UnitySessionActivityPendingOperationRunner is an infrastructure async dispatch adapter for Unity scene operations. It receives commands already classified by SessionActivityPipeline/ActivityEntryPipeline/stages and only reports completion or failure through the callback boundary.`

### 3. CompositionInstaller wiring

Current state:

- `SessionActivityCompositionInstaller` still manually wires multiple concrete adapters and passes many narrow surfaces into `ActivityEntryPipeline`.

Assessment:

- This is verbose but not currently the highest-value issue.
- It is composition root behavior, not lifecycle ownership.
- Avoid creating a service locator or aggregate bridge to reduce constructor length.

Severity: Low/Medium.

Recommendation:

Defer. Only revisit if constructor/wiring starts hiding ownership or blocking a concrete phase.

## Matrix

| Item | Current responsibility | Correct owner | Problem | Severity | Recommended action | Gain |
|---|---|---|---|---:|---|---|
| `ActivityCapabilityInventoryCoordinator` | Registry assembly, scan context adaptation, inventory build, contribution side channels, summary computation | Entry build stage + preview stage/runtime state | Generic coordinator hides deterministic entry-stage work | Medium | Replace/narrow in `SA-19D0-A1` | Real architectural gain |
| `ActivityCapabilityInventoryBuildResult` | Carries passive inventory plus setup/binding contribution feeds | Build result accepted if owner is explicit | Not wrong by itself, but should not imply inventory owns setup/binding | Medium | Keep, but document as build output/feed, not inventory ownership | Moderate |
| `ActivityEntryCapabilityInventoryPreviewStage` | Runs preview build and writes inventory/contribution runtime state | ActivityEntryPipeline stage | Owner is acceptable; depends on generic coordinator | Medium | Consume concrete build boundary | Real but bounded |
| `UnitySessionActivityPendingOperationRunner` | Async dispatch to Unity scene adapters and callback | Infrastructure adapter/runner | Name is generic, behavior acceptable | Low | Document/accept, no runtime change | Mostly cleanup |
| `ISessionActivityPendingOperationRunner` | Runner port for pending operation dispatch | Port is acceptable | Broad but not currently leaking policy | Low | Keep for now | Mostly cleanup |
| `SessionActivityCompositionInstaller` | Manual composition | Composition root | Verbose, but not lifecycle owner | Low/Medium | Defer | Low |

## Decision

Phase 3 should not become a long cleanup phase.

Proceed with only one implementation if desired:

`SA-19D0-A1 — Activity Capability Inventory Build Boundary Narrowing`

Then close Phase 3 unless the implementation reveals a hard boundary issue.

Do not open a separate runtime refactor for `PendingOperationRunner` now.

## Acceptance for D0-A1

- No `Coordinator` named owner remains in the active entry setup path.
- New component is stage/build boundary with concrete name.
- `ActivityEntryPipeline` remains order owner.
- Inventory remains passive snapshot/index.
- Setup/binding contributions are explicitly named as build outputs/feed, not inventory-owned behavior.
- No new manager/coordinator/processor.
- No lifecycle moved into builder/helper.
- Smoke required before PASS.
