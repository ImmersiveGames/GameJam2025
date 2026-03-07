# Audit Index (Docs Layout Normalization)

Date: 2026-03-06

## Canonical layout rules enforced
- Live module docs: `Docs/Modules/**`.
- Audit snapshots: `Docs/Reports/Audits/YYYY-MM-DD/**`.
- Single canonical audit summary: `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`.
- Excerpts: `Docs/Reports/Audits/2026-03-06/Excerpts/**`.

## Detected problems and resolution
- Duplicate summary role (`Docs/Module-Audit-Summary.md` + dated summary) resolved by moving the root excerpt to `Docs/Reports/Audits/2026-03-06/Excerpts/Module-Audit-Summary-Excerpt.md`.
- `Modules/` folder exists in two contexts (`Docs/Modules` live docs and `Docs/Reports/Audits/2026-03-06/Modules` snapshot audits); kept with explicit scope labels in this index.
- Live module docs outside Docs/Modules/: 0.
- Audit docs outside Docs/Reports/Audits/YYYY-MM-DD/: 0.

## Inventory table
| Path | Tipo | Data | Notas |
|---|---|---|---|
| Docs/ADRs/ADR-0005-GlobalCompositionRoot-Modularizacao.md | index | - |  |
| Docs/ADRs/ADR-0007-InputModes.md | index | - |  |
| Docs/ADRs/ADR-0008-RuntimeModeConfig.md | index | - |  |
| Docs/ADRs/ADR-0009-FadeSceneFlow.md | index | - |  |
| Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md | index | - |  |
| Docs/ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md | index | - |  |
| Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md | index | - |  |
| Docs/ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md | index | - |  |
| Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md | index | - |  |
| Docs/ADRs/ADR-0015-Baseline-2.0-Fechamento.md | index | - |  |
| Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md | index | - |  |
| Docs/ADRs/ADR-0017-LevelManager-Config-Catalog.md | index | - |  |
| Docs/ADRs/ADR-0018-Fade-TransitionStyle-SoftFail.md | index | - |  |
| Docs/ADRs/ADR-0019-Navigation-IntentCatalog.md | index | - |  |
| Docs/ADRs/ADR-0020-LevelContent-Progression-vs-SceneRoute.md | index | - |  |
| Docs/ADRs/ADR-0021-Baseline-3.0-Completeness.md | index | - |  |
| Docs/ADRs/ADR-0022-Assinaturas-e-Dedupe-por-Dominio-MacroRoute-vs-Level.md | index | - |  |
| Docs/ADRs/ADR-0023-Dois-Niveis-de-Reset-MacroReset-vs-LevelReset.md | index | - |  |
| Docs/ADRs/ADR-0024-LevelCatalog-por-MacroRoute-e-Contrato-de-Selecao-de-Level-Ativo.md | index | - |  |
| Docs/ADRs/ADR-0025-Pipeline-de-Loading-Macro-inclui-Etapa-de-Level-antes-do-FadeOut.md | index | - |  |
| Docs/ADRs/ADR-0026-Troca-de-Level-Intra-Macro-via-Swap-Local-sem-Transicao-Macro.md | index | - |  |
| Docs/ADRs/ADR-0027-IntroStage-e-PostLevel-como-Responsabilidade-do-Level.md | index | - |  |
| Docs/ADRs/ADR-TEMPLATE.md | index | - |  |
| Docs/ADRs/ADR-TEMPLATE-COMPLETENESS.md | index | - |  |
| Docs/ADRs/README.md | index | - |  |
| Docs/Canon/Canon-Index.md | index | - |  |
| Docs/CHANGELOG.md | index | - |  |
| Docs/Guides.md | index | - |  |
| Docs/Modules/GameLoop.md | module_doc | - |  |
| Docs/Modules/Gates-Readiness-StateDependent.md | module_doc | - |  |
| Docs/Modules/LevelFlow.md | module_doc | - |  |
| Docs/Modules/Navigation.md | module_doc | - |  |
| Docs/Modules/SceneFlow.md | module_doc | - |  |
| Docs/Modules/WorldLifecycle.md | module_doc | - |  |
| Docs/Overview/Overview.md | index | - |  |
| Docs/Overview/SceneFlow-Navigation-LevelFlow-Refactor-Plan.md | index | - |  |
| Docs/Plans/Architecture-Review-MacroRoutes-vs-Levels.md | index | - |  |
| Docs/Plans/Checklist.md | index | - |  |
| Docs/Plans/Plan-Continuous.md | index | - |  |
| Docs/Plans/Plan-Incremental-Baseline-3.0-MacroRoutes-Levels.md | index | - |  |
| Docs/Plans/Plan-MacroRoutes-vs-Levels-Flow.md | index | - |  |
| Docs/Plans/README.md | index | - |  |
| Docs/README.md | index | - |  |
| Docs/Reports/Audit-DataCleanup-Post-StringsToDirectRefs-v1.md | index | - |  |
| Docs/Reports/Audits/2026-02-17/Audit-Plan-ADR-Closure.md | audit_doc | 2026-02-17 |  |
| Docs/Reports/Audits/2026-02-17/Audit-SceneFlow-RouteResetPolicy.md | audit_doc | 2026-02-17 |  |
| Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md | audit_doc | 2026-02-17 |  |
| Docs/Reports/Audits/2026-02-18/Audit-ADR-0005-Closure.md | audit_doc | 2026-02-18 |  |
| Docs/Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md | audit_doc | 2026-02-18 |  |
| Docs/Reports/Audits/2026-02-19/ADR-Sync-Audit-Prompt.md | audit_doc | 2026-02-19 |  |
| Docs/Reports/Audits/2026-02-19/Audit-B3-F1b-MacroSignature-Reason.md | audit_doc | 2026-02-19 |  |
| Docs/Reports/Audits/2026-02-19/Audit-B3-F2a-LevelReset-Mechanism.md | audit_doc | 2026-02-19 |  |
| Docs/Reports/Audits/2026-02-19/Audit-B3-F2a-Touchpoints.md | audit_doc | 2026-02-19 |  |
| Docs/Reports/Audits/2026-02-19/Audit-Baseline3-F0-CurrentState.md | audit_doc | 2026-02-19 |  |
| Docs/Reports/Audits/2026-03-04/ADR-0022-0027-Code-Audit.md | audit_doc | 2026-03-04 |  |
| Docs/Reports/Audits/2026-03-05/H1-Hardening-Changes.md | audit_doc | 2026-03-05 |  |
| Docs/Reports/Audits/2026-03-05/Hygiene-CanonOnly-LevelFlow.md | audit_doc | 2026-03-05 |  |
| Docs/Reports/Audits/2026-03-05/Hygiene-CanonOnly-Report.md | audit_doc | 2026-03-05 |  |
| Docs/Reports/Audits/2026-03-05/Legacy-Compat-Fallback-Inventory.md | audit_doc | 2026-03-05 |  |
| Docs/Reports/Audits/2026-03-06/Audit-Index.md | index | 2026-03-06 |  |
| Docs/Reports/Audits/2026-03-06/Excerpts/Module-Audit-Summary-Excerpt.md | excerpt | 2026-03-06 |  |
| Docs/Reports/Audits/2026-03-06/GameLoop-Cleanup-Audit-v1.md | audit_doc | 2026-03-06 |  |
| Docs/Reports/Audits/2026-03-06/Gates-Readiness-StateDependent-Cleanup-Audit-v1.md | audit_doc | 2026-03-06 |  |
| Docs/Reports/Audits/2026-03-06/Infra-Composition-Cleanup-v1.md | audit_doc | 2026-03-06 |  |
| Docs/Reports/Audits/2026-03-06/LevelFlow-Cleanup-Audit-v1.md | audit_doc | 2026-03-06 |  |
| Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md | summary | 2026-03-06 |  |
| Docs/Reports/Audits/2026-03-06/Modules/ContentSwap.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/Modules/Core.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/Modules/DevQA.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/Modules/GameLoop.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/Modules/Gates-Readiness-StateDependent.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/Modules/Infrastructure-Composition.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/Modules/LevelFlow.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/Modules/Navigation.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/Modules/SceneFlow.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/Modules/WorldLifecycle.md | audit_doc | 2026-03-06 | Snapshot module audit (dated), not live module doc. |
| Docs/Reports/Audits/2026-03-06/WorldLifecycle-Cleanup-Audit-v1.md | audit_doc | 2026-03-06 |  |
| Docs/Reports/Audits/LATEST.md | index | - |  |
| Docs/Reports/Audit-StringsToDirectRefs-v1-Steps-01-02.md | index | - |  |
| Docs/Reports/Baseline/2026-03-06/Baseline-3.1-Freeze.md | baseline | 2026-03-06 |  |
| Docs/Reports/Baseline/2026-03-06/Doc-Freeze-Checklist.md | baseline | 2026-03-06 |  |
| Docs/Reports/Baseline/2026-03-06/doisResets-na-sequencia.txt | baseline | 2026-03-06 |  |
| Docs/Reports/Baseline/2026-03-06/lastlog.log | baseline | 2026-03-06 |  |
| Docs/Reports/Evidence/2026-01-29/Baseline-2.2-Evidence-2026-01-29.md | evidence | 2026-01-29 |  |
| Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md | evidence | 2026-01-31 |  |
| Docs/Reports/Evidence/2026-02-03/ADR-0017-LevelCatalog-Evidence-2026-02-03.md | evidence | 2026-02-03 |  |
| Docs/Reports/Evidence/2026-02-03/Baseline-2.2-Evidence-2026-02-03.md | evidence | 2026-02-03 |  |
| Docs/Reports/Evidence/2026-02-03/Baseline-2.2-Smoke-LastRun.log | evidence | 2026-02-03 |  |
| Docs/Reports/Evidence/ADR-0020-Evidence-ContentSwap-2026-02-18.log | evidence | 2026-02-18 |  |
| Docs/Reports/Evidence/ADR-0020-Evidence-LevelFlow-NTo1-2026-02-18.log | evidence | 2026-02-18 |  |
| Docs/Reports/Evidence/LATEST.md | evidence | - |  |
| Docs/Reports/Evidence/README.md | evidence | - |  |
| Docs/Reports/lastlog.log | evidence | - |  |
| Docs/Reports/SceneFlow-Config-Snapshot-DataCleanup-v1.md | index | - |  |
| Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md | index | - |  |
| Docs/Standards/Standards.md | index | - |  |
## Live vs Snapshot per module
| Module | Live doc (`Docs/Modules`) | Snapshot audit (`Docs/Reports/Audits/2026-03-06/Modules`) | Status |
|---|---|---|---|
| Core | - | `Docs/Reports/Audits/2026-03-06/Modules/Core.md` | Snapshot only |
| Infrastructure-Composition | - | `Docs/Reports/Audits/2026-03-06/Modules/Infrastructure-Composition.md` | Snapshot only |
| Gates-Readiness-StateDependent | `Docs/Modules/Gates-Readiness-StateDependent.md` | `Docs/Reports/Audits/2026-03-06/Modules/Gates-Readiness-StateDependent.md` | Duplicated by design (live + snapshot) |
| GameLoop | `Docs/Modules/GameLoop.md` | `Docs/Reports/Audits/2026-03-06/Modules/GameLoop.md` | Duplicated by design (live + snapshot) |
| SceneFlow | `Docs/Modules/SceneFlow.md` | `Docs/Reports/Audits/2026-03-06/Modules/SceneFlow.md` | Duplicated by design (live + snapshot) |
| WorldLifecycle | `Docs/Modules/WorldLifecycle.md` | `Docs/Reports/Audits/2026-03-06/Modules/WorldLifecycle.md` | Duplicated by design (live + snapshot) |
| Navigation | `Docs/Modules/Navigation.md` | `Docs/Reports/Audits/2026-03-06/Modules/Navigation.md` | Duplicated by design (live + snapshot) |
| LevelFlow | `Docs/Modules/LevelFlow.md` | `Docs/Reports/Audits/2026-03-06/Modules/LevelFlow.md` | Duplicated by design (live + snapshot) |
| ContentSwap | `Docs/Modules/ContentSwap.md` | `Docs/Reports/Audits/2026-03-06/Modules/ContentSwap.md` | Duplicated by design (live + snapshot) |
| DevQA | `Docs/Modules/DevQA.md` | `Docs/Reports/Audits/2026-03-06/Modules/DevQA.md` | Duplicated by design (live + snapshot) |

