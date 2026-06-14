# SA — SessionActivity Architecture, Duplication & Obsolete Hygiene Audit (Re-focused)

**Date:** 2026-06-14 (re-do)  
**Focus (as requested):** Architecture problems, duplications, lack of canonical ownership/paths, obsolete/legacy stages and pieces, folder organization, and code structure (not observability/facts/signaling).

**Scope:** Full SessionActivity module (Pipeline, Contracts, Capabilities, Authoring, Adapters, Simulation).

**References:** ADR-2.0-0002 (ownership decomposition, no god objects, no duplicate owners, no obsolete rails), ADR-2.0-0001 anti-deslocamento rules, previous SA-7B* / SA-19B* bridge work, SessionActivity/Pipeline/README.md.

---

## Executive Summary

Previous audits and work (SA-7B0/7B1, SA-19B0/B1/B2) correctly attacked the broad `IActivityEntryRuntimeBridge` + signaling/observability surface. However, deeper architectural hygiene issues remain largely unaddressed:

- **Obsolete rails** (PlayerActor vs general Actor) were supposed to be cleaned (history of SA-5A0 etc.) but "Player*" special cases, files, and resolvers are still dominant and scattered.
- **Duplication** is high at multiple levels (scanners, builders, identity resolution, stage patterns, "Player" vs general handling).
- **Lack of canonical** ownership and paths: manual wiring, coordinators, composite files, and broad interfaces still exist.
- **Folder / structure smells**: leftover dead files mixed with active ones, god-composite files, overly fragmented Capabilities tree.
- **Architecture problems**: large entry pipeline with "ponte transitória" comments still present, SessionActivityPipeline as god object for many bridges, unclear boundaries between discovery/setup/binding.

The module has made progress on granularity of stages, but the "canonical vs legacy" split is incomplete, leading to parallel paths, duplicated logic, and maintenance burden.

**Severity:** Medium-High (architectural debt that will slow future extensions and increase risk of ownership drift).

**Recommendation:** Treat this as a dedicated hygiene wave (new SA- cut series) focused on removal of obsolete rails, deduplication, and folder cleanup, **before** more feature work or further bridge splits.

---

## 1. Obsolete / Legacy Pieces (High Priority)

### 1.1 Dead/Superseded Stage Files Still Present
- `Pipeline/Stages/PlayerInputBindingStage.cs` — **DELETED** (SA-Arch-H1 executed). It was an empty file containing only the comment: "O caminho ativo de PlayerInput binding agora é ActivityEntryPlayerInputBindingStage." (and a legacy marker). Pure obsolete rail.
- `Pipeline/Stages/PlayerMovementControlStage.cs` — still present and actively used by the macro `SessionActivityPipeline` for player movement control logic (multiple Execute calls and event emission in movement control paths during activities). 
  - During the extra cleanup pass, added clarifying comment at the top of the file and near call sites in SessionActivityPipeline.cs.
  - It is **not** superseded for its current macro role. Removal/migration of this piece is tracked as part of H2 (PlayerActor rail collapse).

**Problem:** Violates "remove the old path when the new one is canonical" rule from ADRs. Creates confusion and potential for drift.

**Recommendation:** Delete the Player* stage files + update any remaining call sites to use the Entry* equivalents or remove the legacy paths entirely. Add to REMOVED_FILES.txt.

### 1.2 Persistent "PlayerActor" Parallel Rail
Despite previous efforts to converge on pure Actor model:
- Dozens of types still carry `PlayerActorId`, `PlayerActorCapabilityIdentity`, `PlayerActorIdentityRecord`, `PlayerActorCapabilityIdentityResolver`.
- Special cases throughout: `PlayerMovementPermissionReceiver`, `PlayerInputBindingState`, `PlayerActorMovementBindingState`, etc.
- In Capabilities/Inventory, Permissions, Camera, Presentation — many scanners and resolvers have explicit `PlayerActor` branches (`ownerKind == ActivityCapabilityOwnerKind.PlayerActor`).
- Authoring and reset paths still have "PruneLegacyEmptyObjectEntryRequirements" and special player handling.

