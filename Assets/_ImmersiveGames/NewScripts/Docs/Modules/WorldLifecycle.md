# WorldLifecycle

## Estado atual

- `WorldResetService` e o owner do macro reset.
- `WorldLifecycleSceneFlowResetDriver` faz o handoff entre `SceneFlow` e `WorldReset`.
- O reset macro segue a semantica atual:
  - `startup` no bootstrap
  - `frontend/gameplay` em `RouteKind`
- O trilho local do reset esta funcionalmente estavel e **ja possui pipeline interno explicito** em `Runtime/SceneReset/*`.
- A superficie interna ainda usa nomes legados `WorldLifecycle*` para responsabilidades que hoje ja sao claramente de **scene reset local**.

## Ownership

- `WorldResetService` + `WorldResetOrchestrator`: pipeline de macro reset.
- `WorldLifecycleSceneFlowResetDriver`: decisao e disparo do reset a partir do SceneFlow.
- `WorldLifecycleController`: fila e lifecycle do reset local no escopo de cena.
- `SceneResetRunner`: montagem do trilho local e coleta de dependencias efemeras.
- `WorldLifecycleOrchestrator`: **façade fina** que delega ao pipeline local.
- `Runtime/SceneReset/SceneResetPipeline`: pipeline deterministico local.

## Regras praticas

- O reset global continua separado do rearm local de gameplay.
- Labels de style/profile podem aparecer em logs, mas nao definem comportamento do reset.
- O pipeline de reset nao depende de catalogs nominais antigos.
- `SceneFlow` continua owner de loading/fade/readiness e do `set-active` macro.
- `SceneComposition` continua owner apenas de `load/unload` tecnico no macro e no local.

## Direcao de naming para a proxima fase

A limpeza seguinte deve alinhar o naming da **superficie local** com a tarefa real, sem alterar o contrato macro publico e sem colidir com o pipeline `Runtime/SceneReset/*` que ja existe.

### Manter como `WorldReset*`

- `WorldResetService`
- `WorldResetOrchestrator`
- `WorldResetExecutor`
- `IWorldResetCommands`
- `WorldLifecycleSceneFlowResetDriver`

### Migrar para `SceneReset*`

- `WorldLifecycleController` -> `SceneResetController`
- `WorldLifecycleSceneResetRunner.cs` -> `SceneResetRunner.cs` *(arquivo; a classe ja e `SceneResetRunner`)*
- `WorldLifecycleOrchestrator` -> `SceneResetFacade`
- `WorldLifecycleControllerLocator` -> `SceneResetControllerLocator`

### Regra

- `WorldReset*` = API/fluxo macro
- `SceneReset*` = superficie e pipeline local deterministico por cena
- `Runtime/SceneReset/*` = nomes ja corretos e nao entram na renomeacao desta fase

## Leitura cruzada

- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/Gameplay.md`
- `Docs/Reports/Audits/2026-03-23/Fase-4c-Tabela-Renomeacao-SceneReset.md`
