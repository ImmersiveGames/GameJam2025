# BATCH-CLEANUP-STD-2

Date: 2026-03-10

## Objective
- Remover shims/no-op/tooling legacy sem callsite canonico no workspace local.
- Aplicar regra padrao de prova: callsite em `.cs` + GUID em assets antes de qualquer remocao.
- Preservar contratos publicos, payloads de eventos e ordem do pipeline.

## Source Of Truth
- Workspace local: `C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`

## Inventory
| Target | ActionCandidate | CallsiteOK | GuidOK | Decision | Notes |
|---|---|---|---|---|---|
| `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs` | DELETE | Yes | Yes | DELETE | Shim legacy/no-op; tipo aparece apenas no proprio arquivo; GUID `fa9f66bffc590944abfdedd7851becaa` sem hits em assets. |
| `Modules/ContentSwap/Legacy/Dev/Runtime/ContentSwapDevBootstrapper.cs` | DELETE | Yes | Yes | DELETE | Bootstrap legacy DevQA; tipo aparece apenas no proprio arquivo; GUID `c84bdb807c054daf9e67eccb0c7df30a` sem hits em assets. |
| `Modules/GameLoop/Legacy/Bindings/Inputs/GamePauseHotkeyController.cs` | DELETE | Yes | Yes | DELETE | Binding legacy; tipo aparece apenas no proprio arquivo; GUID `667975ca949338b4caf32e93a7bffd8d` sem hits em assets. |
| `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmRequestDevDriver.cs` | DELETE | Yes | Yes | DELETE | Tooling editor legacy; tipo aparece apenas no proprio arquivo; GUID `4b5cea7fdfc2456b9286a8c1679b2844` sem hits em assets. |
| `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmKindDevEaterActor.cs` | DELETE | Yes | Yes | DELETE | Tooling editor legacy; tipo aparece apenas no proprio arquivo; GUID `8a4fec27d65745d0b88f72380bf45587` sem hits em assets. |
| `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmDevStepLogger.cs` | DELETE | Yes | Yes | DELETE | Tooling editor legacy; tipo aparece apenas no proprio arquivo; GUID `898f8b99e1cfa5c4ebe93238753b7ea0` sem hits em assets. |
| `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs` | KEEP | No | n/a | KEEP | Ainda possui callsites reais em `.cs` (editor/infra/config), sem base para remocao. |
| `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs` | KEEP | No | n/a | KEEP | Ainda possui callsite real em `LevelCatalogAsset.cs`. |
| `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs` | KEEP | No | n/a | KEEP | Ainda possui callsite real em `LevelCatalogAsset.cs`. |
| `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs` | KEEP | No | n/a | KEEP | Ainda possui callsite real em `LevelCatalogAsset.cs`. |

## Deleted
- `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs`
- `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs.meta`
- `Modules/ContentSwap/Legacy/Dev/Runtime/ContentSwapDevBootstrapper.cs`
- `Modules/ContentSwap/Legacy/Dev/Runtime/ContentSwapDevBootstrapper.cs.meta`
- `Modules/GameLoop/Legacy/Bindings/Inputs/GamePauseHotkeyController.cs`
- `Modules/GameLoop/Legacy/Bindings/Inputs/GamePauseHotkeyController.cs.meta`
- `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmRequestDevDriver.cs`
- `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmRequestDevDriver.cs.meta`
- `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmKindDevEaterActor.cs`
- `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmKindDevEaterActor.cs.meta`
- `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmDevStepLogger.cs`
- `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmDevStepLogger.cs.meta`

## Moved
- None.

## Kept
- `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs`

## Evidence - Precheck
```text
rg -n "InputModeBootstrap" . -g "*.cs"
.\Modules\InputModes\Legacy\Bootstrap\InputModeBootstrap.cs:24:    public sealed class InputModeBootstrap : MonoBehaviour
```

```text
rg -n "ContentSwapDevBootstrapper" . -g "*.cs"
.\Modules\ContentSwap\Legacy\Dev\Runtime\ContentSwapDevBootstrapper.cs:6:    public static class ContentSwapDevBootstrapper
```

```text
rg -n "GamePauseHotkeyController" . -g "*.cs"
.\Modules\GameLoop\Legacy\Bindings\Inputs\GamePauseHotkeyController.cs:18:    public sealed class GamePauseHotkeyController : MonoBehaviour
```

```text
rg -n "RunRearmRequestDevDriver|RunRearmKindDevEaterActor|RunRearmDevStepLogger" . -g "*.cs"
.\Modules\Gameplay\Legacy\Editor\RunRearm\RunRearmRequestDevDriver.cs:22:    public sealed class RunRearmRequestDevDriver : MonoBehaviour
.\Modules\Gameplay\Legacy\Editor\RunRearm\RunRearmKindDevEaterActor.cs:13:    public sealed class RunRearmKindDevEaterActor : MonoBehaviour, IActor, IActorKindProvider, IRunRearmable
.\Modules\Gameplay\Legacy\Editor\RunRearm\RunRearmDevStepLogger.cs:14:    public sealed class RunRearmDevStepLogger : MonoBehaviour, IRunRearmable
```

```text
rg -n "fa9f66bffc590944abfdedd7851becaa" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

```text
rg -n "c84bdb807c054daf9e67eccb0c7df30a" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

```text
rg -n "667975ca949338b4caf32e93a7bffd8d" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

```text
rg -n "4b5cea7fdfc2456b9286a8c1679b2844" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

```text
rg -n "8a4fec27d65745d0b88f72380bf45587" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

```text
rg -n "898f8b99e1cfa5c4ebe93238753b7ea0" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

## Evidence - Postcheck
```text
rg -n "InputModeBootstrap|ContentSwapDevBootstrapper|GamePauseHotkeyController|RunRearmRequestDevDriver|RunRearmKindDevEaterActor|RunRearmDevStepLogger" . -g "*.cs"
0 matches
```

```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "EventBus<\s*GameResetRequestedEvent\s*>\.Register" . -g "*.cs"
.\Modules\Navigation\Runtime\MacroRestartCoordinator.cs:33:            EventBus<GameResetRequestedEvent>.Register(_resetBinding);
.\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:232:                EventBus<GameResetRequestedEvent>.Register(_gameResetBinding);
```

```text
rg -n "EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register" . -g "*.cs"
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:36:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);
```

```text
Tools/Gates/Run-NewScripts-RgGates.ps1
PASS Gate A
PASS Gate A2
PASS Gate B
```

## Final Notes
- Batch concluiu apenas com DELETE; nenhum item precisou de MOVE fallback.
- Runtime canonico permaneceu intacto: nenhum listener duplicado foi reintroduzido.
- `LevelCatalogAsset` e interfaces legacy de `LevelFlow` ficaram em `KEEP` por callsites reais no workspace local.
