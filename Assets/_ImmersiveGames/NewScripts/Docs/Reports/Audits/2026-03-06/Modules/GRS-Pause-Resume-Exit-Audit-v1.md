# GRS Pause Resume Exit Audit v1 (GRS-1.3a)

Date: 2026-03-10

## Objective
- Inventariar todos os consumers de `GamePauseCommandEvent`, `GameResumeRequestedEvent` e `GameExitToMenuRequestedEvent` no workspace local.
- Aplicar hardening same-frame idempotent nos consumers runtime, preservando comportamento.
- Congelar a evidencia estatica e o checklist manual no layout canonico.

## Scope
- `Modules/Gates/Interop/GamePauseGateBridge.cs`
- `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs`
- `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`
- `Modules/Navigation/ExitToMenuNavigationBridge.cs`
- Inventario auxiliar: `Modules/GameLoop/Legacy/Bindings/Inputs/GamePauseHotkeyController.cs`, `Infrastructure/Observability/Baseline/BaselineInvariantAsserter.cs`

## Source Of Truth
- Workspace local: `C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`

## Files Changed
- `Modules/Gates/Interop/GamePauseGateBridge.cs`
- `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs`
- `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`
- `Modules/Navigation/ExitToMenuNavigationBridge.cs`
- `Docs/Modules/Gates-Readiness-StateDependent.md`
- `Docs/Reports/Audits/2026-03-06/Modules/GRS-Pause-Resume-Exit-Audit-v1.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`

## Criteria OK/FAIL
- `OK1`: todos os consumers runtime acima tem dedupe same-frame + logs `[OBS][GRS]` para `consumed` e `dedupe_same_frame`.
- `OK2`: leak sweep runtime fora de `Dev/**`, `Editor/**`, `Legacy/**` e `QA/**` retorna `0 matches`.
- `OK3`: nenhum payload/struct/contrato publico de `GamePauseCommandEvent`, `GameResumeRequestedEvent` ou `GameExitToMenuRequestedEvent` foi alterado.

## Runtime Hardening Contract
- Pause: key `pause|isPaused=<true/false>|reason=<normalized>`.
- Resume: key `resume|reason=<normalized>`.
- Exit: key `exit|reason=<normalized>`.
- Regra: se `Time.frameCount == lastFrame` e `key == lastKey`, o consumer faz dedupe e so loga `[OBS][GRS] ... dedupe_same_frame ...`.
- Fora do dedupe, o consumer preserva a acao existente e loga `[OBS][GRS] ... consumed ...`.

## Evidence/rg

### A) Consumers (register)
```text
rg -n "EventBus<\s*GamePauseCommandEvent\s*>\.Register|EventBus<\s*GameResumeRequestedEvent\s*>\.Register|EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register" Assets/_ImmersiveGames/NewScripts -g "*.cs"
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\ExitToMenuNavigationBridge.cs:25:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitToMenuBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:86:                EventBus<GamePauseCommandEvent>.Register(_pauseBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:87:                EventBus<GameResumeRequestedEvent>.Register(_resumeBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:88:                EventBus<GameExitToMenuRequestedEvent>.Register(_exitToMenuBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:236:                EventBus<GamePauseCommandEvent>.Register(_gamePauseBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:237:                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:238:                EventBus<GameExitToMenuRequestedEvent>.Register(_gameExitToMenuBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:37:            EventBus<GamePauseCommandEvent>.Register(_onPause);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:38:            EventBus<GameResumeRequestedEvent>.Register(_onResume);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:39:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:81:            EventBus<GamePauseCommandEvent>.Register(_onPauseCommand);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:82:            EventBus<GameResumeRequestedEvent>.Register(_onResumeRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:83:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:46:            EventBus<GamePauseCommandEvent>.Register(_onPauseCommand);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:47:            EventBus<GameResumeRequestedEvent>.Register(_onResumeRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:48:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
```

