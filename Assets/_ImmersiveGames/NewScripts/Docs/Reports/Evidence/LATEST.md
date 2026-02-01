# LATEST — Evidence (fonte canônica)

**Última atualização:** 2026-01-31
**Regra:** manter **1 arquivo de evidência por dia** (um snapshot consolidado). Outros artefatos do dia devem ser **mesclados** neste arquivo e removidos.

## Snapshot canônico atual

- **2026-01-31 — Baseline 2.2 Evidence Snapshot:** `Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`

## Histórico recente

- 2026-01-29 — `Reports/Evidence/2026-01-29/Baseline-2.2-Evidence-2026-01-29.md`
- 2026-01-28 — `Reports/Evidence/2026-01-28/Baseline-2.2-Evidence-2026-01-28.md`

> Observação: a pasta `Reports/Evidence/<data>/` pode conter `.meta` por exigência do Unity, mas **não deve** conter múltiplos arquivos de evidência humana por dia.

## Checklist obrigatório para geração de Baseline evidence
- [ ] SceneTransitionStartedEvent (incluir reason + contextSignature)
- [ ] ScenesReadyEvent (mesma contextSignature do SceneTransition)
- [ ] `[OBS][Fade] FadeStart/FadeComplete` (contextSignature) — validar que Fade usa a mesma signature do SceneTransitionService (fallback ok desde que seja a mesma)
- [ ] `[OBS][LoadingHud] LoadingStarted/LoadingReady` (contextSignature)
- [ ] ResetWorldStarted / ResetCompleted (reason + contextSignature)
- [ ] GameplaySimulationBlocked / GameplaySimulationUnblocked
- [ ] GameLoop ENTER Playing (contextSignature)
- [ ] Snapshot dos principais logs + lista de GameObjects essenciais (Player, Eater)
- [ ] Pós-run: validar assinatura consistente entre Fade, LoadingHud, ScenesReady e ResetWorld (automatizar check)

## Exemplo de snapshot mínimo (trecho)
- 2026-01-31T10:00:00Z | SceneTransitionStartedEvent reason=Profile=gameplay signature=2026-01-31T10:00:00Z-run123
- 2026-01-31T10:00:01Z | [OBS][Fade] FadeStart signature=2026-01-31T10:00:00Z-run123
- 2026-01-31T10:00:02Z | [OBS][LoadingHud] LoadingReady signature=2026-01-31T10:00:00Z-run123
- 2026-01-31T10:00:02Z | ScenesReadyEvent signature=2026-01-31T10:00:00Z-run123
- 2026-01-31T10:00:03Z | [OBS] ResetWorldStarted reason=SceneFlow/ScenesReady signature=2026-01-31T10:00:00Z-run123
- 2026-01-31T10:00:04Z | [OBS] ResetWorldCompleted reason=SceneFlow/ScenesReady signature=2026-01-31T10:00:00Z-run123
- 2026-01-31T10:00:05Z | [OBS] IntroStarted signature=2026-01-31T10:00:00Z-run123
- 2026-01-31T10:00:06Z | [OBS] IntroConfirmed signature=2026-01-31T10:00:00Z-run123
- 2026-01-31T10:00:06Z | [OBS] GameplaySimulationUnblocked signature=2026-01-31T10:00:00Z-run123
- 2026-01-31T10:00:06Z | GameLoop ENTER Playing signature=2026-01-31T10:00:00Z-run123

// Adicionar/alterar:
// - Checklist/anchors obrigatórios para Baseline evidence generation.
// - Exemplo de snapshot com SceneTransitionStarted/ScenesReady/ResetCompleted/GameLoop ENTER Playing.
