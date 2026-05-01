# ADR-0061 - Run Pipeline canonico e substituicao do conceito de macro

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

O conceito historico de `macro` juntou em um unico vocabulrio o que hoje precisa ser lido como pipeline de run: identidade, sequencia, lifecycle, handoffs e deactivation.
Isso abriu margem para leitura operacional demais em camada errada.

## Decisao

Adota-se o `Run Pipeline` como rail canonico de run.

Regras:

- `Run Pipeline` substitui `macro` como conceito normativo.
- `RunResult`, `RunDecision` e `PostRun` pertencem ao eixo de deactivation/continuity, nao a um owner global historico.
- o pipeline decide a ordem da run, o lifecycle da run e os handoffs entre etapas.
- a execucao concreta ocorre em adapters ou componentes operacionais comandados pelo pipeline.

## Consequencias

- `macro` passa a ser vocabulario historico.
- a run deixa de depender de owners difusos por conveniencia de fluxo.
- o fechamento de run passa a ser lido por identidade explicita de ciclo.
- a Base 1.1 ganha um rail unico para orquestrar run sem colapsar em baseline ou scene-local code.

## Invariantes

- nenhuma etapa de run pode ser movida por evento foreign/stale.
- o pipeline ativo e unicamente o que corresponde a identidade valida da run.
- adapters nao decidem continuidade final.
- ordens derivadas por compatibilidade historica nao podem reescrever o run pipeline.

## Relacao com Base 1.0 e Base 2.0

- Base 1.0 e historico de consolidacao do conceito de macro e dos rails que o cercavam.
- Base 1.1 redefine o rail final como pipeline deterministico de run.
- Base 2.0 futura so pode generalizar o que o `Run Pipeline` provar na pratica.

## ADRs historicos relacionados

- `ADR-0031`
- `ADR-0033`
- `ADR-0049`
- `ADR-0051`
- `ADR-0056`
