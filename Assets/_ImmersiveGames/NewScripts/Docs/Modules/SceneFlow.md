# SceneFlow

## Estado atual

- `SceneTransitionService` e o owner da timeline de transicao.
- `startup` pertence ao bootstrap por `NewScriptsBootstrapConfigAsset.startupTransitionStyleRef`.
- `frontend` e `gameplay` pertencem a `SceneRouteKind`.
- Navigation e transition operam em direct-ref + fail-fast.
- `GameNavigationCatalogAsset` resolve `routeRef + transitionStyleRef`.
- `TransitionStyleAsset` resolve `profileRef + useFade`.
- `SceneTransitionProfile` permanece asset leaf visual.

## Ownership

- `SceneTransitionService`: fases da transicao e timeline.
- `SceneRouteCatalogAsset` + `SceneRouteDefinitionAsset`: definicao de rota, `RouteKind`, target scene e reset policy.
- `TransitionStyleAsset`: style estrutural da transicao.
- `SceneFlowFadeAdapter`: aplicacao do style no fade.
- `WorldLifecycleResetCompletionGate` e `MacroLevelPrepareCompletionGate`: gates do pipeline.

## Regras praticas

- Nao existe semantica de fluxo em style ou profile.
- `startup` nao passa por navigation.
- Rota `Gameplay` exige reset macro e `LevelCollection` valida.
- Rota `Frontend` nao pode exigir reset de mundo nem carregar `LevelCollection`.

## Leitura cruzada

- `Docs/Modules/Navigation.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/WorldLifecycle.md`
