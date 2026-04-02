# Changelog Docs

## 2026-04-01
- Added `Docs/Reports/Audits/2026-04-01/Round-2-Freeze-Object-Lifecycle.md` as the canonical freeze snapshot for round 2.
- Marked round 2 as concluded in `Docs/Plans/Plan-Round-2-Object-Lifecycle.md`.
- Added `Docs/Reports/Audits/2026-04-01/Round-2-Cut-4-Pooling-Future-Ready-Seam.md` as the canonical snapshot for round 2 cut 4.
- Marked round 2 cut 4 (`Pooling Future-Ready Seam`) as concluded in `Docs/Plans/Plan-Round-2-Object-Lifecycle.md`.
- Clarified in `Infrastructure/Pooling/Pooling-How-To.md` that pooling is backend only and does not own gameplay object lifecycle.
- Added `Docs/Reports/Audits/2026-04-01/Round-2-Cut-3-Runtime-Ownership-Reset-Participation.md` as the canonical snapshot for round 2 cut 3.
- Marked round 2 cut 3 (`Runtime Ownership + Reset Participation`) as concluded in `Docs/Plans/Plan-Round-2-Object-Lifecycle.md`.
- Added `Docs/Guides/GameLoop-Start-Contracts.md` as the canonical short contract for `BootStartPlanRequestedEvent` and `GamePlayRequestedEvent`.
- Renamed the boot/start-plan request contract from `GameStartRequestedEvent` to `BootStartPlanRequestedEvent` to remove semantic overlap with user Play intent.
- Added `Docs/Reports/Audits/2026-04-01/Round-2-Cut-2-Actor-Consumption-Contract.md` as the canonical snapshot for round 2 cut 2.
- Marked round 2 cut 2 (`Actor Consumption Contract`) as concluded in `Docs/Plans/Plan-Round-2-Object-Lifecycle.md`.
- Added `Docs/Reports/Audits/2026-04-01/Round-2-Cut-1-Ownership-Taxonomy.md` as the canonical snapshot for round 2 cut 1.
- Marked round 2 cut 1 (`Ownership Taxonomy`) as concluded in `Docs/Plans/Plan-Round-2-Object-Lifecycle.md`.
- Added `Docs/Plans/Plan-Round-2-Object-Lifecycle.md` as the canonical roadmap for round 2 focused on gameplay object lifecycle.
- Added `Docs/Reports/Audits/2026-04-01/Backbone-Round-1-Freeze.md` as the freeze snapshot for the completed backbone round 1.
- Marked the backbone roadmap as completed for cuts `1A` through `6`.
- Added `Docs/Plans/Plan-Backbone-Execution-Roadmap.md` as the canonical execution roadmap for the backbone.
- Froze the official backbone cut order: `Spawn + Identity`, `SceneReset` executor local, `ResetInterop` seam fino, `LevelLifecycle` vs `SceneComposition`, `GameLoop` puro, and `Experience` edge reativo.
- Split cut 1 into `1A - Spawn + Identity` and `1B - Spawn Completion Contract` and recorded the spawn completion timing contract in the roadmap.

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
