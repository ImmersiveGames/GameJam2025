# GRS ExitToMenu Coordinator Audit v1 (GRS-1.3b)

Date: 2026-03-10

## Objective
- Consolidar `GameExitToMenuRequestedEvent` em um ?nico owner can?nico.
- Desativar listeners duplicados de runtime e manter o trilho behavior-preserving.
- Congelar evid?ncia est?tica do antes/depois no workspace local.

## Scope
- `Modules/Navigation/Runtime/ExitToMenuCoordinator.cs`
- `Modules/Navigation/Legacy/ExitToMenuNavigationBridge.cs`
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs`
- `Modules/Gates/Interop/GamePauseGateBridge.cs`
- `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`
- `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`
- `Modules/GameLoop/Legacy/Bindings/Inputs/GamePauseHotkeyController.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.Navigation.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`

## Source Of Truth
- Workspace local: `C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`

## Files Changed
- `Infrastructure/Composition/GlobalCompositionRoot.Navigation.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
- `Modules/Navigation/Runtime/ExitToMenuCoordinator.cs`
- `Modules/Navigation/Legacy/ExitToMenuNavigationBridge.cs`
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs`
- `Modules/Gates/Interop/GamePauseGateBridge.cs`
- `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`
- `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`
- `Modules/GameLoop/Legacy/Bindings/Inputs/GamePauseHotkeyController.cs`
- `Docs/Modules/Navigation.md`
- `Docs/Modules/Gates-Readiness-StateDependent.md`
- `Docs/Reports/Audits/2026-03-06/Modules/GRS-ExitToMenu-Coordinator-Audit-v1.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`

## Criteria OK/FAIL
- `OK1`: existe um ?nico `Register(...)` runtime para `GameExitToMenuRequestedEvent`.
- `OK2`: o pipeline n?o mudou de ordem; o callsite continua na mesma posi??o l?gica em `RegisterEssentialServicesOnly()`.
- `OK3`: leak sweep runtime fora de `Dev/**`, `Editor/**`, `Legacy/**` e `QA/**` retorna `0 matches`.

## Canonical Contract
- Owner can?nico: `ExitToMenuCoordinator`.
- Ordem can?nica por execu??o:
  1. `IGameLoopService.RequestReady()`
  2. `IGameNavigationService.ExitToMenuAsync(reason)`
- Dedupe same-frame: key `exit|reason=<normalized>`.
- In-flight coalesce: `pending=true`, ?ltimo motivo vence.
- Logs ?ncora:
  - `[OBS][Navigation] ExitToMenuRequested reason='...'`
  - `[OBS][Navigation] ExitToMenuStart runId='N' reason='...'`
  - `[OBS][Navigation] ExitToMenuQueued reason='...' queuedBecause='in_flight'`
  - `[OBS][Navigation] ExitToMenuDeduped reason='...' reason2='dedupe_same_frame' frame='...'`
  - `[OBS][Navigation] ExitToMenuCompleted runId='N' pending='true/false'`

## Evidence/rg

### Step 0.A - Consumers before change
```text
rg -n "EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register|new\s+EventBinding<\s*GameExitToMenuRequestedEvent\s*>" Assets/_ImmersiveGames/NewScripts -g "*.cs"
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:57:            _exitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:88:                EventBus<GameExitToMenuRequestedEvent>.Register(_exitToMenuBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:35:            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:39:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:79:            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:83:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:44:            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(_ => ResetFlagsForMenu());
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:48:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:206:                _gameExitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnGameExitToMenuRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:238:                EventBus<GameExitToMenuRequestedEvent>.Register(_gameExitToMenuBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\ExitToMenuNavigationBridge.cs:24:            _exitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\ExitToMenuNavigationBridge.cs:25:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitToMenuBinding);
```

### Step 0.B - Composition callsites before change
```text
rg -n "ExitToMenuNavigationBridge|RegisterExitToMenuNavigationBridge\(" Assets/_ImmersiveGames/NewScripts -g "*.cs"
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Pipeline.cs:70:            RegisterExitToMenuNavigationBridge();
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:142:        private static void RegisterExitToMenuNavigationBridge()
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:145:                () => new ExitToMenuNavigationBridge(),
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:146:                "[Navigation] ExitToMenuNavigationBridge ja registrado no DI global.",
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:147:                "[Navigation] ExitToMenuNavigationBridge registrado no DI global.");
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\ExitToMenuNavigationBridge.cs:13:    public sealed class ExitToMenuNavigationBridge : IDisposable
```

### Step 0.C - GameLoop bridge before change
```text
rg -n "GameLoopCommandEventBridge|OnExitToMenuRequested|GameExitToMenuRequestedEvent" Assets/_ImmersiveGames/NewScripts/Modules/GameLoop -g "*.cs"
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:35:            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:39:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:128:        private void OnExitToMenuRequested(GameExitToMenuRequestedEvent evt)
```

### Step 4.A - Single owner after change
```text
rg -n "EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register" Assets/_ImmersiveGames/NewScripts -g "*.cs"
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:36:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);
```

### Step 4.B - Composition callsite after change
```text
rg -n "RegisterExitToMenuCoordinator\(|RegisterExitToMenuNavigationBridge\(|ExitToMenuCoordinator|ExitToMenuNavigationBridge" Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition -g "*.cs"
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Pipeline.cs:70:            RegisterExitToMenuCoordinator();
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:142:        private static void RegisterExitToMenuCoordinator()
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:145:                () => new ExitToMenuCoordinator(),
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:146:                "[Navigation] ExitToMenuCoordinator ja registrado no DI global.",
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Navigation.cs:147:                "[Navigation] ExitToMenuCoordinator registrado no DI global.");
```

### Step 4.C - Leak sweep
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

## Notes
- `ExitToMenuNavigationBridge` foi movido para `Modules/Navigation/Legacy/` como stub LEGACY/no-op, sem `Register(...)`.
- `GameLoopCommandEventBridge` ficou limitado a pause/resume; o log LEGACY torna a desativa??o expl?cita.
- `GamePauseGateBridge` preserva a libera??o do gate de pause via m?todo expl?cito chamado pelo coordinator, evitando depender do `GameExitToMenuRequestedEvent` para ownership do handle.
- `PauseOverlayController` e `StateDependentService` deixaram de consumir diretamente `ExitToMenu`; o trilho agora converge no coordinator.
- Nenhum payload/contrato de evento foi alterado.

## Manual Checklist (Unity)
- Gameplay -> ExitToMenu.
- Spam clique r?pido: esperar `ExitToMenuDeduped ... dedupe_same_frame`.
- Spam durante in-flight: esperar `ExitToMenuQueued ... in_flight` e depois novo `ExitToMenuStart` ap?s `ExitToMenuCompleted`.
- Confirmar que pause/resume continuam intactos e que o retorno ao menu passa por `RequestReady()` antes de `ExitToMenuAsync(reason)`.
