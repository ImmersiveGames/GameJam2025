# SessionActivity — Base 2.0 Canonization & Normalization Plan

**Date:** 2026-06-14  
**Based on:**  
- `Docs/Reports/SessionActivity-Base2.0-Ownership-Canonization-Audit-2026-06-14.md` (the parallel audit to the SessionOperational cleanup)  
- ADR-2.0-0002 (SessionActivity Ownership Decomposition e ActivityEntryPipeline)  
- ADR-2.0-0001 (anti-deslocamento rules + ownership categories)  
- `SessionActivity/Pipeline/README.md` (honest sandbox status + existing SA-* closures)  
- Precedent from SessionOperational contract hygiene (2026-06-14)

**Status:** Phase 0 + Phase 1 (B0/B1 + múltiplas ondas de B2) **concluídos com sucesso**. Compilação limpa validada (fulllog.txt). 

**Próxima prioridade:** Concluir os batches B2 de estágios complexos + estabilização + Fase 2 (Host + Route-Exit).

**Execution log (atualizado após correções de compilação):**
- **2026-06-14 (fase inicial):** 
  - SA-19A0: Relatório consolidado `Docs/Reports/SessionActivity-2.0-Current-Status.md` criado + referências atualizadas em READMEs e plano.
  - SA-19A1: Caixa de governança anti-deslocamento adicionada no topo deste plano.
  - SA-19B0: Auditoria de freeze `SessionActivity/Docs/Audits/SA-19B0-Bridge-Surface-Freeze.md` criada com inventário completo e matriz current → target.
  - SA-19B1 (remoção real): Método `BindEntryPipeline` completamente removido. Substituído por `AttachEntryPipeline` transitório. Call site no Installer atualizado. Nome antigo do seam eliminado do código.
- **Ondas de SA-19B2 (stage narrowing) - batches aplicados:**
  - Batches concluídos (cumulativo ~10+ stages): PlayerInputBinding, ActorAttribute, ActorCommandBinding, ParticipantReset, ActorPresentation, MovementBinding, CameraBinding, GateBinding, ObjectContributorUnregister, ActorParticipationEnter.
  - Todos migrados do agregado amplo `IActivityEntryRuntimeBridge` para contratos estreitos (principalmente `IActivityEntryIdentityRuntimeBridge` + `IActivityEntryFactRuntimeBridge`, mantendo bridges específicos de domínio quando aplicável).
  - Call sites em `ActivityEntryPipeline.cs` e `SessionActivityPipeline.cs` atualizados.
  - Padrão identificado e documentado: a maioria dos stages de sinalização de ciclo de vida usa apenas SetCurrentIdentity + EmitFact/EmitSnapshot.
- **Passo de higiene de compilação (pós-batches parciais):**
  - Corrigidos erros residuais de "endpoint does not exist" em arquivos parcialmente migrados (PlayerInputBindingStage e ParticipantResetStage).
  - Corrigido mismatch de assinatura na call site de `ActivityContentReleaseFinalizationStage` para o UnregisterStage.
  - Auditoria rápida + replaces em massa para garantir que todos os batches iniciados estivessem completos.
  - **Resultado:** Compilação limpa confirmada via fulllog.txt. Nenhum erro de bridge mismatch restante nos estágios já batched.
- Próximos: Continuar B2 com estágios complexos (Content unload/release, ObjectSetup composite, Teardown, ParticipantBinding completo) + Fase 2 (Host thinness).

---

## ⚠️ OBRIGATÓRIO ANTES DE QUALQUER ATIVIDADE SA-19+

Antes de abrir qualquer PR, editar código ou criar novo corte relacionado a este plano:

1. Leia (na ordem):
   - Este plano
   - A auditoria de 2026-06-14 (`Docs/Reports/SessionActivity-Base2.0-Ownership-Canonization-Audit-2026-06-14.md`)
   - ADR-2.0-0002
   - `SessionActivity/Pipeline/README.md`

