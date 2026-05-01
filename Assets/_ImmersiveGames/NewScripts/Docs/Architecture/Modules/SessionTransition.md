# Module: SessionTransition

> Status: draft structural reading. Must be audited against current source before finalizing.

## 1. One-line summary

`SessionTransition` composes and executes how the session/runtime changes after a continuation or entry intent has already been resolved.

## 2. Architectural role

Semantic/session-runtime transformation layer above the technical baseline.

## 3. What it owns

- `SessionTransitionContext` shape.
- `SessionTransitionPlan` composition.
- Transition axes such as continuity, phase transition, reset/restart, content/spawn transition and carry-over.
- The canonical decision of whether a phase-local entry handoff should be produced.
- Thin orchestration of the plan.

## 4. What it does not own

- Concrete spawn/despawn implementation.
- Concrete input mode application.
- Concrete navigation route execution.
- Concrete reset internals.
- RunDecision UI.
- Phase authoring.

## 5. Main inputs

- Resolved continuation or entry context.
- Current session/phase runtime context.
- Phase catalog target when applicable.
- Policy data needed to compose the plan.

## 6. Main outputs

- Execution dispatch result.
- Canonical phase-local entry handoff when plan requires it.
- Observability logs for resolved plan and handoff.

## 7. Extension points

- Add a new transition axis.
- Add a new execution kind.
- Add a new plan resolver case.
- Add stricter validation/fail-fast around required plan fields.

## 8. Forbidden changes

- Do not turn it into concrete spawn/reset/input executor.
- Do not publish handoffs from unrelated modules when `SessionTransition` is the owner.
- Do not model every behavior as one giant enum instead of typed axes.
- Do not restore navigation-based restart shortcuts.

## 9. Related ADRs

- ADR-0052 — Session Transition above baseline.
- ADR-0057 — Base 1.0.
- ADR-0055 — Session Integration seam.
- ADR-0053 — Phase Catalog Navigation 2.0.

## 10. Source audit TODO

- List actual context/plan/orchestrator files.
- Confirm all execution kinds currently present.
- Confirm event publication owner.
- Confirm fail-fast points.
- Confirm logs used as canonical observability.
