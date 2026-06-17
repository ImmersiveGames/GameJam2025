# SA-Architecture-Hygiene-2026-06 — SessionActivity Architecture, Duplication & Obsolete Hygiene Plan

**Date:** 2026-06-14  
**Based on:**  
- `SessionActivity/Docs/Audits/SA-Architecture-Hygiene-Duplication-Obsolete-Audit.md` (re-focused audit)  
- ADR-2.0-0002 (SessionActivity Ownership Decomposition)  
- ADR-2.0-0001 (anti-deslocamento rules)  
- Main ownership audit (2026-06-14) and reorganized normalization plan  
- SessionActivity/Pipeline/README.md

**Status:** Proposed / Ready for execution. This is a dedicated hygiene wave focused on structural/architectural debt (distinct from pure bridge-narrowing/observability work).

**Goal:** Eliminate obsolete rails, reduce duplication, enforce canonical ownership and folder hygiene, and clean up architectural smells so that future Base 2.0 work (and maintenance) is not slowed by legacy parallel paths and duplicated logic.

**Guiding Principles (Non-Negotiable):**
- Follow the same 10-question anti-deslocamento checklist from ADR-2.0-0001 before any change.
- No new generic coordinators/managers.
- Remove the old path when a canonical replacement exists (no permanent parallel rails).
- Every removal or dedup must improve the ownership matrix.
- Evidence required: compile clean + smoke baseline (or at minimum no regression in key scenarios) + updated audit/plan.

---

## 1. Current Structural Debt Snapshot (from re-focused audit)

**Obsolete / Legacy (High):**
- Player* stage files still present (`PlayerInputBindingStage.cs`, `PlayerMovementControlStage.cs`) with comments declaring them superseded.
- Persistent "PlayerActor" parallel rail (PlayerActorId, dedicated resolvers, special cases in almost every scanner, permission, reset, inventory, etc.) despite previous convergence attempts.
- "PruneLegacy..." methods and comments in authoring.

**Duplication (High):**
- One scanner + contribution builder per capability (Attributes, Camera, Presentation, Permission, Lifecycle, Object, TransformPathUtility...).
- Repeated `BuildIdentity` + EmitFact/EmitSnapshot + SetCurrentIdentity boilerplate across 30+ stages.
- Duplicated PlayerActor resolution logic scattered across Inventory, Permissions, reset paths, etc.
- `ActivityEntryObjectSetupStages.cs` as a composite god-file.

**Lack of Canonical / Architecture Smells (Medium-High):**
- Broad `IActivityEntryRuntimeBridge` still in use in several places.
- `ActivityEntryPipeline` still carries "ponte transitória SA-7B0" comment and heavy ctor.
- `SessionActivityPipeline` implements too many I*RuntimeBridge interfaces.
- `ActivityCapabilityInventoryCoordinator` and manual wiring in CompositionInstaller (anti-patterns per ADRs).
- Unclear boundaries between discovery/setup/binding/reset.

**Folder & Organization Issues (Medium):**
- `Stages/` mixes canonical `ActivityEntry*` with leftover `Player*` files.
- Over-fragmented `Capabilities/` tree.
- Composite files and lack of clear grouping for transitional code.

**Reference:** Full details and evidence in the re-focused audit document.

---

## 2. Phased Plan (Prioritized, Atomic Tasks)

All tasks must produce a short closure note (or update to the hygiene audit) with:
- Files touched
- Ownership matrix before/after
- Compile + relevant smoke (or explicit "no regression in X scenarios")
- Update to this plan + main audit documents

### Phase 1 — Remove Obsolete Rails & Files (Highest leverage, lowest risk for quick wins)

**SA-Arch-H1 — Delete Superseded Player* Stage Files** (EXECUTED 2026-06-14)
- `PlayerInputBindingStage.cs` + .meta **DELETED** (was empty legacy file with only the comment declaring the canonical path is now the ActivityEntry version).
- `PlayerMovementControlStage.cs` + .meta **NOT DELETED**: audit showed it is still the active implementation called by the macro `SessionActivityPipeline` for player movement control (multiple paths in activity running, windows, reset, etc.). It is not pure legacy for its current role. Removal would require migrating the movement control logic first (future sub-task if desired). Updated audit and this plan to reflect reality.
- Cleaned references in current docs/audits/plans.
- Added to REMOVED_FILES.txt.
- **Status:** H1 partially completed (the truly obsolete file removed; the other audited and documented as still canonical for macro). No compile impact. Ownership matrix improved by removal of dead parallel file.
- Updated the re-focused audit and hygiene plan.

