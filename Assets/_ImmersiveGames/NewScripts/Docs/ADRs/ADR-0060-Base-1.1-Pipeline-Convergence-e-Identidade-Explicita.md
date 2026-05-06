# ADR-0060 - Base 1.1: Pipeline Convergence / Convergência para Pipelines Determinísticos e Identidade Explícita

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 estabilizou vários fluxos concretos, mas ainda deixou a arquitetura com linguagem mista entre semântica, rails operacionais e owners por conveniência.
Isso funciona como histórico de consolidação, mas não é suficiente para congelar uma base determinística.

A Base 1.1 entra para convergir os fluxos já materializados em pipelines com identidade explícita, sem reabrir a discussão de uma Base 2.0 nem tratar este freeze como transição descartável.

## Decisão

Adota-se a Base 1.1 como **Pipeline Convergence / Convergência para Pipelines Determinísticos**.

Este freeze define:

- `Run Pipeline` substitui o conceito histórico de `macro`.
- `Session Pipeline` substitui o conceito histórico de `local`.
- módulos produzem `Pipeline Facts` ou `Pipeline Commands`.
- pipelines decidem ordem, lifecycle, `Pipeline Policies` e `Pipeline Handoffs`.
- `Pipeline Adapters` executam side-effects comandados pelo pipeline.
- todo ciclo relevante possui identidade explícita.
- foreign/stale events não podem alterar o pipeline ativo.

Somente os ADRs `ADR-0060` a `ADR-0067` permanecem vivos na pasta principal.
ADRs anteriores viram histórico de referência em `Docs/ADRs/Historico/`.

## Consequências

- A pasta principal deixa de carregar normativa da Base 1.0.
- A leitura do sistema passa a ter um grupo vivo único para a Base 1.1.
- Ownership deixa de ser inferido por quem executa o código.
- O histórico continua disponível como referência, mas não como fonte normativa.

## Invariantes

- Base 1.1 não é Base 2.0.
- Base 1.1 não é transição descartável.
- Identidade explícita é obrigatória em todo ciclo relevante.
- Foreign/stale events não alteram o pipeline ativo.
- Pipeline ativo é decidido por identidade e contrato, não por ambiguidade temporal.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 é antecedente histórico e prova de materialização dos fluxos.
- Base 1.1 dá shape final de pipeline aos fluxos concretos já materializados na Base 1.0.
- Base 2.0 futura só pode extrair, generalizar ou reorganizar o que a Base 1.1 provar.

## ADRs históricos relacionados

- `ADR-0052`
- `ADR-0055`
- `ADR-0056`
- `ADR-0057`
- `ADR-0058`
- `ADR-0059`
- `ADR-0049`
- `ADR-0050`
- `ADR-0051`

## Materializacao Base11Sandbox - checkpoint congelado

Evidencia de runtime consolidada no checkpoint `Base11Sandbox Minimal Route + Session Activity Cycle - PASS`:

- rota operacional minima por asset direto;
- `SessionOperationalRouteAsset` como contrato de rota, nao como policy;
- `SessionOperationalPipeline` como owner de comando, completude e handoff;
- `Base11SandboxOperationalRouteTransitionAdapter` como adapter executor;
- `SceneCompositionExecutor` como executor fisico;
- `SessionActivityPipeline` como owner do ciclo interno de activity.

Este checkpoint confirma, na pratica, a decisao central deste ADR:

- pipelines decidem;
- adapters executam side-effects;
- identity explicita protege o ciclo;
- foreign/stale events permanecem inertes.
