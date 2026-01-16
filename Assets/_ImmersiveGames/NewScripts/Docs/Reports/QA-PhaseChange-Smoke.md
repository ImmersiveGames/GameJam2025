# QA — Smoke: PhaseChange (In-Place vs With Transition)

## Objetivo
Registrar um smoke **mínimo e determinístico** para validar os dois tipos explícitos de troca de fase:
- **PhaseChange/In-Place** (sem SceneFlow)
- **PhaseChange/WithTransition** (com SceneFlow)

Este report não altera o Baseline 2.0; ele serve como **evidência suplementar** para ADR-0016/0017.

## Pré-requisitos
- Projeto em Play Mode.
- Fase atual conhecida (`PhaseContext.Current`).

## Caso 1 — Advance In-Place (TestCase: PhaseInPlace)

### Como executar
- Usar uma ferramenta de QA/Context Menu para disparar o In-Place, se disponível.
- Nomes *comuns* (quando presentes no projeto; ver também `Reports/Audit-Docs-Inventory.md`):
  - `QA/Phase/Advance In-Place (TestCase: PhaseInPlace)`

### Evidência mínima esperada (logs)
- Algum marcador de solicitação contendo:
  - `RequestPhaseInPlace` (ou equivalente)
  - `reason` com prefixo canônico (recomendado): `PhaseChange/InPlace/...`
- Gate/token de serialização do in-place:
  - token `flow.phase_inplace` (Acquire/Release)
- Commit do pending para current:
  - `PhasePendingSet` → `PhasePendingCleared` e `Current` atualizado

### Critério de PASS
- A fase muda sem SceneFlow (sem unload/load).
- Não há Loading HUD (por design).
- Se `UseFade=true` foi usado, o fade ocorre apenas como “mini transição local”, sem virar SceneFlow.

## Caso 2 — Advance With Transition (TestCase: PhaseWithTransition)

### Como executar
- Usar uma ferramenta de QA/Context Menu para disparar o WithTransition, se disponível.
- Nomes *comuns* (quando presentes no projeto; ver também `Reports/Audit-Docs-Inventory.md`):
  - `QA/Phase/Advance With Transition (TestCase: PhaseWithTransition)`

### Evidência mínima esperada (logs)
- Registro de intent (ou equivalente):
  - `PhaseTransitionIntentRegistry.Register` / `TryConsume`
- SceneFlow inicia e governa o gate padrão:
  - `SceneTransitionStartedEvent` com token `flow.scene_transition`
- Em `ScenesReady`, WorldLifecycle executa reset e conclui:
  - `WorldLifecycleResetCompletedEvent(signature, reason)`
- Commit da fase após reset:
  - `Pending` → `Current`
- `SceneTransitionCompletedEvent` (finalização visual)

### Critério de PASS
- A fase muda **e** a transição completa ocorre (SceneFlow).
- Fade/HUD seguem o profile da transição.
- `Current` reflete a nova fase ao final do pipeline.

## Referências
- [ADR-0017 — Tipos de troca de fase](../ADRs/ADR-0017-Tipos-de-troca-fase.md)
- [ADR-0016 — Phases + IntroStage opcional](../ADRs/ADR-0016-Phases-WorldLifecycle.md)
- [Checklist-phase.md](../ADRs/Checklist-phase.md)
