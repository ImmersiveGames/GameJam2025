# ADR-0049 - Shape conceitual minimo do PhaseDefinition no V1

## Status
- Estado: Aceito
- Data: 2026-04-03
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047` e `ADR-0048` ja fecharam a leitura canonica da gameplay, do `GameplaySessionFlow` e da autoridade autoral do `PhaseDefinition`.

Falta registrar o shape conceitual minimo do `PhaseDefinition` no V1, sem congelar ainda o shape tecnico final, sub-assets, modularizacao futura ou o modelo completo de authoring.

## Decisao

O `PhaseDefinition` no V1 passa a ter um shape conceitual minimo e simples, embutido no proprio asset, como agregador central dos blocos semanticos da fase jogavel.

Essa decisao registra a leitura autoral da fase, mas nao transforma o `PhaseDefinition` em monolito obrigatorio de tudo.

### Leitura canonica

- `PhaseDefinition` e a fonte de verdade autoral da fase jogavel
- o `PhaseDefinition` comeca minimo
- os blocos do V1 sao declarativos, nao operacionais
- no V1, os blocos ficam embutidos de forma simples dentro do proprio `PhaseDefinition`
- o bloco de identidade / metadados e um pouco mais rico que o minimo tecnico
- o V1 deve manter simplicidade e generalidade, sem overdesign e sem modularizacao precoce

### Blocos obrigatorios de primeiro nivel no V1

- identidade / metadados
- conteudo da fase
- players
- rules/objectives
- initial state
- fechamento da fase

## O que esta sendo afirmado

- estes blocos formam o shape conceitual minimo do V1
- `PhaseDefinition` e o agregador semantico central da fase
- a arquitetura do V1 gravita diretamente em torno de `PhaseDefinition`, nao de `Level`
- a fase continua sendo lida como um conjunto autoral, nao apenas como um pacote tecnico

## O que nao esta sendo decidido

- shape tecnico final de cada bloco
- nome e forma interna final de cada bloco
- futura extracao de partes modulares
- eventual separacao em assets independentes
- relacao final com conteudo remoto, DLC ou pacotes
- qualquer evolucao especifica do jogo concreto

## Em aberto de proposito

- quais partes serao reutilizaveis
- qual sera o modelo completo de authoring
- como o V1 evolui para cortes mais modulares quando houver necessidade real

## Consequencias

- o V1 ganha um contrato conceitual minimo sem overdesign
- o projeto preserva espaco para evolucao sem reescrever a decisao autoral
- o `PhaseDefinition` fica claro como centro semantico da fase, mas sem receber ainda o shape tecnico final

## Fechamento

Este ADR congela apenas o minimo conceitual necessario para o V1.
Tudo o que for detalhe tecnico, modular ou dependente de produto permanece para ADRs futuros.