2. Responda por escrito (em nota do PR ou em audit note da atividade) as 10 perguntas anti-deslocamento do ADR-2.0-0001:

   - Qual pipeline é dono desta decisão?
   - Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?
   - Isso é comportamento final ou bridge transitória?
   - Essa compatibilidade ainda é necessária?
   - O erro está no sintoma ou na fronteira arquitetural errada?
   - Existe owner duplicado para o mesmo lifecycle?
   - A extração remove owner duplicado ou apenas desloca responsabilidade?
   - O novo componente tem nome concreto e fronteira estável?
   - O novo componente pode ser descrito sem “manager/coordinator/processor” genérico?
   - O smoke/log comprova comportamento, mas a matriz comprova ownership?

3. Só então implemente a mudança mais estreita possível que melhora o ownership.

Qualquer atividade que pular este passo será revertida.

---

---

## 1. Guiding Principles (Non-Negotiable)

All activities in this plan **must** obey:

1. **Anti-deslocamento checklist** (from ADR-2.0-0001) — before any code change, answer the 10 questions in a small note or the activity's audit file.
2. **Ownership categories** (Pipeline / Stage / Boundary / Policy / Command / Fact / Recorder / Adapter / Endpoint / Snapshot / Authoring data).
3. **No new generic layers** — no new "Coordinator", "Manager", "Runner" (as owner of policy or lifecycle), "Bridge aggregate" that hides boundaries.
4. **Explicit transitional shapes** — if something must stay as "ponte transitória" (e.g. current `IActivityEntryRuntimeBridge`), it must be time-boxed, documented, and have a concrete removal or promotion plan in the same or next activity.
5. **Single canonical writer** for any runtime snapshot/index/state (ADR-2.0-0002 rule 12).
6. **Pipeline vs EntryPipeline split** (ADR-2.0-0002):
   - `SessionActivityPipeline` = macro lifecycle, handoff, route-exit decision, foreign/stale protection, ordering of big phases.
   - `ActivityEntryPipeline` + stages = deterministic entry lifecycle (content load, inventory, setup, binding, readiness, snapshot, reset).
7. **Host is thin** — only delegator + boundary implementer for Operational. No lifecycle ownership, no heavy state.
8. **Evidence required** — every activity ends with:
   - Smoke baseline (the one listed in Pipeline/README: RestartCurrentActivity, Activity01ToActivity02, RouteExitBackToMenu, no FATAL/route_transition_failed, object snapshot/release when applicable).
   - Ownership matrix (who decides vs who executes).
   - Updated docs (Pipeline/README, this plan, new or updated SA-* audit note).

"Reduce file size" or "the code looks ugly" is **not** a valid reason for extraction.

---

## 2. Current State Snapshot (atualizado após ondas de batches - 2026-06-14)

**Progresso significativo na redução de bridge aggregate:**
- Mais de 10 stages já migrados do agregado amplo `IActivityEntryRuntimeBridge` para contratos estreitos (Identity + Fact + domain-specific).
- Seam `BindEntryPipeline` completamente removido.
- Compilação bem-sucedida após correção de resíduos de batches parciais (confirmado em fulllog.txt).

**Strengths (atualizado):**
- Contracts folder hygiene bom.
- `ActivityEntryPipeline` concreto.
- Muitos stages granulares + histórico rico de audits SA-7B/SA-8A/SA-13+.
- Redução concreta da proliferação de bridges (o maior gap identificado no audit original).
- Pipeline/README e este plano mantidos atualizados.

**Gaps ainda relevantes (prioridade para próximos batches):**
- Estágios complexos que usam mais do bridge (Content unload/release, ObjectSetup composite, full ParticipantBinding, ExitActorTeardown).
- Host ainda não thin.
- Coordinators/runners genéricos.
- Risco de múltiplos writers em runtime states (especialmente content release).
- Documentação consolidada (Current-Status.md) precisa ser mantida viva.

**Reference documents:**
- Auditoria completa de 2026-06-14 (com tabela detalhada de findings).
- Este plano (com log de execução atualizado).
- Pipeline/README.md (com baseline de smoke e histórico de cortes).

**Strengths:**
- Contracts folder hygiene is good (no deprecated stubs like we removed from Operational).
- `ActivityEntryPipeline` is already a concrete class.
- Many granular stages exist.
- Rich history of targeted audits (SA-7B* bridge work, SA-8A* exit policy/route-exit, SA-13/14/17/18 series).
- Pipeline/README is candid.

