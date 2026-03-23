# ResetInterop

## Papel
`ResetInterop` contém a **superfície residual/bridge** entre o trilho de reset e módulos externos, principalmente `SceneFlow`.

## Owner
- driver de integração `SceneFlow -> WorldReset`
- completion gate consumido pelo trilho de transição
- eventos públicos observáveis de reset
- tokens públicos ainda com naming legado

## Estrutura atual
- `Runtime/`
  - `WorldLifecycleSceneFlowResetDriver`
  - `WorldLifecycleResetStartedEvent`
  - `WorldLifecycleResetCompletedEvent`
  - `WorldLifecycleResetEvents`
  - `WorldLifecycleResetCompletionGate`
  - `WorldLifecycleTokens`

## Observações
- o módulo já foi separado corretamente de `WorldReset` e `SceneReset`
- o nome `WorldLifecycle*` aqui é **residual de superfície**
- esse naming pode ser revisado em fase própria, mas não bloqueia o estado atual
