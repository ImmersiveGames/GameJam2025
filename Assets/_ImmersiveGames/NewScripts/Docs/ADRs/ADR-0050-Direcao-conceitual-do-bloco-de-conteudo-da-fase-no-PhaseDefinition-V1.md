# ADR-0050 - Direcao conceitual do bloco de conteudo da fase no PhaseDefinition V1

## Status
- Estado: Aceito
- Data: 2026-04-04
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048` e `ADR-0049` ja fecharam a leitura canonica da gameplay, da fase jogavel e do shape conceitual minimo do `PhaseDefinition` no V1.

Falta registrar a direcao conceitual minima do bloco de conteudo da fase dentro do `PhaseDefinition`, sem congelar ainda o shape tecnico final, a implementacao, sub-assets ou modularizacao futura.

## Decisao

O bloco de conteudo da fase passa a ser tratado como composicao declarativa de conteudo dentro do `PhaseDefinition`.

No V1, essa composicao comeca como uma lista simples de entradas. O payload principal/default desse bloco e cena, preferencialmente via Addressables. Uma fase pode ser composta por uma ou mais cenas locais, inclusive em arranjo additive. Cada entrada referencia uma cena auto-declarativa, e a fase compoe essas cenas sem sobrescrever sua identidade ou seu papel base.

### Leitura canonica

- o bloco de conteudo da fase e obrigatorio no V1
- `PhaseDefinition` nao carrega o conteudo bruto como truth source principal; ele declara a composicao da fase
- a cena continua sendo o conteudo concreto e auto-declarativo no V1
- a fase compoe as cenas, nao redefine semanticamente a cena
- a direcao do V1 e simples e evolutiva, sem overdesign

### Estrutura conceitual minima de cada entrada

- id local da entrada dentro da fase
- referencia da cena
- papel / tipo da cena
- tags / classificacao local simples

## O que esta sendo afirmado

- o bloco de conteudo existe como composicao declarativa
- o id local serve para ancoragem interna da entrada dentro da fase
- o papel / tipo da cena comeca com tipagem forte e fechada no V1
- as tags locais classificam a entrada no contexto da fase sem sobrescrever a cena
- esta e apenas a direcao conceitual do V1, nao o shape tecnico final

## O que nao esta sendo decidido

- shape tecnico final da entrada de conteudo
- nome final do bloco / estrutura interna
- futura evolucao para grupos / categorias
- futura extracao para sub-assets
- relacao futura com conteudo remoto, DLC ou pacotes
- qualquer refinamento especifico exigido pelo jogo concreto

## Em aberto de proposito

- como essa composicao sera representada tecnicamente depois
- quais partes poderao ser reutilizaveis no futuro
- quando e como o bloco pode evoluir para cortes mais modulares

## Consequencias

- o V1 registra a composicao de conteudo como leitura declarativa, nao como monolito tecnico
- o `PhaseDefinition` continua como autor da composicao da fase, sem rebaixar a cena concreta
- o projeto preserva espaco para evolucao sem congelar decisoes prematuras

## Fechamento

Este ADR congela apenas a direcao conceitual minima do bloco de conteudo no V1.
Tudo o que for detalhe tecnico, modular ou dependente de produto permanece para ADRs futuros.
