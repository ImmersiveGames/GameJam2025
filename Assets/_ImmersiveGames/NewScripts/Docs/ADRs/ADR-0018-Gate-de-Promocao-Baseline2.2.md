# ADR-0018 - Gate de Promocao (Baseline 2.2)

## Status

- Estado: Aceito
- Data (decisão): 2026-01-31
- Última atualização: 2026-01-31
- Escopo: Gate de promoção para Baseline 2.2 (evidência + auditoria obrigatórias)

## Contexto

O Baseline e tratado como contrato verificavel (logs + invariantes). Para evitar regressoes silenciosas, precisamos de um criterio objetivo para promover mudancas que afetam WorldLifecycle/SceneFlow/gates e/ou as assinaturas do contrato de observabilidade.

## Decisao

Mudancas que impactem qualquer um dos itens abaixo devem trazer um novo snapshot (evidencia + auditoria):

- Transicoes (SceneFlow, SceneTransitionStarted, ScenesReady)
- ResetWorld / ResetCompleted
- Gate tokens e InputMode
- ContentSwap / PostGame / Pause
- Strings/ancoras de log cobertas por Observability-Contract

Requisitos minimos na mesma entrega:
1) Evidencia datada em Docs/Reports/Evidence/<YYYY-MM-DD>/Baseline-2.2-Evidence-<YYYY-MM-DD>.md
2) Auditoria datada em Docs/Reports/Audits/<YYYY-MM-DD>/Invariants-StrictRelease-Audit.md
3) Atualizar Docs/Reports/Evidence/LATEST.md

## Fonte de verdade

- Docs/Reports/Evidence/LATEST.md e a referencia unica do snapshot canonico aprovado.
- Evidencias mais antigas sao historico.

## Consequencias

- Promocoes deixam de ser subjetivas; o baseline tem checkpoints versionados.
- Mudancas de contrato (reason/ancoras) passam a exigir evidencia.

## Referencias

- Evidencia canonica: ../Reports/Evidence/LATEST.md
- Evidencia datada: ../Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md
- Auditoria: ../Reports/Audits/2026-01-31/Invariants-StrictRelease-Audit.md
- Contrato de observabilidade: ../Standards/Standards.md#observability-contract
- Politica Strict/Release: ../Standards/Standards.md#politica-strict-vs-release
