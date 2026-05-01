# Flow: InitialEntry

> Status: draft. Event names and code files must be verified against current source.

## Goal

Enter gameplay from a frontend/menu route into the first valid playable session/phase.

## Canonical reading

InitialEntry is not a restart and not a reentry shortcut. It should enter through the canonical route/session preparation path and produce the same downstream phase-local readiness contract consumed by actors, IntroStage and GameLoop.

## High-level sequence

```mermaid
sequenceDiagram
    participant Nav as Navigation
    participant Scene as SceneFlow
    participant GSF as GameplaySessionFlow
    participant ST as SessionTransition
    participant SI as Session Integration
    participant Actors as Actors Materialization
    participant Intro as IntroStage
    participant Loop as GameLoop

    Nav->>Scene: Gameplay navigation intent resolved
    Scene->>GSF: gameplay route ready / prepare window
    GSF->>ST: InitialEntry context
    ST->>SI: phase-local entry handoff
    SI->>Actors: materialization intent
    Actors->>Intro: actors/runtime ready
    Intro->>Loop: release Playing
```

## Owners by step

| Step | Owner | Responsibility |
|---|---|---|
| Intent/route resolution | Navigation | Resolve gameplay route/style |
| Macro transition | SceneFlow | Execute macro scene flow, loading/fade/gates |
| Session/phase preparation | GameplaySessionFlow | Prepare playable session/phase semantics |
| Runtime transformation | SessionTransition | Compose/execute transition plan |
| Operational handoff | Session Integration | Translate to operational intents |
| Actor materialization | Actors execution/spawn | Concrete materialization/preservation |
| Entry presentation | IntroStage | Present entry or skip no-content |
| Gameplay start | GameLoop | Enter Playing |

## Not allowed

- Treat InitialEntry as Retry, ResetRun or RestartCurrentPhase.
- Use operational scene navigation as a substitute for session transformation.
- Let SceneFlow own gameplay session semantics.
- Let IntroStage perform phase preparation.
