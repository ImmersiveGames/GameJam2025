# Module Audit Summary - 2026-03-06

## Status
- `DOC-RETENTION-2`: DONE
- `EDITOR-QA-1`: DONE
- `EDITOR-QA-2`: DONE
- `EDITOR-QA-3`: DONE
- `EDITOR-QA-4`: DONE
- `EDITOR-QA-5`: DONE
- Snapshot root: `Docs/Reports/Audits/2026-03-06/Modules/`
- History root: `Docs/Reports/Audits/2026-03-06/Archive/`
- Scope: docs-only retention cleanup under `Docs/**`

## Canonical pointers
- Live module docs: `Docs/Modules/**`
- Shared live docs: `Docs/Shared/**`
- Latest audit report index: `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- Retention report: `Docs/Reports/Audits/2026-03-06/Modules/DOC-RETENTION-2.md`
- Editor/QA report: `Docs/Reports/Audits/2026-03-06/Modules/EDITOR-QA-1.md`
- Editor/QA follow-up: `Docs/Reports/Audits/2026-03-06/Modules/EDITOR-QA-2.md`
- Editor/QA prune verification: `Docs/Reports/Audits/2026-03-06/Modules/EDITOR-QA-3.md`
- Editor/QA consolidation: `Docs/Reports/Audits/2026-03-06/Modules/EDITOR-QA-4.md`
- Editor/QA unblock A2 + prune: `Docs/Reports/Audits/2026-03-06/Modules/EDITOR-QA-5.md`

## Editor/QA
- `EDITOR-QA-1`: inventory + prune + canonical menu audit completed inside the strict editable scope.
- Result in scope: no editable `[MenuItem]` definitions, no top-level `QA/**` directory, no top-level `Dev/**` tooling `.cs`, and no obsolete editor tooling eligible for safe prune.
- Kept canonical editor hooks:
  - `Editor/Core/Events/EventBusUtil.Editor.cs`
  - `Editor/Core/Logging/HardFailFastH1.Editor.cs`
  - `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs`
- Required checks passed:
  - runtime leak sweep outside `Dev/Editor/QA/Legacy`: `0 matches`
  - gates: `PASS Gate A`, `PASS Gate A2`, `PASS Gate B`
- Out-of-scope debt from `EDITOR-QA-1` was partially retired in `EDITOR-QA-2`.
- `EDITOR-QA-2` result: editor-only menu tooling moved to canonical `Editor/**` module paths, obsolete `DataCleanup v1` tools pruned/archived, menu roots normalized, and runtime leak sweep stayed on the documented allowlist only.
- Net result: `-2` `.cs` in scope, with `PASS Gate A`, `PASS Gate A2`, `PASS Gate B`.
- Follow-up report: `Docs/Reports/Audits/2026-03-06/Modules/EDITOR-QA-2.md`.
- `EDITOR-QA-3` re-verified the canonical replacements, kept the same safe deletions, and re-ran uniqueness + gates with PASS/PASS/PASS.
- `EDITOR-QA-4` completed the remaining naming consolidation in SceneFlow editor tooling and confirmed no new safe delete/move candidate exists in the current editor-only scope.
- `EDITOR-QA-5` confirmed `A2` was already clear, then merged the SceneFlow editor ID-source support types into the drawer base and removed three standalone editor-only `.cs` files.

## Families
| Family | Latest | History |
|---|---|---|
| BATCH-CLEANUP-STD | `Docs/Reports/Audits/2026-03-06/Modules/BATCH-CLEANUP-STD-6.md` | `Docs/Reports/Audits/2026-03-06/Archive/BATCH-CLEANUP-STD/` |
| ContentSwap-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/ContentSwap-Cleanup-Audit-v2.md` | `Docs/Reports/Audits/2026-03-06/Archive/ContentSwap-Cleanup-Audit/` |
| Core-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v10.md` | `Docs/Reports/Audits/2026-03-06/Archive/Core-Cleanup-Audit/` |
| DevQA-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Cleanup-Audit-v3.md` | `Docs/Reports/Audits/2026-03-06/Archive/DevQA-Cleanup-Audit/` |
| DevQA-Guard-Governance-Audit | `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v4.md` | `Docs/Reports/Audits/2026-03-06/Archive/DevQA-Guard-Governance-Audit/` |
| DevQA-LeakSweep-Audit | `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v2.md` | `Docs/Reports/Audits/2026-03-06/Archive/DevQA-LeakSweep-Audit/` |
| DOC-RETENTION | `Docs/Reports/Audits/2026-03-06/Modules/DOC-RETENTION-2.md` | `Docs/Reports/Audits/2026-03-06/Archive/DOC-RETENTION/` |
| GameLoop-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/GameLoop-Cleanup-Audit-v3.md` | `Docs/Reports/Audits/2026-03-06/Archive/GameLoop-Cleanup-Audit/` |
| Gates-Readiness-StateDependent-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/Gates-Readiness-StateDependent-Cleanup-Audit-v2.md` | `Docs/Reports/Audits/2026-03-06/Archive/Gates-Readiness-StateDependent-Cleanup-Audit/` |
| GRS-ExitToMenu-Coordinator-Audit | `Docs/Reports/Audits/2026-03-06/Modules/GRS-ExitToMenu-Coordinator-Audit-v2.md` | `Docs/Reports/Audits/2026-03-06/Archive/GRS-ExitToMenu-Coordinator-Audit/` |
| Infra-Composition-Cleanup | `Docs/Reports/Audits/2026-03-06/Modules/Infra-Composition-Cleanup-v2.md` | `Docs/Reports/Audits/2026-03-06/Archive/Infra-Composition-Cleanup/` |
| InputModes-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/InputModes-Cleanup-Audit-v6.md` | `Docs/Reports/Audits/2026-03-06/Archive/InputModes-Cleanup-Audit/` |
| LevelFlow-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/LevelFlow-Cleanup-Audit-v2.md` | `Docs/Reports/Audits/2026-03-06/Archive/LevelFlow-Cleanup-Audit/` |
| RuntimeMode-Logging-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Logging-Cleanup-Audit-v4.md` | `Docs/Reports/Audits/2026-03-06/Archive/RuntimeMode-Logging-Cleanup-Audit/` |
| SceneFlow-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/SceneFlow-Cleanup-Audit-v5.md` | `Docs/Reports/Audits/2026-03-06/Archive/SceneFlow-Cleanup-Audit/` |
| SceneFlow-Signature-Dedupe-Audit | `Docs/Reports/Audits/2026-03-06/Modules/SceneFlow-Signature-Dedupe-Audit-v2.md` | `Docs/Reports/Audits/2026-03-06/Archive/SceneFlow-Signature-Dedupe-Audit/` |
| WorldLifecycle-Cleanup-Audit | `Docs/Reports/Audits/2026-03-06/Modules/WorldLifecycle-Cleanup-Audit-v2.md` | `Docs/Reports/Audits/2026-03-06/Archive/WorldLifecycle-Cleanup-Audit/` |

## Notes
- Live-vs-snapshot pairs such as `Docs/Modules/WorldLifecycle.md` and `Docs/Reports/Audits/2026-03-06/Modules/WorldLifecycle.md` remain duplicated by design.
- Accidental duplicates that were previously in `Docs/Reports/Audits/2026-03-06/` root were normalized so that only the latest stays in `Modules/` and older versions stay under family archive folders.


