# Docs Consolidation - Baseline 4.0

## Objective

Clean the `Docs` tree after Baseline 4.0 stabilization so the current canon is obvious, active entry points are easy to find, and superseded material lives under archive only.

## Canonical Set Kept

| Path | Type | Reason |
|---|---|---|
| `Docs/ADRs/ADR-0001-Glossario-Fundamental-Contextos-e-Rotas-v2.md` | ADR | glossary and domain spine for the current canon |
| `Docs/ADRs/ADR-0043-Ancora-de-Decisao-para-o-Baseline-4.0.md` | ADR | decision anchor for Baseline 4.0 |
| `Docs/ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md` | ADR | canonical architecture reference |
| `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md` | Plan | canonical target architecture |
| `Docs/Plans/Plan-Baseline-4.0-Execution-Guardrails.md` | Plan | operational format and acceptance rules |

## Active Set Kept

| Path | Type | Reason |
|---|---|---|
| `Docs/README.md` | Guide | current entry point for the trimmed docs tree |
| `Docs/ADRs/README.md` | Index | active ADR navigation and precedence note |
| `Docs/Guides/Production-How-To-Use-Core-Modules.md` | Guide | still useful operational entry |
| `Docs/Guides/Event-Hooks-Reference.md` | Guide | current hook reference |
| `Docs/Guides/How-To-Add-A-New-Module-To-Composition.md` | Guide | still useful for future module work |
| `Docs/Modules/GameLoop.md` | Module | active runtime reference |
| `Docs/Modules/Gameplay.md` | Module | active runtime reference |
| `Docs/Modules/InputModes.md` | Module | active runtime reference |
| `Docs/Modules/LevelFlow.md` | Module | active runtime reference |
| `Docs/Modules/Navigation.md` | Module | active runtime reference |
| `Docs/Modules/PostGame.md` | Module | active runtime reference |
| `Docs/Modules/ResetInterop.md` | Module | active runtime reference |
| `Docs/Modules/SceneFlow.md` | Module | active runtime reference |
| `Docs/Modules/SceneReset.md` | Module | active runtime reference |
| `Docs/Modules/WorldReset.md` | Module | active runtime reference |
| `Docs/Reports/Audits/LATEST.md` | Audit index | current audit entry point |
| `Docs/Reports/Audits/2026-03-29/Baseline-4.0-Residual-Housekeeping-Audit.md` | Audit | current residual housekeeping reference |
| `Docs/Reports/Audits/2026-03-30/Docs-Consolidation-Baseline-4.0.md` | Audit | this consolidation report |
| `Docs/Reports/Evidence/LATEST.md` | Evidence index | current evidence entry point |
| `Docs/CHANGELOG-docs.md` | Changelog | current doc change log |

## Historical Set Kept

| Path | Type | Reason |
|---|---|---|
| `Docs/Canon/Canon-Index.md` | Index | legacy reading order and historical snapshot only |

## Archived

| Path | Type | Reason |
|---|---|---|
| `Docs/Archive/TopLevel/ADR-Canonical-Consolidation-Summary-2026-03-25.md` | Doc | superseded consolidation summary |
| `Docs/Archive/TopLevel/ARCHITECTURE.md` | Doc | duplicate architecture overview now replaced by canonical chain |
| `Docs/Archive/TopLevel/CHANGELOG.md` | Doc | older project changelog, no longer part of the active docs chain |
| `Docs/Archive/Plans/Plan-Baseline-4.0-Reorganization.md` | Plan | auxiliary backlog superseded by the execution guardrails and canonical ADR chain |
| `Docs/Archive/Guides/Manual-Operacional.html` | HTML guide | duplicate render of active markdown guidance |
| `Docs/Archive/Guides/Hooks-Reference.html` | HTML guide | duplicate render of active markdown guidance |
| `Docs/Archive/Guides/How-To-Add-A-New-Module-To-Composition.html` | HTML guide | duplicate render of active markdown guidance |
| `Docs/Archive/Modules/WorldLifecycle.md` | Module | historical-only reset terminology, now replaced by active reset docs |
| `Docs/Archive/Reports/Baseline/Baseline-3.5.md` | Report | older baseline reference, no longer part of the active chain |
| `Docs/Archive/Reports/Baseline/Baseline-4.0-Phase-1.md` | Report | phase closure report superseded by the current canonical chain |
| `Docs/Archive/Reports/Audits/2026-03-19/` | Audit batch | older audit batch moved out of the active path |
| `Docs/Archive/Reports/Audits/2026-03-20/` | Audit batch | older audit batch moved out of the active path |
| `Docs/Archive/Reports/Audits/2026-03-22/` | Audit batch | older audit batch moved out of the active path |
| `Docs/Archive/Reports/Audits/2026-03-23/` | Audit batch | older audit batch moved out of the active path |
| `Docs/Archive/Reports/Audits/2026-03-28/` | Audit batch | older audit batch moved out of the active path |
| `Docs/Archive/Reports/Audits/2026-03-29/Baseline-4.0-Docs-Alignment-Audit.md` | Audit | superseded by this consolidation report |
| `Docs/Archive/Reports/Audits/2026-03-29/Baseline-4.0-Docs-Boundary-Finalization.md` | Audit | superseded by this consolidation report |
| `Docs/Archive/Reports/Audits/2026-03-29/Baseline-4.0-Phase-GameLoop-Audit.md` | Audit | superseded by this consolidation report |
| `Docs/Archive/Reports/Audits/2026-03-29/Baseline-4.0-Phase-PostGame-Audit.md` | Audit | superseded by this consolidation report |
| `Docs/Archive/Reports/Audits/2026-03-29/Slice-8-Checkpoint-Minimal-Implementation-Closure.md` | Audit | slice closure history, now archival only |

## Deleted

| Path | Type | Reason |
|---|---|---|
| `Docs/Archive/Reports/Audits/2026-03-22/Cleanup-Final-Manual.txt` | Manual cleanup note | no tracking value after consolidation |
| `Docs/Archive/Reports/Audits/2026-03-22/DELETE-MANUALLY-ContentSwap.txt` | Manual cleanup note | no tracking value after consolidation |

## Notes

- No canonical Baseline 4.0 doc was deleted.
- Ambiguous material was archived, not hard-removed.
- The active chain now starts from `ADR-0001`, `ADR-0043`, `ADR-0044`, the blueprint and the execution guardrails.
- The next project direction is the layer that populates the game: `player`, `enemies` and programmatic objects.
