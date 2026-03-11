# EDITOR-QA-4

## Summary
- auditDateDir: `2026-03-06`
- Canonical files confirmed:
  - `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs`
  - `Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs`
  - `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs`
- Result: consolidation-only pass. No additional `.cs` delete or move was safe under `A1 + A2 + A3` inside the current editor-only scope.
- Change applied: normalized the remaining SceneFlow validation menu/report naming from legacy `DataCleanup v1` wording to the canonical tool root.
- No file under `Assets/_ImmersiveGames/Scripts/**` was touched.

## Final Table
| FilePath | TypeName(s) | MenuItems (paths) | Meta GUID | Asset refs (GUID scan) | Runtime callsites (non-editor) | Decision |
|---|---|---|---|---|---|---|
| `Editor/Core/Events/EventBusUtil.Editor.cs` | `EventBusUtil` | - | `12bc6d378f5ae094` | `0` | `0` | `KEEP` |
| `Editor/Core/Logging/HardFailFastH1.Editor.cs` | `HardFailFastH1` | - | `b95a32c874ef1d60` | `0` | `0` | `KEEP` |
| `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs` | `GlobalCompositionRoot` | - | `d159cb4a2ef86307` | `0` | `0` | `KEEP` |
| `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs` | `IntroStageQaMenuItems` | `ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object`; `ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Complete (Force)`; `ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Skip (Force)` | `7a5695e8cce465c4181acc5a706c5efd` | `0` | `0` | `KEEP` |
| `Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs` | `ContentSwapQaMenuItems` | `ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object` | `dbeab39e7e2b4d4bb55f05ca4e1ee495` | `0` | `0` | `KEEP` |
| `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs` | `GameNavigationCatalogNormalizer` | `ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs` | `f526c71f820b463caa74adf84f4673bf` | `0` | `0` | `KEEP` |
| `Modules/Navigation/Editor/IdSources/NavigationIntentIdSourceProvider.cs` | `NavigationIntentIdSourceProvider`, `NavigationIntentIdSourceResult` | - | `716e4b7ffbba4ee091db4b52dce67577` | `0` | `0` | `KEEP` |
| `Modules/Navigation/Editor/Drawers/NavigationIntentIdPropertyDrawer.cs` | `NavigationIntentIdPropertyDrawer`, `NavigationIntentOptionsCache` | - | `07f426bbe0474bdb8f8f319c36477479` | `0` | `0` | `KEEP` |
| `Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs` | `SceneFlowConfigValidator`, `ValidationContext`, `LoadedAssets`, `AssetStatus`, `CoreIntentRecord`, `CoreSlotRecord`, `StyleValidationRecord` | `ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config` | `9e7d10ceec8b4f54bf4a40a4ab722c32` | `0` | `0` | `KEEP (normalized)` |
| `Modules/SceneFlow/Editor/IdSources/SceneFlowProfileIdSourceProvider.cs` | `SceneFlowProfileIdSourceProvider` | - | `143412ae29373004986ce5ac32477fe6` | `0` | `0` | `KEEP` |
| `Modules/SceneFlow/Editor/IdSources/SceneRouteIdSourceProvider.cs` | `SceneRouteIdSourceProvider` | - | `c95db80df8f5e374eb45fa8180992fa9` | `0` | `0` | `KEEP` |
| `Modules/SceneFlow/Editor/IdSources/TransitionStyleIdSourceProvider.cs` | `TransitionStyleIdSourceProvider` | - | `0a10497ec712c3a4586711c0b1479e1c` | `0` | `0` | `KEEP` |
| `Modules/SceneFlow/Editor/Drawers/SceneFlowProfileIdPropertyDrawer.cs` | `SceneFlowProfileIdPropertyDrawer`, `SceneFlowProfileOptionsCache` | - | `48aea0a179434c519afa0d666f574a8d` | `0` | `0` | `KEEP` |
| `Modules/SceneFlow/Editor/Drawers/SceneRouteIdPropertyDrawer.cs` | `SceneRouteIdPropertyDrawer`, `SceneRouteIdOptionsCache` | - | `9edcfe1205b649a42a0118a248fbdb37` | `0` | `0` | `KEEP` |
| `Modules/SceneFlow/Editor/Drawers/TransitionStyleIdPropertyDrawer.cs` | `TransitionStyleIdPropertyDrawer`, `TransitionStyleOptionsCache` | - | `d36d5fb93d9447cba7e42b409b507ce7` | `0` | `0` | `KEEP` |

## File Changes
### Moved
- None in `EDITOR-QA-4`.

### Deleted
- None in `EDITOR-QA-4`.
- No new candidate in the current editor-only scope passed `A1 + A2 + A3` for delete safety.

### Normalized
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs`
  - `MenuItem`: `ImmersiveGames/NewScripts/QA/SceneFlow/Validate Config (DataCleanup v1)` -> `ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config`
  - generated report path: `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md` -> `Docs/Reports/SceneFlow-Config-ValidationReport.md`
  - report title: `SceneFlow Config Validation Report (DataCleanup v1)` -> `SceneFlow Config Validation Report`

## Commands and Proofs
### TypeName / callsite spot-check
```text
rg -n "\bSceneFlowConfigValidator\b" . -g "*.cs" -g "*.md"
.\Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs:18:    public static class SceneFlowConfigValidator
... docs/history only beyond the file itself
```

### GUID spot-check
```text
rg -n "9e7d10ceec8b4f54bf4a40a4ab722c32" -g "*.unity" -g "*.prefab" -g "*.asset" .
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

### Legacy string check
```text
rg -n "DataCleanup v1" Modules Editor Docs -g "*.cs" -g "*.md"
Result: remaining hits are historical docs/evidence only; no active editor tooling path still uses the legacy menu/report label.
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

## Scope confirmation
- nao toquei em `Assets/_ImmersiveGames/Scripts/**`.
