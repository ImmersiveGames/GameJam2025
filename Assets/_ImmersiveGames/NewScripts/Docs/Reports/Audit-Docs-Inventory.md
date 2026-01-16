# Audit — Docs Inventory

Data: 2026-01-16

## Escopo
Inventário e varredura de referências em `Assets/_ImmersiveGames/NewScripts/Docs`.

## Inventário por pasta

### Docs (raiz)
- `README.md`
- `ARCHITECTURE.md`
- `WORLD_LIFECYCLE.md`
- `CHANGELOG-docs.md`
- `HowTo-Criar-Novo-Spawnable.md`

### Docs/ADRs
- `ADR-0009-FadeSceneFlow.md`
- `ADR-0010-LoadingHud-SceneFlow.md`
- `ADR-0011-WorldDefinition-MultiActor-GameplayScene.md`
- `ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`
- `ADR-0013-Ciclo-de-Vida-Jogo.md`
- `ADR-0014-GameplayReset-Targets-Grupos.md`
- `ADR-0015-Baseline-2.0-Fechamento.md`
- `ADR-0016-Phases-WorldLifecycle.md`
- `ADR-0017-Tipos-de-troca-fase.md`
- `Checklist-phase.md`

### Docs/Reports
- `Audit-Docs-Inventory.md`
- `Audit-Reports-Cleanup.md`
- `Baseline-2.0-Checklist.md`
- `Baseline-2.0-ChecklistVerification-LastRun.md`
- `Baseline-2.0-Smoke-LastRun.md`
- `Baseline-2.0-Spec.md`
- `Baseline-Audit-2026-01-03.md`
- `GameLoop.md`
- `QA-GameplayReset-RequestMatrix.md`
- `QA-GameplayResetKind.md`
- `ResetWorld-Audit-2026-01-05.md`
- `SceneFlow-Assets-Checklist.md`
- `SceneFlow-Production-EndToEnd-Validation.md`
- `WORLDLIFECYCLE_RESET_ANALYSIS.md`
- `WORLDLIFECYCLE_SPAWN_ANALYSIS.md`
- `Archive/2025/*` (históricos arquivados)

## Mapa de referências (QA / paths antigos)

| Referência | Arquivo(s) que apontam | Alvo (existe?) |
| --- | --- | --- |
| `QA/Phase/Advance In-Place (TestCase: PhaseInPlace)` | `ADRs/ADR-0017-Tipos-de-troca-fase.md` | Não aplicável (texto de menu) |
| `QA/Phase/Advance With Transition (TestCase: PhaseWithTransition)` | `ADRs/ADR-0017-Tipos-de-troca-fase.md` | Não aplicável (texto de menu) |
| `QA/IntroStage/...` / `Tools/NewScripts/QA/...` | `ADRs/ADR-0016-Phases-WorldLifecycle.md` | Não aplicável (texto de menu) |
| `Reports/Archive/2025/Report-SceneFlow-Production-Log-2025-12-31.md` | `ADRs/ADR-0009-FadeSceneFlow.md`, `ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md`, `Reports/Baseline-Audit-2026-01-03.md` | Sim |
| `Reports/Archive/2025/SceneFlow-Smoke-Result.md` | `Reports/Baseline-Audit-2026-01-03.md` | Sim |
| `Reports/Archive/2025/SceneFlow-Production-Evidence-2025-12-31.md` | `Reports/Baseline-Audit-2026-01-03.md` | Sim |

## Suspeitos de obsolescência (por nome/data/duplicação)

- `Archive/2025/SceneFlow-Smoke-Result.md` (histórico 2025-12-27).
- `Archive/2025/SceneFlow-Gameplay-To-Menu-Report.md` (histórico 2025-12-28/29).
- `Archive/2025/Report-SceneFlow-Production-Log-2025-12-31.md` (log histórico de produção).
- `Archive/2025/SceneFlow-Production-Evidence-2025-12-31.md` (evidência histórica).
- `Archive/2025/QA-Audit-2025-12-27.md` (inventário histórico de QA).
- `Archive/2025/Legacy-Cleanup-Report.md` (histórico).
- `Archive/2025/Marco0-Phase-Observability-Checklist.md` (marco inicial).

## Links quebrados detectados

- Nenhum link quebrado identificado nesta varredura.
