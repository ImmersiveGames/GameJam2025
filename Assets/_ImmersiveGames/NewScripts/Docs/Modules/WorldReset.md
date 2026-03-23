# WorldReset

## Papel
`WorldReset` é o módulo **macro** de reset. Ele recebe pedidos canônicos de reset, aplica política/guarda/validação e coordena a execução do hard reset macro.

## Owner
- API pública de reset macro
- request/result do reset
- políticas de reset
- guards de reset
- validação de assinatura/contexto
- orquestração macro
- bridge macro → reset local + validação de pós-condição

## Estrutura atual
- `Application/`
  - `WorldResetService`
  - `WorldResetOrchestrator`
  - `WorldResetExecutor`
- `Domain/`
  - `WorldResetRequest`
  - `WorldResetContext`
  - `WorldResetScope`
  - `WorldResetOrigin`
  - `WorldResetReasons`
  - `WorldResetFlags`
  - `ResetDecision`
- `Guards/`
  - `IWorldResetGuard`
  - `SimulationGateWorldResetGuard`
- `Policies/`
  - `IWorldResetPolicy`
  - `IRouteResetPolicy`
  - `ProductionWorldResetPolicy`
  - `SceneRouteResetPolicy`
- `Runtime/`
  - `IWorldResetCommands`
  - `IWorldResetService`
  - `IWorldResetRequestService`
  - `WorldResetCommands`
  - `WorldResetRequestService`
  - `WorldResetResult`
- `Validation/`
  - `IWorldResetValidator`
  - `WorldResetSignatureValidator`
  - `WorldResetValidationPipeline`

## Não é owner
- pipeline local de reset da cena
- hooks locais de cena
- spawn local
- integração de SceneFlow como owner de transição
- surface/eventos legados `WorldLifecycle*` em `ResetInterop`

## Observações
- `IWorldResetGuard` **permanece ativo** no estado atual. Não remover.
- `SimulationGateWorldResetGuard` **permanece ativo** no estado atual. Não remover.
