# BATCH-CLEANUP-STD-1

Date: 2026-03-10

## Objective
- Aplicar o fluxo padrao provar -> remover -> documentar em bridges legacy de Navigation.
- Reduzir redundancia sem alterar contratos publicos, payloads de eventos ou ordem do pipeline.
- Congelar evidencias estaticas do workspace local em um unico snapshot.

## Source Of Truth
- Workspace local: `C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`

## Inventory
| Target | ActionCandidate | CallsiteOK | GuidOK | Decision | Notes |
|---|---|---|---|---|---|
| `Modules/Navigation/Legacy/RestartNavigationBridge.cs` | DELETE | Yes | Yes | DELETE | Tipo aparece apenas no proprio arquivo; GUID `37d7bb77caa245747bb1cdfe99d31fd7` sem hits em assets. |
| `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs` | DELETE | Yes | Yes | DELETE | Tipo aparece apenas no proprio arquivo; GUID `ebe05fdbf2cf4d94892c5a153a540d51` sem hits em assets. |
| `Modules/Navigation/Legacy/ExitToMenuNavigationBridge.cs` | DELETE | Yes | Yes | DELETE | Tipo aparece apenas no proprio arquivo; GUID `e4adffa51401a2646a5c29e963cd22c3` sem hits em assets. |

## Commands
```text
rg -n "RestartNavigationBridge" . -g "*.cs"
rg -n "RestartSnapshotContentSwapBridge" . -g "*.cs"
rg -n "ExitToMenuNavigationBridge" . -g "*.cs"
rg -n "<guid>" . -g "*.unity" -g "*.prefab" -g "*.asset"
rg -n "RestartNavigationBridge|RestartSnapshotContentSwapBridge|ExitToMenuNavigationBridge" . -g "*.cs"
rg -n "EventBus<\s*GameResetRequestedEvent\s*>\.Register" . -g "*.cs"
rg -n "EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register" . -g "*.cs"
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
Tools/Gates/Run-NewScripts-RgGates.ps1
```

## Deleted
- `Modules/Navigation/Legacy/RestartNavigationBridge.cs`
- `Modules/Navigation/Legacy/RestartNavigationBridge.cs.meta`
- `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs`
- `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs.meta`
- `Modules/Navigation/Legacy/ExitToMenuNavigationBridge.cs`
- `Modules/Navigation/Legacy/ExitToMenuNavigationBridge.cs.meta`

## Moved
- None.

## Kept
- None.

## Evidence - Precheck
```text
rg -n "RestartNavigationBridge" . -g "*.cs"
.\Modules\Navigation\Legacy\RestartNavigationBridge.cs:6:    public sealed class RestartNavigationBridge : IDisposable
.\Modules\Navigation\Legacy\RestartNavigationBridge.cs:8:        public RestartNavigationBridge()
.\Modules\Navigation\Legacy\RestartNavigationBridge.cs:10:            DebugUtility.LogWarning<RestartNavigationBridge>(
```

```text
rg -n "RestartSnapshotContentSwapBridge" . -g "*.cs"
.\Modules\Navigation\Legacy\RestartSnapshotContentSwapBridge.cs:8:    public sealed class RestartSnapshotContentSwapBridge : IDisposable
.\Modules\Navigation\Legacy\RestartSnapshotContentSwapBridge.cs:13:        public RestartSnapshotContentSwapBridge()
.\Modules\Navigation\Legacy\RestartSnapshotContentSwapBridge.cs:18:            DebugUtility.LogVerbose<RestartSnapshotContentSwapBridge>(
```

```text
rg -n "ExitToMenuNavigationBridge" . -g "*.cs"
.\Modules\Navigation\Legacy\ExitToMenuNavigationBridge.cs:8:    public sealed class ExitToMenuNavigationBridge : IDisposable
```

```text
rg -n "37d7bb77caa245747bb1cdfe99d31fd7" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

```text
rg -n "ebe05fdbf2cf4d94892c5a153a540d51" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

```text
rg -n "e4adffa51401a2646a5c29e963cd22c3" . -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

## Evidence - Postcheck
```text
rg -n "RestartNavigationBridge|RestartSnapshotContentSwapBridge|ExitToMenuNavigationBridge" . -g "*.cs"
0 matches
```

```text
rg -n "EventBus<\s*GameResetRequestedEvent\s*>\.Register" . -g "*.cs"
.\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:232:                EventBus<GameResetRequestedEvent>.Register(_gameResetBinding);
.\Modules\Navigation\Runtime\MacroRestartCoordinator.cs:33:            EventBus<GameResetRequestedEvent>.Register(_resetBinding);
```

```text
rg -n "EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register" . -g "*.cs"
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:36:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);
```

```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

```text
Tools/Gates/Run-NewScripts-RgGates.ps1
PASS Gate A
PASS Gate A2
PASS Gate B
```

## Final Notes
- Batch concluido com DELETE nos tres targets inicialmente listados.
- Nenhum target precisou de MOVE fallback.
- Behavior-preserving: owners canonicos permaneceram `MacroRestartCoordinator` para reset e `ExitToMenuCoordinator` para exit.
