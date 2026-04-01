# Plan - Backbone Execution Roadmap

## 1. Objetivo do plano

Este plano existe para congelar a ordem oficial de execucao do backbone antes de qualquer implementacao.
Ele serve para manter foco, evitar desvio de escopo e registrar a sequencia aceita dos cortes arquiteturais.
Nao e proposta de brainstorming nem documento de implementacao.

## 2. Leitura arquitetural consolidada

Leitura aceita para o backbone:

- `Navigation` resolve intencoes de rota.
- `SceneFlow` executa transicoes de cena.
- `SceneComposition` aplica composicao de cenas.
- `LevelLifecycle` governa selecao, snapshot e entrada de conteudo local.
- `WorldReset` e o owner macro da decisao de reset.
- `SceneReset` e o executor local de reset.
- `Spawn` e o owner da materializacao e da identidade.
- `ActorRegistry` e o diretorio runtime dos vivos.
- `GameplayReset` fica restrito a `cleanup / restore / rebind`.
- `GameLoop` e o owner da run.
- `Experience` reage ao backbone, nao o possui.

## 3. Ordem oficial dos cortes

| Ordem | Nome do corte | Objetivo curto | Risco | Status final |
|---|---|---|---|---|
| 1A | `Spawn + Identity` | centralizar materializacao, identidade unica e registro valido | baixo | concluido |
| 1B | `Spawn Completion Contract` | fechar timing/observabilidade apos o spawn | baixo / medio | concluido |
| 2 | `SceneReset` executor local | rebaixar `SceneReset` para executor local | medio | concluido |
| 3 | `ResetInterop` seam fino | reduzir `ResetInterop` a borda fina | baixo / medio | concluido |
| 4 | `LevelLifecycle` vs `SceneComposition` | separar conteudo/snapshot de composicao de cena | medio | concluido |
| 5 | `GameLoop` puro | manter `GameLoop` como owner puro da run | alto | concluido |
| 6 | `Experience` edge reativo | fazer `Experience` reagir ao backbone | baixo / medio | concluido |

## 4. Detalhamento por corte

### 4.1 `Spawn + Identity`

Objetivo: fazer `Spawn` ser o owner unico da materializacao e da atribuicao de identidade.

Problema que resolve: duplicacao de `ActorId` e spawn espalhado em mais de um owner.

Entra:

- materializacao
- atribuicao unica de `ActorId`
- registro do objeto ja identificado

Fica fora:

- reset macro
- reset local
- composicao de cena

Criterio de aceite:

- nenhum actor gera identidade em paralelo
- `Spawn` passa a ser a fonte unica de nascimento do objeto
- `ActorRegistry` recebe apenas objetos validos

Dependencias:

- nenhuma dependencia previa obrigatoria

Observacoes de risco:

- e o primeiro corte oficial e tambem o de maior ganho com menor risco

### 4.2 `Spawn Completion Contract`

Objetivo: fechar o contrato de timing do spawn com um marco explicito de spawn concluido.

Problema que resolve: janela entre instanciacao, composicao, registro e observabilidade externa.

Entra:

- contrato explicito de spawn completed
- observabilidade segura para binders e consumidores
- publicacao somente apos identidade, composicao e registro

Fica fora:

- significado de ready no registry
- dependencia de Awake/OnEnable para descobrir actor por id
- refactor amplo de registry

Criterio de aceite:

- existe um marco canonico de spawn concluido
- consumidores externos podem reagir sem inferir readiness pelo registry
- `ActorRegistry` continua significando vivo/consultavel

Dependencias:

- `Spawn + Identity`

Observacoes de risco:

- e uma extensao pequena, mas e a correcao arquitetural que fecha o timing do corte 1

### 4.3 `SceneReset` executor local

Objetivo: rebaixar `SceneReset` para executor local de reset.

Problema que resolve: `SceneReset` acumulando gate, hooks, despawn, scoped reset e spawn.

Entra:

- gate local
- hooks locais
- ordem de despawn
- chamada para `GameplayReset`

Fica fora:

- spawn como ownership
- identidade
- policy macro de reset

Criterio de aceite:

- `SceneReset` nao decide materializacao
- `SceneReset` nao e owner de spawn
- `SceneReset` so executa a sequencia local

Dependencias:

- `Spawn + Identity`

