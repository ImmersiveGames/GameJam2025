# ADR-0052 - Direcao conceitual do bloco de Rules/Objectives no PhaseDefinition V1

## Status
- Estado: Aceito
- Data: 2026-04-04
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048` e `ADR-0049` ja fecharam a leitura canonica da gameplay, da fase jogavel e do shape conceitual minimo do `PhaseDefinition` no V1.

Falta registrar a direcao conceitual minima do bloco `Rules/Objectives` dentro do `PhaseDefinition`, sem congelar ainda shape tecnico final, implementacao, modularizacao futura ou separacao definitiva dos dois dominios.

## Decisao

O bloco `Rules/Objectives` passa a ser tratado como um bloco declarativo unico dentro do `PhaseDefinition`.

No V1, esse bloco permanece unico por simplicidade externa, mas internamente ja possui duas listas separadas: `Rules` e `Objectives`.

### Leitura canonica

- `Rules/Objectives` e obrigatorio no V1
- o bloco declara o que vale e o que precisa ser alcancado na fase
- o bloco e declarativo, nao operacional
- no V1, o bloco permanece unico por simplicidade externa
- internamente, `Rules` e `Objectives` ja sao distinguidos por listas separadas
- o V1 deve manter simplicidade e generalidade, sem overdesign

### Estrutura conceitual minima

Cada item de `Rules` e `Objectives` deve comecar com:

- id local
- tipo forte
- parametros declarativos

Os parametros declarativos devem ser simples e especificos por tipo.

## O que esta sendo afirmado

- `Rules/Objectives` em `PhaseDefinition` declara o que vale e o que precisa ser alcancado na fase
- o id local existe para identificacao interna dentro da fase
- a tipagem forte e os parametros por tipo existem para preservar clareza, consistencia e evolucao futura
- esta e apenas a direcao conceitual do V1, nao o shape tecnico final

## O que nao esta sendo decidido

- shape tecnico final de cada item
- futura modularizacao ou intercambialidade maior
- futura extracao para sub-assets
- eventual separacao mais forte entre `Rules` e `Objectives`
- qualquer refinamento especifico exigido pelo jogo concreto

## Em aberto de proposito

- como essas listas serao representadas tecnicamente depois
- quais partes poderao ser reutilizaveis no futuro
- quando e como o bloco pode evoluir para cortes mais modulares

## Consequencias

- o V1 registra o conjunto de regras e objetivos como leitura declarativa, nao como operacao
- o `PhaseDefinition` continua compondo a fase sem congelar a separacao final entre os dois dominios
- o projeto preserva espaco para evolucao sem congelar decisoes prematuras

## Fechamento

Este ADR congela apenas a direcao conceitual minima do bloco `Rules/Objectives` no V1.
Tudo o que for detalhe tecnico, modular ou dependente de produto permanece para ADRs futuros.
