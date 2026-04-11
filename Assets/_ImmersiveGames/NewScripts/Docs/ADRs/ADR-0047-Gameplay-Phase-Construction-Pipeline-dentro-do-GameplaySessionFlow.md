# ADR-0047 - Pipeline canonico de montagem da fase em `GameplaySessionFlow`

## Status
- Estado: Aceito
- Data: 2026-04-03
- Tipo: Direction / Canonical architecture

## 1. Objetivo

Este ADR congela o pipeline canonico de lifecycle da phase dentro de `GameplaySessionFlow`.

Ele define, de forma positiva e operacional, como a phase entra na janela protegida de preparacao, aplica o conteudo local, deriva o runtime minimo e so depois se apresenta ao jogador em `IntroStage`.

## 2. Escopo

Este ADR cobre o fluxo de montagem da fase em `GameplaySessionFlow`, incluindo:

- a entrada canonica via `PhaseSelected`
- a aplicacao de conteudo local em `ContentApplied`
- a derivacao runtime minima antes do reveal final
- o reveal canonico via `SceneTransitionCompleted`
- a entrada operacional da `IntroStage`
- a liberacao para `Playing`
- a derivacao de `SessionContext`, `PhaseRuntime`, `Players`, `Rules/Objectives` e `InitialState`
- a leitura de `PhaseDefinition` ja resolvida

## 3. Estrutura do pipeline

O pipeline canonico da phase e estruturado em uma sequencia unica:

1. `PhaseSelected`
2. `ContentApplied`
3. derivacao de `SessionContext`, `PhaseRuntime`, `Players`, `Rules/Objectives` e `InitialState`
4. `SceneTransitionCompleted`
5. `IntroStage`
6. `Playing`

Cada bloco possui contrato proprio, ciclo proprio e infraestrutura tipada propria.

## 4. Ownership

`IntroStage` e `phase-owned`.

O owner semantico explicito da lifecycle da phase pode morar dentro de `GameplaySessionFlow`, por exemplo como `PhaseFlowService`, mas continua sendo owner da phase e nao do backbone.

O `GameplaySessionFlow` permanece macro e consome a phase ja resolvida; a execucao operacional nao reatribui ao backbone a semantica da phase.

## 5. Fluxo canonico

O fluxo canonico de montagem ocorre assim:

1. `PhaseSelected`
2. `ContentApplied`
3. derivacao de `SessionContext`, `PhaseRuntime`, `Players`, `Rules/Objectives` e `InitialState`
4. `SceneTransitionCompleted`
5. `IntroStage`
6. `Playing`

### 5.1 Sucesso de entrada da phase

A phase e considerada corretamente entregue quando:

- o conteudo local foi aplicado
- o runtime minimo da phase ja existe
- os registros necessarios de `Intro` e `RunResult` foram feitos

`SceneTransitionCompleted` so ocorre depois que a phase ja esta semanticamente montada.
`IntroStage` continua antes de `Playing`, mas nao e a unica fronteira de sucesso da entrada.

## 6. Handoff para o fim de run

O handoff canonico para o fim de run ocorre quando `Playing` publica `RunEndIntent(reason)`.

Esse handoff transfere a continuidade da run para o owner canonico do fim de run, que passa a ser `ADR-0049`.

## 7. Lifecycle de `IntroStage`

`IntroStage` e o stage canonico de entrada da phase, pos-reveal.

Lifecycle:

1. recebe a transicao depois de `SceneTransitionCompleted`
2. recebe a phase ja montada semanticamente
3. adota o contrato tipado da intro da phase
4. resolve o presenter valido da intro no escopo correto
5. executa a apresentacao local da entrada
6. confirma a liberacao operacional para `Playing`
7. encerra o stage quando a entrada foi concluida

## 8. Consequencias arquiteturais

Este contrato consolida `GameplaySessionFlow` como leitor operacional da fase.

Consequencias principais:

- a fase passa a ser lida como conjunto autoral derivado de `PhaseDefinition`
- o pipeline de entrada e montagem fica claro e tipado
- `GameplaySessionFlow` preserva a fronteira entre definicao e runtime
- o conteudo local e os derivados runtime entram na preparacao protegida antes do reveal
- o handoff para o fim de run fica explicitado como continuidade downstream
