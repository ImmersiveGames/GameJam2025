# Auditoria de publishers — EventBus (Assets/_ImmersiveGames/NewScripts)

## Escopo

- Código C# dentro de `Assets/_ImmersiveGames/NewScripts`.
- Prefabs existentes no inventário:
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Content/Prefabs/DummyActor.prefab`
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Content/Prefabs/Eater_NewScripts.prefab`
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Content/Prefabs/Player_NewScripts.prefab`

## Resultado por evento

### A) `EventBus<WorldLifecycleResetCompletedEvent>.Raise(...)`

**Status:** DUPLICADO (3 publishers encontrados)

| Arquivo | Classe / Método | Linha com Raise |
| --- | --- | --- |
| `Assets/_ImmersiveGames/NewScripts/Lifecycle/World/Bridges/SceneFlow/WorldLifecycleSceneFlowResetDriver.cs` | `WorldLifecycleSceneFlowResetDriver` / `PublishResetCompleted` | `EventBus<WorldLifecycleResetCompletedEvent>.Raise(` |
| `Assets/_ImmersiveGames/NewScripts/Lifecycle/World/Runtime/ResetWorldService.cs` | `ResetWorldService` / `TriggerResetAsync` | `EventBus<WorldLifecycleResetCompletedEvent>.Raise(new WorldLifecycleResetCompletedEvent(ctx, rsn));` |
| `Assets/_ImmersiveGames/NewScripts/Lifecycle/World/Runtime/ResetWorldService.cs` | `ResetWorldService` / `TriggerResetAsync` (catch) | `EventBus<WorldLifecycleResetCompletedEvent>.Raise(new WorldLifecycleResetCompletedEvent(ctx, rsn));` |

### B) `EventBus<GameRunEndRequestedEvent>.Raise(...)`

**Status:** DUPLICADO (3 publishers encontrados)

| Arquivo | Classe / Método | Linha com Raise |
| --- | --- | --- |
| `Assets/_ImmersiveGames/NewScripts/Gameplay/CoreGameplay/PostGame/GameRunEndRequestTrigger.cs` | `GameRunEndRequestTrigger` / `Request` | `EventBus<GameRunEndRequestedEvent>.Raise(new GameRunEndRequestedEvent(outcome, reason));` |
| `Assets/_ImmersiveGames/NewScripts/Gameplay/CoreGameplay/GameLoop/GameRunEndRequestService.cs` | `GameRunEndRequestService` / `RequestEnd` | `EventBus<GameRunEndRequestedEvent>.Raise(new GameRunEndRequestedEvent(outcome, reason));` |
| `Assets/_ImmersiveGames/NewScripts/Runtime/Gameplay/Commands/GameCommands.cs` | `GameCommands` / `RequestRunEnd` | `EventBus<GameRunEndRequestedEvent>.Raise(new GameRunEndRequestedEvent(outcome, reason));` |

## Observações sobre prefabs

- Não há referências textuais a `EventBus<WorldLifecycleResetCompletedEvent>.Raise(` ou `EventBus<GameRunEndRequestedEvent>.Raise(` nos 3 prefabs do inventário. (Pesquisa textual simples em arquivos `.prefab`.)
