# Baseline 2.1 — Evidência consolidada (2026-01-17)

## Objetivo

Consolidar o snapshot datado de evidências do Baseline 2.1 para suportar ADRs e validações de regressão.

## Artefatos desta execução

- Log bruto (Console → arquivo): `./Logs/Baseline-2.1-Smoke-2026-01-17.log`
- Hash (SHA-256) do log bruto: `2198490005e0b671b7447b674c771d45d88c8d8d0d4d879eb9b324cf50129c78`
- Evidência curada (âncoras/invariantes): `./Verifications/Baseline-2.1-Evidence-2026-01-17.md`

## Escopo do run

- Startup -> Menu (reset skip)
- Menu -> Gameplay (SceneFlow + WorldLifecycle reset + spawn)
- IntroStage (bloqueio `sim.gameplay` até `IntroStage/UIConfirm`)
- PostGame (Victory/Defeat) + Restart + ExitToMenu

## Invariantes validadas (ponte)

A lista canônica de âncoras (com snippets) está em `./Verifications/Baseline-2.1-Evidence-2026-01-17.md`, incluindo:

- `flow.scene_transition` (SceneTransitionStarted/Completed)
- ScenesReady -> ResetWorld -> ResetCompleted
- Spawn determinístico (Player + Eater)
- IntroStage bloqueio/liberação do gameplay
- PostGame -> Restart/ExitToMenu com SceneFlow

## Nota metodológica

- Fonte de verdade: **log do Console**.
- Artefatos derivados (resumos/verificações) são auxiliares; se houver divergência, prevalece o log do Console.
