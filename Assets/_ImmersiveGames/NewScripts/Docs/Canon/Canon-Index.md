# Canon Index

Este indice descreve apenas owners e contratos canonicos vigentes.

## Ownership atual

| Eixo | Owner canonico | Contrato atual |
|---|---|---|
| Startup transition | `NewScriptsBootstrapConfigAsset.startupTransitionStyleRef` | Bootstrap e o unico owner de `startup` |
| Navigation | `GameNavigationCatalogAsset` | Cada entrada resolve por `routeRef + transitionStyleRef` |
| Route semantics | `SceneRouteDefinitionAsset.RouteKind` | `frontend` e `gameplay` existem somente em `RouteKind` |
| Transition style | `TransitionStyleAsset` | Resolve por `profileRef + useFade` |
| Transition profile | `SceneTransitionProfile` | Asset leaf visual; nao decide semantica de fluxo |
| SceneFlow runtime | `SceneTransitionService` | Executa timeline e publica fases |
| Level start/restart | `LevelFlowRuntimeService` + `LevelStageOrchestrator` | Gameplay default, restart e IntroStage |
| GameLoop state | `GameLoopService` | Estado da run, pause/resume, ready/playing |
| World reset | `WorldResetService` + `WorldLifecycleSceneFlowResetDriver` | Macro reset orientado por rota/bootstrap |
| Gameplay rearm | `ActorGroupRearmOrchestrator` | Rearm/reset canonico no escopo de gameplay |
| InputMode sync | `SceneFlowInputModeBridge` | Ajuste por `RouteKind` |

## Regras de leitura

- Docs nesta pasta representam um unico estado atual.
- Historico de migracao nao e fonte de verdade operacional.
- Quando houver conflito, o codigo atual e a evidencia runtime validada prevalecem.

## Cadeia oficial

1. `Docs/README.md`
2. `Docs/Modules/SceneFlow.md`
3. `Docs/Modules/Navigation.md`
4. `Docs/Modules/LevelFlow.md`
5. `Docs/Modules/GameLoop.md`
6. `Docs/Modules/Gameplay.md`
7. `Docs/Modules/WorldLifecycle.md`
8. `Docs/Modules/InputModes.md`
9. `Docs/ADRs/README.md`
10. `Docs/Reports/Audits/LATEST.md`
11. `Docs/Reports/Evidence/LATEST.md`
