# ResetInterop

## Papel
`ResetInterop` é a superfície de integração e observabilidade entre o reset macro e outros módulos, principalmente `SceneFlow`.

## Responsabilidades
- bridge `SceneFlow -> WorldReset`
- eventos públicos de início/fim do reset
- gate de completion usado pela transição
- tokens públicos do trilho de reset

## O que não pertence aqui
- política macro de reset
- validação macro
- execução local do reset de cena
- pipeline de `SceneReset`

## Relação com os outros módulos
### `WorldReset`
Dono do reset macro:
- comandos
- service
- orchestrator
- executor
- policies/guards/validation

### `SceneReset`
Dono do reset local:
- controller
- runner
- façade
- pipeline
- phases
- hooks locais

### `ResetInterop`
Só interop/superfície:
- driver com `SceneFlow`
- eventos públicos
- completion gate
- tokens

## Naming alvo da superfície
A superfície deve usar `WorldReset*` ou nome específico do bridge, e não mais `WorldLifecycle*`.

### Exemplos alvo
- `SceneFlowWorldResetDriver`
- `WorldResetStartedEvent`
- `WorldResetCompletedEvent`
- `WorldResetEvents`
- `WorldResetCompletionGate`
- `WorldResetTokens`
