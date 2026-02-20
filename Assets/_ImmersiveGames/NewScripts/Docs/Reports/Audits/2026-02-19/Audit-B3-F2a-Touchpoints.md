# Audit B3-F2a — Touchpoints de Reset (pré-implementação)

## a) API atual de reset macro
- `Modules/WorldLifecycle/Runtime/IWorldResetService.cs`
  - `TriggerResetAsync(string? contextSignature, string? reason)`
  - `TriggerResetAsync(WorldResetRequest request)`
- `Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs`
  - Implementação canônica do reset macro.
- `Modules/WorldLifecycle/Runtime/IWorldResetRequestService.cs`
- `Modules/WorldLifecycle/Runtime/WorldResetRequestService.cs`
  - Entry-point de produção para reset manual fora de transição.

## b) Evento/gate atual de ResetCompleted usado pelo SceneFlow
- Evento legado/canônico do gate:
  - `Modules/WorldLifecycle/Runtime/WorldLifecycleResetCompletedEvent.cs`
- Gate do SceneFlow que depende desse evento (não quebrar):
  - `Modules/WorldLifecycle/Runtime/WorldLifecycleResetCompletionGate.cs`
- Driver SceneFlow->WorldLifecycle que também publica completed em fallback/skip:
  - `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`

## c) Mecanismos existentes de restart/retry para apoiar LevelReset
- Snapshot de restart:
  - `Modules/LevelFlow/Runtime/IRestartContextService.cs`
  - `Modules/LevelFlow/Runtime/RestartContextService.cs`
  - `Modules/LevelFlow/Runtime/GameplayStartSnapshot.cs`
  - `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs`
- Restart de navegação (macro):
  - `Modules/Navigation/RestartNavigationBridge.cs`
  - `Modules/Navigation/GameNavigationService.cs` (`RestartAsync`)
- Reset local de conteúdo (mais local para LevelReset):
  - `Modules/ContentSwap/Runtime/IContentSwapChangeService.cs`
  - `Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs`
  - `Modules/Navigation/RestartSnapshotContentSwapBridge.cs` (sincroniza contentId no snapshot)

## Touchpoints de DI/composição
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
  - `InstallWorldLifecycleServices()` registra `WorldResetService` e `IWorldResetRequestService`.

## Comandos rg usados
```bash
rg -n "IWorldResetService|WorldResetService|IWorldResetRequestService|WorldResetRequestService" Assets/_ImmersiveGames/NewScripts/Modules Assets/_ImmersiveGames/NewScripts/Infrastructure -g '!*.meta'
rg -n "WorldLifecycleResetCompletedEvent|WorldLifecycleResetCompletionGate|WorldLifecycleSceneFlowResetDriver" Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle -g '!*.meta'
rg -n "Restart|Snapshot|IRestartContextService|IContentSwapChangeService|InPlaceContentSwapService" Assets/_ImmersiveGames/NewScripts/Modules -g '!*.meta'
```
