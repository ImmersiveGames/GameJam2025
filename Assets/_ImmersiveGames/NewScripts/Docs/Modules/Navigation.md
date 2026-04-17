# Navigation

## Estado atual

- `GameNavigationCatalogAsset` e o unico asset canonico de navigation.
- Cada entry resolve por `routeRef + transitionStyleRef`.
- Navigation nao usa catalogos nominais paralelos.
- `startup` nao pertence a navigation; pertence ao bootstrap.
- `Restart` e um intent de contexto que precisa de continuidade externa; Navigation apenas despacha a rota macro.

## Ownership

- `GameNavigationCatalogAsset`: owner de slots e extras de navigation.
- `GameNavigationService`: resolve a entry canonica e despacha a transicao fail-fast.
- `TransitionStyleAsset`: style ligado a cada entry.
- `ExitToMenu` e dispatch canonico direto em `GameNavigationService`.
- `Restart` nao e owner de `Navigation`; a continuidade e resolvida por `GameplaySessionFlow` e `RestartContextService`.
- `Navigation` e owner do dispatch macro de rota, incluindo a saida ao menu.
- `Navigation` nao escolhe phase; ela entrega a gameplay macro route para o rail canônico resolver a sessao.
- `Game/Content/Definitions/Levels` nao e owner daqui; ele e apenas conteudo consumido por rotas de gameplay.

## Regras praticas

- Semantica de `frontend/gameplay` vem da rota resolvida.
- Labels de style/profile sao apenas observabilidade.
- `Exit` pode encerrar o rail terminal, mas a navegacao continua centralizada em `GameNavigationService`.
- `Restart` nao passa por ponte de menu ou coordenador de UI.
- `ExitToMenu` nao depende semanticamente de `GameLoop`.
- O log `StartGameplayRouteAsync without explicit level selection` representa apenas a ausencia de selecao no dispatch macro, nao uma decisao de phase dentro de `Navigation`.
- A decisao concreta de restart atualiza o contexto no rail canônico de gameplay, nao em `Navigation`.

## Leitura cruzada

- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
