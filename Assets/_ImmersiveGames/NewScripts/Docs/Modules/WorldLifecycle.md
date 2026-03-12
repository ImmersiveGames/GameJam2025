# WorldLifecycle

## Estado atual

- `WorldResetService` e owner do macro reset.
- `WorldLifecycleSceneFlowResetDriver` faz o handoff entre `SceneFlow` e `WorldLifecycle`.
- O reset segue a semantica canonica do sistema:
  - `startup` no bootstrap
  - `frontend/gameplay` em `RouteKind`

## Ownership

- `WorldResetService` + `WorldResetOrchestrator`: pipeline de macro reset.
- `WorldLifecycleSceneFlowResetDriver`: decisao e disparo do reset a partir do SceneFlow.
- `WorldLifecycleController`/`WorldLifecycleOrchestrator`: reset local no escopo de cena.

## Observacao

Labels de style/profile podem aparecer em logs, mas nao decidem comportamento do reset.
