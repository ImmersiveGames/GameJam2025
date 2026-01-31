# ADR-0013 - Ciclo de Vida do Jogo (WorldLifecycle / SceneFlow)

## Status

- Estado: **Aceito**
- Data: **2026-01-31** (atualizado pelo snapshot canonico)

## Contexto

Precisamos de um ciclo de vida deterministico e observavel para suportar:
- Boot -> Menu -> Gameplay -> PostGame -> Restart/ExitToMenu
- ResetWorld deterministico na entrada de gameplay
- gating (flow.scene_transition, sim.gameplay) e InputMode coerentes

## Decisao

O fluxo do jogo e modelado como uma sequencia de transicoes dirigidas por SceneFlow/WorldLifecycle, com:
- SceneTransitionStarted fechando o gate de transicao
- ScenesReady sinalizando prontidao de cena
- ResetWorld executado no perfil de gameplay antes de liberar simulacao
- ResetCompleted como marco oficial do reset

## Evidencia

- Snapshot canonico: `../Reports/Evidence/LATEST.md`
- Evidencia datada (2026-01-31): `../Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`

## Relacionados

- ADR-0014 (driver ResetWorld / ScenesReady)
- Politica Strict/Release: `../Standards/Production-Policy-Strict-Release.md`
- Contrato de observabilidade: `../Standards/Observability-Contract.md`