## CS-1.1 Status
- live doc: `Docs/Modules/ContentSwap.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/ContentSwap-Cleanup-Audit-v1.md`
- status: Duplicated by design (live + snapshot)


## DQ-1.1 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Cleanup-Audit-v1.md`
- status: Duplicated by design (live + snapshot)


## DQ-1.2 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Cleanup-Audit-v2.md`
- status: Duplicated by design (live + snapshot)

## SF-1.2a Status
- live doc: `Docs/Modules/SceneFlow.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/SceneFlow-Cleanup-Audit-v2.md`
- status: Inventory only (no code changes)

## SF-1.2b Status
- live doc: `Docs/Modules/SceneFlow.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/SceneFlow-Cleanup-Audit-v3.md`
- status: Hardening minimo aplicado (behavior-preserving)

## GRS-1.2 Status
- live doc: `Docs/Modules/Gates-Readiness-StateDependent.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Gates-Readiness-StateDependent-Cleanup-Audit-v2.md`
- status: Hardening minimo aplicado (behavior-preserving)

## DQ-1.3 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Cleanup-Audit-v3.md`
- status: Hardening/isolamento DevQA aplicado (behavior-preserving)
- Release excludes DevQA by compile guards; DevBuild required for QA harness.

## GL-1.2 Status
- live doc: `Docs/Modules/GameLoop.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/GameLoop-Cleanup-Audit-v2.md`
- status: Cleanup mÃ­nimo aplicado (behavior-preserving)
- Release excludes DevQA by compile guards; DevBuild required for QA harness.