**Evidence:** Massive grep hits on "PlayerActor" (hundreds of lines). `PlayerActorCapabilityIdentityResolver.cs` is a dedicated class whose only job is to handle the old rail.

**Problem:** This is exactly the "rails paralelos permanentes" that ADR-2.0-0002 and SA-5A0 explicitly forbade. It creates duplicated discovery/setup/binding/reset logic for "player" vs "general actor".

**Recommendation:**
- Finish the convergence: make PlayerActor a special case only at the very edges (participation binding + input).
- Remove dedicated `PlayerActorCapability*` types where possible; use general Actor + participation context.
- One dedicated audit/cut (SA-Architecture-PlayerRail-Removal) to delete or collapse the parallel types.

### 1.3 "Ponte Transitória" Comments and Partial Migrations Still in Production Code
- `ActivityEntryPipeline.cs:41` still contains: `// Ponte transitória SA-7B0: mantida apenas para stages ainda não migrados`
- Broad `IActivityEntryRuntimeBridge _runtimeBridge` field and parameter still present in the ctor and many places.
- Many stages in Stages/ still declare `IActivityEntryRuntimeBridge endpoint` (even after previous B2 work).

**Problem:** "Transitional" shapes from 2026-06 are still the active shape months later. This is the exact anti-pattern the anti-deslocamento rules were written to prevent.

---

## 2. Duplications

### 2.1 Capability Scanners (High Duplication)
Capabilities/Inventory/ has one scanner per concern:
- ActivityCapabilityActorAttributeScanner
- ActivityCapabilityActorLifecycleScanner
- ActivityCapabilityActorPresentationScanner
- ActivityCapabilityCameraTargetScanner
- ActivityCapabilityPermissionScanner
- ActivityCapabilityTransformPathUtility (and ObjectCapabilityScanner)

Each scanner has very similar structure (TryScan, contribution building, player vs general handling).

**Folder smell:** Extremely deep tree under Capabilities/Inventory/ with many tiny files.

**Recommendation:** Extract common scanner infrastructure (base class or policy-driven scanner) + collapse per-capability files where the logic is mostly boilerplate.

### 2.2 "BuildIdentity" / EmitFact / EmitSnapshot Boilerplate
Almost every static *Stage class repeats:
```csharp
var startedIdentity = BuildIdentity(...);
factBridge.EmitFact(...);
factBridge.EmitSnapshot(...);
identityBridge.SetCurrentIdentity(...);
```

This is duplicated across 30+ files.

### 2.3 Player vs General Identity Resolution
`ResolvePlayerActorCapabilityTargetsForCurrentEntry`, `TryResolvePlayerActorHandleForCapabilityInventory`, `AddPlayerActorCapabilityTargetsFromParticipationContext`, etc. — duplicated patterns in Inventory, Permissions, reset paths, etc.

### 2.4 Contribution Builders
Separate `*ContributionBuilder.cs` files for almost every capability (Attributes, Camera, Presentation, etc.). Very similar code.

### 2.5 Composite File Smell
`ActivityEntryObjectSetupStages.cs` is one file containing multiple internal static classes (ObjectContributorDiscovery, SetupInventory, ObjectReset, SnapshotRestore, etc.). This is both duplication of stage pattern and poor organization.

---

## 3. Lack of Canonical Ownership & Paths

### 3.1 God-Object Tendencies Remain
- `SessionActivityPipeline.cs` still implements a long list of `I*RuntimeBridge` interfaces (entry identity, fact, content pending, preparation, actor presentation/participation, permission, movement, camera, teardown, etc.).
- `ActivityEntryPipeline.cs` has a very large constructor (~80+ lines of fields + assignments) and still holds the broad `_runtimeBridge`.

This contradicts the "no god object" and "narrow contracts" goals of ADR-2.0-0002.

