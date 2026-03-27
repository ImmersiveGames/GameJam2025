> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0017 — LevelManager / Config / Catalog (nome histórico preservado)

## Status

- Estado: **Implementado e absorvido pelo trilho canônico de LevelFlow**
- Data (decisão): **2026-01-31**
- Última atualização: **2026-03-25**
- Tipo: **Implementação / base histórica de catálogo de level**
- Escopo atual: `LevelFlow` + `SceneRouteDefinitionAsset.LevelCollection`

## Precedência

O nome do arquivo foi preservado por rastreabilidade, mas o shape operacional atual é dado por ADRs posteriores:
- `ADR-0020`: separação entre rota macro e semântica local;
- `ADR-0024`: coleção de levels por macro route e contrato do level ativo;
- `ADR-0026`: swap local sem trilho macro;
- `ADR-0027`: intro opcional level-owned e pós-run global.

Este ADR permanece como a **base histórica da decisão de sair do hardcode** e adotar assets/configuração como fonte de verdade.

## Contexto

O sistema precisava parar de depender de lista hardcoded/manager legado para enumerar conteúdo jogável.

A decisão estrutural que continua válida é:
- definição de level deve viver em assets/configuração;
- resolução de level deve ser explícita, auditável e fail-fast;
- o runtime canônico não deve depender de `Modules/Levels` legado.

## Decisão canônica atual

### 1) O trilho canônico é `LevelFlow`, não `LevelManager`

A superfície atual é:
- `LevelDefinitionAsset`
- `LevelCollectionAsset`
- `LevelMacroPrepareService`
- `LevelSwapLocalService`
- `RestartContextService`
- `GameplayStartSnapshot`

### 2) A configuração de level é asset-based e fail-fast

A definição de conteúdo jogável continua orientada a assets. Falhas de configuração obrigatória devem parar o fluxo cedo, não produzir default silencioso fora do contrato.

### 3) O catálogo de levels não é mais um catálogo global paralelo solto

No shape atual, a fonte canônica para gameplay está vinculada à macro route via `SceneRouteDefinitionAsset.LevelCollection`.

Ou seja:
- a route define o domínio macro;
- a `LevelCollection` dessa route define os levels válidos naquele domínio.

## O que este ADR ainda afirma com segurança

- conteúdo jogável não deve depender de hardcode;
- `LevelFlow` é o trilho canônico atual para semântica de level;
- assets/configs são a fonte de verdade;
- bridges/managers legados ficam fora do trilho principal.

## O que foi refinado depois

Este ADR não deve mais ser lido para decidir sozinho:
- como a identidade de level é calculada (`ADR-0022`);
- como macro reset e level reset se separam (`ADR-0023`);
- como o level ativo é selecionado (`ADR-0024`);
- como o swap local funciona (`ADR-0026`);
- como intro/post se distribuem (`ADR-0027`).

## Consequências

### Positivas
- a intenção original de sair do hardcode foi preservada;
- o runtime atual fica coerente com uma arquitetura asset-based;
- a semântica de level fica fora de `SceneFlow` e fora de managers legados.

### Trade-offs
- o nome do ADR ficou desatualizado em relação ao runtime;
- leituras antigas sobre `LevelManager` devem ser tratadas como históricas.

## Relação com outros ADRs

- `ADR-0019`: navigation/transition em direct-ref.
- `ADR-0020`: rota macro vs conteúdo/local.
- `ADR-0024`: level ativo por `LevelCollection` da route.
- `ADR-0026`: swap local sem transição macro.
