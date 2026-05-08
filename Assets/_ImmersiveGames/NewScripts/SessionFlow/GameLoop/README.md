# GameLoop

O ciclo legado `GameLoopService`/`GameLoopStateMachine` foi removido.

Estado atual do módulo:
- `GameLoop` não é owner de lifecycle.
- `Pause`/`Resume` canônicos pertencem ao `SessionActivityPipeline`.
- `Run outcome` é tratado pelo `Run Pipeline`.
- `IntroStage` e `StartupRoute` não devem depender de `GameLoop` para liberar start/reset.
- Nenhum ticker runtime é mantido para o ciclo legado.

Peças que permanecem aqui são helpers/bridges de transição ou contratos residuais que ainda são usados por outros módulos:
- `IntroStageIntegrationInstaller`
- `SessionOperationalStartupRouteInstaller`
- `RunPipelineBridgeInstaller`
- `GameLoopContracts`
- `GameLoopEvents`
- `GameLoopEventSubscriptionSet`
- `GameLoopReasonFormatter`
- `GameLoopStateTransitionEffects`
- `GameLoopStartRequestEmitter`

Notas de migração:
- `GameLoopInputCommandBridge`, `GameLoopCommands`, `IPauseCommands`, `LegacyPauseCompatibilityInstaller`, `GamePauseOverlayController`, `AudioPauseDuckingBridge` e `GameLoopInputDriver` foram removidos.
- UI/botões legados não devem ser remendados aqui.
- Qualquer producer canônico novo deve entrar pelos rails de `Navigation`, `SessionOperational`, `SessionActivityPipeline` ou `Run Pipeline`.
