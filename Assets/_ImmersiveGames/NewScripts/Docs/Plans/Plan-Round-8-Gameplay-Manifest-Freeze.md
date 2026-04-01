# Plan - Round 8 - Gameplay Manifest Freeze

## Resumo

Esta rodada congela o estado implantado da camada declarativa de gameplay por level.
Ela consolida a decisao arquitetural, o runtime integrado, o authoring real e a observabilidade minima ja ativos.

## Decisao Arquitetural

- `LevelDefinitionAsset` e o owner autoral do manifesto por level;
- `GameplayContentManifest` e a declaracao level-scoped;
- `GameplayContentEntry` e a unidade declarativa minima;
- `LevelFlowContentService` resolve e valida o manifesto no boundary canonico de entrada do level;
- `WorldDefinition` continua fora dessa responsabilidade.

## Implantacao Runtime

O manifesto ja esta integrado no fluxo real de entrada do level:

- no prepare macro, apos resolver o `LevelDefinitionAsset`;
- no swap local, apos resolver o `LevelDefinitionAsset`;
- sem alterar spawn, registry, reset, reconstituicao ou materializacao operacional.

## Authoring Real

O level de teste `Level1` ja possui authoring real do manifesto com as entries:

- `player_main`
- `eater_aux`
- `dummy_prototype`

Essas entradas usam `Player`, `Eater` e `Dummy` como mocks ou proxies de validacao, nao como modelagem final.

## Observabilidade Minima

O runtime ja emite observacao de aceitacao do manifesto quando ele e resolvido com sucesso.
O log expõe, de forma deterministica:

- `levelRef`;
- quantidade de entries;
- ids declarativos resumidos;
- roles presentes;
- estado vazio quando aplicavel.

## Limites do Contrato

Fica fora deste contrato:

- spawn operacional;
- registry runtime;
- reset e reconstituicao;
- observabilidade downstream;
- taxonomia final de objetos, entidades ou componentes;
- migracao do legado `Scripts`.

## Decisao Final

A camada declarativa de gameplay por level fica congelada como contrato canonico de entrada, com authoring real em `Level1`, resolucao valida no hook canonico e observabilidade minima ativa, sem assumir execucao operacional de gameplay.
