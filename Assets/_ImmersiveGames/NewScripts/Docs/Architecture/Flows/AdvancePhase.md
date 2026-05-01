# Flow: AdvancePhase

> Status: draft. Validate against current ordinal navigation and SessionTransition code.

## Goal

Advance to the next phase through the canonical runtime/session transformation rail.

## Canonical reading

`AdvancePhase` should use phase ordering from `PhaseCatalog`, then transform the session/runtime through `SessionTransition`, and only then publish the canonical phase-local entry handoff.

## High-level sequence

```mermaid
sequenceDiagram
    participant Decision as RunDecision / QA / Caller
    participant Catalog as PhaseCatalog
    participant ST as SessionTransition
    participant SI as Session Integration
    participant Actors as Actors Materialization
    participant Intro as IntroStage
    participant Loop as GameLoop

    Decision->>Catalog: resolve next phase
    Catalog->>ST: committed target / transition context
    ST->>SI: phase-local entry ready handoff
    SI->>Actors: actor materialization directives
    Actors->>Intro: readiness
    Intro->>Loop: Playing
```

## Owners by step

| Step | Owner | Responsibility |
|---|---|---|
| Next target resolution | PhaseCatalog | Resolve and commit next ordinal target |
| Session/runtime transformation | SessionTransition | Compose content/spawn/carry-over axes |
| Operational handoff | Session Integration | Emit operational intents |
| Actor materialization | Actors execution | Preserve/rematerialize/register/spawn as required |
| Phase-local entry | GameplayPhaseFlow / IntroStage | Queue/release local entry |
| Gameplay state | GameLoop | Enter or remain Playing idempotently |

## Not allowed

- Restore `PhaseNextPhaseService` as executable path.
- Publish `SessionTransitionPhaseLocalEntryReadyEvent` directly from ordinal navigation request services.
- Treat PhaseCatalog as owner of content/runtime execution.
- Treat `StableNoAction` as readiness unless explicitly converted to a valid preserve/current directive by owner policy.
