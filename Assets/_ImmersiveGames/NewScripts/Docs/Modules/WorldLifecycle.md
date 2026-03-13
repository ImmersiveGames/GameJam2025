# WorldLifecycle

## Estado atual

- `WorldResetService` e owner do macro reset.
- `WorldLifecycleSceneFlowResetDriver` faz o handoff entre `SceneFlow` e `WorldLifecycle`.
- O reset macro segue a semantica atual:
  - `startup` no bootstrap
  - `frontend/gameplay` em `RouteKind`

## Ownership

- `WorldResetService` + `WorldResetOrchestrator`: pipeline de macro reset.
- `WorldLifecycleSceneFlowResetDriver`: decisao e disparo do reset a partir do SceneFlow.
- `WorldLifecycleController`/`WorldLifecycleOrchestrator`: reset local no escopo de cena.

## Regras praticas

- O reset global continua separado do rearm local de gameplay.
- Labels de style/profile podem aparecer em logs, mas nao definem comportamento do reset.
- O pipeline de reset nao depende de catalogs nominais antigos.

## Leitura cruzada

- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/Gameplay.md`
