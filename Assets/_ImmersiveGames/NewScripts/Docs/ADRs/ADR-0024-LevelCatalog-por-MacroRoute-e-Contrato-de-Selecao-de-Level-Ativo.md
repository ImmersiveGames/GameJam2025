# ADR-0024 - LevelCatalog por MacroRoute e Contrato de Selecao de Level Ativo

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05
- Tipo: Implementacao
- Escopo: NewScripts/Modules (LevelFlow, Navigation)

## Resumo

Padronizar a relacao **macroRoute -> levels** com selecao deterministica de level ativo por macro gameplay.

## Decisao

1. A fonte canonica de levels por macro gameplay e `SceneRouteDefinitionAsset.LevelCollection`.
2. Sem snapshot valido para a mesma macro, o default e sempre `levels[0]` (`source='catalog_index_0'`).
3. Nao existe fallback para `LevelCatalogAsset` na selecao/aplicacao durante `LevelPrepare`.
4. Configuracao invalida (catalogo de rotas ausente, rota ausente, macro gameplay sem colecao, colecao invalida) gera fail-fast duro via `[FATAL][H1][LevelFlow]`.

## Estado real no codigo (2026-03-05)

- Novos assets de dados de level:
  - `Modules/LevelFlow/Config/LevelDefinitionAsset.cs`
  - `Modules/LevelFlow/Config/LevelCollectionAsset.cs`
  - `Modules/LevelFlow/Config/SceneBuildIndexRef.cs`
- `SceneRouteDefinitionAsset` recebe `LevelCollectionAsset` por macro route.
- `LevelMacroPrepareService` resolve obrigatoriamente `SceneRouteCatalogAsset -> routeAsset -> LevelCollection`.
- `LevelMacroPrepareService` sempre executa:
  - selecao default index 0 quando nao ha snapshot valido da mesma macro;
  - `ResetLevelAsync(...)`;
  - unload/load aditivo do level via `LevelAdditiveSceneRuntimeApplier`.
- Sem configuracao valida, o fluxo para com fail-fast (sem degrade/fallback).

## Criterios de aceite (DoD)

- [x] Fonte unica para levels em gameplay: `LevelCollectionAsset` na macro route.
- [x] Default deterministico por index 0 quando nao ha selecao valida.
- [x] Sem fallback para `LevelCatalogAsset` no `LevelPrepare`.
- [x] Invalidos de configuracao quebram com `[FATAL][H1][LevelFlow]`.

## Changelog

- 2026-03-05: atualizado para estado real no codigo com politica no-fallback em `LevelPrepare`.
- 2026-03-05: revisado com base nas auditorias de 2026-03-04/2026-03-05 e no codigo atual.
