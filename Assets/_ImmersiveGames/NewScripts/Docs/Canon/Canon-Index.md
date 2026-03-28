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
| RuntimeMode | `BootstrapConfigAsset` + `RuntimeModeConfig` | config obrigatória, resolvida por referência direta no bootstrap canônico |
| SceneFlow runtime | `SceneTransitionService` | timeline de transicao, fases e gates |
| Loading macro | `LoadingHudScene` + `ILoadingPresentationService` + `LoadingHudService` | HUD canonica do macro flow; apresentacao de barra, porcentagem, etapa e spinner |
| Audio | `Modules/Audio` | `AudioCompositionDescriptor` + `AudioInstaller` + `AudioRuntimeComposer` são o caminho canônico |
| Level prepare/swap | `LevelMacroPrepareService` + `LevelSwapLocalService` | prepare macro e troca local de level |
| IntroStage | `LevelFlowRuntimeService` + `LevelEnteredEvent` + `LevelIntroCompletedEvent` + `LevelStageOrchestrator` + `ILevelIntroStagePresenterRegistry` + `ILevelIntroStagePresenterScopeResolver` | intro opcional por level, disparada pelo hook canonico pos-aplicacao do level e finalizada por handoff canonico |
| PostGame / PostStage | `Modules/PostGame` | owner do stage pos-outcome; handoff final para `GameLoop` ocorre somente apos `PostStageCompletedEvent` |
| Level post reaction | `ILevelPostGameHookService` | hook opcional do level, complementar ao post global |
| Restart | `MacroRestartCoordinator` | restart segue direto por reset macro, sem post hook |
| World reset | `WorldResetService` + `WorldLifecycleSceneFlowResetDriver` | reset macro orientado por rota |
| Gameplay rearm | `ActorGroupRearmOrchestrator` | rearm local por grupo de atores |
| InputMode sync | `InputModeCoordinator` + `InputModeService` + `IPlayerInputLocator` | requests canônicos por `InputModeRequestKind` e leitura via `IInputModeStateService` |

## Regras de leitura

- A docs oficial conta uma unica historia operacional atual.
- Guias e modulos devem concordar com os ADRs vigentes e com a auditoria canonica mais recente.
- Historico nao substitui contrato atual.
- `LoadingHudScene` faz parte do estado atual oficial: apresentacao canonica do macro flow, sem ownership do pipeline.
- `BootstrapConfigAsset` continua sendo o único entrypoint aceitável por `Resources` no boot global.
- `Victory/Defeat` pertencem ao baseline atual via mock explicito e controlado, sem canonizar regra final de gameplay.
- `PostStage` e fluxo canonico implementado e validado em `Modules/PostGame`.
- `PostGame` permanece global na implementacao atual, mas o ownership alvo do stage e `Modules/PostGame`.
- O default operacional continua sendo sem PostStage; levels com presenter explicito executam stage real.
- `IntroStage` e level-owned, opcional, disparada por hook canonico pos-level-applied (`LevelEnteredEvent`) e finalizada por `LevelIntroCompletedEvent`.
- O presenter canonico da intro e resolvido por contrato, com escopo fornecido por `ILevelIntroStagePresenterScopeResolver`.
- `RuntimeModeConfig` e obrigatorio no bootstrap canônico e nao possui fallback oculto por `Resources.Load`.

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
