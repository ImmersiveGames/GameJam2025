# ADR-0060 - Base 1.1: Pipeline Convergence e Identidade Explicita

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

A Base 1.0 estabilizou varios fluxos concretos, mas ainda deixou a arquitetura com linguagem mista entre semantica, rails operacionais e owners por conveniencia.
Isso funciona como historico de consolidacao, mas nao e suficiente para congelar uma base deterministica.

A Base 1.1 entra para convergir os fluxos ja materializados em pipelines com identidade explicita, sem reabrir a discussao de uma Base 2.0 nem tratar este freeze como transicao descartavel.

## Decisao

Adota-se a Base 1.1 como **Pipeline Convergence / Convergencia para Pipelines Deterministicos**.

Este freeze define:

- `Run Pipeline` substitui o conceito historico de `macro`.
- `Session Pipeline` substitui o conceito historico de `local`.
- modulos produzem fatos ou comandos.
- pipelines decidem ordem, lifecycle, policies e handoffs.
- adapters executam side-effects comandados pelo pipeline.
- todo ciclo relevante possui identidade explicita.
- eventos foreign ou stale nao podem alterar o pipeline ativo.

Somente os ADRs `ADR-0060` a `ADR-0067` permanecem vivos na pasta principal.
ADRs anteriores viram historico de referencia em `Docs/ADRs/Historico/`.

## Consequencias

- A pasta principal deixa de carregar normativa da Base 1.0.
- A leitura do sistema passa a ter um grupo vivo unico para a Base 1.1.
- Ownership deixa de ser inferido por quem executa o codigo.
- O historico continua disponivel como referencia, mas nao como fonte normativa.

## Invariantes

- Base 1.1 nao e Base 2.0.
- Base 1.1 nao e transicao descartavel.
- Identidade explicita e obrigatoria em todo ciclo relevante.
- Foreign/stale events nao alteram o pipeline ativo.
- Pipeline ativo e decidido por identidade e contrato, nao por ambiguidade temporal.

## Relacao com Base 1.0 e Base 2.0

- Base 1.0 e antecedente historico e prova de materializacao dos fluxos.
- Base 1.1 da shape final de pipeline aos fluxos concretos ja materializados na Base 1.0.
- Base 2.0 futura so pode extrair, generalizar ou reorganizar o que a Base 1.1 provar.

## ADRs historicos relacionados

- `ADR-0052`
- `ADR-0055`
- `ADR-0056`
- `ADR-0057`
- `ADR-0058`
- `ADR-0059`
- `ADR-0049`
- `ADR-0050`
- `ADR-0051`
