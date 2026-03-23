# SceneReset

## Papel
`SceneReset` é o módulo **local** de reset da cena. Ele contém o pipeline determinístico do reset local, os hooks locais e a infraestrutura de spawn usada pelo trilho local.

## Owner
- controller local do reset
- runner local
- façade local
- pipeline local explícito
- fases do reset local
- hook registry local
- spawn registry/context do reset local

## Estrutura atual
- `Bindings/`
  - `SceneResetController`
  - `SceneResetRunner`
- `Runtime/`
  - `SceneResetContext`
  - `SceneResetControllerLocator`
  - `SceneResetFacade`
  - `SceneResetHookRunner`
  - `SceneResetPipeline`
  - `ISceneResetPhase`
  - `Phases/*`
- `Hooks/`
  - `ISceneResetHook`
  - `ISceneResetHookOrdered`
  - `SceneResetHookBase`
  - `SceneResetHookRegistry`
- `Spawn/`
  - `IWorldSpawnContext`
  - `IWorldSpawnService`
  - `IWorldSpawnServiceRegistry`
  - `WorldSpawnContext`
  - `WorldSpawnServiceFactory`
  - `WorldSpawnServiceRegistry`

## Não é owner
- pedido macro de reset
- política macro
- eventos públicos de reset em `ResetInterop`
- integração com SceneFlow como owner de transição

## Observações
- o naming local já foi consolidado em `SceneReset*`
- o pipeline local já não deve ser descrito como `WorldLifecycle*`
