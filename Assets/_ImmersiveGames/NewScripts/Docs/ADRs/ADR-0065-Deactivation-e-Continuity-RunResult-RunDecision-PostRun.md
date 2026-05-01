# ADR-0065 - Deactivation e Continuity: RunResult, RunDecision, PostRun

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

A Base 1.0 fixou rails de fim de run em torno de `RunResultStage`, `RunDecision` e do vocabulario historico `PostRun`.
Na Base 1.1, isso precisa ser congelado como eixo de deactivation/continuity, sem owners globais antigos.

## Decisao

Adota-se o eixo de `Deactivation / Continuity`.

Regras:

- `RunResult`, `RunDecision` e `PostRun` pertencem ao eixo de deactivation/continuity.
- esses elementos nao sao owners globais da arquitetura.
- o pipeline decide quando o ciclo ativo fecha e quando a continuidade inicia.
- a ausencia de stage valida gera `skip/no-content` explicito.

## Consequencias

- `PostRun` fica como vocabulario historico de leitura, nao como centro normativo.
- a continuidade passa a ser uma derivacao do fechamento, nao um owner difuso.
- o resultado de run deixa de competir com o pipeline ativo.
- a deactivation pode ser diferente por identidade de ciclo sem quebrar o contrato.

## Invariantes

- run ativa e deactivation nao podem coexistir como owners concorrentes.
- eventos foreign/stale nao podem reabrir o ciclo fechado.
- continuidade sempre depende de identidade explicita.
- ausencia de presenter local valido nao rompe o contrato; gera skip/no-content.

## Relacao com Base 1.0 e Base 2.0

- Base 1.0 formalizou a existencia dos rails de resultado e continuidade.
- Base 1.1 reposiciona esses rails como deactivation/continuity.
- Base 2.0 futura so pode generalizar essa separacao se os pipelines provarem o modelo.

## ADRs historicos relacionados

- `ADR-0013`
- `ADR-0049`
- `ADR-0051`
- `ADR-0056`
- `ADR-0057`
