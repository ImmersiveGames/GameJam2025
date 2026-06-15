# SessionActivity — Base 2.0 Ownership & Canonization Audit (Parallel to SessionOperational Cleanup)

**Date:** 2026-06-14  
**Snapshot:** Post SessionOperational contract hygiene + remnant cleanup (7 deprecated contract stubs removed from Pipeline/, empty Debug/ purged, redundant guide in Docs/Guides/ removed and recorded).  
**Authoring lens:** Same as the Operational audit — ADR-2.0-0001 (anti-deslocamento, categories Pipeline/Stage/Boundary/Policy/Command/Fact/Recorder/Adapter), ADR-2.0-0002 (SessionActivity decomposition rules), Pipeline README honesty, existing SA-* audit series.  
**Goal:** Surface the same classes of problems we found and fixed in SessionOperational (duplicated contracts in wrong folders, transitória bridges, remnants, things outside canonical model but still actively used) and produce a clear evaluation document with findings, severity, and canonical-track recommendations.

---

## Executive Summary

SessionActivity is larger and more complex than SessionOperational because it owns the full deterministic entry lifecycle (content, actors, objects, capabilities, inventory, binding, reset, snapshot, release). It has already received a long series of targeted "SA-7B*" and "SA-8A*" audits focused on exactly bridge reduction, stage injection, runtime state ownership, and route-exit enforcement.

**Key parallels to what we cleaned in Operational:**
- No identical "DEPRECATED - MOVED TO CANONICAL LOCATION" contract stubs in the wrong place (Contracts/ is respected).
- Significant **bridge/transitória debt** and aggregated runtime bridges (the direct analog of the contract duplication and recorder-overload we removed).
- `BindEntryPipeline(...)` seam + heavy `IActivityEntryRuntimeBridge` + sub-bridge proliferation (still present and explicitly labeled "ponte transitória").
- Coordinators and runners (`ActivityCapabilityInventoryCoordinator`, `PendingOperationRunner`) that match the "não criar manager/coordinator genérico" rule.
- `SessionActivityPipeline` still implements a large number of `I*RuntimeBridge` interfaces and acts as a central callback/owner for many concerns.
- `SessionActivityHost` implements key Operational boundaries but is not yet a pure thin delegator.
- Multiple runtime state stores + inventory snapshot concerns (actively worked in prior audits, still tracked risk).
- Documentation points to the consolidated plan/status file `Docs/Plans/SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md`.

**Positive signals:**
- Contracts/ folder is clean and centralized.
- `ActivityEntryPipeline` is a real concrete class (progress vs early ADR state).
- Many granular deterministic stages exist.
- Extensive existing audit trail (10+ focused SA- documents) with several "CLOSED / AUDITED" and "PASS funcional + PASS arquitetural parcial".
- Pipeline/README.md is candid about sandbox status, frozen items, and debt.

**Overall canonical maturity:** Further along in some dimensions than pre-clean Operational (no stub pollution), but the "god object + bridge proliferation + incomplete entry surface split" pattern is larger and more entrenched. Many items are already on documented tracks (SA-13, SA-7B series, SA-17, SA-18); the audit surfaces what still needs explicit canonical-track decisions before bigger cleanups or new features.

---

## Scope & Method

Same methodology used for SessionOperational:
- Structural mapping (Contracts/ vs Pipeline/ vs Stages/ vs Adapters/ vs Capabilities/).
- Greps for legacy/transitória/bridge/DEPRECATED/coordinator/runner/god patterns.
- Reading of normative files (ADR-2.0-0002, Pipeline/README.md, key classes, composition, Host, EntryPipeline).
- Cross-check against Operational handoff adapters (`SessionActivityOperationalRoute*Adapter`).
- Review of existing `SessionActivity/Docs/Audits/` documents.
- Identification of "outside canonical but actively used" items (stop & resolve rule).

No blind deletions were performed in this pass (except prior incidental .cs~ temp file cleanup). Findings that are ambiguous were flagged for canonical-track resolution.