**Main gaps (same class of problems fixed in Operational):**
- Bridge proliferation: explicit "Ponte transitória SA-7B0", huge `IActivityEntryRuntimeBridge` aggregate, `BindEntryPipeline` seam still active.
- `SessionActivityPipeline` implements too many bridges and acts as god object.
- `SessionActivityHost` is not yet thin.
- Generic coordinators/runners (`ActivityCapabilityInventoryCoordinator`, `PendingOperationRunner`).
- Incomplete split between macro pipeline and entry surface.
- Multiple runtime states with writer-ownership risk (content release, object exit, inventory).
- Missing consolidated status report (`Docs/Reports/SessionActivity-2.0-Current-Status.md` referenced but absent).
- Some high-risk areas deferred in prior closures (movement retained/control, ActivityContentReleaseRuntimeState).

**Reference documents:**
- The full audit (linked above) contains the detailed findings table and the list of 6 items that require explicit "put on canonical track" resolution.

---

## 3. Phased Plan

### Phase 0 — Foundations & Documentation (Low risk, high value, do first)

**SA-19A0 — Documentation Baseline**
- Create the missing `Docs/Reports/SessionActivity-2.0-Current-Status.md` (or decide it lives elsewhere and fix all references).
- Update `SessionActivity/Pipeline/README.md` to point to:
  - This plan
  - The 2026-06-14 audit
  - The new Current-Status report
- Add a short "Normalization Plan" section to the Pipeline README summarizing the phases below.
- Record any doc-only removals/updates in `REMOVED_FILES.txt` if applicable.
- **Acceptance:** All internal references are consistent. No broken "see the status report" links.

**SA-19A1 — Governance Refresh**
- Add a one-paragraph "Before starting any SA-19+ activity" box at the top of this plan (or in a new `Docs/Guides/How-To-Work-on-SessionActivity-Normalization.md`).
- The box must require re-reading the audit + answering the 10 anti-deslocamento questions.
- Update the existing audit document with a "Plan Adoption" note once this plan is approved.

**Quick win sub-activity:** Re-scan for any remaining `*.cs~` or obvious editor artifacts across SessionActivity (we cleaned 7 in the audit pass).

---

### Phase 1 — Bridge Surface Reduction & Entry Runtime Split (Core of SA-13 + SA-7B continuation)

This is the highest-priority structural work. It directly mirrors the contract + recorder cleanup we did in Operational.

**SA-19B0 — Audit & Freeze the Current Aggregate Bridge Surface**
- Produce a small focused audit note (e.g. `SessionActivity/Docs/Audits/SA-19B0-Bridge-Surface-Freeze.md`).
- Inventory every usage of `IActivityEntryRuntimeBridge` and the sub-bridges.
- Clearly mark the current aggregate as **transitional only**.
- Decide the target narrow contracts per major concern (Content, Actor Setup, Binding, Inventory, Permission, Snapshot, Release, etc.).
- **Acceptance:** Matrix of "current bridge" → "target narrow port" + owner (EntryPipeline or specific stage).

**SA-19B1 — Remove or Obsolete the BindEntryPipeline Seam**
- Goal: `ActivityEntryPipeline` can be constructed by the CompositionInstaller (or a dedicated small factory) with only the narrow ports it actually needs. The macro `SessionActivityPipeline` no longer needs to be the giant "endpoint" passed everywhere.
- Remove the `BindEntryPipeline` method and the call in the installer.
- Update the composition to inject the EntryPipeline with proper narrow dependencies.
- Update any places that still reach back through the macro pipeline for entry concerns.
- **Acceptance:** 
  - `BindEntryPipeline` no longer exists or is only a no-op stub with clear deprecation.
  - EntryPipeline ctor is smaller and talks only to narrow contracts.
  - Smoke passes (same baseline + explicit check for entry preparation logs).

**SA-19B2 — Stage-by-Stage Bridge Narrowing (em andamento / major waves concluídas)**
- **Status:** Múltiplas ondas concluídas com ~10+ stages migrados com sucesso (ver execution log no topo). Compilação validada.
- Prioridade atual: Estágios de baixo risco já feitos. Agora focar nos complexos (Content unload/dispatch/release, ObjectSetupStages composite, ParticipantBinding completo, ExitActorTeardown).
- Para cada stage/grupo:
  - Substituir o parâmetro do bridge amplo pelas interfaces estreitas específicas que ele realmente usa.
  - Mover lógica de "cast para sub-bridge" para o narrow port ou para o próprio EntryPipeline.
  - Produzir nota curta de fechamento por batch (ex: SA-19B2-Gate, SA-19B2-Unregister, etc.).
