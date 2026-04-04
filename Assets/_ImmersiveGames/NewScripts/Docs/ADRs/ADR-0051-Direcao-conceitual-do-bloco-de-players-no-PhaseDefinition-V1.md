# ADR-0051 - Direcao conceitual do bloco de Players no PhaseDefinition V1

## Status
- Estado: Aceito
- Data: 2026-04-04
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048` e `ADR-0049` ja fecharam a leitura canonica da gameplay, da fase jogavel e do shape conceitual minimo do `PhaseDefinition` no V1.

Falta registrar a direcao conceitual minima do bloco `Players` dentro do `PhaseDefinition`, sem congelar ainda operação runtime, instanciação, shape tecnico final ou modularizacao futura.

## Decisao

O bloco `Players` passa a ser tratado como declaracao semantica de participacao da fase dentro do `PhaseDefinition`.

No V1, esse bloco e uma lista explicita de participantes. Cada participante possui um id local semantico e um papel / tipo de participacao forte.

### Leitura canonica

- `Players` e obrigatorio no V1
- o bloco representa quem participa semanticamente da fase
- o bloco e declarativo, nao operacional
- a lista explicita existe para permitir leitura clara do conjunto de participantes da fase
- o V1 deve manter simplicidade e generalidade, sem overdesign

### Estrutura conceitual minima de cada participante

- id local semantico
- papel / tipo de participacao forte

## O que esta sendo afirmado

- `Players` em `PhaseDefinition` declara participacao semantica da fase
- o bloco nao deve ser tratado como operacao, instanciação ou wiring
- o id local semantico existe para identificacao interna dentro da fase
- o papel / tipo de participacao deve comecar com tipagem forte e fechada no V1
- esta e apenas a direcao conceitual do V1, nao o shape tecnico final

## O que nao esta sendo decidido

- shape tecnico final da entrada de participante
- eventual evolucao para estrutura mais rica
- futura extracao para sub-assets
- qualquer integracao runtime
- qualquer refinamento especifico exigido pelo jogo concreto

## Em aberto de proposito

- como essa lista sera representada tecnicamente depois
- quais partes poderao ser reutilizaveis no futuro
- quando e como o bloco pode evoluir para cortes mais modulares

## Consequencias

- o V1 registra a participacao da fase como leitura declarativa, nao como operacao
- o `PhaseDefinition` continua compondo a fase sem misturar identidade da peca com integracao runtime
- o projeto preserva espaco para evolucao sem congelar decisoes prematuras

## Fechamento

Este ADR congela apenas a direcao conceitual minima do bloco `Players` no V1.
Tudo o que for detalhe tecnico, modular ou dependente de produto permanece para ADRs futuros.
