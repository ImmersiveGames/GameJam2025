# ADR-0061 - Run Pipeline canônico e substituição do conceito de macro

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

O conceito histórico de `macro` juntou em um único vocabulário o que hoje precisa ser lido como `Run Pipeline`: identidade, sequência, lifecycle, handoffs e deactivation.
Isso abriu margem para leitura operacional demais em camada errada.

## Decisão

Adota-se o `Run Pipeline` como rail canônico de run.

Regras:

- `Run Pipeline` substitui `macro` como conceito normativo.
- `RunResult`, `RunDecision` e `PostRun` pertencem ao eixo de deactivation/continuity, não a um owner global histórico.
- o pipeline decide a ordem da run, o lifecycle da run e os `Pipeline Handoffs` entre etapas.
- a execução concreta ocorre em `Pipeline Adapters` ou componentes operacionais comandados pelo pipeline.

## Consequências

- `macro` passa a ser vocabulário histórico.
- a run deixa de depender de owners difusos por conveniência de fluxo.
- o fechamento de run passa a ser lido por identidade explícita de ciclo.
- a Base 1.1 ganha um rail único para orquestrar run sem colapsar em baseline ou scene-local code.

## Invariantes

- nenhuma etapa de run pode ser movida por foreign/stale events.
- o pipeline ativo é unicamente o que corresponde à identidade válida da run.
- adapters não decidem continuidade final.
- ordens derivadas por compatibilidade histórica não podem reescrever o `Run Pipeline`.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 é histórico de consolidação do conceito de macro e dos rails que o cercavam.
- Base 1.1 redefine o rail final como pipeline determinístico de run.
- Base 2.0 futura só pode generalizar o que o `Run Pipeline` provar na prática.

## ADRs históricos relacionados

- `ADR-0031`
- `ADR-0033`
- `ADR-0049`
- `ADR-0051`
- `ADR-0056`