---

## Findings

### 1. Contract Organization & Hygiene

**Status:** Largely clean (improvement over pre-clean Operational).

- `SessionActivity/Contracts/` contains the expected family of `*Contracts.cs` files (ActivityEntryPipeline*, ActivityObject*, ActivityReset*, Snapshot*, Permission*, etc.) plus the handoff boundaries (`ISessionActivity*Boundary`).
- No `OperationalFadeContracts.cs`-style deprecated empty stubs were present in `Pipeline/`, `Stages/`, or elsewhere.
- Some contract files are very large (e.g. `ActivityEntryPipelineContracts.cs` defines many sub `IActivityEntry*RuntimeBridge` interfaces). This is a symptom of the bridge aggregation problem rather than file-location duplication.

**Finding:** Hygiene of *location* is good. Hygiene of *shape and number* of aggregated bridge contracts is a current debt (see below).

### 2. Bridge / "Ponte Transitória" / Aggregated Runtime Surface (Highest Parallel to Operational Cleanup)

This is the strongest analog to the 7 contract stubs and recorder-overload we removed.

- `ActivityEntryPipeline.cs` (line ~41 comment):
  > `// Ponte transitória SA-7B0: mantida apenas para stages ainda não migrados para bridges menores.`
- Constructor takes a huge list: `_runtimeBridge` (the aggregate), plus many specific sub-bridges, plus concrete adapters, registries, states, `ActivityCapabilityInventoryCoordinator`, `PendingOperationRunner`, etc. Many are initialized by casting the same `endpoint` object.
- `SessionActivityPipeline` (ctor + class declaration) implements:
  `ISessionActivityEntryHandoffReceiver`, `ISessionActivityPendingOperationCallback`, `ISessionActivitySnapshotPayloadProvider`, + 7+ `IActivityEntry*RuntimeBridge` + `IActivityExitActorTeardownRuntimeBridge`.
- Composition installer (`SessionActivityCompositionInstaller.cs:92`):
  ```csharp
  var activityEntryPipeline = new ActivityEntryPipeline( _pipeline, _pipeline, _pipeline, ... );
  _pipeline.BindEntryPipeline(activityEntryPipeline);
  ```
- `BindEntryPipeline` is still present and called (explicitly called out in Pipeline/README as remaining SA-13 debt).
- Dozens of stages receive `IActivityEntryRuntimeBridge endpoint` (and sometimes additional specific bridges) and call through it. Some stages also receive the `ActivityCapabilityInventoryCoordinator` directly.

**Severity:** High (structural). This is the "bridge transitória" + "stage-to-aggregated-bridge" pattern that ADR-2.0-0001 and ADR-2.0-0002 explicitly want to reduce. The existing SA-7B0 / SA-7B1 / SA-7B2 / SA-7B5 audits were precisely attacking this; several sub-cuts closed, but the aggregate remains in the active path.

**Canonical track recommendation:** Treat the current `IActivityEntryRuntimeBridge` + sub-bridges as an explicit **transitional surface** (documented). Prioritize splitting into smaller, concern-specific ports (per stage or per phase) so that `ActivityEntryPipeline` ctor and stages talk to narrow contracts. The `BindEntryPipeline` seam should be removed or reduced as part of SA-13 closure. Do not grow the aggregate further.

### 3. SessionActivityPipeline as God Object / Macro + Entry Owner

- Very large class (thousands of lines) that owns:
  - Macro rail lifecycle (activation, running, pause, completion, internal transitions, route-exit).
  - Many runtime states (content, release, object exit, etc.).
  - Direct implementation of entry bridges (so stages and Operational handoff can call back).
  - Pending operation runner dispatch for windows/content.
  - Snapshot payload provider for RouteActivitySave.
- It creates/injects `ActivityEntryPipeline` and then calls `BindEntryPipeline` on itself.
- Multiple private state machines and pending* fields for different rails (route exit, restart, internal activity transition, visual readiness, etc.).

**Severity:** High (matches the god-object risk called out at the top of ADR-2.0-0002).

