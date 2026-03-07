# GL-1.3 - GameLoop redundancy audit v3 (behavior-preserving)

Date: 2026-03-06
Source of truth: local workspace files.

## Scope
- `Modules/GameLoop/Bindings/Bootstrap/GameStartRequestEmitter.cs`
- `Modules/GameLoop/Bindings/Inputs/GamePauseHotkeyController.cs`

## Mandatory evidence (rg)

### Commands executed
```text
rg -n "RuntimeInitializeOnLoadMethod|InitializeOnLoad|DidReloadScripts|ExecuteAlways" Modules/GameLoop -g "*.cs"
rg -n "class\s+GameStartRequestEmitter|class\s+GamePauseHotkeyController" Modules/GameLoop -g "*.cs"
rg -n "Register.*GameStartRequestEmitter|GameStartRequestEmitter|GamePauseHotkeyController" Infrastructure/Composition Modules -g "*.cs"
rg -n "GameStartRequestEmitter|GamePauseHotkeyController" -g "*.unity" -g "*.prefab" -g "*.asset" .
```

### Relevant outputs
- Bootstrap auto (antes da correção):
  - `Modules/GameLoop/Bindings/Bootstrap/GameStartRequestEmitter.cs:17:[RuntimeInitializeOnLoadMethod(...)]`
- Classes encontradas:
  - `Modules/GameLoop/Bindings/Bootstrap/GameStartRequestEmitter.cs`
  - `Modules/GameLoop/Bindings/Inputs/GamePauseHotkeyController.cs` (depois movido)
- Callsites canônicos:
  - `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs:20: GameStartRequestEmitter.EnsureInstalled();` (após GL-1.3)
  - Nenhum callsite canônico para `GamePauseHotkeyController`.
- Asset refs (`.unity/.prefab/.asset`): sem matches para ambos os alvos.

## Decision by target

| Target | Criteria result | Action | Rationale |
|---|---|---|---|
| `GameStartRequestEmitter` | tinha bootstrap automático (`RuntimeInitializeOnLoadMethod`) | **B) DEVQA canônico** | bootstrap automático removido; substituído por `EnsureInstalled()` chamado apenas no trilho DevQA canônico. |
| `GamePauseHotkeyController` | sem bootstrap auto + sem refs de asset + sem callsite em composition | **A) DEAD/LEGACY** | movido para `Modules/GameLoop/Legacy/Bindings/Inputs/` e guardado para compilar só em Editor/DevBuild. |

## Applied changes
- `Modules/GameLoop/Bindings/Bootstrap/GameStartRequestEmitter.cs`
  - removido `RuntimeInitializeOnLoadMethod`.
  - adicionado `EnsureInstalled()` com log `[OBS][LEGACY][DevQA]`.
  - arquivo inteiro sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs`
  - adicionado `RegisterGameLoopQaInstaller()`.
  - `RegisterGameLoopQaInstaller()` chama `GameStartRequestEmitter.EnsureInstalled()`.
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
  - `InstallDevQaServices()` passou a chamar `RegisterGameLoopQaInstaller()` (dentro do guard de DevQA).
- `Modules/GameLoop/Bindings/Inputs/GamePauseHotkeyController.cs`
  - movido para `Modules/GameLoop/Legacy/Bindings/Inputs/GamePauseHotkeyController.cs` (com `.meta`).
  - arquivo inteiro sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

## Acceptance checks
- Nenhum listener ativo de `GameResetRequestedEvent` em `Modules/GameLoop/**`: OK.
  - `rg -n "EventBus<GameResetRequestedEvent>\.Register|Register\(new EventBinding<GameResetRequestedEvent>" Modules/GameLoop -g "*.cs"` -> sem matches.
- Restart canônico preservado (`GameCommands -> GameResetRequestedEvent -> MacroRestartCoordinator`): OK.
- Release exclui DevQA/DevTools alterados por compile guards: OK.
- Development Build mantém trilho DevQA para os itens aplicados: OK.

## Behavior-preserving note
- Produção (Release): sem novo fluxo e sem reintrodução de listener de reset no GameLoop.
- Mudanças restritas a isolamento/legado e centralização DevQA com observabilidade `[OBS][LEGACY]`.
