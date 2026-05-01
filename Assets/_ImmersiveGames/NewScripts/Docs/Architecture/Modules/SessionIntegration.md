# Module: SessionIntegration

> Status: draft structural reading. Must be audited against current source before finalizing.

## 1. One-line summary

`SessionIntegration` is the explicit seam that translates canonical session semantics into canonical operational intent.

## 2. Architectural role

Integration seam between semantic layer and operational domains.

## 3. What it owns

- Translation from semantic session/phase/participation/actor state to operational requests or handoffs.
- Canonical request publication for adjacent operational domains.
- Thin correlation and dispatch where semantic state must cross into execution.

## 4. What it does not own

- Semantic truth of session, phase, participation or actors.
- Concrete spawn execution.
- Concrete reset execution.
- Concrete input mode application.
- Concrete navigation execution.
- IntroStage presentation.

## 5. Main inputs

- Semantic snapshots or handoffs from `GameplaySessionFlow`, `SessionTransition`, `Participation`, `ActorsSystem`.
- Scene/macro lifecycle hooks when they are required as timing gates.

## 6. Main outputs

- Operational handoffs.
- Input mode requests.
- Spawn/materialization intents.
- Reset handoff requests.
- Continuation operational handoffs.

## 7. Extension points

- Add a translator for a new operational domain.
- Add a new canonical handoff contract.
- Add observability around a seam boundary.

## 8. Forbidden changes

- Do not execute final concrete effects in the seam.
- Do not put ownership semantics here just because the seam observes many modules.
- Do not hide integration in bootstraps.
- Do not use dedupe in operational domains to mask multiple semantic emitters.

## 9. Related ADRs

- ADR-0055 — Session Integration as explicit seam.
- ADR-0057 — Base 1.0.
- ADR-0056 — Baseline thin executor.

## 10. Source audit TODO

- List actual `Integration` folders and bridges.
- Verify which outputs are canonical and which are legacy.
- Verify seam does not execute final concrete effects.
- Verify canonical emitters for input, reset, actors and continuation.