## GL-1.3 Status
- live doc: `Docs/Modules/GameLoop.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/GameLoop-Cleanup-Audit-v3.md`
- status: Redundancy reduction applied (behavior-preserving)
- Release excludes DevQA by compile guards; DevBuild required for QA harness.

## SF-1.3b.2a Status
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/SceneFlow-Signature-Dedupe-Audit-v2.md`
- status: CODE consolidation applied (behavior-preserving)



## WL-1.2 Status
- live doc: `Docs/Modules/WorldLifecycle.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/WorldLifecycle-Cleanup-Audit-v2.md`
- status: V1/V2 publishers consolidated (behavior-preserving)

## CS-1.2 Status
- live doc: `Docs/Modules/ContentSwap.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/ContentSwap-Cleanup-Audit-v2.md`
- status: ContentSwap state event publishing centralized in ContextService (behavior-preserving)

## IC-1.3 Status
- snapshot: `Docs/Reports/Audits/2026-03-06/Infra-Composition-Cleanup-v2.md`
- status: Composition stage-module boilerplate removed; direct stage dispatch in pipeline (behavior-preserving)

## IC-1.4 Status
- live doc: `Docs/Reports/Audits/2026-03-06/Modules/Infrastructure-Composition.md` (snapshot-as-live for infra composition)
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Infra-SceneScope-Cleanup-Audit-v1.md`
- status: SceneScopeCompositionRoot split into partials (RunRearm + DevQA) behavior-preserving

