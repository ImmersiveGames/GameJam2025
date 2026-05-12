# ADR-0065 - Deactivation e Continuity: RunResult, RunDecision, PostRun

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 fixou rails de fim de run em torno de `RunResultStage`, `RunDecision` e do vocabulário histórico `PostRun`.
Na Base 1.1, isso precisa ser congelado como eixo de `Deactivation / Continuity`, sem owners globais antigos.

## Decisão

Adota-se o eixo de `Deactivation / Continuity`.

Regras:

- `RunResult`, `RunDecision` e `PostRun` pertencem ao eixo de `Deactivation / Continuity`.
- esses elementos não são owners globais da arquitetura.
- o pipeline decide quando o ciclo ativo fecha e quando a continuidade inicia.
- a ausência de stage válida gera `skip/no-content` explícito.

## Consequências

- `PostRun` fica como vocabulário histórico de leitura, não como centro normativo.
- a continuidade passa a ser uma derivação do fechamento, não um owner difuso.
- o resultado de run deixa de competir com o pipeline ativo.
- a deactivation pode ser diferente por identidade de ciclo sem quebrar o contrato.

## Invariantes

- run ativa e deactivation não podem coexistir como owners concorrentes.
- foreign/stale events não podem reabrir o ciclo fechado.
- continuidade sempre depende de identidade explícita.
- ausência de presenter local válido não rompe o contrato; gera skip/no-content.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 formalizou a existência dos rails de resultado e continuidade.
- Base 1.1 reposiciona esses rails como `Deactivation / Continuity`.
- Base 2.0 futura só pode generalizar essa separação se os pipelines provarem o modelo.

## ADRs históricos relacionados

- `ADR-0013`
- `ADR-0049`
- `ADR-0051`
- `ADR-0056`
- `ADR-0057`
