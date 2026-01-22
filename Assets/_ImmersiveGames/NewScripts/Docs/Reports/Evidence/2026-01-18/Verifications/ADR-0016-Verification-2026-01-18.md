# ADR-0016 — Verification Anchors (2026-01-18)

Este arquivo é um índice rápido (não substitui o Console). Use como checklist de grep.

## TC-ADR0016-INPLACE
- `[QA][ContentSwap] TC-ADR0016-INPLACE start`
- `ContentSwapRequested` + `mode=InPlace`
- `token='flow.contentswap_inplace'`
- `ContentSwapPendingSet`
- `Reset` (requested/completed)
- `ContentSwapCommitted`
- `[QA][ContentSwap] TC-ADR0016-INPLACE done`

## TC-ADR0016-TRANSITION
- `[QA][ContentSwap] TC-ADR0016-TRANSITION start`
- `ContentSwapIntent` + `Registered`
- `ContentSwapRequested` + `mode=SceneTransition`
- `TransitionStarted` / `ScenesReady` / `TransitionCompleted` (mesma `signature`)
- `token='flow.scene_transition'`
- `ResetWorld` reason `SceneFlow/ScenesReady`
- `ContentSwapIntent` + `Consumed`
- `ContentSwapCommitted`

## Visual contract (ADR-0017)
- In-Place: ausência de logs de Fade/LoadingHUD no trecho do TC.
