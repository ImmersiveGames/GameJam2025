# Canon Index

Este indice resume owners e contratos canonicos vigentes.

## Ownership atual

| Eixo | Owner canonico | Contrato atual |
|---|---|---|
| Startup | `NewScriptsBootstrapConfigAsset.startupTransitionStyleRef` | `startup` pertence ao bootstrap |
| Route semantics | `SceneRouteDefinitionAsset.RouteKind` | `frontend` e `gameplay` existem em `RouteKind` |
| Navigation | `GameNavigationCatalogAsset` | cada entrada resolve por `routeRef + transitionStyleRef` |
| Transition style | `TransitionStyleAsset` | style estrutural por `profileRef + useFade` |
| Transition profile | `SceneTransitionProfile` | asset leaf visual |
| SceneFlow runtime | `SceneTransitionService` | timeline de transicao, fases e gates |
| Level prepare/swap | `LevelMacroPrepareService` + `LevelSwapLocalService` | prepare macro e troca local de level |
| IntroStage | `LevelStageOrchestrator` + `ILevelStagePresentationService` | intro opcional por level, orquestrada globalmente |
| PostGame | `GameLoopService` + `PostGameOwnershipService` + `PostGameResultService` | post global com `Victory`, `Defeat` e `Exit` |
| Level post reaction | `ILevelPostGameHookService` | hook opcional do level, complementar ao post global |
| Restart | `MacroRestartCoordinator` | restart segue direto por reset macro, sem post hook |
| World reset | `WorldResetService` + `WorldLifecycleSceneFlowResetDriver` | reset macro orientado por rota |
| Gameplay rearm | `ActorGroupRearmOrchestrator` | rearm local por grupo de atores |
| InputMode sync | `SceneFlowInputModeBridge` + `PostGameOwnershipService` | sincronizacao por `RouteKind`, gameplay e post global |

## Regras de leitura

- A docs oficial conta uma unica historia operacional atual.
- Guias e modulos devem concordar com os ADRs vigentes e com o runtime validado em `Docs/Reports/lastlog.log`.
- Historico nao substitui contrato atual.

## Cadeia oficial

1. `Docs/README.md`
2. `Docs/Guides/Production-How-To-Use-Core-Modules.md`
3. `Docs/Guides/Event-Hooks-Reference.md`
4. `Docs/Modules/SceneFlow.md`
5. `Docs/Modules/Navigation.md`
6. `Docs/Modules/LevelFlow.md`
7. `Docs/Modules/GameLoop.md`
8. `Docs/Modules/Gameplay.md`
9. `Docs/Modules/WorldLifecycle.md`
10. `Docs/Modules/InputModes.md`
11. `Docs/ADRs/README.md`
12. `Docs/Reports/Audits/LATEST.md`
13. `Docs/Reports/Evidence/LATEST.md`
14. `Docs/Plans/Plan-Continuous.md`
15. `Docs/CHANGELOG.md`
