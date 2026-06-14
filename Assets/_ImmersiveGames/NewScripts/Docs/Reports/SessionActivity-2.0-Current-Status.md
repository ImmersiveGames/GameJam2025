# SessionActivity — Base 2.0 Current Status (Consolidated)

**Date:** 2026-06-14 (initial creation as part of SA-19A0)  
**Last updated:** 2026-06-14 (reorganized after compile validation and B2 batch waves)  
**Canonical references:**
- [SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md](../Plans/SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md)
- [SessionActivity-Base2.0-Ownership-Canonization-Audit-2026-06-14.md](../Reports/SessionActivity-Base2.0-Ownership-Canonization-Audit-2026-06-14.md)
- ADR-2.0-0002 — SessionActivity Ownership Decomposition
- `SessionActivity/Pipeline/README.md` (operational details and historical SA-* closures)

---

## Overall Maturity

SessionActivity is operating as a **functional sandbox for Base 1.1** with incremental Base 2.0 decomposition in progress.

It has received extensive cut-by-cut work (SA-7B* bridge decomposition, SA-8A* exit policy/route-exit, SA-13/14/17/18 series). Many sub-areas have "CLOSED / AUDITED" or "PASS funcional + PASS arquitetural parcial".

Significant progress has been made on the entry runtime surface through the SA-19 normalization wave (multiple B2 batches). The `BindEntryPipeline` seam has been fully removed, and 10+ stages have been narrowed from the broad `IActivityEntryRuntimeBridge` aggregate to specific narrow contracts (primarily `IActivityEntryIdentityRuntimeBridge` + `IActivityEntryFactRuntimeBridge`, plus domain-specific bridges).

Compilation now succeeds cleanly (validated via fulllog.txt after a hygiene pass to clean up partial-batch leftovers).

**Key positive indicators (current):**
- Contracts organized under `Contracts/`.
- `ActivityEntryPipeline` is a concrete class (not just a contract).
- Granular stages for content, actor setup, bindings, gate, inventory, snapshot, release, reset, etc.
- No "DEPRECATED contract stub" pollution of the kind recently cleaned from SessionOperational.
- Operational handoff from `SessionOperationalPipeline` is explicit via boundaries (`ISessionActivityEntryHandoffReceiver`, `ISessionActivityRouteExitTeardownBoundary`, `ISessionActivityVisualReadinessBoundary`).
- Bridge aggregate usage significantly reduced; "ponte transitória SA-7B0" and Bind seam resolved.
- Multiple low/medium-risk stages successfully migrated in B2 waves.

**Main gaps requiring canonical-track work (see the 2026-06-14 audit and reorganized plan for details):**
- Remaining complex stages still using the broad aggregate (Content unload/release, ObjectSetup composite, full ParticipantBinding, ExitActorTeardown).
- `SessionActivityPipeline` still implements too many runtime bridges in some areas (god-object symptom in progress).
- `SessionActivityHost` is still non-thin.
- Generic coordinators/runners (`ActivityCapabilityInventoryCoordinator`, `PendingOperationRunner`).
- Residual risk in runtime state ownership (especially content release and movement retained/control).
- Need to continue B2 on high-complexity stages and move to Fase 2 (Host thinness + Route-Exit).
- H2 close applied in the codebase, pending user smoke: reset/scanner cleanup removed the dedicated player-identity resolver rail from the active path.

---

## Current Canonical Ownership (High Level)

| Area                              | Owner                                      | Notes |
|-----------------------------------|--------------------------------------------|-------|
| Macro lifecycle (activation, running, pause/resume, completion, internal transitions, route-exit decision) | `SessionActivityPipeline` | Macro ordering, handoff receipt, foreign/stale protection |
| Deterministic entry lifecycle (content load/readiness, inventory, actor/object setup, bindings, snapshot, reset, release) | `ActivityEntryPipeline` + Stages | Should use narrow contracts, not one big aggregate bridge |
| Route-exit teardown decision & ordering | `SessionActivityPipeline` | Host and stages are executors only |
| Visual readiness for handoff      | `SessionActivityPipeline` (via boundary)  | — |
| Technical adapters (scene load, window, pending ops runner, input/pause) | Adapters (in `Adapters/`) | Pure side-effect |
| Capability inventory & scanning   | `ActivityCapabilityInventory` + Scanners + Stages | Coordinator is transitional |
| Snapshot for RouteActivitySave    | `SessionActivityPipeline` (as provider) + `ActivityObjectSnapshotCaptureStage` / Release stages | Single writer discipline required |
| Host (MonoBehaviour)              | Thin delegator only                        | Must not own lifecycle or decisions |

---

## Active Normalization Wave (SA-19)

See the reorganized full plan: `Docs/Plans/SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md`

**Completed (as of latest update):**
- SA-19A0: Documentation baseline (this report + plan references).
- SA-19A1: Governance refresh (anti-deslocamento checklist enforcement + mandatory process box in plan).
- SA-19B0: Bridge surface freeze & inventory (explicit transitional declaration + target mapping).
- SA-19B1: Real removal of the `BindEntryPipeline` seam (method deleted; transitional `AttachEntryPipeline` introduced and documented).
- Multiple waves of SA-19B2: 10+ stages narrowed from broad aggregate (PlayerInputBinding, ActorAttribute, ActorCommandBinding, ParticipantReset, ActorPresentation, MovementBinding, CameraBinding, GateBinding, ObjectContributorUnregister, ActorParticipationEnter, etc.). 
- Hygiene pass: Fixed residual compile errors from partial batches (`endpoint` references and call-site signature mismatches). Clean build confirmed in fulllog.txt.

**Current focus:**
- Continue SA-19B2 on remaining complex stages (Content unload/release, ObjectSetup composite, ParticipantBinding, ExitActorTeardown).
- Move to Fase 2 (Host thinness + Route-Exit ownership finalization).

All activities must follow the anti-deslocamento checklist and produce evidence (smoke + ownership matrix).

---

## Smoke Baseline (required for any SA-19+ closure)

From `SessionActivity/Pipeline/README.md`:

```
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
RestartCurrentActivity
Activity01ToActivity02
RouteExitBackToMenu
ActivityObjectSnapshotCapture, ActivityObjectRelease, ActivityObjectContributorUnregister quando aplicavel
```

Additional for normalization cuts:
- No new fallback silencioso.
- Ownership matrix visibly improved in audit notes.
- "ponte transitória" comments do not increase.

---

## Historical Checkpoints (selected, see Pipeline/README for full list)

- SA-13D: CLOSED / AUDITED (Bind seam left as SA-13 debt)
- SA-14B1: CLOSED / PASS funcional + PASS arquitetural (exit correlation explicit)
- SA-17 series: Multiple bridge dispatch and state store splits
- SA-18A*: Retained binding + permission scanner guard closures

High-risk items still deferred: Movement retained/control, ActivityContentReleaseRuntimeState.

---

## Next Steps (current)

Follow the reorganized plan (`Docs/Plans/SessionActivity-Base2.0-Canonization-Normalization-Plan-2026-06-14.md`).

Priority:
1. Complete SA-19B2 batches for complex stages (audit first to identify exact narrow interfaces needed).
2. Begin Fase 2: Host surface audit + thin delegator implementation (SA-19C).
3. Maintain this report and the plan in sync after each batch wave.
4. Prepare for full smoke validation on canonical scenarios once remaining B2 is advanced.

This report is kept in sync with the plan and the 2026-06-14 audit document.

**End of consolidated status.**
