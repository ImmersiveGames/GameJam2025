# ADR-0058 - Direcao conceitual do consumo de PhaseDefinition pelo GameplaySessionFlow no V1

## Status
- Estado: Aceito
- Data: 2026-04-04
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048`, `ADR-0049`, `ADR-0056` e `ADR-0057` ja fecharam a leitura canonica da fase como composicao autoral autocontida, do catalogo explicito e da resolucao de `PhaseDefinition`.

Falta registrar a direcao conceitual minima de como `GameplaySessionFlow` consome `PhaseDefinition` no V1, sem congelar shape tecnico final, implementacao, runtime wiring detalhado ou otimizacoes futuras.

## Decisao

`GameplaySessionFlow` consome a `PhaseDefinition` inteira como input principal.

O catalogo organiza e expõe, a resolucao encontra a `PhaseDefinition`, e o `GameplaySessionFlow` consome a propria definicao autoral como referencia imutavel. O runtime nao manipula o asset autoral; ele deriva dele uma versao em memoria.

### Leitura canonica

- `GameplaySessionFlow` recebe `PhaseDefinition` ja resolvida, nao um request indireto como entrada principal
- `PhaseDefinition` permanece como source of truth autoral e nao deve ser mutada pelo runtime
- os blocos runtime nascem por derivacao a partir dessa definicao
- a derivacao segue trilho canonico e previsivel, nao montagem livre
- a sequencia de derivacao no V1 permanece alinhada ao runtime ja consolidado

## O que esta sendo afirmado

- a entrada principal do `GameplaySessionFlow` e a propria `PhaseDefinition`
- `SessionContext`, `PhaseRuntime`, `Players`, `Rules/Objectives` e `InitialState` podem ser lidos como parte da derivacao canonica
- o runtime deriva a leitura operacional da definicao autoral, sem reescrever a definicao
- esta e apenas a direcao conceitual do V1, nao o shape tecnico final

## O que nao esta sendo decidido

- shape tecnico final da estrutura em memoria
- estrategia concreta de derivacao / lookup
- otimizacoes futuras
- caching
- runtime wiring detalhado
- qualquer refinamento especifico exigido pelo jogo concreto

## Consequencias

- o `GameplaySessionFlow` opera sobre uma definicao autoral ja resolvida e estavel
- o projeto preserva uma separacao clara entre asset autoral e derivacao runtime
- a derivacao ganha um trilho previsivel e canonicamente alinhado

## Fechamento

Este ADR congela apenas a direcao conceitual minima do consumo de `PhaseDefinition` pelo `GameplaySessionFlow` no V1.
Tudo o que for detalhe tecnico, wiring, runtime ou dependente de produto permanece para ADRs futuros.