**Canonical track:** Per ADR-2.0-0002:
- Keep `SessionActivityPipeline` as owner of **macro** lifecycle, handoff receipt, next/restart/route-exit policy, foreign/stale protection.
- Move as much deterministic entry work as possible into `ActivityEntryPipeline` + its stages (already happening).
- The current heavy bridge implementation inside the macro pipeline is the debt. Reduce by making EntryPipeline own more of the entry surface directly and have narrower communication with the macro pipeline.

### 4. Host Thinness & Route-Exit Ownership

- `SessionActivityHost` implements `ISessionActivityRouteExitTeardownBoundary` and `ISessionActivityVisualReadinessBoundary` (the exact ports used by Operational handoff adapters).
- It still holds the catalog + pipeline, does composition in Awake, has QA observe logic, public `Pipeline` property, and some guards.
- Per ADR-2.0-0002 section 3: "RouteExit teardown deve ter owner único" (`SessionActivityPipeline`). Host must be endpoint/delegador externo, not owner of decision or state.

**Existing audits (SA-8A* series)** have worked on ExitOrderingPolicy, route-exit active path enforcement, and rails vs policy. Progress noted, but Host is still non-trivial.

**Canonical track:** Make Host a thin MonoBehaviour that:
- Exposes only the two boundary interfaces to Operational.
- Forwards to `SessionActivityPipeline`.
- Does not hold or expose the full pipeline for general use.
- Removes or clearly marks all QA/autoStart as non-canonical.

### 5. Coordinators, Runners, and Generic Layers

- `ActivityCapabilityInventoryCoordinator` is instantiated inside `ActivityEntryPipeline` ctor and passed to some object setup stages.
- `ISessionActivityPendingOperationRunner` + `UnitySessionActivityPendingOperationRunner` (created in installer, wired to pipeline).
- `ActivitySetupInventoryBuilder`, `ActorPresentationPlanResolver`, etc.

These match the warning in multiple ADRs: "Não criar manager/coordinator/processor genérico para esconder fronteira ruim."

**Severity:** Medium (recurring pattern).

**Canonical track:** Prefer explicit stage + narrow policy or contribution model. If a coordinator/runner is required for technical orchestration (pending operations, window scenes), document it as infrastructure (like the Operational adapters) and keep its surface minimal. Do not let it become the place where lifecycle or policy decisions live.

### 6. Snapshots, Inventory, Runtime State Writers & Multiple Writers Risk

- Many `*RuntimeState` classes (`ActivityContentRuntimeState`, `ActivityContentReleaseRuntimeState`, `ActivityObjectExitRuntimeState`, `ActivityEntryInventoryRuntimeState`, `ActivityParticipationRuntimeState`, `ActivityActorExitRuntimeState`).
- `ActivityCapabilityInventory`, preview, contributor discovery, setup inventory all have clear/ write paths in entry stages and pipeline.
- Existing audit series (SA-7B2 Runtime-State-Store-Bridge-Split, SA-7B3 Object-Actor-Inventory-Store-Source-Ownership, SA-17* pending-operation and exit correlation) shows this area has been actively normalized.
- ADR-2.0-0002 rule 12 is explicit: "Snapshots/índices runtime têm writer canônico único". Consumers must not become writers.

**Status:** Partially on track (many splits already performed and audited). Still listed as residual risk in recent closures (movement retained/control and ActivityContentReleaseRuntimeState called "high risk").

**Canonical track:** Continue the store-source split discipline. Make sure `ActivityObjectExitRuntimeState` and equivalent for content are the single technical writers for their correlation data, with the macro pipeline only committing/freezing at the right boundary.

### 7. Documentation & Status Report Debt

- `SessionActivity/Pipeline/README.md` is the best current operational view and is refreshingly honest ("congelado como sandbox funcional minimo Base 1.1", lists many SA- cuts, defers RunPipeline, etc.).
- It references the consolidated plan/status file `Docs/Plans/SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md` as the place for the resumo atual.
- Rich history lives in `SessionActivity/Docs/Audits/` (10 focused documents, mostly SA-7B* bridge decomposition and SA-8A* exit policy/route-exit enforcement).

