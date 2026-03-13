# Navigation

## Estado atual

- `GameNavigationCatalogAsset` e o unico asset canonico de navigation.
- Cada entry resolve por `routeRef + transitionStyleRef`.
- Navigation nao usa catalogos nominais paralelos.
- `startup` nao pertence a navigation; pertence ao bootstrap.

## Ownership

- `GameNavigationCatalogAsset`: owner de slots e extras de navigation.
- `GameNavigationService`: resolve a entry canonica e despacha a transicao fail-fast.
- `TransitionStyleAsset`: style ligado a cada entry.
- `ExitToMenuCoordinator`: owner da saida global para menu.
- `MacroRestartCoordinator`: owner do restart macro.

## Regras praticas

- Semantica de `frontend/gameplay` vem da rota resolvida.
- Labels de style/profile sao apenas observabilidade.
- `Restart` segue por reset macro.
- `Exit` pode encerrar o `PostGame` global, mas a navegacao continua centralizada em navigation.

## Leitura cruzada

- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
