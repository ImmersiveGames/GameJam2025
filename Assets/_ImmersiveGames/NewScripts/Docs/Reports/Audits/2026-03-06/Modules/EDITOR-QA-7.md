# EDITOR-QA-7

## Summary
- Result: net-negative `.cs` = `-1`
- Scope: final redundant editor/QA tooling prune.
- No runtime/pipeline changes.
- nao tocou em `Assets/_ImmersiveGames/Scripts/**`.

## Inventory Summary
### MenuItems
- `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs`
- `Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs`
- `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs`
- `Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs`

### Candidate Tools
| FilePath | Classification | Reason |
|---|---|---|
| `Modules/SceneFlow/Editor/IdDrawers/SceneFlowTypedIdDrawerBase.cs` | `DELETE` | abstract base no longer referenced by active `.cs`; helper types migrated to active drawer owner |
| `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs` | `RISK` | runtime-attached QA component in play mode |
| `Modules/GameLoop/IntroStage/Dev/IntroStageDevContextMenu.cs` | `RISK` | runtime-attached QA component in play mode |
| `Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs` | `RISK` | runtime-attached QA component in play mode |
| `Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs` | `RISK` | runtime-attached QA component in play mode |

## Merges
- Shared SceneFlow editor helper types moved into `Modules/SceneFlow/Editor/Drawers/TransitionStyleIdPropertyDrawer.cs`:
  - `ISceneFlowIdSourceProvider<TId>`
  - `SceneFlowIdSourceResult`
  - `SceneFlowIdSourceUtility`

## Deletes
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdDrawers\SceneFlowTypedIdDrawerBase.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdDrawers\SceneFlowTypedIdDrawerBase.cs.meta`

## Empty Folder Prune
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdDrawers`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdDrawers.meta`

## rg Proofs
### Candidate usage
```text
rg -n "\bSceneFlowTypedIdDrawerBase\b" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**"
Result: 0 matches

rg -n "\bSceneFlowTypedIdDrawerBase\b|\bISceneFlowIdSourceProvider\b|\bSceneFlowIdSourceResult\b|\bSceneFlowIdSourceUtility\b" Assets/_ImmersiveGames/NewScripts -g "*.cs"
Result: active `.cs` references point only to merged drawer files plus docs/history.
```

### GUID scan
```text
rg -n "d00bc6f2ad1bf7d4cbb76ce1607f49a0" -g "*.unity" -g "*.prefab" -g "*.asset" . -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**"
Result: 0 matches
```

### MenuItem uniqueness
```text
rg -n "\[MenuItem\(" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "**/Editor/**" -g "**/QA/**"
Result set contains only the four canonical menu hubs; no duplicated canonical menu strings.
```

## Post-checks
### Leak sweep
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/QA/**" -g "!**/Legacy/**"
Result: 0 matches
```

### Gates
```text
PASS Gate A
PASS Gate A2
PASS Gate B
```
