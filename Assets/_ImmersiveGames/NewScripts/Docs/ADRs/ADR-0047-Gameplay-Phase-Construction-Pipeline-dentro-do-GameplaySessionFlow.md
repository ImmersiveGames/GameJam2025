# ADR-0047 - Pipeline canonico de montagem da fase em `GameplaySessionFlow`

## Status
- Estado: Aceito
- Data: 2026-04-03
- Tipo: Direction / Canonical architecture

## 1. Objetivo

Este ADR congela o pipeline canonico de montagem da fase em `GameplaySessionFlow`.

Ele define, de forma positiva e operacional, como a fase entra em `IntroStage`, como transita para `Playing` e como a leitura runtime da phase e derivada a partir de `PhaseDefinition`.

## 2. Escopo

Este ADR cobre o fluxo de montagem da fase em `GameplaySessionFlow`, incluindo:

- a entrada canonica via `SceneTransitionCompleted`
- a entrada operacional da `IntroStage`
- a liberacao para `Playing`
- a derivacao de `PhaseRuntime`
- a composicao de `Players`
- a fixacao de `Rules/Objectives`
- a seed de `InitialState`
- a leitura de `PhaseDefinition` ja resolvida

## 3. Estrutura do pipeline

O pipeline canonico da fase e estruturado em uma sequencia unica:

1. `SceneTransitionCompleted`
2. `IntroStage`
3. `Playing`
4. derivacao de `PhaseRuntime`
5. composicao de `Players`
6. fixacao de `Rules/Objectives`
7. seed de `InitialState`

Cada bloco possui contrato proprio, ciclo proprio e infraestrutura tipada propria.

## 4. Ownership

`IntroStage` e `phase-owned`.

O pipeline de montagem da fase e `phase-owned` no trecho de entrada e derivacao runtime.

O ownership do significado da fase permanece na `PhaseDefinition`, e a execucao operacional permanece no `GameplaySessionFlow`.

## 5. Fluxo canonico

O fluxo canonico de montagem ocorre assim:

1. `SceneTransitionCompleted`
2. entrada da `IntroStage`
3. transicao para `Playing`
4. derivacao da leitura runtime da phase

## 6. Handoff para o fim de run

O handoff canonico para o fim de run ocorre quando `Playing` publica `RunEndIntent(reason)`.

Esse handoff transfere a continuidade da run para o owner canonico do fim de run, que passa a ser `ADR-0049`.

## 7. Lifecycle de `IntroStage`

`IntroStage` e o stage canonico de entrada da phase.

Lifecycle:

1. recebe a transicao depois de `SceneTransitionCompleted`
2. adota o contrato tipado da intro da phase
3. resolve o presenter valido da intro no escopo correto
4. executa a apresentacao local da entrada
5. confirma a liberacao operacional para `Playing`
6. encerra o stage quando a entrada foi concluida

## 8. Consequencias arquiteturais

Este contrato consolida `GameplaySessionFlow` como leitor operacional da fase.

Consequencias principais:

- a fase passa a ser lida como conjunto autoral derivado de `PhaseDefinition`
- o pipeline de entrada e montagem fica claro e tipado
- `GameplaySessionFlow` preserva a fronteira entre definicao e runtime
- o handoff para o fim de run fica explicitado como continuidade downstream
