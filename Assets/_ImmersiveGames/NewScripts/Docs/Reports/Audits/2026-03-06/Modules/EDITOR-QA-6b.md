# EDITOR-QA-6b

## Summary
- Result: net-negative `.cs` = `-4`
- Canonical menu hubs kept:
  - `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs`
  - `Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs`
  - `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs`
  - `Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs`
- No runtime/pipeline/owners/event payloads changed.
- nao tocou em `Assets/_ImmersiveGames/Scripts/**`.

## Inventory
| FilePath | Classification | Reason |
|---|---|---|
| `Editor/Core/Events/EventBusUtil.Editor.cs` | `KEEP` | canonical global editor hook (`InitializeOnLoadMethod`) |
| `Editor/Core/Logging/HardFailFastH1.Editor.cs` | `KEEP` | canonical global editor fail-fast hook |
| `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs` | `KEEP` | canonical global editor composition hook |
| `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs` | `KEEP` | canonical menu hub already adopted |
| `Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs` | `KEEP` | canonical menu hub already adopted |
| `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs` | `KEEP` | canonical module tool |
| `Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs` | `KEEP` | canonical module validator |
| `Modules/Navigation/Editor/Drawers/NavigationIntentIdPropertyDrawer.cs` | `MERGE TARGET` | absorbed provider/result types from `NavigationIntentIdSourceProvider.cs` |
| `Modules/SceneFlow/Editor/Drawers/SceneFlowProfileIdPropertyDrawer.cs` | `MERGE TARGET` | absorbed `SceneFlowProfileIdSourceProvider.cs` |
| `Modules/SceneFlow/Editor/Drawers/SceneRouteIdPropertyDrawer.cs` | `MERGE TARGET` | absorbed `SceneRouteIdSourceProvider.cs` |
| `Modules/SceneFlow/Editor/Drawers/TransitionStyleIdPropertyDrawer.cs` | `MERGE TARGET` | absorbed `TransitionStyleIdSourceProvider.cs` |
| `Modules/Navigation/Editor/IdSources/NavigationIntentIdSourceProvider.cs` | `DELETE` | provider/result used only by Navigation drawer; A2 clear |
| `Modules/SceneFlow/Editor/IdSources/SceneFlowProfileIdSourceProvider.cs` | `DELETE` | provider used only by SceneFlow profile drawer; A2 clear |
| `Modules/SceneFlow/Editor/IdSources/SceneRouteIdSourceProvider.cs` | `DELETE` | provider used only by SceneFlow route drawer; A2 clear |
| `Modules/SceneFlow/Editor/IdSources/TransitionStyleIdSourceProvider.cs` | `DELETE` | provider used only by SceneFlow transition drawer; A2 clear |
| `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs` | `RISK` | runtime-attached QA component installed in play mode; not editor-only |
| `Modules/GameLoop/IntroStage/Dev/IntroStageDevContextMenu.cs` | `RISK` | runtime-attached QA component installed in play mode; not editor-only |
| `Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs` | `RISK` | runtime-attached QA component installed in play mode; not editor-only |
| `Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs` | `RISK` | runtime-attached QA component installed in play mode; not editor-only |

## Moves / Deletes
### Deletes
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Editor\IdSources\NavigationIntentIdSourceProvider.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Editor\IdSources\NavigationIntentIdSourceProvider.cs.meta`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneFlowProfileIdSourceProvider.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneFlowProfileIdSourceProvider.cs.meta`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneRouteIdSourceProvider.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneRouteIdSourceProvider.cs.meta`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\TransitionStyleIdSourceProvider.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\TransitionStyleIdSourceProvider.cs.meta`

### Merges
- `NavigationIntentIdSourceProvider` + `NavigationIntentIdSourceResult` -> `Modules/Navigation/Editor/Drawers/NavigationIntentIdPropertyDrawer.cs`
- `SceneFlowProfileIdSourceProvider` -> `Modules/SceneFlow/Editor/Drawers/SceneFlowProfileIdPropertyDrawer.cs`
- `SceneRouteIdSourceProvider` -> `Modules/SceneFlow/Editor/Drawers/SceneRouteIdPropertyDrawer.cs`
- `TransitionStyleIdSourceProvider` -> `Modules/SceneFlow/Editor/Drawers/TransitionStyleIdPropertyDrawer.cs`

### Empty folder prune
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Editor\IdSources`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources`

## rg Proofs
### MenuItem inventory
```text
rg -n "\[MenuItem\(" Assets/_ImmersiveGames/NewScripts -g "*.cs"
Result highlights:
- IntroStageQaMenuItems.cs
- ContentSwapQaMenuItems.cs
- GameNavigationCatalogNormalizer.cs
- SceneFlowConfigValidator.cs
```

### Candidate typename usage
```text
rg -n "\bNavigationIntentIdSourceProvider\b|\bNavigationIntentIdSourceResult\b|\bSceneFlowProfileIdSourceProvider\b|\bSceneRouteIdSourceProvider\b|\bTransitionStyleIdSourceProvider\b" Assets/_ImmersiveGames/NewScripts -g "*.cs"
Result after merge: active `.cs` references point only to the merged drawer files.
```

### GUID scans
```text
rg -n "716e4b7ffbba4ee091db4b52dce67577|143412ae29373004986ce5ac32477fe6|c95db80df8f5e374eb45fa8180992fa9|0a10497ec712c3a4586711c0b1479e1c" -g "*.unity" -g "*.prefab" -g "*.asset" . -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**"
Result: 0 matches
```

### Unique menu paths
```text
rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object' . -g '*.cs'
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:12:        private const string SelectQaMenuPath = "ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object";

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object' . -g '*.cs'
.\Modules\ContentSwap\Editor\ContentSwapQaMenuItems.cs:11:        [MenuItem("ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object", priority = 1310)]

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs' . -g '*.cs'
.\Modules\Navigation\Editor\Tools\GameNavigationCatalogNormalizer.cs:30:        [MenuItem("ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs", priority = 1510)]

rg -n --fixed-strings 'ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config' . -g '*.cs'
.\Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs:34:        [MenuItem("ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config", priority = 1410)]
```

## Post-checks
### Empty-folder reinventory
```text
Result: no remaining empty-or-meta-only subfolders outside the protected roots.
```

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