**Finding:** Operational documentation is more up-to-date in one place (the module README + Etapa notes). SessionActivity has better per-cut audit granularity but the consolidated status report is missing or mis-referenced.

### 8. Other Remnants & Minor Items

- "PruneLegacyEmptyObjectEntryRequirements" in authoring (ActivityContentProfileAsset + ActivitySetupRequirementsAuthoring) — intentional legacy data pruning, not runtime logic debt.
- 7 `*.cs~` editor temp files were present and cleaned in the preceding step (non-source).
- No empty "Debug/"-style remnant directories found in this pass.
- Simulation/ folder exists with gate + contracts (mentioned as part of the model in the Pipeline README).

---

## Comparison Snapshot (SessionOperational Cleanup vs Current SessionActivity)

| Area                        | SessionOperational (post-clean)          | SessionActivity (current audit)                     | Analog Problem? |
|-----------------------------|------------------------------------------|-----------------------------------------------------|-----------------|
| Contract location           | Clean in Contracts/                      | Clean in Contracts/                                 | No (good) |
| Deprecated stubs            | 7 removed                                | None found                                          | No |
| Bridge/transitória          | Largely resolved (post recorder + camera + handoff normalizations) | Heavy (SA-7B0 comment, aggregate I*RuntimeBridge, BindEntryPipeline seam) | Yes (core) |
| God object / macro owner    | Pipeline owns order + handoff            | Pipeline still very central + implements many bridges | Yes |
| Coordinator/runner          | None prominent                           | InventoryCoordinator + PendingOperationRunner       | Yes |
| Host thinness               | N/A (no host equivalent)                 | Host implements boundaries but is non-trivial       | Partial |
| Entry lifecycle split       | N/A                                      | ActivityEntryPipeline exists but split incomplete   | Yes |
| Multiple state writers      | RouteActivitySave scopes via resolver    | Multiple *RuntimeState + inventory (active audits)  | Yes (tracked) |
| Existing audit trail        | ADR-2.0-0001 + module README Etapas      | 10+ SA-7B/SA-8A audits (good granularity)           | Better in SA |
| Consolidated status doc     | Strong in module README                  | Referenced file missing                             | Debt |

---

## Items That Require Explicit "Put on Canonical Track" Resolution

Per the original rule ("se alguma coisa tiver dúvida sobre remoção, porque esta fora do canonico mas de alguma forma ainda é usada, vamos parar e resolver colocar isso em um trilho canonico"):

1. **Aggregated `IActivityEntryRuntimeBridge` + sub-bridges + `BindEntryPipeline` seam** — already partially on SA-7B / SA-13 tracks. Make the transitional nature + reduction plan explicit and time-boxed.
2. **`ActivityCapabilityInventoryCoordinator`** — decide: infrastructure helper owned by EntryPipeline, or policy object injected narrowly, or eliminate in favor of direct stage + inventory contracts.
3. **`ISessionActivityPendingOperationRunner` / window & content operation dispatch** — confirm as technical runner (like Operational adapters) with narrow contract; do not let it absorb lifecycle decisions.
4. **Host as boundary implementer** — explicitly document the minimal surface it must expose to Operational (the two *Boundary interfaces) and the rule that it must not own teardown or readiness decisions.
5. **Writer uniqueness for runtime states (content release, object exit correlation, inventory preview)** — continue the store-source discipline; any new consumer that writes must be treated as a bug.
6. **Consolidated status file** — keep `Docs/Plans/SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md` as the canonical status hub and update references accordingly. Treat as documentation canonical-track item.

---

## Recommendations & Proposed Next Steps (Prioritized)

1. **Short term (low risk hygiene)**
   - Keep the consolidated status block in `Docs/Plans/SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md` in sync with the Pipeline README.
   - Continue the pattern of small, named SA-* audit + closure documents for each remaining bridge or store split.
   - Clean any remaining `.cs~` or editor artifacts if they reappear (already done once).

