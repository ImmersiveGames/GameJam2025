# Changelog Docs

## 2026-03-30
- Consolidated the Docs tree after Baseline 4.0 stabilization.
- Archived the redundant `Docs/Canon` mirror docs into `Docs/Archive/TopLevel`.
- Archived `Docs/Reports/Audits/2026-03-29/Baseline-4.0-Residual-Housekeeping-Audit.md` into `Docs/Archive/Reports/Audits/2026-03-29`.
- Added `Docs/Reports/Audits/2026-03-30/Docs-Consolidation-Baseline-4.0.md` as the tracking record for the cleanup.
- Moved superseded plans, duplicate top-level docs, HTML guide exports and older audit batches to `Docs/Archive`.
- Kept the active canon centered on `ADR-0001`, `ADR-0043`, `ADR-0044`, the blueprint and the execution guardrails.
- Archived the auxiliary reorganization backlog so it no longer competes with the canonical chain.
- Normalized the active operational docs and ADRs to match the current runtime terminology and ownership boundaries.
- Formalized the four canonical domains in `ADR-0001` as the project-wide ownership taxonomy.
- Aligned the active module docs to the current physical roots `Core`, `Orchestration`, `Game` and `Experience`.
- Documented the current owners and seams for `LevelLifecycle`, `PostRun`, `GameLoop`, `Gameplay/State`, `Gameplay/GameplayReset`, `Audio`, `Save` and `Camera`.
- Kept `Orchestration/LevelFlow/Runtime` as transition compat, removed the dead QA shell, and kept `SceneResetFacade` plus `FilteredEventBus.Legacy` as historical compat.
- Recorded the validated pruning of empty shells from Lote A so they no longer appear as live areas.
- Added a structural freeze snapshot at `Docs/Reports/Audits/2026-03-30/Structural-Freeze-Snapshot.md` to freeze the consolidated tree before the next functional evolution.
- Reframed `Experience/Save` as an official hook surface and integration placeholder, with `Progression` and `Checkpoint` documented as placeholders rather than final features.
- Reserved `IManualCheckpointRequestService` as the canonical seam for future manual checkpoint integration, without wiring it into runtime.
- The next project direction is `player`, `enemies` and programmatic objects.

## 2026-03-29
- Closed the remaining Baseline 4.0 runtime boundary work in the active docs chain.
- Kept `GameLoop`, `PostRun`, `LevelFlow` and `Navigation` stabilized as the last major baseline front.
- Documented the residual housekeeping that remained after the main closure pass.

## 2026-03-28
- Marked the earlier baseline stabilization and operational guide work as historical context for the current baseline.

