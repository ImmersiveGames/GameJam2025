﻿# Auditoria de publishers — EventBus (Assets/_ImmersiveGames/NewScripts)

## Escopo

- Código C# dentro de `Assets/_ImmersiveGames/NewScripts`.
- Prefabs existentes no inventário:
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Content/Prefabs/DummyActor.prefab`
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Content/Prefabs/Eater_NewScripts.prefab`
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Content/Prefabs/Player_NewScripts.prefab`

## Resultado por evento

### A) `EventBus<WorldLifecycleResetCompletedEvent> Raise(...)`

**Status:** DUPLICADO (3 publishers encontrados)

| Arquivo | Linha | Classe / Método | Linha com Raise | Duplicado? |
| --- | ---: | --- | --- | --- |
| `Assets/_ImmersiveGames/NewScripts/Lifecycle/World/Bridges/SceneFlow/WorldLifecycleSceneFlowResetDriver.cs` | 251 | `WorldLifecycleSceneFlowResetDriver` / `PublishResetCompleted` | `EventBus<WorldLifecycleResetCompletedEvent> Raise(...)` | SIM |
| `Assets/_ImmersiveGames/NewScripts/Lifecycle/World/Runtime/ResetWorldService.cs` | 49 | `ResetWorldService` / `TriggerResetAsync` | `EventBus<WorldLifecycleResetCompletedEvent> Raise(...)` | SIM |
| `Assets/_ImmersiveGames/NewScripts/Lifecycle/World/Runtime/ResetWorldService.cs` | 56 | `ResetWorldService` / `TriggerResetAsync` (catch) | `EventBus<WorldLifecycleResetCompletedEvent> Raise(...)` | SIM |

### B) `EventBus<GameRunEndRequestedEvent> Raise(...)`

**Status:** ÚNICO (1 publisher encontrado)

| Arquivo | Linha | Classe / Método | Linha com Raise | Duplicado? |
| --- | ---: | --- | --- | --- |
| `Assets/_ImmersiveGames/NewScripts/Gameplay/CoreGameplay/GameLoop/GameRunEndRequestService.cs` | 30 | `GameRunEndRequestService` / `RequestEnd` | `EventBus<GameRunEndRequestedEvent> Raise(...)` | NÃO |

## Observações sobre prefabs

- Não há referências textuais a `EventBus<WorldLifecycleResetCompletedEvent> Raise(` ou `EventBus<GameRunEndRequestedEvent> Raise(` nos 3 prefabs do inventário. (Pesquisa textual simples em arquivos `.prefab`.)