Observacoes de risco:

- mexe no coracao do backbone, mas com boundary ja reduzido pelo corte anterior

### 4.4 `ResetInterop` seam fino

Objetivo: reduzir `ResetInterop` a uma borda fina entre `SceneFlow` e `WorldReset`.

Problema que resolve: camada conceitualmente fraca e pouco dona de semantica.

Entra:

- traducao de fronteira
- passagem de evento ou contrato minimo

Fica fora:

- policy propria
- execucao local
- ownership novo

Criterio de aceite:

- a camada nao carrega semantica propria
- nao existe adapter permanente so para preservar desenho ruim

Dependencias:

- `SceneReset` como executor local
- `WorldReset` como owner macro claro

Observacoes de risco:

- risco baixo a medio, desde que a borda nao vire novo orquestrador

### 4.5 `LevelLifecycle` vs `SceneComposition`

Objetivo: separar selecao, snapshot e entrada de conteudo local da aplicacao de cenas.

Problema que resolve: `LevelLifecycle` absorvendo composicao ou `SceneComposition` carregando semantica de level.

Entra:

- selecao de level
- snapshot de restart
- entrada local de conteudo
- aplicacao de cenas

Fica fora:

- reset local material
- ownership da run
- materializacao de objetos de gameplay

Criterio de aceite:

- `LevelLifecycle` governa nivel e snapshot
- `SceneComposition` so aplica composicao de cena

Dependencias:

- `SceneReset` mais simples
- `Spawn + Identity` ja estabilizado

Observacoes de risco:

- risco medio, porque separa duas zonas que hoje encostam bastante

### 4.6 `GameLoop` puro

Objetivo: fazer `GameLoop` ser owner puro da run.

Problema que resolve: `GameLoop` sincronizando demais com fluxo de level, reset e transicao.

Entra:

- start da run
- estado de playing
- fim da run
- outcome da run

Fica fora:

- selecao de level
- composicao de cena
- materializacao
- reset local

Criterio de aceite:

- `GameLoop` nao decide conteudo
- `GameLoop` nao executa reset local
- `GameLoop` so governa a run

Dependencias:

- `ResetInterop` reduzido
- `LevelLifecycle` e `SceneFlow` mais limpos

Observacoes de risco:

- risco alto; e o corte mais sensivel do backbone

### 4.7 `Experience` edge reativo

Objetivo: fazer `Experience` reagir ao backbone, sem possuir backbone.

Problema que resolve: save, audio, postrun, preferences e frontend tentando dirigir o fluxo principal.

Entra:

- hooks
- reacoes a eventos do backbone
- efeitos de edge

Fica fora:

- ownership de run
- ownership de reset
- ownership de navigation

Criterio de aceite:

- `Experience` reage a eventos do backbone
- `Experience` nao governa o backbone

Dependencias:

- `GameLoop` mais puro
- backbone com ownership claro

Observacoes de risco:

- risco baixo a medio; fica melhor no fim, quando o backbone ja esta honesto

## 5. Regra de execucao

- Nao pular corte sem fechar o anterior.
- Nao abrir frentes paralelas desnecessarias.
- Nao criar adapters temporarios so para preservar desenho ruim.
- Nao usar compatibilidade como desculpa para manter ownership errado.
- Quando surgir duvida local, voltar ao plano central.

## 6. O que esta explicitamente fora do plano

- renomeacoes fisicas por enquanto
- cleanup de namespace
- redesign amplo de docs
- pooling completo antes do corte certo
- refactor global de restart
- qualquer frente que desvie do backbone

## 7. Proximo passo oficial

A rodada 1 ficou dividida em `1A - Spawn + Identity` e `1B - Spawn Completion Contract`, e agora esta fechada.
Todos os cortes da rodada 1 estao concluídos.
A proxima frente parte deste freeze e nao reabre a ordem oficial ja executada.
O ganho principal da rodada foi separar ownership, timing e boundaries do backbone sem preservar misturas indevidas.
O freeze atual serve como referencia canonica para qualquer evolucao futura.
O escopo minimo consolidado da rodada foi:

- gate local
- hooks locais
- ordem de despawn
- chamada para `GameplayReset`

## 8. Changelog minimo

Referencia consolidada no changelog de docs e no snapshot de freeze da rodada 1.