**SA-Arch-H2 — Collapse Persistent PlayerActor Parallel Rail (Core of the structural debt) — INICIADO**

**Auditoria inicial (2026-06-14):**
- `PlayerActorCapabilityIdentityResolver` e interface dedicada ainda usados em coordinator e scanners (Permission, Camera).
- `PlayerActorCapabilityIdentity` struct.
- Special cases em `ActivityCapabilityOwnerKind.PlayerActor`, InventoryCoordinator, PermissionScanner, CameraTargetScanner, etc.
- Muitos `PlayerActorId`, `PlayerActorIdentityRecord`, `ActivityPlayerActorRegistry` espalhados em Pipeline, Contracts, Capabilities.
- `PlayerActorPlacementMarker` em runtime.
- Decisão canônica: PlayerActor vira detalhe de participação nas bordas. Usar Actor geral + participation context para capability inventory, binding, etc. Remover resolvers dedicados e branches PlayerActor.

**Sub-cuts planejados:**
- **H2a (atual - iniciado):** Inventory/Permissions/Camera — remover `PlayerActorCapabilityIdentityResolver`, `PlayerActorCapabilityIdentity`, atualizar scanners/coordinator para usar general Actor + participation. Remover branches `OwnerKind.PlayerActor` onde possível.
- **H2b:** Reset paths e Authoring (colapsar PlayerActor handling em reset).
- **H2c:** Scanners restantes e OwnerKind deprecate.
- **H2d:** Limpeza em Pipeline/Contracts (reduzir PlayerActor* em registros, bindings).

**Aceite por sub-área:**
- Nenhum novo caso especial PlayerActor introduzido.
- Smoke nos cenários com players.
- Matriz ownership atualizada mostrando redução do rail paralelo.
- Update em plano, auditoria e REMOVED_FILES se tipos removidos.

**Progresso atual (H2a):**
- Auditoria mapeada (PlayerActorCapabilityIdentityResolver, struct, OwnerKind.PlayerActor branches, usages in Permission/Camera/InventoryCoordinator).
- Resolver e interface marcados DEPRECATED e removidos do uso ativo no coordinator.
- PermissionScanner e CameraTargetScanner atualizados para ctor opcional (sem mais throw se null).
- Comentários de transição adicionados.
- Próximo em H2a: limpar branches `if (ownerKind == ActivityCapabilityOwnerKind.PlayerActor)` e atualizar chamadas que ainda passam o resolver (se houver em outros lugares).
- Matriz ownership: rail paralelo reduzido em capability discovery (agora general Actor + participation).

**SA-Arch-H3 — Remove Legacy Pruning Code**
- Remove `PruneLegacyEmptyObjectEntryRequirements` and related logic in Authoring.
- Clean any associated authoring data or comments.
- **Acceptance:** No behavior change for valid data. Compile clean.

**Priority for Phase 1:** Do H1 immediately (quick deletion). Then H2 (biggest structural win). H3 as quick follow-up.

---

### Phase 2 — Deduplication (High impact on maintainability)

**SA-Arch-H4 — Scanner & Contribution Builder Infrastructure**
- Introduce common scanner base/policy or shared infrastructure so we stop writing one nearly-identical scanner class per capability.
- Collapse or rationalize the many `*ContributionBuilder.cs` files.
- **Acceptance:** Reduced number of scanner/builder files. No duplication of scanning logic. New capabilities should be able to plug in with minimal boilerplate.

**SA-Arch-H5 — Centralize Lifecycle Signaling Boilerplate**
- Extract common helpers for `BuildIdentity` + EmitFact/EmitSnapshot + SetCurrentIdentity (or make them part of the narrow identity/fact bridges).
- Apply across stages.
- **Acceptance:** Significant reduction in repeated code blocks. New stages follow the helper pattern.

**SA-Arch-H6 — Deduplicate PlayerActor Resolution Logic**
- Centralize the repeated "resolve player actors for current entry / from participation / from inventory" logic (currently scattered in Inventory, reset, etc.).
- Provide narrow, canonical helpers or policies.
- **Acceptance:** Single source of truth for player actor resolution in the context of entry. Duplicated methods removed.

**Priority:** H4 and H5 can run in parallel after Phase 1 starts. H6 ties back to H2.

---

### Phase 3 — Architecture & Folder Hygiene

**SA-Arch-H7 — Clean Composite Files & Stage Organization**
- Split or properly document `ActivityEntryObjectSetupStages.cs` (multiple internal stages in one file is a smell).
- Ensure each stage file has single responsibility.
- **Acceptance:** No god-composite files. Clear ownership per stage file.

