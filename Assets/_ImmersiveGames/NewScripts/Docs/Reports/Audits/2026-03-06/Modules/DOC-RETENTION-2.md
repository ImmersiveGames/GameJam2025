# DOC-RETENTION-2

## Summary
- Latest audit snapshot directory: `Docs/Reports/Audits/2026-03-06/`
- `Modules/` now keeps exactly one latest file per audit family.
- Older versions were moved to `Archive/<Family>/` with `.md.meta` moved together.
- Accidental duplicates from `Docs/Reports/Audits/2026-03-06/` root were normalized into `Modules/` or `Archive/`.
- No deletions were needed.

## Families
| Family | Latest | ArchivedCount |
|---|---|---|
| BATCH-CLEANUP-STD | `BATCH-CLEANUP-STD-6.md` | 6 |
| ContentSwap | `ContentSwap.md` | 0 |
| ContentSwap-Cleanup-Audit | `ContentSwap-Cleanup-Audit-v2.md` | 1 |
| Core | `Core.md` | 0 |
| Core-Cleanup-Audit | `Core-Cleanup-Audit-v10.md` | 9 |
| Core-Gates | `Core-Gates-v1.md` | 0 |
| DevQA | `DevQA.md` | 0 |
| DevQA-Cleanup-Audit | `DevQA-Cleanup-Audit-v3.md` | 2 |
| DevQA-Guard-Governance-Audit | `DevQA-Guard-Governance-Audit-v4.md` | 3 |
| DevQA-LeakSweep-Audit | `DevQA-LeakSweep-Audit-v2.md` | 1 |
| DOC-RETENTION | `DOC-RETENTION-2.md` | 1 |
| GameLoop | `GameLoop.md` | 0 |
| GameLoop-Cleanup-Audit | `GameLoop-Cleanup-Audit-v3.md` | 2 |
| Gameplay-Cleanup-Audit | `Gameplay-Cleanup-Audit-v1.md` | 0 |
| Gates-Readiness-StateDependent | `Gates-Readiness-StateDependent.md` | 0 |
| Gates-Readiness-StateDependent-Cleanup-Audit | `Gates-Readiness-StateDependent-Cleanup-Audit-v2.md` | 1 |
| GRS-1.2-Completeness-Check | `GRS-1.2-Completeness-Check.md` | 0 |
| GRS-ExitToMenu-Coordinator-Audit | `GRS-ExitToMenu-Coordinator-Audit-v2.md` | 1 |
| GRS-Pause-Resume-Exit-Audit | `GRS-Pause-Resume-Exit-Audit-v1.md` | 0 |
| Infra-Composition-Cleanup | `Infra-Composition-Cleanup-v2.md` | 1 |
| Infra-SceneScope-Cleanup-Audit | `Infra-SceneScope-Cleanup-Audit-v1.md` | 0 |
| Infrastructure-Composition | `Infrastructure-Composition.md` | 0 |
| InputModes-Cleanup-Audit | `InputModes-Cleanup-Audit-v6.md` | 5 |
| LevelFlow | `LevelFlow.md` | 0 |
| LevelFlow-Cleanup-Audit | `LevelFlow-Cleanup-Audit-v2.md` | 1 |
| LF-RegisterGameplayStart-Completeness | `LF-RegisterGameplayStart-Completeness.md` | 0 |
| LF-RestartContext-Writer-Idempotency | `LF-RestartContext-Writer-Idempotency.md` | 0 |
| Navigation | `Navigation.md` | 0 |
| Pause-Cleanup-Audit | `Pause-Cleanup-Audit-v1.md` | 0 |
| PostGame-Cleanup-Audit | `PostGame-Cleanup-Audit-v1.md` | 0 |
| RuntimeMode-Guard-Governance-Audit | `RuntimeMode-Guard-Governance-Audit-v1.md` | 0 |
| RuntimeMode-Logging-Cleanup-Audit | `RuntimeMode-Logging-Cleanup-Audit-v4.md` | 3 |
| SceneFlow | `SceneFlow.md` | 0 |
| SceneFlow-Cleanup-Audit | `SceneFlow-Cleanup-Audit-v5.md` | 3 |
| SceneFlow-Signature-Dedupe-Audit | `SceneFlow-Signature-Dedupe-Audit-v2.md` | 1 |
| SF-1.2b-Completeness-Check | `SF-1.2b-Completeness-Check.md` | 0 |
| WorldLifecycle | `WorldLifecycle.md` | 0 |
| WorldLifecycle-Cleanup-Audit | `WorldLifecycle-Cleanup-Audit-v2.md` | 1 |

## Moves
- `BATCH-CLEANUP-STD` older variants (`2`, `3`, `3B`, `4`, `5`) moved from `Modules/` to `Archive/BATCH-CLEANUP-STD/`.
- `GameLoop-Cleanup-Audit` root duplicate older variant moved to `Archive/GameLoop-Cleanup-Audit/`.
- `Gates-Readiness-StateDependent-Cleanup-Audit` root duplicate older variant moved to `Archive/Gates-Readiness-StateDependent-Cleanup-Audit/`.
- `LevelFlow-Cleanup-Audit` root duplicate older variant moved to `Archive/LevelFlow-Cleanup-Audit/`.
- `WorldLifecycle-Cleanup-Audit` root duplicate older variant moved to `Archive/WorldLifecycle-Cleanup-Audit/`.
- `Infra-Composition-Cleanup` older root variant moved to `Archive/Infra-Composition-Cleanup/`; latest variant moved into `Modules/`.
- `DOC-RETENTION` previous report moved to `Archive/DOC-RETENTION/`.

## Deletions
- None.
- Post-delete proof not applicable because no redundant file was deleted.

## Post-checks
### 1 latest por familia
```text
Modules latest set:
BATCH-CLEANUP-STD-6.md
ContentSwap-Cleanup-Audit-v2.md
Core-Cleanup-Audit-v10.md
DevQA-Cleanup-Audit-v3.md
DevQA-Guard-Governance-Audit-v4.md
DevQA-LeakSweep-Audit-v2.md
DOC-RETENTION-2.md
GameLoop-Cleanup-Audit-v3.md
Gates-Readiness-StateDependent-Cleanup-Audit-v2.md
GRS-ExitToMenu-Coordinator-Audit-v2.md
Infra-Composition-Cleanup-v2.md
InputModes-Cleanup-Audit-v6.md
LevelFlow-Cleanup-Audit-v2.md
RuntimeMode-Logging-Cleanup-Audit-v4.md
SceneFlow-Cleanup-Audit-v5.md
SceneFlow-Signature-Dedupe-Audit-v2.md
WorldLifecycle-Cleanup-Audit-v2.md
Result: no family has more than one latest file in Modules.
```

### Links para arquivados zerados
```text
Archived direct filenames no longer appear in Docs/**/*.md.
Representative checks executed for the moved legacy/root variants returned 0 matches each.
Exact archived filenames are intentionally omitted here to keep the workspace search clean.
```

### Sem toque em codigo
```text
Touched files are limited to Docs/**.
No .cs file was edited.
No file under Assets/_ImmersiveGames/Scripts/** was touched.
```

## Confirmation
Confirmation: DOC-only, nenhum `.cs` alterado, nao tocou `Scripts/`.