2. **Medium term (structural canonization, matching what we did for Operational)**
   - Close or make concrete progress on SA-13 (decomposition of the entry runtime surface so `ActivityEntryPipeline` can be constructed with narrower contracts and without the `Bind` seam back into the macro pipeline).
   - Reduce the number of interfaces the macro `SessionActivityPipeline` must implement for entry concerns.
   - Drive Host toward pure thin delegator (update existing SA-8A audits if needed).

3. **Governance**
   - Before any new feature or large extraction in SessionActivity, run the same 10-question anti-deslocamento checklist from ADR-2.0-0001.
   - Treat "ponte transitória" comments as time-bombed debt that must either be removed or promoted to documented canonical shape in the same or next cut.
   - Require that any new coordinator/runner has a one-sentence "why this cannot be a stage + narrow policy" justification in its file header.

4. **Validation**
   - Any cut that touches the entry bridge surface or route-exit path must re-run the smoke baseline listed in the Pipeline README (RestartCurrentActivity, Activity01ToActivity02, RouteExitBackToMenu, object snapshot/release when applicable, no FATAL / route_transition_failed).

---

## Appendix

**Existing focused audits (SessionActivity/Docs/Audits/ as of 2026-06-14):**
- SA-7A5, SA-7A6, SA-7B0..SA-7B5 (bridge reduction / decomposition / stage injection / state store / content pending operation)

---

## Execution Update (2026-06-14) — Phase 0 + Start of Phase 1 Completed

As part of executing the SA-19 plan ("faça 0 e 1"):

- **SA-19A0 (Documentation Baseline)**: Created the consolidated plan/status/freeze block in `Docs/Plans/SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md`. Updated cross-references in `SessionActivity/Pipeline/README.md`, `Docs/ADRs/README.md`, and this plan.
- **SA-19A1 (Governance Refresh)**: Added mandatory "OBRIGATÓRIO ANTES DE QUALQUER ATIVIDADE SA-19+" box (the 10 anti-deslocamento questions + required reading order) to the top of the SA-19 plan.
- **SA-19B0 (Bridge Surface Freeze)**: Created `SessionActivity/Docs/Audits/SA-19B0-Bridge-Surface-Freeze.md` — full inventory of the current `IActivityEntryRuntimeBridge` aggregate, the "ponte transitória SA-7B0" comment, the `BindEntryPipeline` seam, list of stages still using the aggregate, and an initial current → target mapping.
- **SA-19B1 real removal completed**:
  - The method `BindEntryPipeline` was completely removed from `SessionActivityPipeline.cs`.
  - Replaced with a clearly documented transitional `AttachEntryPipeline` (temporary only, to be removed when construction is restructured with narrow contracts).
  - Installer call site updated.
  - Old seam method no longer exists.

- **Batches of stage narrowing (SA-19B2 - continued waves)**:
  - Cumulative narrowed stages: PlayerInputBinding, ActorAttribute, ActorCommandBinding, ParticipantReset, ActorPresentation, MovementBinding, CameraBinding, GateBinding, ObjectContributorUnregister, ActorParticipationEnter.
  - 10+ stages successfully migrated to narrow contracts.
  - Call sites in ActivityEntryPipeline and related updated.
  - Ongoing audit shows significant reduction in broad bridge usage. Complex holdouts (teardown, content, object composites) for next targeted batches.

All work followed the anti-deslocamento checklist before editing. No new aggregate bridge usage was introduced. The `BindEntryPipeline` method remains functional for this cut but is now explicitly time-bombed.

**Immediate follow-up planned**: Deeper removal of the seam (constructor injection or internal creation of EntryPipeline with narrower contracts) in the next sub-activity of SA-19B1.
- SA-8A1..SA-8A3-H1 (exit ordering policy, rails vs policy, route-exit active path enforcement)

