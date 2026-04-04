# ADR-0056 - Direcao conceitual do catalogo de PhaseDefinition no V1

## Status
- Estado: Aceito
- Data: 2026-04-04
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048`, `ADR-0049` e os ADRs dos blocos internos e de referencias internas do `PhaseDefinition` no V1 ja fecharam a leitura canonica da fase como composicao autoral autocontida.

Falta registrar a direcao conceitual minima do catalogo / selecao de `PhaseDefinition` no V1, sem congelar shape tecnico final, implementacao, tooling avancado ou integracao runtime.

## Decisao

A selecao de fase no desenho ideal passa a acontecer por catalogo explicito de `PhaseDefinition`.

No V1, esse catalogo e contido e deterministico. Ele nao depende de descoberta implicita por pasta, naming ou busca automatica. A arquitetura nao pressupoe catalogo unico/global por natureza: o catalogo e conceitualmente multiplicavel, mesmo que o projeto comece com um catalogo concreto so.

### Leitura canonica

- `PhaseDefinition` entra no sistema por registro explicito, nao por descoberta implicita
- o catalogo organiza e orienta a selecao da phase
- a identidade principal continua sendo da propria `PhaseDefinition`
- o catalogo nao cria um id paralelo para a phase no V1
- cada entrada do catalogo referencia a propria `PhaseDefinition`
- os metadados do catalogo servem para organizacao editorial e sinais simples de selecao / uso
- o V1 pode comecar com um catalogo concreto unico, sem transformar unicidade em principio arquitetural
- o V1 deve manter simplicidade, clareza e baixo acoplamento

## O que esta sendo afirmado

- o catalogo e o ponto explicito de entrada da fase
- a referencia da fase e a base principal da identidade
- os metadados do catalogo existem para ordenacao, agrupamento, visibilidade e sinais simples de uso
- o catalogo e multiplicavel por conceito, nao preso a uma instancia global unica
- esta e apenas a direcao conceitual do V1, nao o shape tecnico final

## O que nao esta sendo decidido

- shape tecnico final do catalogo
- forma concreta de serializacao / asset
- tooling / editor especifico
- integracao runtime
- criterios finais de default / featured / visibilidade
- qualquer refinamento especifico exigido pelo jogo concreto

## Consequencias

- o V1 ganha selecao explicita e previsivel de `PhaseDefinition`
- o projeto evita descoberta implicita e reduz ambiguidade operacional
- o catalogo organiza a fase sem criar uma identidade paralela desnecessaria
- a possibilidade de multiplos catalogos fica preservada sem exigir isso no estado inicial

## Fechamento

Este ADR congela apenas a direcao conceitual minima do catalogo de `PhaseDefinition` no V1.
Tudo o que for detalhe tecnico, tooling, runtime ou dependente de produto permanece para ADRs futuros.