- **Acceptance por batch:** Nenhum novo bridge genérico introduzido; o stage depende só do que precisa; smoke + ownership matrix atualizados.
- **Nota de higiene:** Após batches parciais, foi necessário um passo de correção de "leftovers" (nomes 'endpoint' residuais e call sites desatualizados) para restaurar compilação (ver fulllog.txt). Este padrão deve ser evitado em futuras ondas — batches devem ser atômicos por arquivo/stage.

**SA-19B3 — Reduce Bridge Implementations on SessionActivityPipeline**
- As stages stop needing the big aggregate, remove the corresponding `I*RuntimeBridge` implementations from the macro `SessionActivityPipeline`.
- Keep only the ones that truly belong at macro level (handoff, route-exit decision, snapshot provider for Operational, pending operation callback at macro scope).
- **Acceptance:** `SessionActivityPipeline` class declaration and interface list visibly smaller. Clear comment explaining what remains and why.

---

### Phase 2 — Host Thinness & Route-Exit Ownership Finalization (Continuation of SA-8A series)

**SA-19C0 — Host Surface Audit**
- Small audit: list every responsibility currently in `SessionActivityHost` (composition, QA observe, guards, public pipeline exposure, boundary implementations).
- Classify each as: must-stay-on-Host (MonoBehaviour lifetime, inspector fields for catalog), must-move-to-Pipeline, or must-be-removed (autoStart in non-QA, etc.).
- **Acceptance:** Clear matrix + proposal for the thin shape.

**SA-19C1 — Make Host a Thin Delegator**
- Host should only:
  - Hold the catalog + sessionStateId for composition.
  - Implement the two Operational boundaries (`ISessionActivityRouteExitTeardownBoundary`, `ISessionActivityVisualReadinessBoundary`) by delegating to the pipeline.
  - Expose minimal read-only state for debug/QA panels (if needed).
- Remove or mark as non-canonical: public `Pipeline` property for general use, heavy Update logic, etc.
- Move any remaining decision logic into `SessionActivityPipeline`.
- Update the Operational handoff adapters if they rely on anything beyond the two boundary interfaces.
- **Acceptance:** Host file is dramatically smaller. All route-exit and readiness decisions are visibly inside the pipeline. Smoke (especially RouteExitBackToMenu) passes.

**SA-19C2 — Lock Route-Exit Teardown Owner**
- Confirm (or finish confirming from SA-8A3) that `SessionActivityPipeline` is the single owner of the teardown decision and ordering.
- Host and any other component are only executors via the boundary.
- Produce a closure note updating the previous SA-8A audits.

---

### Phase 3 — Remove or Narrow Generic Coordinators & Runners

**SA-19D0 — Inventory Coordinator**
- Audit current usage of `ActivityCapabilityInventoryCoordinator`.
- Options (choose one and put on canonical track):
  A. Turn it into a pure policy object (no state, no lifecycle) injected narrowly where needed.
  B. Eliminate it — push the coordination into explicit stages + the inventory runtime state.
  C. Keep as very narrow technical helper inside `ActivityEntryPipeline` only, with clear justification.
- **Acceptance:** The chosen shape has a one-sentence canonical justification. No new god object created.

