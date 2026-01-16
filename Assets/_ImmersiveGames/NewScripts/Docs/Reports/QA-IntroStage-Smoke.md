# QA — Smoke: IntroStage (opcional, pós-revelação)

## Objetivo
Validar que a **IntroStage é opcional e não bloqueia o fluxo de forma irreversível**, garantindo sempre um caminho de saída:
- auto-skip (sem conteúdo)
- confirmação (UI)
- mitigação determinística (QA/Tools)

Este report não altera o Baseline 2.0; ele existe como evidência suplementar para ADR-0016.

## Pré-requisitos
- Navegar para gameplay com profile `gameplay` (SceneFlow completo).
- Confirmar que a IntroStage foi solicitada após `SceneTransitionCompletedEvent` (pós-revelação).

## Caso A — NoContent (auto-skip)

### Como executar
- Configurar o ambiente de forma que o step não exista ou retorne `HasContent=false`.

### Evidência mínima esperada (logs)
- IntroStage inicia (ou tentativa de iniciar) e resolve automaticamente:
  - `SkipIntroStage("IntroStage/NoContent")`
- Token `sim.gameplay` não permanece preso após o skip.

### Critério de PASS
- O jogo segue para `Playing` sem intervenção manual.

## Caso B — UIConfirm (produção/dev)

### Como executar
- Usar o caminho de UI (Runtime Debug GUI) para concluir:
  - `CompleteIntroStage("IntroStage/UIConfirm")`

### Evidência mínima esperada (logs)
- Confirmação aplicada e IntroStage encerrada.
- Token `sim.gameplay` liberado ao final.

### Critério de PASS
- A run entra em `Playing` após a confirmação.

## Caso C — Mitigação determinística (Context Menu / Tools)

### Como executar
- Context Menu (Play Mode):
  - `QA/IntroStage/Complete (Force)` → `CompleteIntroStage("QA/IntroStage/Complete")`
  - `QA/IntroStage/Skip (Force)` → `SkipIntroStage("QA/IntroStage/Skip")`
- MenuItem (Editor):
  - `Tools/NewScripts/QA/IntroStage/Complete (Force)`
  - `Tools/NewScripts/QA/IntroStage/Skip (Force)`

### Evidência mínima esperada (logs)
- `reason` refletindo a ação escolhida (`QA/IntroStage/...`).

### Critério de PASS
- Mesmo com conteúdo travado, é possível sair de forma determinística.

## Invariante de não-bloqueio
- O sistema sempre deve oferecer **um caminho canônico de saída** (produção) e **um caminho de mitigação** (QA/dev).

## Referências
- [ADR-0016 — Phases + IntroStage opcional](../ADRs/ADR-0016-Phases-WorldLifecycle.md)