**SA-Arch-H8 — Folder Structure Cleanup**
- Remove leftover Player* files (already in H1).
- Group transitional/legacy code clearly (e.g., mark with comments or subfolder if needed; prefer deletion).
- Consider light reorganization of Capabilities/ (reduce fragmentation) or add READMEs explaining the scanner/builder pattern.
- Review Contracts/ for over-fragmentation.
- **Acceptance:** Cleaner navigation. No dead files mixed with active ones. Updated folder-level docs if helpful.

**SA-Arch-H9 — Enforce Narrow Contracts & Reduce God-Object Surface**
- Finish migration away from broad `IActivityEntryRuntimeBridge` in remaining stages (build on previous B2 work).
- Reduce the number of I*RuntimeBridge interfaces implemented by `SessionActivityPipeline` and `ActivityEntryPipeline`.
- Replace or narrow `ActivityCapabilityInventoryCoordinator` (push to explicit policy + stage ownership).
- Review CompositionInstaller wiring for manual service-locator patterns.
- **Acceptance:** Fewer broad-bridge usages. Visible reduction in interfaces on the two pipelines. Coordinator usage justified or removed. Ownership matrix improved.

---

### Phase 4 — Governance & Closure

**SA-Arch-H10 — Standing Rules & Documentation**
- Add explicit rule in Pipeline/README and this plan: "No new PlayerActor special casing or duplicated scanner/builder without explicit hygiene justification."
- Update the main ownership audit and reorganized normalization plan with cross-references to this hygiene wave.
- Produce final hygiene audit closure note when major phases complete.
- **Acceptance:** Rules documented. All key docs (plan, audits, README) cross-referenced. "Largely complete" criteria from the main plan can be partially satisfied for the structural axis.

---

## 3. Prioritization & Sequencing

**Wave 1 (Quick wins + high structural impact):**
- H1 (delete Player* files) — **DONE** (PlayerInputBindingStage removed; PlayerMovementControlStage audited and kept as active for macro movement control).
- **Limpeza extra de comentários "Player*" residuais em código vivo** — **DONE**: 
  - Adicionado comentário de status no topo de PlayerMovementControlStage.cs esclarecendo seu papel atual e link para H2.
  - Adicionado comentário explicativo próximo às chamadas em SessionActivityPipeline.cs.

**H2 — Collapse Persistent PlayerActor Parallel Rail — INICIADO (2026-06-14)**
- Auditoria inicial do rail PlayerActor paralela concluída (ver seção abaixo).
- Começando por H2a: Inventory/Permissions/Camera (remover dedicated resolver, colapsar special cases para general Actor + participation context).
- Próximos sub-cuts: H2b Reset, H2c Scanners gerais, H2d OwnerKind e contratos.
  - Nenhum comentário morto referenciando o arquivo deletado (PlayerInputBindingStage) foi encontrado em código ativo (já estavam apenas no arquivo deletado ou em docs históricos).
- Start H2 (PlayerActor rail collapse) + H3 (legacy pruning)

**Wave 2 (Deduplication):**
- H4, H5, H6 (scanners, boilerplate, resolution logic)

**Wave 3 (Deeper cleanup):**
- H7, H8, H9 (composites, folders, god-object surface)

**Wave 4 (Governance):**
- H10

**Risk guidance:** H2 (PlayerActor rail) is the highest structural win but also the broadest — split it. Anything touching movement retained/control or content release should have extra audit before changes (per previous risk notes).

**Parallelization:** H1 + H3 can run early. Dedup work (H4-H6) can overlap with rail removal once the most obvious PlayerActor duplication is mapped.

---

## 4. Success Criteria for This Hygiene Wave

- No more Player* stage files or "ponte transitória SA-7B0" comments in production code.
- Significant reduction in PlayerActor special-casing and duplicated scanner/builder/resolution code.
- Cleaner folder structure with no dead files mixed in.
- Measurable improvement in ownership matrix (fewer broad interfaces, clearer stage responsibilities).
- All changes follow anti-deslocamento checklist.
- Documentation (this plan + main audit + Pipeline/README) reflects the work and new rules.
- Compile + smoke baseline passes (or explicit no-regression evidence).

---

## 5. Open Questions / Decisions

- Exact scope of "PlayerActor rail collapse" — how much special handling to keep at the participation/input edges?
- Preferred scanner infrastructure pattern (base class vs policy vs registry) — decide early in H4.
- Whether to keep or further collapse some of the tiny Contracts files.

---

**End of Plan**

