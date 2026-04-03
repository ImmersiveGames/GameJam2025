# ADR-0045 - Gameplay Runtime Composition como centro semantico do gameplay

## Status
- Estado: Aceito
- Data: 2026-04-03
- Tipo: Direction / Canonical architecture

## Contexto

O backbone atual segue necessario e forte para:
- boot
- SceneFlow
- Fade/Loading
- gates
- reset e materializacao operacional
- estado macro transversal

Mas ele nao deve continuar carregando a semantica principal do gameplay.
O jogo ainda aparece fragmentado em WorldDefinition, LevelManager, reset/retry/restart e handoffs espalhados.
A linguagem final do projeto nao deve girar em torno de swap de conteudo.

## Decisao

O centro semantico do gameplay passa a ser o **Gameplay Runtime Composition**.

Esse subsistema e o ponto de leitura canonica para:
- sessao ativa
- level runtime ativo
- players
- objetivos
- itens e estado local
- timers e contadores
- retry / restart / respawn / advance
- persistencia parcial entre fases e run

## Relacao com os ADRs seguintes

Este ADR fecha a direcao macro.

- `ADR-0046` fecha o primeiro bloco interno do centro semantico.
- `ADR-0047` fecha o pipeline minimo de construcao de fase dentro desse bloco.

## O que fica no backbone

O backbone continua dono de:
- boot
- SceneFlow
- Fade/Loading
- gates
- GameLoop macro
- reset e materializacao operacional
- estado macro transversal

O backbone continua existindo e continua necessario.
Ele deixa de ser o lugar onde mora a semantica principal do gameplay.

## O que sobe para o Gameplay Runtime Composition

Sobem para o novo centro semantico:
- sessao de jogo atual
- level runtime atual
- participacao de players
- objetivos e condicoes de vitoria/derrota
- itens e estado local de fase
- timers e contadores de gameplay
- checkpoints
- regras de retry/restart/respawn/advance
- persistencia parcial entre fases e run

## O que passa a ser linguagem historica

Passam a ser lidos como historicos, transitivos ou de menor peso semantico:
- `WorldDefinition` como fundacao final do jogo
- `LevelManager` como eixo separado de mesmo peso
- linguagem central baseada em swap de conteudo
- reset/retry/restart como responsabilidade semantica do backbone

## Consequencias praticas

- `WorldDefinition` nao deve virar base final do jogo.
- `LevelManager` nao deve permanecer como eixo semanticamente autonomo.
- retry / restart / respawn / advance passam a nascer da semantica do gameplay.
- o backbone permanece como executor tecnico, nao como dono do significado do gameplay.
- a leitura dos modulos deve seguir a composicao de gameplay, nao a forma historica da pasta.

## Proximos passos arquiteturais imediatos

1. Revisar `Modules/GameLoop` sob a nova leitura canonica.
2. Revisar `Modules/PostRun` como parte interna do fluxo da composicao.
3. Revisar `Modules/LevelFlow` como contexto local de conteudo dentro da composicao.
4. Usar esta leitura como base para `ADR-0046` e `ADR-0047`.
