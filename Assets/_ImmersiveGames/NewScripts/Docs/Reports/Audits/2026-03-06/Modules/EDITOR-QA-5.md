# EDITOR-QA-5

## Summary
- auditDateDir: `2026-03-06`
- Result: net-negative `.cs` = `-3`
- `A2` blockers inventory was fully clear in the current editor-only scope: all editor scripts scanned to `AssetRefCount = 0`.
- Real prune came from merge-by-consolidation inside the SceneFlow editor ID-source cluster.
- nao toquei em `Assets/_ImmersiveGames/Scripts/**`.

## A2 Blockers (before -> after)
| EditorScriptPath | MetaGuid | AssetRefCount | AssetPaths(sample) | TypeKind | Decision |
|---|---|---:|---|---|---|
| `Editor/Core/Events/EventBusUtil.Editor.cs` | `12bc6d378f5ae094` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Editor/Core/Logging/HardFailFastH1.Editor.cs` | `b95a32c874ef1d60` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs` | `d159cb4a2ef86307` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs` | `dbeab39e7e2b4d4bb55f05ca4e1ee495` | 0 | - | `static MenuItem/tool` | `A2 clear -> KEEP` |
| `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs` | `7a5695e8cce465c4181acc5a706c5efd` | 0 | - | `static MenuItem/tool` | `A2 clear -> KEEP` |
| `Modules/Navigation/Editor/Drawers/NavigationIntentIdPropertyDrawer.cs` | `07f426bbe0474bdb8f8f319c36477479` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Modules/Navigation/Editor/IdSources/NavigationIntentIdSourceProvider.cs` | `716e4b7ffbba4ee091db4b52dce67577` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs` | `f526c71f820b463caa74adf84f4673bf` | 0 | - | `static MenuItem/tool` | `A2 clear -> KEEP` |
| `Modules/SceneFlow/Editor/Drawers/SceneFlowProfileIdPropertyDrawer.cs` | `48aea0a179434c519afa0d666f574a8d` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Modules/SceneFlow/Editor/Drawers/SceneRouteIdPropertyDrawer.cs` | `9edcfe1205b649a42a0118a248fbdb37` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Modules/SceneFlow/Editor/Drawers/TransitionStyleIdPropertyDrawer.cs` | `d36d5fb93d9447cba7e42b409b507ce7` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Modules/SceneFlow/Editor/IdDrawers/SceneFlowTypedIdDrawerBase.cs` | `d00bc6f2ad1bf7d4cbb76ce1607f49a0` | 0 | - | `other editor type` | `A2 clear -> CANONICAL MERGE TARGET` |
| `Modules/SceneFlow/Editor/IdSources/ISceneFlowIdSourceProvider.cs` | `16d163c00d2fe634ea9c1255fb66ef5d` | 0 | - | `other editor type` | `A2 clear -> MERGE+DELETE` |
| `Modules/SceneFlow/Editor/IdSources/SceneFlowIdSourceResult.cs` | `eee1365bf4e78d1438cfb791b1bb179c` | 0 | - | `other editor type` | `A2 clear -> MERGE+DELETE` |
| `Modules/SceneFlow/Editor/IdSources/SceneFlowIdSourceUtility.cs` | `b48f59637adefc84ab5e02fd7b1b1b64` | 0 | - | `static MenuItem/tool` | `A2 clear -> MERGE+DELETE` |
| `Modules/SceneFlow/Editor/IdSources/SceneFlowProfileIdSourceProvider.cs` | `143412ae29373004986ce5ac32477fe6` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Modules/SceneFlow/Editor/IdSources/SceneRouteIdSourceProvider.cs` | `c95db80df8f5e374eb45fa8180992fa9` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Modules/SceneFlow/Editor/IdSources/TransitionStyleIdSourceProvider.cs` | `0a10497ec712c3a4586711c0b1479e1c` | 0 | - | `other editor type` | `A2 clear -> KEEP` |
| `Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs` | `9e7d10ceec8b4f54bf4a40a4ab722c32` | 0 | - | `static MenuItem/tool` | `A2 clear -> KEEP` |

## Assets Altered
- None.
- `A2` blockers count before: `0`
- `A2` blockers count after: `0`

## Merged / Deleted `.cs`
Merged into canonical target:
- target: `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdDrawers\SceneFlowTypedIdDrawerBase.cs`

Deleted after merge:
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\ISceneFlowIdSourceProvider.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\ISceneFlowIdSourceProvider.cs.meta`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneFlowIdSourceResult.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneFlowIdSourceResult.cs.meta`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneFlowIdSourceUtility.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneFlowIdSourceUtility.cs.meta`

Merge rationale:
- `A1`: `ISceneFlowIdSourceProvider`, `SceneFlowIdSourceResult` and `SceneFlowIdSourceUtility` are referenced only by the SceneFlow editor drawer/provider cluster plus docs/history.
- `A2`: all three GUID scans returned `0 matches` in `.unity/.prefab/.asset`.
- `A3`: the same functionality now lives in the canonical editor file `SceneFlowTypedIdDrawerBase.cs`, preserving namespace/type names and behavior.

## Commands and Proofs
### A1 callsites after merge
```text
rg -n "\bISceneFlowIdSourceProvider\b|\bSceneFlowIdSourceResult\b|\bSceneFlowIdSourceUtility\b" . -g "*.cs" -g "*.md"
Result: active `.cs` references now point to `Modules/SceneFlow/Editor/IdDrawers/SceneFlowTypedIdDrawerBase.cs` plus SceneFlow editor drawers/providers and docs/history only.
```

### A2 GUID scans
```text
rg -n "16d163c00d2fe634ea9c1255fb66ef5d|eee1365bf4e78d1438cfb791b1bb179c|b48f59637adefc84ab5e02fd7b1b1b64" . -g "*.unity" -g "*.prefab" -g "*.asset"
Result: 0 matches
```

### MenuItem uniqueness
```text
rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object' . -g '*.cs'
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:12:        private const string SelectQaMenuPath = "ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object";

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Complete (Force)' . -g '*.cs'
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:39:        [MenuItem("ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Complete (Force)", priority = 1291)]

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Skip (Force)' . -g '*.cs'
.\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs:62:        [MenuItem("ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Skip (Force)", priority = 1292)]

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object' . -g '*.cs'
.\Modules\ContentSwap\Editor\ContentSwapQaMenuItems.cs:11:        [MenuItem("ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object", priority = 1310)]

rg -n --fixed-strings 'ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs' . -g '*.cs'
.\Modules\Navigation\Editor\Tools\GameNavigationCatalogNormalizer.cs:30:        [MenuItem("ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs", priority = 1510)]

rg -n --fixed-strings 'ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config' . -g '*.cs'
.\Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs:34:        [MenuItem("ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config", priority = 1410)]
```

### Leak sweep
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
Result: 0 matches
```

### Gates
```text
PASS Gate A
PASS Gate A2
PASS Gate B
```
