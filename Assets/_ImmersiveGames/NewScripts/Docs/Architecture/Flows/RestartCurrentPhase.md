# Flow: RestartCurrentPhase

> Status: draft. Validate event names and current services against source.

## Goal

Restart the current phase without changing to the first phase of the catalog.

## Canonical reading

`Retry` maps to `RestartCurrentPhase`.
It resets/re-enters the current phase through `SessionTransition` and the phase-local handoff rail.

## High-level sequence

```mermaid
sequenceDiagram
    participant Decision as RunDecision
    participant Routing as RunContinuationRouting
    participant Handoff as OperationalHandoff
    participant ST as SessionTransition
    participant Reset as ResetFlow / PhaseReset
    participant Actors as Actors Materialization
    participant Intro as IntroStage
    participant Loop as GameLoop

    Decision->>Routing: RestartCurrentPhase selected
    Routing->>Handoff: canonical continuation route
    Handoff->>ST: SessionTransitionContext
    ST->>Reset: reset current phase axis
    Reset->>ST: reset completed
    ST->>Actors: phase-local entry ready
    Actors->>Intro: actors/runtime ready
    Intro->>Loop: release Playing
```

## Owners by step

| Step | Owner | Responsibility |
|---|---|---|
| Visual decision | RunDecision | Emits selected continuation |
| Continuation route | RunContinuation routing/handoff | Routes resolved continuation |
| Runtime transformation | SessionTransition | Builds and executes restart-current plan |
| Concrete reset | ResetFlow / PhaseReset executor | Executes reset operation |
| Actor readiness | Actors execution | Re-materialize/register/preserve as plan requires |
| Entry presentation | IntroStage | Reopens or skips phase-local intro |
| Gameplay state | GameLoop | Returns to Playing |

## Not allowed

- Map visual `Restart` to `RestartCurrentPhase`.
- Reintroduce `ResetRun`.
- Call `Navigation/StartGameplayRoute` as a restart rail.
- Disable QA buttons instead of routing them canonically and surfacing errors.
