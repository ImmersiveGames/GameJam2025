# Plan - Baseline 4.0 - Audio / BGM Context Ownership Cleanup

Subordinate to `ADR-0043`, `ADR-0044`, the Blueprint, the Execution Guardrails and the Reorganization backlog.

## Objective

Audit and normalize the ownership of BGM context in Baseline 4.0, specifically the boundary between route context, confirmed level context and the audio runtime that reacts to them.

## Canonical Focus

The canonical focus is the cleanup of BGM context ownership across `Audio`, `Navigation`, `SceneFlow` and `LevelFlow`.

## Scope

- audit first, implementation second
- identify where BGM context is decided, updated and consumed
- distinguish route context from confirmed level context
- identify late-correction bridges or duplicate rails that delay the final BGM owner
- keep runtime working, but prefer canonical ownership over inherited shape

## Out of Scope

- `Save`
- `Checkpoint`
- `PostGame`
- `GameLoop / PostGame` frontiers already closed
- backend final work
- cloud sync
- migration work
- broad refactor of `Audio` beyond the BGM context boundary
- broad refactor of `SceneFlow` beyond the BGM context boundary

## Acceptance

- the owner of BGM context is explicit
- route context vs confirmed level context is explicit
- no duplicate BGM decision rail survives by inertia
- temporary correction bridges are removed or reduced to a minimal technical seam
- fail-fast behavior exists when required context is missing
- the current runtime remains compilable and validated

## Evidence Expected

- ownership map for BGM context across `Audio`, `Navigation`, `SceneFlow` and `LevelFlow`
- audit notes for route-context vs level-context divergence
- list of bridges or late-correction steps that must move, shrink or disappear
- runtime evidence that the chosen owner receives the final confirmed context
