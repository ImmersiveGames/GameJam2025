# BATCH-CLEANUP-STD-5

Date: 2026-03-10
Source of truth: workspace local (`C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`)

## Goal
Prune empty `Modules/**/Legacy/**` trees after the previous legacy file cleanup waves, without touching `Dev/**`, `Editor/**` or `QA/**`.

## Inventory result
No `Modules/**/Legacy/**/*.cs` files remained in the local workspace at the start of this batch.

## Final table

| FilePath | TypeName | MetaGuid | A(CallsitesOutside) | B(AssetRefsOutside) | Decision | Notes |
|---|---|---|---:|---:|---|---|
| _none_ | - | - | - | - | n/a | No remaining legacy `.cs` files under `Modules/**/Legacy/**`. |

## Deleted folders
- `Modules/ContentSwap/Legacy/`
- `Modules/GameLoop/Legacy/`
- `Modules/Gameplay/Legacy/`
- `Modules/InputModes/Legacy/`
- `Modules/LevelFlow/Legacy/`
- `Modules/Navigation/Legacy/`

## Notes
- Each deleted folder tree was empty except for Unity folder `.meta` files.
- No `Dev/**`, `Editor/**`, or `QA/**` folder was touched.
- No runtime canonic rail, payload, owner, or pipeline ordering changed.

## Proof / outputs
```text
Get-ChildItem Modules -Directory -Recurse | Where-Object { $_.Name -eq 'Legacy' }
0 matches after cleanup
```

```text
rg -n "Legacy/.*\.cs" Assets/_ImmersiveGames/NewScripts -g "*.cs"
0 matches
```

```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "EventBus<\s*GameResetRequestedEvent\s*>\.Register" Assets/_ImmersiveGames/NewScripts -g "*.cs"
.\Modules\Navigation\Runtime\MacroRestartCoordinator.cs:33:            EventBus<GameResetRequestedEvent>.Register(_resetBinding);
.\Modules\Gameplay\Runtime\Actions\States\StateDependentService.cs:232:                EventBus<GameResetRequestedEvent>.Register(_gameResetBinding);
```

```text
rg -n "EventBus<\s*GameExitToMenuRequestedEvent\s*>\.Register" Assets/_ImmersiveGames/NewScripts -g "*.cs"
.\Modules\Navigation\Runtime\ExitToMenuCoordinator.cs:36:            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);
```

```text
Tools/Gates/Run-NewScripts-RgGates.ps1
PASS Gate A
PASS Gate A2
PASS Gate B
```

## Outcome
- Behavior-preserving.
- Empty legacy module folders removed together with their Unity folder metas.
- Canonical owners and runtime gates remained unchanged.
