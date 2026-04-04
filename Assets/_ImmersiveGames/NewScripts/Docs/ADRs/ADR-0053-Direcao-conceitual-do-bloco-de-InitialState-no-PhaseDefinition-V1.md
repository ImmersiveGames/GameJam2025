# ADR-0053 - Direcao conceitual do bloco de InitialState no PhaseDefinition V1

## Status
- Estado: Aceito
- Data: 2026-04-04
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048` e `ADR-0049` ja fecharam a leitura canonica da gameplay, da fase jogavel e do shape conceitual minimo do `PhaseDefinition` no V1.

Falta registrar a direcao conceitual minima do bloco `InitialState` dentro do `PhaseDefinition`, sem congelar ainda shape tecnico final, implementacao, modularizacao futura ou integracao runtime.

## Decisao

O bloco `InitialState` passa a ser tratado como um bloco declarativo unico dentro do `PhaseDefinition`.

No V1, esse bloco permanece unico por simplicidade externa, mas internamente ja e organizado como uma lista de entradas de estado inicial.

### Leitura canonica

- `InitialState` e obrigatorio no V1
- o bloco declara como a fase nasce semanticamente
- o bloco e declarativo, nao operacional
- no V1, o bloco permanece unico por simplicidade externa
- internamente, ele ja e tratado como lista de entradas para manter clareza e evolucao futura
- o V1 deve manter simplicidade e generalidade, sem overdesign

### Estrutura conceitual minima de cada entrada

- id local
- tipo forte
- parametros declarativos

Os parametros declarativos devem ser simples e especificos por tipo.

## O que esta sendo afirmado

- `InitialState` em `PhaseDefinition` declara como a fase nasce semanticamente
- o id local existe para identificacao interna dentro da fase
- a tipagem forte e os parametros por tipo existem para preservar clareza, consistencia e evolucao futura
- esta e apenas a direcao conceitual do V1, nao o shape tecnico final

## O que nao esta sendo decidido

- shape tecnico final de cada entrada
- futura modularizacao ou estrutura mais rica
- futura extracao para sub-assets
- qualquer integracao runtime
- qualquer refinamento especifico exigido pelo jogo concreto

## Em aberto de proposito

- como essa lista sera representada tecnicamente depois
- quais partes poderao ser reutilizaveis no futuro
- quando e como o bloco pode evoluir para cortes mais modulares

## Consequencias

- o V1 registra o estado inicial da fase como leitura declarativa, nao como operacao
- o `PhaseDefinition` continua compondo a fase sem congelar a integracao runtime
- o projeto preserva espaco para evolucao sem congelar decisoes prematuras

## Fechamento

Este ADR congela apenas a direcao conceitual minima do bloco `InitialState` no V1.
Tudo o que for detalhe tecnico, modular ou dependente de produto permanece para ADRs futuros.