## GP-1.1 Status
- live doc: `Docs/Modules/Gameplay.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Gameplay-Cleanup-Audit-v1.md`
- status: Gameplay runtime/dev/editor boundaries audited; orphan Editor tooling moved to Legacy (behavior-preserving)



## PG-1.1 Status
- live doc: `Docs/Modules/PostGame.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/PostGame-Cleanup-Audit-v1.md`
- status: DevQA isolation applied in PostGame overlay via partial split (behavior-preserving)
- notes: runtime contracts/timeline preserved; release excludes DevQA by compile guards.

## PA-1.1 Status
- live doc: `Docs/Modules/GameLoop.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Pause-Cleanup-Audit-v1.md`
- status: DevQA isolation applied in PauseOverlayController via partial split (behavior-preserving)
- notes: runtime file preserved in place (no move), no UnityEditor/ContextMenu leakage in runtime bindings.

## DQ-1.4 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`
- status: Leak sweep executed; one embedded DevQA block extracted via partial (`TransitionStyleCatalogAsset`), remaining runtime suspects classified A/C.

## DQ-1.4.1 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`
- status: `SceneRouteDefinitionAsset` leak extraído para parcial DevQA; runtime sem símbolos Editor.

## DQ-1.4.2 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`
- status: `GameNavigationCatalogAsset` runtime leak de UnityEditor isolado em `Modules/Navigation/Dev/GameNavigationCatalogAsset.DevQA.cs`.

## DQ-1.4.3 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`
- status: `SceneRouteResetPolicy` runtime leak isolado em `Modules/WorldLifecycle/Dev/SceneRouteResetPolicy.DevQA.cs`.

## DQ-1.4.4+ Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`
- status: batch leak sweep aplicado em 6 runtimes com extração para DevQA partials; global sweep fora de `Dev/Editor/Legacy` com 0 matches para tokens de Editor/DevQA.

## DQ-1.5 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v2.md`
- status: DONE - Editor API leak sweep full-fix applied; QA tooling moved to `Editor/QA`; runtime files cleaned via partial editor hooks.