### B) Bindings (event binding patterns)
```text
rg -n "new\s+EventBinding<\s*GamePauseCommandEvent\s*>|new\s+EventBinding<\s*GameResumeRequestedEvent\s*>|new\s+EventBinding<\s*GameExitToMenuRequestedEvent\s*>" Assets/_ImmersiveGames/NewScripts -g "*.cs"
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:202:                _gamePauseBinding = new EventBinding<GamePauseCommandEvent>(OnGamePauseEvent);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:204:                _gameResumeBinding = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:206:                _gameExitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnGameExitToMenuRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\ExitToMenuNavigationBridge.cs:24:            _exitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:33:            _onPause = new EventBinding<GamePauseCommandEvent>(OnGamePause);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:34:            _onResume = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:35:            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:77:            _onPauseCommand = new EventBinding<GamePauseCommandEvent>(OnPauseCommand);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:78:            _onResumeRequested = new EventBinding<GameResumeRequestedEvent>(OnResumeRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:79:            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:42:            _onPauseCommand = new EventBinding<GamePauseCommandEvent>(OnPauseCommand);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:43:            _onResumeRequested = new EventBinding<GameResumeRequestedEvent>(_ => _paused = false);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:44:            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(_ => ResetFlagsForMenu());
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Observability\Baseline\BaselineInvariantAsserter.cs:124:            _pause = new EventBinding<GamePauseCommandEvent>(OnPause);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Observability\Baseline\BaselineInvariantAsserter.cs:125:            _resume = new EventBinding<GameResumeRequestedEvent>(_ => CheckPauseTokenReleased("GameResumeRequestedEvent"));
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:55:            _pauseBinding = new EventBinding<GamePauseCommandEvent>(OnGamePause);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:56:            _resumeBinding = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:57:            _exitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
```

### C) Leak sweep (runtime)
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

## Notes
- `GamePauseHotkeyController` ficou fora do hardening por estar em `Legacy/**`.
- `BaselineInvariantAsserter` apareceu no inventario de bindings, mas nao e owner de fluxo de pause/resume/exit e nao foi alterado nesta etapa.
- Esta etapa nao remove consumidores nem define ownership unico; ela apenas endurece overlap same-frame com observabilidade uniforme.
- Etapa mista: runtime hardening + freeze de evidencias; Unity nao foi executado aqui.

## Manual Checklist (Unity)
- Gameplay -> Pause -> Resume -> ExitToMenu.
- Verificar logs `[OBS][GRS] ... consumed ...` para os consumers runtime esperados.
- Repetir clique/acao rapida no mesmo frame quando possivel e confirmar `[OBS][GRS] ... dedupe_same_frame ...`.
- Confirmar ausencia de regressao funcional em pause, resume e exit-to-menu.

## Commands Reproduced
```text
rg -n "EventBus<\s*GamePauseCommandEvent\s*>\.Register|EventBus<\s*GameResumeRequestedEvent\s*>\.Register|EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register" Assets/_ImmersiveGames/NewScripts -g "*.cs"
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\ExitToMenuNavigationBridge.cs:25:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitToMenuBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:86:                EventBus<GamePauseCommandEvent>.Register(_pauseBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:87:                EventBus<GameResumeRequestedEvent>.Register(_resumeBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gates\Interop\GamePauseGateBridge.cs:88:                EventBus<GameExitToMenuRequestedEvent>.Register(_exitToMenuBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:236:                EventBus<GamePauseCommandEvent>.Register(_gamePauseBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:237:                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:238:                EventBus<GameExitToMenuRequestedEvent>.Register(_gameExitToMenuBinding);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:37:            EventBus<GamePauseCommandEvent>.Register(_onPause);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:38:            EventBus<GameResumeRequestedEvent>.Register(_onResume);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Bridges\GameLoopCommandEventBridge.cs:39:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:81:            EventBus<GamePauseCommandEvent>.Register(_onPauseCommand);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:82:            EventBus<GameResumeRequestedEvent>.Register(_onResumeRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:83:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:46:            EventBus<GamePauseCommandEvent>.Register(_onPauseCommand);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:47:            EventBus<GameResumeRequestedEvent>.Register(_onResumeRequested);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:48:            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
```

```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```