**Key files inspected in this audit:**
- `SessionActivity/Pipeline/README.md`
- `SessionActivity/Pipeline/SessionActivityPipeline.cs` (declaration + BindEntryPipeline + bridge impls)

---

## Re-focused Structural Audit Addition (2026-06-14)

**Context:** The user requested a re-do of the audit with less emphasis on observability/signaling/facts and more on architecture problems, duplications, lack of canonical ownership, obsolete/legacy pieces, and folder/code organization.

**New dedicated document created:**
- `SessionActivity/Docs/Audits/SA-Architecture-Hygiene-Duplication-Obsolete-Audit.md`

**Summary of key structural findings incorporated from the re-focused audit:**

- **Obsolete/Legacy Rails (High severity):**
  - `PlayerInputBindingStage.cs` and `PlayerMovementControlStage.cs` still present with comments declaring them superseded ("O caminho ativo agora é...").
  - Persistent "PlayerActor" parallel rail (PlayerActorId, dedicated resolvers, special cases in scanners/permissions/reset/inventory) despite previous convergence work (SA-5A0 etc.). This directly contradicts ADR-2.0-0002 rules against permanent parallel rails (player/nonplayer).

- **Duplication (High severity):**
  - One scanner + builder per capability (Attributes, Camera, Presentation, Permission, Lifecycle, Object...).
  - Heavy repetition of BuildIdentity + EmitFact/EmitSnapshot boilerplate across stages.
  - Duplicated player actor resolution logic in multiple places.
  - `ActivityEntryObjectSetupStages.cs` acts as a composite/god-file containing multiple internal stages.

- **Lack of Canonical / Architecture Smells:**
  - Broad `IActivityEntryRuntimeBridge` and "ponte transitória SA-7B0" comment still active in `ActivityEntryPipeline.cs`.
  - `SessionActivityPipeline` implements excessive I*RuntimeBridge interfaces.
  - `ActivityCapabilityInventoryCoordinator` and heavy manual wiring in CompositionInstaller (violates "no generic coordinators" and anti-deslocamento rules).
  - Unclear boundaries between discovery, setup, binding, and reset.

- **Folder & Organization Issues:**
  - `Stages/` mixes canonical `ActivityEntry*` with leftover `Player*` files.
  - Over-fragmented `Capabilities/` tree.
  - Composite files and lack of clear transitional code hygiene.

**Derived Plan:**
- A short, prioritized hygiene plan was created in parallel: `Docs/Plans/SA-Architecture-Hygiene-2026-06-Plan.md`
- It contains atomic tasks (H1–H10) grouped in phases: remove obsolete rails, deduplication, folder/architecture cleanup, and governance.
- Each task has explicit acceptance criteria (compile + smoke/ownership matrix, docs update, anti-deslocamento checklist).

This addition ensures the main audit now reflects both the previous observability/bridge work and the requested structural/architectural hygiene lens. The two are complementary.

**End of re-focused addition.**
- `SessionActivity/Pipeline/ActivityEntryPipeline.cs` (ctor + ponte transitória comment)
- `SessionActivity/Pipeline/SessionActivityHost.cs`
- `SessionActivity/Pipeline/SessionActivityCompositionInstaller.cs`
- `SessionActivity/Contracts/ActivityEntryPipelineContracts.cs` (bridge definitions)
- Multiple stages that receive `IActivityEntryRuntimeBridge`
- Cross references from `SessionOperational/Adapters/SessionActivityOperationalRoute*Adapter.cs`

**Related normative documents:**
- ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md (primary)
- ADR-2.0-0001-SessionOperational-Ownership-Stabilization.md (anti-deslocamento rules apply by extension)
- SessionOperational cleanup work (2026-06-14) for the parallel lens.

---

**End of audit snapshot.** This document is intended as a living evaluation artifact. Update it (or supersede with a new SA- cut) when the next bridge split, SA-13 work, or Host thinness normalization lands.

Next action suggestion: if the team agrees on priorities, we can execute the highest-confidence safe normalizations (e.g. further narrow the bridges in a new SA- cut) or update the consolidated status block.
