# Round 2 runtime ownership
- New cut snapshot: `Docs/Reports/Audits/2026-04-01/Round-2-Cut-3-Runtime-Ownership-Reset-Participation.md`

# Round 2 pooling seam
- New cut snapshot: `Docs/Reports/Audits/2026-04-01/Round-2-Cut-4-Pooling-Future-Ready-Seam.md`

# Round 2 freeze
- New freeze snapshot: `Docs/Reports/Audits/2026-04-01/Round-2-Freeze-Object-Lifecycle.md`

# Round 2 actor consumption
- New cut snapshot: `Docs/Reports/Audits/2026-04-01/Round-2-Cut-2-Actor-Consumption-Contract.md`

# Round 2 object ownership
- New cut snapshot: `Docs/Reports/Audits/2026-04-01/Round-2-Cut-1-Ownership-Taxonomy.md`

# Backbone round 1 freeze
- New freeze snapshot: `Docs/Reports/Audits/2026-04-01/Backbone-Round-1-Freeze.md`

## Orchestration backbone analysis
- New consultation report: `Docs/Reports/Audits/2026-03-31/Orchestration-Backbone-Analysis.md`
- Companion diagrams: `Docs/Reports/Audits/2026-03-31/Orchestration-Backbone-Graphs.md`
- Focus: real runtime flow across `SceneFlow`, `WorldReset`, `SceneReset`, `ResetInterop`, `Navigation`, `LevelLifecycle` and `GameLoop`, with the `Game` cross-links that drive spawn and restart.

## Docs consolidation
- The Baseline 4.0 docs chain is now short and explicit.
- Canonical entry points: `ADR-0001`, `ADR-0043`, `ADR-0044`, the blueprint and the execution guardrails.
- Current audit trail: `Docs/Reports/Audits/2026-03-30/Structural-Freeze-Snapshot.md`, `Docs/Reports/Audits/2026-03-30/Structural-Xray-NewScripts.md` and `Docs/Reports/Audits/2026-03-30/Docs-Consolidation-Baseline-4.0.md`.
- The physical axis `Core` vs `Infrastructure` was corrected: `Infrastructure` is now a sibling root, `SceneComposition` lives under `Orchestration`, and `Core` is limited to foundational primitives.
- The 2026-03-29 residual housekeeping audit was archived and no longer sits in the active chain.
- Superseded plans, duplicate top-level docs, canon mirror docs, HTML guide exports and older audit batches moved to `Docs/Archive`.
- The auxiliary reorganization backlog was archived and no longer competes with the canonical chain.
- The next project direction is the layer that populates the game: `player`, `enemies` and programmatic objects.
