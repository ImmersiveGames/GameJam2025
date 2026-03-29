# Plan - Baseline 4.0 Phase 2 - Core Boundary Cleanup

Subordinate to `ADR-0043`, `ADR-0044`, the Blueprint, the Execution Guardrails and the Reorganization backlog.

## Objective

Audit and normalize the central boundary owners of Baseline 4.0 before any broad code movement.

## Canonical Focus

The canonical focus is the core boundary cleanup of `GameLoop`, `PostGame`, `LevelFlow` and `Navigation`.

## Execution Stance

The project is in a structural, pre-production phase. Canon-first takes priority over preserving the current shape of the runtime.

- wide rewrite/refactor is allowed when it is the most canonical way to close ownership and boundary
- preserving current behavior is desirable when cheap, but not mandatory
- the primary acceptance signal is correct ownership, correct semantics, and removal of parallel rails
- temporary bridges are not to be preserved by provisional compatibility alone
- legacy naming and legacy surfaces may be replaced when they keep the wrong boundary alive
- the restart contract is already split and validated: `Restart` keeps current context, `RestartFromFirstLevel` forces the canonical first level
- the remaining focus of this frontier is `ExitToMenu`

## Scope

- audit first, implementation second
- validate ownership lines and semantic boundaries
- identify duplicated intent paths and stale bridges
- keep runtime working, but prefer canonical replacement over preserving the current structure

## Priority Modules

- `GameLoop`
- `PostGame`
- `LevelFlow`
- `Navigation`

## Out of Scope

- `Save`
- `Checkpoint`
- `Audio`
- `SceneFlow`
- `Frontend/UI`
- backend final work
- cloud sync
- migration work

## Acceptance

- the canonical owner of each boundary is explicit
- duplicated intent paths are identified and normalized
- no broad blind refactor is introduced
- the phase may replace wrong structure when that is the canonical fix
- current gameplay start, run end, post-run and navigation flows stay compilable and validated
- the phase remains subordinate to the blueprint and guardrails
- restart semantics must not collapse back into a single path

## Evidence Expected

- updated ownership mapping for the four priority modules
- audit notes for semantic drift and duplicate paths
- clear list of reusable pieces versus pieces that must move or be replaced
- validation that the current runtime behavior was not regressed
