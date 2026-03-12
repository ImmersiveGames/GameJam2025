# Navigation

## Estado atual

- `GameNavigationCatalogAsset` e o unico asset canonico de navigation.
- Cada entry resolve por `routeRef + transitionStyleRef`.
- Navigation nao depende de catalogos nominais paralelos.
- `startup` nao pertence a navigation; pertence ao bootstrap.

## Ownership

- `GameNavigationCatalogAsset`: owner de `routeRef + transitionStyleRef` por slot/entry.
- `GameNavigationService`: resolve intent core para entry canonica e despacha `SceneTransitionRequest` fail-fast.
- `TransitionStyleAsset`: owner do estilo visual associado a cada entry.

## Semantica de fluxo

- `frontend` e `gameplay` pertencem a `SceneRouteKind` da rota resolvida.
- Labels de style/profile sao apenas observabilidade.

## Leitura cruzada

- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/GameLoop.md`
