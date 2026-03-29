# Plan - Baseline 4.0 Phase 2 - Core Boundary Cleanup

Subordinate to `ADR-0043`, `ADR-0044`, the Blueprint, the Execution Guardrails and the Reorganization backlog.

## Objective

Audit and normalize the central boundary owners of Baseline 4.0 before any broad code movement.

## Canonical Focus

The canonical focus is the core boundary cleanup of `GameLoop`, `PostGame`, `LevelFlow` and `Navigation`.

## Scope

- audit first, implementation second
- validate ownership lines and semantic boundaries
- identify duplicated intent paths and stale bridges
- preserve the existing runtime behavior while clarifying canonical ownership

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
- current gameplay start, run end, post-run and navigation flows remain behaviorally stable
- the phase remains subordinate to the blueprint and guardrails

## Evidence Expected

- updated ownership mapping for the four priority modules
- audit notes for semantic drift and duplicate paths
- clear list of reusable pieces versus pieces that must move or be replaced
- validation that the current runtime behavior was not regressed
