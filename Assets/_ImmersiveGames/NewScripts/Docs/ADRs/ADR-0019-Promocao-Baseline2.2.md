# ADR-0019 - Promocao do Baseline 2.2

## Status

- Estado: **Aceito**
- Data: **2026-01-31**

## Contexto

O Baseline 2.2 consolida um contrato verificavel (logs + invariantes) para o fluxo end-to-end (A-E), incluindo ContentSwap InPlace e gating consistente (SceneFlow/ResetWorld/gates/InputMode/PostGame).

## Decisao

Promover oficialmente o Baseline 2.2 como referencia canonica do projeto, baseada em:

- Evidencia datada (cenarios A-E) registrada em Docs/Reports/Evidence.
- Auditoria Strict/Release registrada em Docs/Reports/Audits.
- Ponteiro unico do snapshot canonico em Docs/Reports/Evidence/LATEST.md.

O que entra no Baseline 2.2:
- ContentSwap InPlace (sem transicao de cena) com razoes padronizadas.
- Gate tokens e InputMode consistentes nas transicoes e no Pause/Resume.
- PostGame (Victory/Defeat) com Restart e ExitToMenu rastreaveis.

Fora de escopo do 2.2:
- LevelManager/ConfigCatalog e outras mudancas estruturais maiores (candidatas a um Baseline futuro).

## Evidencia e auditoria

- Snapshot canonico: `../Reports/Evidence/LATEST.md`
- Evidencia datada (2026-01-31): `../Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`
- Auditoria Strict/Release (2026-01-31): `../Reports/Audits/2026-01-31/Invariants-StrictRelease-Audit.md`

## Relacionados

- ADR-0018 (Gate de promocao): `ADR-0018-Gate-de-Promocao-Baseline2.2.md`
- Contrato de observabilidade: `../Standards/Standards.md#observability-contract`
- Politica Strict/Release: `../Standards/Standards.md#politica-strict-vs-release`
