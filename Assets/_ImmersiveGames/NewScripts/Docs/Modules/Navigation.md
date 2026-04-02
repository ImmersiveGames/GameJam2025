# Navigation

## Estado atual

- `GameNavigationCatalogAsset` e o unico asset canonico de navigation.
- Cada entry resolve por `routeRef + transitionStyleRef`.
- Navigation nao usa catalogos nominais paralelos.
- `startup` nao pertence a navigation; pertence ao bootstrap.
- `Restart` e executado pelo contexto local em `LevelLifecycle`, nao por `Navigation`.

## Ownership

- `GameNavigationCatalogAsset`: owner de slots e extras de navigation.
- `GameNavigationService`: resolve a entry canonica e despacha a transicao fail-fast.
- `TransitionStyleAsset`: style ligado a cada entry.
- `ExitToMenu` e dispatch canonico direto em `GameNavigationService`.
- `Restart` nao e owner de `Navigation`; a execucao canonica fica em `LevelLifecycle`.
- `Navigation` e owner do dispatch macro de rota, incluindo a saida ao menu.
- `Navigation` nao escolhe level; ela entrega a gameplay macro route para `LevelLifecycle` finalizar a selecao.
- `Game/Content/Definitions/Levels` nao e owner daqui; ele so e consumido pelas rotas de gameplay.

## Regras praticas

- Semantica de `frontend/gameplay` vem da rota resolvida.
- Labels de style/profile sao apenas observabilidade.
- `Exit` pode encerrar o `PostRun` global, mas a navegacao continua centralizada em `GameNavigationService`.
- `Restart` nao passa por ponte de menu ou coordinador no `GameLoop`.
- `ExitToMenu` nao depende semanticamente de `GameLoop`.
- O log `StartGameplayRouteAsync without explicit level selection` representa apenas a ausencia de selecao no dispatch macro, nao uma decisao de nivel dentro de `Navigation`.
- A decisao concreta de restart atualiza o contexto em `LevelLifecycle`, nao em `Navigation`.

## Leitura cruzada

- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
