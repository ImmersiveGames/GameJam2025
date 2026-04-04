# ADR-0057 - Direcao conceitual da resolucao de PhaseDefinition a partir do catalogo no V1

## Status
- Estado: Aceito
- Data: 2026-04-04
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048`, `ADR-0049` e `ADR-0056` ja fecharam a leitura canonica da fase como composicao autoral autocontida e da existencia de catalogo explicito de `PhaseDefinition` no V1.

Falta registrar a direcao conceitual minima da resolucao / selecao de `PhaseDefinition` a partir do catalogo no V1, sem congelar shape tecnico final, implementacao, runtime wiring ou tooling avancado.

## Decisao

A resolucao de `PhaseDefinition` no V1 acontece principalmente por id interno da phase.

Esse id interno deve ser globalmente unico no ecossistema de phases. O catalogo organiza e expõe, mas nao cria identidade paralela para a phase.

### Leitura canonica

- o catalogo organiza, o id resolve
- a identidade principal pertence a propria `PhaseDefinition`
- multiplos catalogos podem referenciar a mesma phase sem alterar sua identidade
- a resolucao por id evita acoplamento a ordem editorial do catalogo
- catalogo e resolucao permanecem separados conceitualmente
- o V1 deve manter simplicidade, clareza e baixo acoplamento

## O que esta sendo afirmado

- o id interno e a chave principal de resolucao da phase
- o catalogo e uma camada de organizacao e exposicao
- a mesma `PhaseDefinition` pode aparecer em multiplos catalogos
- esta e apenas a direcao conceitual do V1, nao o shape tecnico final

## O que nao esta sendo decidido

- shape tecnico final da resolucao
- estrategia concreta de lookup / runtime
- integracao com `GameplaySessionFlow`
- tooling / editor
- qualquer refinamento especifico exigido pelo jogo concreto

## Consequencias

- o V1 evita acoplamento entre identidade da phase e ordem do catalogo
- o projeto preserva uma separacao limpa entre catalogo, identidade e resolucao
- a selecao de fase fica previsivel sem criar identidade paralela desnecessaria

## Fechamento

Este ADR congela apenas a direcao conceitual minima da resolucao de `PhaseDefinition` a partir do catalogo no V1.
Tudo o que for detalhe tecnico, tooling, runtime ou dependente de produto permanece para ADRs futuros.
