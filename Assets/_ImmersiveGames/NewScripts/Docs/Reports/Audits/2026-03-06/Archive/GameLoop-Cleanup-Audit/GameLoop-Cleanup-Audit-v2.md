# GL-1.2 - GameLoop cleanup audit v2 (behavior-preserving)

Date: 2026-03-06
Source of truth: local workspace files.

## Inventory A/B/C

### A) Runtime canônico (sempre compila)
- `Modules/GameLoop/Commands/GameCommands.cs`
- `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs` (reset listener legado explicitamente desativado)
- `Modules/GameLoop/Bindings/Bootstrap/GameLoopBootstrap.cs`
- `Modules/GameLoop/Bindings/Bootstrap/GameStartRequestEmitter.cs`
- `Modules/GameLoop/Bindings/Inputs/GamePauseHotkeyController.cs`
- `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`

### B) Compat/Legacy (runtime, desativado/compat)
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs`
  - evidência: log `[OBS][LEGACY] GameResetRequestedEvent listener disabled ...`.

### C) Dev/Editor
- `Modules/GameLoop/IntroStage/Dev/**`
- alvo tratado em GL-1.2: `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs`.

## Evidence (rg)

### Commands executed
```text
rg -n "Bootstrap|Emitter|Hotkey|MenuItem|DevTools|ContextMenu|RuntimeInitializeOnLoadMethod" Modules/GameLoop -g "*.cs"
rg -n "EventBus<.*>|Register\(|Unregister\(" Modules/GameLoop -g "*.cs"
rg -n "GameStartRequestEmitter|GamePauseHotkeyController|IntroStageDevTools" -g "*.unity" -g "*.prefab" -g "*.asset" .
rg -n "GameResetRequestedEvent|EventBus<GameResetRequestedEvent>|Register\(new EventBinding<GameResetRequestedEvent>" Modules/GameLoop -g "*.cs"
rg -n "GameStartRequestEmitter|GamePauseHotkeyController|IntroStageDevTools" Infrastructure/Composition -g "*.cs"
```

### Key results
- Assets scan (`.unity/.prefab/.asset`) para os 3 alvos iniciais: sem matches.
- `Modules/GameLoop/Commands/GameCommands.cs` publica `GameResetRequestedEvent`.
- Não há listener ativo de `GameResetRequestedEvent` em `Modules/GameLoop/**`.
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs` mantém observabilidade legado: `[OBS][LEGACY] ... listener disabled ...`.
- `Infrastructure/Composition`: sem callsite canônico para `GameStartRequestEmitter`, `GamePauseHotkeyController`, `IntroStageDevTools`.

## Candidate -> action -> evidence

| Candidate | Action | Evidence | Decision |
|---|---|---|---|
| `Modules/GameLoop/Bindings/Bootstrap/GameStartRequestEmitter.cs` | KEEP (sem move/guard extra) | publisher de `GameStartRequestedEvent`; classe runtime de produção | mantido canônico |
| `Modules/GameLoop/Bindings/Inputs/GamePauseHotkeyController.cs` | KEEP (sem move/guard extra) | controller runtime de input/pause | mantido canônico |
| `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs` | Guard explícito de compilação | arquivo agora com `#if UNITY_EDITOR` | aplicado |

## Applied changes
- Guard explícito adicionado em:
  - `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs`

## Moves / renames
- Nenhum move/rename aplicado em GL-1.2.

## Acceptance checks
- Nenhum listener ativo de `GameResetRequestedEvent` em `Modules/GameLoop/**`: OK.
- Nenhuma referência canônica em `Infrastructure/Composition` para alvos movidos/guardados: OK.
- Trilho canônico de restart preservado (`GameCommands -> GameResetRequestedEvent -> MacroRestartCoordinator`): OK.
- Mudança behavior-preserving (sem alteração de fluxo runtime Release): OK.
