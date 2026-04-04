# ADR-0054 - Direcao conceitual do bloco de fechamento da fase no PhaseDefinition V1

## Status
- Estado: Aceito
- Data: 2026-04-04
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048` e `ADR-0049` ja fecharam a leitura canonica da gameplay, da fase jogavel e do shape conceitual minimo do `PhaseDefinition` no V1.

Falta registrar a direcao conceitual minima do bloco de fechamento da fase dentro do `PhaseDefinition`, sem congelar ainda shape tecnico final, implementacao, modularizacao futura ou integracao runtime.

## Decisao

O bloco de fechamento da fase passa a ser tratado como um bloco declarativo unico dentro do `PhaseDefinition`.

No V1, esse bloco permanece unico por simplicidade externa e e estruturado como um conjunto de campos / parametros de fechamento, nao como lista.

### Leitura canonica

- o bloco de fechamento da fase e obrigatorio no V1
- o bloco declara como a fase consolida semanticamente seu encerramento
- o bloco e declarativo, nao operacional
- no V1, o bloco permanece unico por simplicidade externa
- o resultado da run e a continuidade pos-run ja sao distinguidos dentro da leitura do bloco
- o V1 deve manter simplicidade e generalidade, sem overdesign

### Estrutura conceitual minima

O bloco deve declarar explicitamente:

- resultado da run
- politica de continuidade pos-run

Ambos comecam com tipagem forte e fechada no V1.

## O que esta sendo afirmado

- o bloco de fechamento da fase declara como a fase consolida semanticamente seu encerramento
- o resultado da run existe como leitura forte do encerramento
- a continuidade pos-run existe como conjunto fechado de intencoes permitidas no V1
- esta e apenas a direcao conceitual do V1, nao o shape tecnico final

## O que nao esta sendo decidido

- shape tecnico final dos campos internos
- futura modularizacao ou estrutura mais rica
- futura extracao para sub-assets
- qualquer integracao runtime
- qualquer refinamento especifico exigido pelo jogo concreto

## Em aberto de proposito

- como esses campos serao representados tecnicamente depois
- quais partes poderao ser reutilizaveis no futuro
- quando e como o bloco pode evoluir para cortes mais modulares

## Consequencias

- o V1 registra o fechamento da fase como leitura declarativa, nao como operacao
- o `PhaseDefinition` continua compondo a fase sem congelar a integracao runtime
- o projeto preserva espaco para evolucao sem congelar decisoes prematuras

## Fechamento

Este ADR congela apenas a direcao conceitual minima do bloco de fechamento da fase no V1.
Tudo o que for detalhe tecnico, modular ou dependente de produto permanece para ADRs futuros.
