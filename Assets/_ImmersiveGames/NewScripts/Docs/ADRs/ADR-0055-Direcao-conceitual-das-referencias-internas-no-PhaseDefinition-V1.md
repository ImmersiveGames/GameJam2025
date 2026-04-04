# ADR-0055 - Direcao conceitual das referencias internas no PhaseDefinition V1

## Status
- Estado: Aceito
- Data: 2026-04-04
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048`, `ADR-0049` e os ADRs dos blocos internos do `PhaseDefinition` no V1 ja fecharam a leitura canonica da fase como composicao autoral autocontida.

Falta registrar a direcao conceitual minima da linguagem de referencias internas entre esses blocos, sem congelar shape tecnico final, implementacao, integracao runtime ou tooling avancado.

## Decisao

As referencias internas entre blocos do `PhaseDefinition` no V1 passam a usar ids internos estaveis, tipados por dominio, e apenas quando houver necessidade explicita.

O id interno e a chave de contrato entre blocos. Um label externo pode existir como apoio editorial, mas nao substitui o id interno e nao e obrigatorio no contrato minimo do V1.

### Leitura canonica

- as referencias internas apontam apenas para elementos da propria `phase`
- o `PhaseDefinition` permanece autocontido no V1
- o id interno e a chave de referencia entre blocos
- o label externo, quando existir, e apoio editorial e nao chave de referencia
- a tipagem por dominio existe para preservar clareza, fail-fast e evitar referencia cruzada ambigua
- as referencias existem para ancorar a receita da fase, nao a operacao/runtime

## O que esta sendo afirmado

- os ids internos sao estaveis dentro do contrato da fase
- os ids internos nao sao o nome visivel principal
- a referencia entre blocos deve ser usada so quando houver necessidade explicita
- a simplicidade do V1 depende de baixo acoplamento e responsabilidade clara
- esta e apenas a direcao conceitual do V1, nao o shape tecnico final

## O que nao esta sendo decidido

- shape tecnico final dessas referencias
- estrategia futura de tooling/editor
- futura camada de localizacao
- eventual evolucao para referencias externas
- qualquer refinamento especifico exigido pelo jogo concreto

## Consequencias

- o `PhaseDefinition` ganha uma linguagem interna de contrato mais clara entre blocos
- o V1 preserva referenciais autocontidos e tipados sem exigir modelagem avancada
- o projeto evita ambiguidade de referencia e dependencia desnecessaria de runtime

## Fechamento

Este ADR congela apenas a direcao conceitual minima das referencias internas no V1.
Tudo o que for detalhe tecnico, tooling, localizacao ou dependente de produto permanece para ADRs futuros.