This hygiene wave is meant to run alongside (or immediately after) the current B2 bridge-narrowing work, but with a distinct structural focus. It directly addresses the gaps highlighted in the re-focused architecture audit.

Update this plan after each atomic task. Cross-reference with the main SA-19 normalization plan and the 2026-06-14 audits.
## Checkpoint complementar — BASE-ID identity stabilization closure (2026-06-14)

Status: `BASE-ID identity stabilization — CLOSED FOR NOW`.

Esta atualização registra o fechamento da frente complementar de identidade iniciada durante a higiene de arquitetura. Ela não substitui `SA-Arch-H2`; apenas congela o resultado dos cortes de estabilização de identidade já aceitos.

### Fechado

```text
BASE-ID-0        CLOSED / AUDIT
BASE-ID-1A       CLOSED / PASS
BASE-ID-1B       CLOSED / PASS
BASE-ID-1C       CLOSED / PASS
BASE-ID-1D       CLOSED / PASS
BASE-ID-1E       CLOSED / PASS
BASE-ID-1F       CLOSED / AUDIT
BASE-ID-1G       CLOSED / PASS
BASE-ID-1H       CLOSED / PASS
BASE-ID-1I-AUDIT CLOSED / NO RUNTIME CHANGES
BASE-ID-1I       DEFERRED
```

### Decisão operacional

Não executar `BASE-ID-1I — Type inventory lookup keys` agora.

Motivo: a auditoria concluiu que o risco restante não está no `Dictionary<string, IActivityCapabilityRuntimeReference>` em si. O inventário continua sendo índice técnico centralizado e os consumidores auditados usam `capability.CapabilityId` vindo do descriptor do inventário. A tipagem agora seria ampla e com ganho baixo.

### Resíduo aceito para backlog controlado

```text
ActivityCapabilityCameraTargetScanner
ActivityCapabilityActorPresentationScanner
ActivityCapabilityActorAttributeScanner
```

Esses scanners ainda podem derivar owner funcional via `ownerPath`. A correção futura, se necessária, deve ser tratada como subcorte direto de `BASE-ID-1I`, não como nova frente. Não reabrir apenas por `componentPath` aparecer em log ou metadata.

### Regra de continuidade

A partir deste checkpoint, não abrir novos cortes de identity sem uma das condições abaixo:

```text
1. item já existente em plano/ADR;
2. subcorte direto de item existente;
3. regressão concreta demonstrada por smoke/log/auditoria.
```

Qualquer mudança futura nessa área deve responder a matriz de ownership antes da implementação e preservar o baseline de smoke canônico.


---

## Baseline freeze — SA-19D0-A1-H1 / 2026-06-15

```text
SA-19D0-A1-H1 — CLOSED / PASS
Phase 3 — CLOSED
Baseline — FROZEN TEMPORARY FUNCTIONAL BASELINE
```

### Evidência aceita

```text
error CS: 0
warning CS: 0
FATAL: 0
Exception: 0
route_transition_failed: 0
checkpointStatus='Failed': 0
RejectedForeign: 0
RejectedStale: 0
fallback: 0
RestartCurrentActivity Passed: 1
Activity01ToActivity02 Passed: 1
RouteExitBackToMenu Passed: 1
ActivityCapabilityInventoryPreviewObserved: 3
ActivityCapabilityInventoryCoordinator: 0
```

### Decisão congelada

```text
ActivityEntryPipeline continua order owner.
ActivityEntryCapabilityInventoryBuildStage é build boundary determinístico.
ActivityEntryCapabilityInventoryPreviewStage é preview/fact/snapshot owner.
ActivityCapabilityInventory permanece snapshot/index passivo.
ActivityCapabilityInventoryCoordinator não deve voltar ao active path.
PendingOperationRunner não vira corte agora; ganho classificado como baixo/limpeza.
```

### Continuidade

```text
Não abrir D1.
Não reabrir B2/B3/C2 sem regressão concreta.
Próxima frente somente com auditoria + matriz de ownership.
```


---

## Activity Freeze Constraint — 2026-06-15

This hygiene plan must not be used to continue `SessionActivity` cleanup by inertia.

After `SA-19D0-A1-H1`, Activity runtime is frozen. Hygiene waves may continue only when a focused audit proves non-cosmetic value:

```text
concrete behavior improvement
or measurable ownership correction
or removal of active duplicated lifecycle owner
or required dependency for a new runtime feature
```

Cosmetic file/folder cleanup, naming cleanup, comment cleanup, or manual wiring cleanup is not enough to reopen Activity.
