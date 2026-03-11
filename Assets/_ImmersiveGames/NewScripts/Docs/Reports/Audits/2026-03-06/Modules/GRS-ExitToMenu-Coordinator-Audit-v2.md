# GRS ExitToMenu Coordinator Audit v2 (GRS-1.3c)

Date: 2026-03-10

## Objective
- Finalizar o GC de ExitToMenu apos GRS-1.3b.
- Remover ruido LEGACY/no-op e congelar o trilho canonico com um unico owner.
- Registrar a evidenca estatica do workspace local sem alterar contratos ou pipeline.

## Scope
- `Modules/Navigation/Runtime/ExitToMenuCoordinator.cs`
- `Modules/Navigation/Legacy/ExitToMenuNavigationBridge.cs`
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs`
- `Modules/Gates/Interop/GamePauseGateBridge.cs`
- `Docs/Modules/Navigation.md`
- `Docs/Modules/Gates-Readiness-StateDependent.md`
- `Docs/Reports/Audits/2026-03-06/Modules/GRS-ExitToMenu-Coordinator-Audit-v2.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`

## Source Of Truth
- Workspace local: `C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`

## Conceptual Diff
- O owner canonico continua sendo `ExitToMenuCoordinator`.
- O GC removeu ruido residual de `ExitToMenu` em runtime:
  - sem logs `[OBS][LEGACY]` no `GameLoopCommandEventBridge`
  - sem logs/branch legacy de `ExitToMenu` no `GamePauseGateBridge`
  - `ExitToMenuNavigationBridge` reduzido a stub minimo de compatibilidade de tipo, sem `EventBus` e sem logs
- O pipeline e os contratos publicos permaneceram intactos.

## Files Changed
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs`
- `Modules/Gates/Interop/GamePauseGateBridge.cs`
- `Modules/Navigation/Legacy/ExitToMenuNavigationBridge.cs`
- `Docs/Modules/Navigation.md`
- `Docs/Modules/Gates-Readiness-StateDependent.md`
- `Docs/Reports/Audits/2026-03-06/Modules/GRS-ExitToMenu-Coordinator-Audit-v2.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`

## Criteria OK/FAIL
- `OK1`: existe um unico `Register(...)` runtime para `GameExitToMenuRequestedEvent`.
- `OK2`: `RegisterExitToMenuNavigationBridge(...)` nao existe mais.
- `OK3`: leak sweep runtime fora de `Dev/**`, `Editor/**`, `Legacy/**` e `QA/**` retorna `0 matches`.
- `OK4`: cleanup behavior-preserving; contratos publicos e ordem do pipeline permaneceram intactos.

## Evidence/rg

### Step 0 - Inventory
```text
rg -n "EventBus<\s*GameExitToMenuRequestedEvent\s*>\.(Register|Unregister)|new EventBinding<\s*GameExitToMenuRequestedEvent" . -g "*.cs"
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:35:            _exitBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:36:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:51:            EventBus<GameExitToMenuRequestedEvent>.Unregister(_exitBinding);
```

```text
rg -n "GameExitToMenuRequestedEvent|ExitToMenu" . -g "*.cs"
.\Modules\Gates\Interop\GamePauseGateBridge.cs:23:        private const string ReasonExitToMenuRequested = "GameExitToMenuRequestedEvent";
.\Modules\Gates\Interop\GamePauseGateBridge.cs:68:        internal void ReleaseForExitToMenu(string reason)
.\Modules\Gates\Interop\GamePauseGateBridge.cs:70:            ReleasePauseGate(ReasonExitToMenuRequested);
.\Modules\Gates\Interop\GamePauseGateBridge.cs:165:                if (reason == ReasonResumeRequested || reason == ReasonExitToMenuRequested)
.\Modules\PostGame\Bindings\PostGameOverlayController.cs:130:        public void OnClickExitToMenu()
.\Modules\LevelFlow\Runtime\PostLevelActionsService.cs:116:        public async Task ExitToMenuAsync(string reason = null, CancellationToken ct = default)
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:14:    public sealed class ExitToMenuCoordinator : IDisposable
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:35:            _exitBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:36:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);
.\Modules\Navigation\Legacy\ExitToMenuNavigationBridge.cs:8:    public sealed class ExitToMenuNavigationBridge : IDisposable
.\Modules\GameLoop\Runtime\GameLoopEvents.cs:114:    public sealed class GameExitToMenuRequestedEvent : IEvent
.\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:199:            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent(ExitToMenuReason));
.\Modules\GameLoop\Commands\GameCommands.cs:82:            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent(normalizedReason));
.\Infrastructure\Composition\GlobalCompositionRoot.Pipeline.cs:70:            RegisterExitToMenuCoordinator();
.\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:142:        private static void RegisterExitToMenuCoordinator()
```

### Step 3 - Final proofs
```text
rg -n "EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register" . -g "*.cs"
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:36:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);
```

```text
rg -n "RegisterExitToMenuNavigationBridge\(" . -g "*.cs"
0 matches
```

```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

## Notes
- `GamePauseGateBridge` continua com `ReleaseForExitToMenu(...)` apenas como API de colaboracao chamada pelo coordinator; nao existe listener do evento nesse bridge.
- `GameLoopCommandEventBridge` continua owner apenas de pause/resume.
- `ExitToMenuNavigationBridge` permanece em `Legacy/` somente para compatibilidade de tipo/nome.
- Nenhum payload de evento foi alterado.

## Manual Checklist (Unity)
- Gameplay -> ExitToMenu.
- Duplo clique rapido: confirmar `ExitToMenuDeduped` no coordinator.
- Repetir durante in-flight: confirmar `ExitToMenuQueued` e novo `ExitToMenuStart` apos `Completed`.
- Confirmar ausencia de logs LEGACY espalhados para `ExitToMenu`.