### 3.2 Coordinators and Manual Wiring as Canonical
- `ActivityCapabilityInventoryCoordinator.cs` exists and is used in the hot path.
- `ActivitySetupInventoryBuilder.cs`
- CompositionInstaller does heavy manual `new` + property extraction + `Bind` style wiring.

These are exactly the "manager/coordinator/processor genérico" and "manual service locator in installer" anti-patterns the ADRs warn against.

### 3.3 Unclear Boundaries Between Discovery / Setup / Binding / Reset
- Object contributor discovery, capability inventory preview, actor inventory feed, participant binding, and reset are spread across many stages and the two pipelines with overlapping responsibilities.
- No single canonical "entry setup plan" owner.

### 3.4 Folder Organization Problems
- `Stages/` mixes canonical `ActivityEntry*` with leftover `Player*` and old names (`ActorPresentationSetupStage.cs` vs `ActivityEntryActorPresentationStage.cs`).
- `Capabilities/` tree is over-fragmented (one folder + scanner + contribution per tiny concern).
- `Contracts/` has many tiny *Contracts.cs files — this is good for separation but makes navigation hard; some feel like they could be grouped.
- `Runtime/` contains state + some lookup helpers mixed.
- No clear "Legacy/" or "Obsolete/" folder or clear naming convention for transitional code.

### 3.5 Missing Canonical "Narrow Bridge" Story
Even after all the SA-19B work, the production code still has many places passing the broad bridge. The "target narrow contracts" matrix from SA-19B0 exists in an audit note but is not yet the enforced reality in the code.

---

## 4. Recommendations (Prioritized)

1. **Immediate Hygiene Cut (new SA- cut):**
   - Delete obsolete `PlayerInputBindingStage.cs` and `PlayerMovementControlStage.cs`.
   - Remove or collapse "PlayerActor" parallel rail types (keep only at participation + input boundaries).
   - Delete "PruneLegacy..." methods and related authoring hacks.

2. **Deduplication Wave:**
   - Introduce scanner infrastructure so we stop writing one scanner class per capability.
   - Centralize BuildIdentity + EmitFact/EmitSnapshot helpers (or make them part of the narrow fact/identity bridges).
   - Collapse or document why we have separate *ContributionBuilder for every capability.

3. **Folder & Structure Cleanup:**
   - Move or delete leftover Player* files.
   - Split or clearly document `ActivityEntryObjectSetupStages.cs`.
   - Consider grouping some of the tiny Contracts/ files or adding a README per major contract area.
   - Enforce that any new transitional code must live in a clearly named "Transitional" or "Legacy" subfolder with removal date.

4. **Architecture Enforcement:**
   - Finish the narrow-bridge migration so that **no new stage** is allowed to take the broad `IActivityEntryRuntimeBridge`.
   - Reduce the number of interfaces that `SessionActivityPipeline` and `ActivityEntryPipeline` must implement.
   - Replace `ActivityCapabilityInventoryCoordinator` with explicit policy + stage ownership (per ADR rules).

5. **Governance:**
   - Add a standing rule: before any new stage or capability, the author must answer "is this creating a new PlayerActor special case or duplicating scanner/builder logic?"

---

## 5. Comparison to Previous Work

Previous SA-7B* / SA-19B* work correctly reduced signaling/observability coupling. This audit shows that the **structural** debt (obsolete rails, duplication of discovery/setup logic, folder entropy, and god-object wiring) was not the primary focus and remains largely intact.

The two efforts are complementary — we need both the narrow runtime contracts **and** the elimination of the old PlayerActor parallel world + duplicated scanner/builder machinery.

---

**End of re-focused audit.**

This document should be used as input for the next planning session and a new hygiene-focused SA- cut (distinct from pure bridge-narrowing work). 

Recommend creating a short "SA-Architecture-Hygiene-2026-06" plan with concrete deletion + dedup + folder tasks, each with compile + smoke acceptance.