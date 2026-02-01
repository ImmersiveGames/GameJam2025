# ADR-0015 - Baseline 2.0: Fechamento

## Status

- Estado: Aceito
- Data (decisão): 2026-01-31
- Última atualização: 2026-01-31
- Escopo: Baseline 2.0 (fechamento do contrato)

## Contexto

O Baseline 2.0 foi o primeiro contrato minimo para:
- Boot -> Menu com skip de reset em frontend
- Menu -> Gameplay com ResetWorld + spawn deterministico
- PostGame com Restart/ExitToMenu e gating consistente

A partir do Baseline 2.2, as mesmas garantias ficam cobertas e ampliadas por evidencia e auditoria.

## Decisao

Considerar o Baseline 2.0 fechado e nao evoluir mais o contrato 2.0. Evolucoes devem acontecer via Baseline 2.2+ (com gate, evidencia e auditoria).

## Referencias

- Snapshot canonico (2.2): `../Reports/Evidence/LATEST.md`
- Evidencia datada (2026-01-31): `../Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`
- Auditoria Strict/Release (2026-01-31): `../Reports/Audits/2026-01-31/Invariants-StrictRelease-Audit.md`
