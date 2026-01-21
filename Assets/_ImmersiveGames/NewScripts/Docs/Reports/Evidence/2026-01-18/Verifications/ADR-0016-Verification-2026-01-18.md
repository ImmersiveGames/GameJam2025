# ADR-0016 — Verification Anchors (2026-01-18)

Este arquivo é um índice rápido (não substitui o Console). Use como checklist de grep.

## TC-ADR0016-INPLACE
- `[QA][Phase] TC-ADR0016-INPLACE start`
- `PhaseChangeRequested` + `mode=InPlace`
- `token='flow.phase_inplace'`
- `PhasePendingSet`
- `Reset` (requested/completed)
- `PhaseCommitted`
- `[QA][Phase] TC-ADR0016-INPLACE done`

## TC-ADR0016-TRANSITION
- `[QA][Phase] TC-ADR0016-TRANSITION start`
- `PhaseIntent` + `Registered`
- `PhaseChangeRequested` + `mode=SceneTransition`
- `TransitionStarted` / `ScenesReady` / `TransitionCompleted` (mesma `signature`)
- `token='flow.scene_transition'`
- `ResetWorld` reason `SceneFlow/ScenesReady`
- `PhaseIntent` + `Consumed`
- `PhaseCommitted`

## Visual contract (ADR-0017)
- In-Place: ausência de logs de Fade/LoadingHUD no trecho do TC.
