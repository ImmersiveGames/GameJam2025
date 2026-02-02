# Fix de publisher — GameRunEndRequestedEvent

## Antes (publishers encontrados)

- Script de trigger em `Gameplay/CoreGameplay/PostGame` — **removido**.
- `Gameplay/CoreGameplay/GameLoop/GameRunEndRequestService.cs` (`GameRunEndRequestService.RequestEnd`) — **publisher canônico**.
- `Runtime/Gameplay/Commands/GameCommands.cs` (`GameCommands.RequestRunEnd`) — **fallback indevido**.

## Depois (publisher canônico)

- **Único publisher:** `Gameplay/CoreGameplay/GameLoop/GameRunEndRequestService.cs` (`RequestEnd`).
- `GameCommands` agora apenas delega para `IGameRunEndRequestService` via DI e **não** publica direto.
- O script de trigger foi removido do codebase (script e `.meta`).

### Arquivos removidos

- `Assets/_ImmersiveGames/NewScripts/Gameplay/CoreGameplay/PostGame/[trigger].cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/CoreGameplay/PostGame/[trigger].cs.meta`

## Evidências de ausência de referências do trigger no workspace

- Busca pelo tipo do trigger em C# retornou apenas o arquivo removido.
- Verificação do GUID `9804bed61395f554dbe30a82db545ff4` (meta do script) não encontrou uso em `Assets/_ImmersiveGames/NewScripts` além do próprio `.meta`.
- Pesquisa textual nos 3 prefabs inventariados não encontrou referências ao script.

> Observação: como não há cenas (.unity) no workspace, a validação foi limitada aos arquivos presentes no repositório (C# e 3 prefabs do inventário).
