# ADR-0015 — Baseline 2.0: Fechamento

## Status

- Estado: Implementado
- Data (decisão): 2026-01-31
- Última atualização: 2026-02-04
- Tipo: Completude (governança)
- Escopo: Baseline 2.0 (fechamento do contrato)

## Contexto

O Baseline 2.0 foi o primeiro contrato mínimo para:
- Boot → Menu com skip de reset em frontend
- Menu → Gameplay com ResetWorld + spawn determinístico
- PostGame com Restart/ExitToMenu e gating consistente

A partir do Baseline 2.2, as mesmas garantias ficam cobertas e ampliadas por evidência e auditoria.

## Decisão

### Objetivo de fechamento

Considerar o Baseline 2.0 fechado e não evoluir mais o contrato 2.0. Evoluções devem acontecer via Baseline 2.2+ (com gate, evidência e auditoria).

### Critérios de fechamento (DoD)

- Evidência canonicamente registrada em `Docs/Reports/Evidence/LATEST.md`.
- Auditoria Strict/Release vigente para as invariantes do ciclo (Fade/Loading HUD/Reset/PostGame).
- Contratos de observabilidade e reasons padronizados documentados.

### Não-objetivos (resumo)

- Criar novos requisitos de gameplay para o Baseline 2.0.
- Alterar o pipeline de produção sem atualizar Baseline 2.2.

## Escopo e fora de escopo

- **Dentro:** governança do fechamento do Baseline 2.0, evidências e auditorias.
- **Fora:** novas features e refactors fora do baseline vigente.

## Evidência

- Última evidência (log bruto): `Docs/Reports/lastlog.log`
- Fonte canônica atual: `Docs/Reports/Evidence/LATEST.md`
- Evidências adicionais: `Docs/CHANGELOG.md (entrada histórica de 2026-01-31)`

## Implementação (arquivos impactados)

Este ADR é **documental/governança**: o fechamento do Baseline 2.0 é efetivado pela promoção do Baseline 2.2 como fonte de verdade e pela auditoria das invariantes.

Artefatos canônicos:

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Evidence/2026-02-03/Baseline-2.2-Evidence-2026-02-03.md`
- `Docs/CHANGELOG.md (entrada histórica de 2026-01-31)`
- `Docs/CHANGELOG.md (entrada histórica de 2026-01-31)`

## Riscos / Observações

- Mudanças de comportamento exigem nova evidência e atualização do LATEST.

## Referências

- `../Reports/Evidence/LATEST.md`
- `../CHANGELOG.md`