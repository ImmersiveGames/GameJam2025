# Architecture

## Rota canônica
- `SceneRouteDefinitionAsset` é a definição canônica de rota.
- `GameNavigationCatalogAsset` é um catálogo fino de intent.
- `SceneFlow` consome `ResolvedRouteDefinition` / rota já materializada.

## Fluxo atual
1. navigation resolve `intent -> routeRef + transitionStyleRef`
2. `SceneTransitionRequest` carrega a rota já resolvida
3. `SceneTransitionService` executa a rota sem resolver catálogo global de novo
4. `LevelFlow` e startup usam `routeRef` direto quando precisam de rota canônica

## Regras
- Não existe mais catálogo global de rota no runtime principal.
- `SceneRouteCatalogAsset` não faz parte da arquitetura ativa.
- `SceneRouteDefinitionAsset` é a fonte de verdade da rota.
- Validação/editor leem `SceneRouteDefinitionAsset`, `GameNavigationCatalogAsset` e configs de bootstrap.

## Leitura cruzada
- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/Navigation.md`
- `Docs/ADRs/ADR-0039-Canonical-Scene-Identity-and-Addressables-Seam.md`
