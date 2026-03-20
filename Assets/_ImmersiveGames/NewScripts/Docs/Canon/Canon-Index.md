# Canon Index

Este indice resume owners e contratos canonicos vigentes.

## Status do baseline atual

- Baseline V3 vigente: `PASS`
- Auditoria canonica atual: `Docs/Reports/Audits/2026-03-19/Audit-NewScripts-Canonical-Cleanup-Round1.md`
- Evidencia vigente: `Docs/Reports/Evidence/LATEST.md`

## Ownership atual

| Eixo | Owner canonico | Contrato atual |
|---|---|---|
| Startup | `NewScriptsBootstrapConfigAsset.startupTransitionStyleRef` | `startup` pertence ao bootstrap |
| Route semantics | `SceneRouteDefinitionAsset.RouteKind` | `frontend` e `gameplay` existem em `RouteKind` |
| Navigation | `GameNavigationCatalogAsset` | cada entrada resolve por `routeRef + transitionStyleRef` |
| Transition style | `TransitionStyleAsset` | style estrutural por `profileRef + useFade` |
| Transition profile | `SceneTransitionProfile` | asset leaf visual |
| SceneFlow runtime | `SceneTransitionService` | timeline de transicao, fases e gates |
| Loading macro | `LoadingHudScene` + `ILoadingPresentationService` + `LoadingHudService` | HUD canonica do macro flow; apresentacao de barra, porcentagem, etapa e spinner |
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
- Guias e modulos devem concordar com os ADRs vigentes e com a auditoria canonica mais recente.
- Historico nao substitui contrato atual.
- `LoadingHudScene` faz parte do estado atual oficial: apresentacao canonica do macro flow, sem ownership do pipeline.
- `Victory/Defeat` pertencem ao baseline atual via mock explicito e controlado, sem canonizar regra final de gameplay.

## Cadeia oficial

1. `Docs/README.md`
2. `Docs/Guides/Production-How-To-Use-Core-Modules.md`
3. `Docs/Guides/Pooling-How-To.md`
4. `Docs/Guides/Pooling-Quick-Access.html`
5. `Docs/Guides/Event-Hooks-Reference.md`
6. `Docs/Modules/SceneFlow.md`
7. `Docs/Modules/Navigation.md`
8. `Docs/Modules/LevelFlow.md`
9. `Docs/Modules/GameLoop.md`
10. `Docs/Modules/Gameplay.md`
11. `Docs/Modules/WorldLifecycle.md`
12. `Docs/Modules/InputModes.md`
13. `Docs/ADRs/README.md`
14. `Docs/Reports/Audits/LATEST.md`
15. `Docs/Reports/Evidence/LATEST.md`
16. `Docs/CHANGELOG.md`

