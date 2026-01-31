# Baseline 2.2 — Evidence (LATEST)

Este arquivo é a **ponte canônica** para o snapshot recomendado para auditoria e detecção de regressões.

## Snapshot recomendado (canônico)

- **Snapshot:** `2026-01-29`
- **Arquivo:** `Docs/Reports/Evidence/2026-01-29/Baseline-2.2-Evidence-2026-01-29.md`

Este snapshot cobre:

- Boot → Menu (startup) com **SKIP** de reset no frontend
- Menu → Gameplay com **ResetWorld + ResetCompleted + spawn** (Player + Eater)
- IntroStage bloqueia `sim.gameplay` e conclui via `IntroStage/UIConfirm` → Playing
- ContentSwap QA in-place (`QA/ContentSwap/InPlace/NoVisuals`)
- Pause/Resume (`state.pause`, InputMode PauseOverlay)
- PostGame (Victory/Defeat), Restart e ExitToMenu (frontend skip)

## Snapshot mais recente (rastreamento)

- **Snapshot:** `2026-01-31`
- **Nota:** captura **parcial** (trecho colado) — útil para rastreabilidade, não substitui o canônico.

## Retenção

Mantemos apenas o **mais recente + 2 anteriores** (além deste `LATEST.md`).