**SA-19D1 — Pending Operation Runner**
- Confirm its role is purely technical dispatch for window scenes and content operations (not lifecycle owner).
- Ensure it does not decide when operations run or what the next phase is (that stays in EntryPipeline or the macro pipeline).
- If it currently holds state that should be in `ActivityContentRuntimeState` or a dedicated loaded-set store, move it.
- **Acceptance:** Runner is documented as "infrastructure adapter/runner" (like Operational's LoadingAdapter or FadeAdapter). Clear boundary.

---

### Phase 4 — Runtime State Ownership & Writer Uniqueness (Finish the SA-7B / SA-17 work)

**SA-19E0 — State Writer Audit (remaining high-risk areas)**
- Focus on:
  - `ActivityContentReleaseRuntimeState`
  - Movement retained/control targets
  - Any remaining places where entry stages or the macro pipeline rebuild or overwrite state that another component should own.
- For each, document the single canonical writer + readers.
- **Acceptance:** Updated matrix in the Current-Status report or a new small audit note. No consumer is also a writer except at the defined owner.

**SA-19E1 — Lock Snapshot & Inventory Writer Rules**
- Re-confirm (for the current `CurrentActivityObjectSnapshot` scope) that `ActivityObjectSnapshotCaptureStage` + `ActivityObjectReleaseStage` + the object exit state are the writers.
- Ensure `RouteActivitySave` (via Operational) only consumes the payload produced by the proper path.
- Any future expansion to "RouteAndActivitySaveContributors" must go through the same single-writer discipline.

---

### Phase 5 — Macro Pipeline Surface Hygiene (reduce god-object surface without creating new layers)

**SA-19F0 — Callback & Bridge Hygiene on the Macro Pipeline**
- After Phase 1, re-audit what the `SessionActivityPipeline` still implements or holds for entry concerns.
- Move any remaining entry-specific callbacks into the EntryPipeline or into narrow adapters/endpoints owned by the relevant domain (Actors, CameraPresentation, etc.).
- Keep the macro pipeline as the place that *orchestrates* the phases and protects foreign/stale, not the place that *executes* entry details.

**SA-19F1 — Pending Navigation / Internal Transition State**
- Review the various `Pending*Transition` fields and structs.
- Make sure they are only carriers of resolved intent (Command-like), not mini state machines that bypass the stage order.

---

### Phase 6 — Documentation, Evidence & Closure

**SA-19Z0 — Consolidated Status Report**
- Maintain `Docs/Reports/SessionActivity-2.0-Current-Status.md` (or the chosen canonical location) with:
  - Current canonical maturity per area (Entry, Macro, RouteExit, Snapshot/RouteActivitySave, Host, etc.).
  - List of open SA-19 activities with status.
  - Smoke baseline results.
  - Ownership matrix summary.

**SA-19Z1 — Plan & Audit Maintenance**
- After each major activity, update this plan (mark completed, add new sub-activities if discovered).
- Update the 2026-06-14 audit with "Follow-up" notes.
- When a wave of activities reaches a checkpoint (e.g. "Bridge Surface significantly reduced"), produce a "PASS funcional + PASS arquitetural parcial" closure note (following the style of SA-14B1, SA-17 series).

**SA-19Z2 — Final Validation Wave**
- Full smoke on all three canonical scenarios + any new scenarios introduced.
- Re-run the "Validate SceneFlow Config" if applicable.
- Confirm no new foreign/stale issues, no FATALs from missing narrow contracts, no fallback silencioso.

---

## 4. Prioritization Guidance

**Priorização atualizada (pós-ondas B2 + correção de compilação):**

**Já concluído (fazer referência para histórico):**
- Phase 0 (documentação + governança)
- SA-19B0 (freeze + inventário)
- SA-19B1 (remoção real do Bind seam)
- Múltiplas ondas de SA-19B2 (10+ stages de baixo/médio risco estreitados + passo de higiene para compilar)

**Próxima prioridade imediata (alto leverage):**
- Concluir SA-19B2 com os estágios complexos restantes (Content unload/release, ObjectSetup composite, ParticipantBinding, Teardown). Auditoria rápida primeiro para identificar quais sub-interfaces exatas cada um precisa.
- Fase 2: Host thinness + Route-Exit ownership (SA-19C0/C1/C2) — começa com audit pequeno do Host.

**Médio/alto risco (auditar antes):**
- Qualquer coisa tocando movement retained/control ou ActivityContentReleaseRuntimeState.
- Mudanças grandes em como snapshots são capturados para RouteActivitySave.
- Redução de implementações de bridges no macro `SessionActivityPipeline` (SA-19B3) — só depois de B2 mais completo.

**Defer (a menos que haja regressão concreta):**
- Itens já marcados como DEFER_HIGH_RISK no Pipeline/README.
- Progression genérico completo (ainda placeholder).

**Governança reforçada:** Todo batch futuro deve ser "atómico por stage" + teste de compilação local antes de commit, para evitar leftovers como os que exigiram o passo de correção recente.

---

## 5. How to Start an Activity (Mandatory Process)

1. Read the latest version of:
   - This plan
   - The 2026-06-14 audit
   - ADR-2.0-0002
   - `SessionActivity/Pipeline/README.md`
2. Answer the 10 anti-deslocamento questions (write them in a small note or the activity's audit file).
3. Create or extend a focused SA-19X audit note (even if small).
4. Implement the narrowest possible change that moves ownership in the right direction.
5. Validate with the smoke baseline + ownership matrix.
6. Update this plan and the Current-Status report.
7. Record any deletions in `REMOVED_FILES.txt`.

---

## 6. Success Criteria for the Whole Plan

When this plan is considered largely complete:
- No more "ponte transitória" comments without a removal date or promotion path.
- `SessionActivityPipeline` only implements interfaces that truly belong at the macro + handoff + route-exit level.
- `ActivityEntryPipeline` + stages are constructible with narrow, stable contracts.
- `SessionActivityHost` is obviously a thin MonoBehaviour boundary for Operational.
- No generic Coordinator/Runner owns policy or lifecycle decisions.
- Every runtime state/snapshot has a single documented canonical writer.
- All internal documentation (README, status report, this plan) is consistent and points to each other.
- Smoke baseline passes cleanly on the three canonical scenarios.

---

## 7. Open Questions / Decisions Needed Before Execution

- Where exactly should the consolidated `SessionActivity-2.0-Current-Status.md` live? (Docs/Reports/ vs inside SessionActivity/Docs/)
- Naming convention for the new narrow ports that will replace pieces of `IActivityEntryRuntimeBridge` (e.g. `IActivityEntryContentPort`, `IEntryActorSetupPort`, etc.).
- Whether the `PendingOperationRunner` should stay under SessionActivity or be pulled into a more cross-cutting infrastructure module later.
- Any team preference on order of Phase 1 batches (which stages first?).

---

**Fim do Plano (reorganizado)**

Este plano é intencionalmente incremental e dirigido por auditoria, exatamente como o trabalho que normalizou o SessionOperational. Ele constrói sobre (e não descarta) a série existente de audits SA-7B / SA-8A / SA-13+.

**Situação atual (resumo para referência rápida):**
- Fases 0 e 1 (B0 + B1 + várias ondas B2) **concluídas**.
- Compilação validada com sucesso (fulllog.txt).
- Redução concreta da proliferação de bridges aggregate.
- Próximo: finalizar B2 nos estágios complexos + Fase 2 (Host).

Se quiser que eu:
- Refinar alguma fase/atividade
- Começar o próximo batch específico (ex: ContentSceneUnload ou o composite ObjectSetup)
- Gerar uma lista Kanban simples ou grafo de dependências a partir deste plano
- Atualizar o Current-Status.md com o estado mais recente

É só falar. Vamos manter o ritmo até o plano estar em "largely complete".
## Checkpoint adicional — BASE-ID identity stabilization (2026-06-14)

Status: `CLOSED FOR NOW`.

A frente `BASE-ID` foi encerrada como estabilização complementar de identidade. Ela não substitui as fases SA-19 deste plano; apenas registra que a normalização de ownership de identities runtime chegou a um ponto seguro para não continuar abrindo cortes novos sem evidência.

### Resultado

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

### Decisão

`BASE-ID-1I — Type inventory lookup keys` fica adiado. A auditoria concluiu que o lookup técnico do `ActivityCapabilityInventory` ainda usa string, mas está centralizado e não é o risco principal agora.

O resíduo real está em scanners específicos que ainda podem usar `ownerPath` para owner identity funcional:

```text
ActivityCapabilityCameraTargetScanner
ActivityCapabilityActorPresentationScanner
ActivityCapabilityActorAttributeScanner
```

Esses itens ficam em backlog controlado. Só devem ser abertos como subcorte direto de `BASE-ID-1I` se houver regressão concreta ou decisão explícita de reduzir esse risco. Não abrir nova frente apenas para limpar logs ou metadata.

### Efeito no plano SA-19

A próxima prioridade volta para o plano maior já registrado: finalizar B2 nos estágios complexos e avançar Fase 2 somente com auditoria pequena, mantendo a regra de não inventar cortes fora do plano.
