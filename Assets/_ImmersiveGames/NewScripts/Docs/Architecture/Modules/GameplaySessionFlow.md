# Module: GameplaySessionFlow

> Status: draft structural reading. Must be audited against current source before finalizing.

## 1. One-line summary

`GameplaySessionFlow` organizes the playable session and phase-side semantic preparation before gameplay is released.

## 2. Architectural role

Semantic gameplay-session area above the technical baseline.

## 3. What it owns

- Playable session semantic preparation.
- Phase-side preparation pipeline.
- Consumption of resolved `PhaseDefinition`.
- Derivation of session/phase runtime state needed before local entry.
- Coordination with participation semantics.

## 4. What it does not own

- Macro scene transition execution.
- Concrete spawn/materialization execution.
- Concrete input binding.
- Concrete reset execution.
- Phase catalog ordering.
- Final UI presentation of RunDecision as concrete UI.

## 5. Main inputs

- Resolved phase definition/catalog target.
- Session entry/reentry context.
- Participation semantic input.
- Lifecycle hooks from macro baseline when appropriate.

## 6. Main outputs

- Semantic session/phase snapshots.
- Phase preparation result.
- Handoffs into `SessionTransition` or `SessionIntegration`.
- Readiness data for downstream phase-local entry.

## 7. Extension points

- Add a new semantic preparation block.
- Add a new phase-side derived snapshot.
- Add a new session-side policy, if it belongs above baseline.

## 8. Forbidden changes

- Do not let it directly execute concrete spawn/input/reset.
- Do not let SceneFlow own its session semantics.
- Do not collapse PhaseCatalog order into session preparation.
- Do not use historical Level/World names as ownership proof.

## 9. Related ADRs

- ADR-0046 — GameplaySessionFlow as first internal block.
- ADR-0047 — Phase construction pipeline.
- ADR-0054 — Participation semantics.
- ADR-0057 — Base 1.0.

## 10. Source audit TODO

- List current services and ownership boundaries.
- Confirm phase preparation sequence.
- Confirm outputs consumed by SessionTransition/SessionIntegration.
- Identify legacy names still present but not normative.
