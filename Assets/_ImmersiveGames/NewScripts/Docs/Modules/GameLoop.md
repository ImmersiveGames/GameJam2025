# GameLoop

## Estado atual

- `GameLoopService` e owner do estado da run.
- `GameLoopSceneFlowCoordinator` sincroniza start plan e readiness com o SceneFlow.
- `IntroStage` e `PostLevel` pertencem ao nivel, conforme o contrato vigente.

## Ownership

- `GameLoopService`: estados da run, ready/playing/pause/resume.
- `GameLoopSceneFlowCoordinator`: sincronizacao entre SceneFlow e GameLoop.
- `IntroStageCoordinator` + `LevelStageOrchestrator`: controle do intro stage no contexto do level.

## Semantica de fluxo

- `startup` vem do bootstrap.
- `frontend` e `gameplay` entram no GameLoop via `RouteKind` do contexto de transicao.
- O GameLoop nao deve inferir semantica por style/profile labels.
