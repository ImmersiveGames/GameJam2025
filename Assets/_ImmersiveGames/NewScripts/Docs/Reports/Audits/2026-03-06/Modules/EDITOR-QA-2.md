# EDITOR-QA-2

## Summary
- auditDateDir: `2026-03-06`
- Result: net-negative `.cs` (`-2` in scope: 3 deletions, 1 new helper file, 2 file moves/renames)
- Runtime leak sweep outside `Dev/Editor/QA/Legacy` stayed on the documented allowlist only.
- Gates passed: `PASS Gate A`, `PASS Gate A2`, `PASS Gate B`.

## Inventory
| FilePath | TypeNames | Kind | HasMenuItem | UsesUnityEditor | CompileGuards | RuntimeDependencies | AssetRefs | Decision |
|---|---|---|---|---|---|---|---|---|
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Editor\Core\Events\EventBusUtil.Editor.cs` | `EventBusUtil` | `EditorTool` | `false` | `true` | `UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Editor\Core\Logging\HardFailFastH1.Editor.cs` | `HardFailFastH1` | `EditorTool` | `false` | `true` | `UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Editor\Infrastructure\Composition\GlobalCompositionRoot.Editor.cs` | `GlobalCompositionRoot` | `EditorTool` | `false` | `true` | `UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\ContentSwap\Dev\Bindings\ContentSwapDevContextMenu.cs` | `ContentSwapDevContextMenu` | `DevBuildTool` | `false` | `false` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\ContentSwap\Dev\Runtime\ContentSwapDevInstaller.cs` | `ContentSwapDevInstaller` | `DevBuildTool` | `false` | `false` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\ContentSwap\Editor\ContentSwapQaMenuItems.cs` | `ContentSwapQaMenuItems` | `EditorTool` | `true` | `true` | `UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Dev\GameLoopSceneFlowCoordinator.DevQA.cs` | `GameLoopSceneFlowCoordinator` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\IntroStage\Dev\IntroStageDevContextMenu.cs` | `IntroStageDevContextMenu` | `DevBuildTool` | `false` | `false` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\IntroStage\Dev\IntroStageDevInstaller.cs` | `IntroStageDevInstaller` | `DevBuildTool` | `false` | `false` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\IntroStage\Dev\IntroStageDevTester.cs` | `IntroStageDevTester` | `DevBuildTool` | `false` | `false` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\IntroStage\Dev\IntroStageRuntimeDebugGui.cs` | `IntroStageRuntimeDebugGui` | `DevBuildTool` | `false` | `false` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\IntroStage\Editor\IntroStageQaMenuItems.cs` | `IntroStageQaMenuItems` | `EditorTool` | `true` | `true` | `UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Dev\PauseOverlayController.DevQA.cs` | `PauseOverlayController` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\LevelFlow\Config\Dev\SceneBuildIndexRef.DevQA.cs` | `SceneBuildIndexRef` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\LevelFlow\Dev\LevelFlowDevContextMenu.cs` | `LevelFlowDevContextMenu` | `DevBuildTool` | `false` | `false` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\LevelFlow\Dev\LevelFlowDevInstaller.cs` | `LevelFlowDevInstaller` | `DevBuildTool` | `false` | `false` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Dev\GameNavigationCatalogAsset.DevQA.cs` | `GameNavigationCatalogAsset` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Dev\GameNavigationIntentCatalogAsset.DevQA.cs` | `GameNavigationIntentCatalogAsset` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Dev\MenuQuitButtonBinder.DevQA.cs` | `MenuQuitButtonBinder` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Editor\Drawers\NavigationIntentIdPropertyDrawer.cs` | `NavigationIntentIdPropertyDrawer, NavigationIntentOptionsCache` | `EditorTool` | `false` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Editor\IdSources\NavigationIntentIdSourceProvider.cs` | `NavigationIntentIdSourceProvider` | `EditorTool` | `false` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\Navigation\Editor\Tools\GameNavigationCatalogNormalizer.cs` | `GameNavigationCatalogNormalizer` | `EditorTool` | `true` | `true` | `UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\PostGame\Dev\PostGameOverlayController.DevQA.cs` | `PostGameOverlayController` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Dev\SceneFlowDevContextMenu.cs` | `SceneFlowDevContextMenu` | `DevBuildTool` | `false` | `false` | `UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Dev\SceneFlowDevInstaller.cs` | `SceneFlowDevInstaller` | `DevBuildTool` | `false` | `false` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\Drawers\SceneFlowProfileIdPropertyDrawer.cs` | `SceneFlowProfileIdPropertyDrawer, SceneFlowProfileOptionsCache` | `EditorTool` | `false` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\Drawers\SceneRouteIdPropertyDrawer.cs` | `SceneRouteIdPropertyDrawer, SceneRouteIdOptionsCache` | `EditorTool` | `false` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\Drawers\TransitionStyleIdPropertyDrawer.cs` | `TransitionStyleIdPropertyDrawer, TransitionStyleOptionsCache` | `EditorTool` | `false` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdDrawers\SceneFlowTypedIdDrawerBase.cs` | `SceneFlowTypedIdDrawerBase` | `EditorTool` | `false` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\ISceneFlowIdSourceProvider.cs` | `ISceneFlowIdSourceProvider` | `EditorTool` | `false` | `false` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneFlowIdSourceResult.cs` | `-` | `EditorTool` | `false` | `false` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneFlowIdSourceUtility.cs` | `SceneFlowIdSourceUtility` | `EditorTool` | `false` | `false` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneFlowProfileIdSourceProvider.cs` | `SceneFlowProfileIdSourceProvider` | `EditorTool` | `false` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\SceneRouteIdSourceProvider.cs` | `SceneRouteIdSourceProvider` | `EditorTool` | `false` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\IdSources\TransitionStyleIdSourceProvider.cs` | `TransitionStyleIdSourceProvider` | `EditorTool` | `false` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs` | `SceneFlowConfigValidator, ValidationContext, LoadedAssets, CoreIntentRecord, CoreSlotRecord, StyleValidationRecord` | `EditorTool` | `true` | `true` | `None` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Navigation\Dev\SceneRouteCatalogAsset.DevQA.cs` | `SceneRouteCatalogAsset` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Navigation\Dev\SceneRouteDefinitionAsset.DevQA.cs` | `SceneRouteDefinitionAsset` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Navigation\Dev\TransitionStyleCatalogAsset.DevQA.cs` | `TransitionStyleCatalogAsset` | `PartialDevQA` | `false` | `false` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\SceneFlow\Transition\Dev\SceneTransitionService.DevQA.cs` | `SceneTransitionService` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\WorldLifecycle\Dev\SceneRouteResetPolicy.DevQA.cs` | `SceneRouteResetPolicy` | `PartialDevQA` | `false` | `true` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\WorldLifecycle\Dev\WorldLifecycleHookLoggerA.cs` | `WorldLifecycleHookLoggerA` | `DevBuildTool` | `false` | `false` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\WorldLifecycle\Dev\WorldResetRequestHotkeyBridge.cs` | `WorldResetRequestHotkeyBridge` | `DevBuildTool` | `false` | `false` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |
| `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\WorldLifecycle\Dev\WorldResetRequestHotkeyDevBootstrap.cs` | `WorldResetRequestHotkeyDevBootstrap` | `DevBuildTool` | `false` | `false` | `UNITY_EDITOR || DEVELOPMENT_BUILD; UNITY_EDITOR` | `Scoped tooling references runtime only inside Dev/Editor/QA` | `0` | `Keep` |

## Moves (with `.meta`)
- `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevMenuItems.cs` -> `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs`
- `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevMenuItems.cs.meta` -> `Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs.meta`
- `Modules/Navigation/Dev/Editor/GameNavigationCatalogNormalizer.cs` -> `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs`
- `Modules/Navigation/Dev/Editor/GameNavigationCatalogNormalizer.cs.meta` -> `Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs.meta`
- New folder metas created for canonical layout: `Modules/GameLoop/IntroStage/Editor.meta`, `Modules/Navigation/Editor/Tools.meta`, `Modules/ContentSwap/Editor.meta`
- Empty folder metas removed after move: `Modules/GameLoop/IntroStage/Dev/Editor.meta`, `Modules/Navigation/Dev/Editor.meta`

## Deletes (with proof)
- `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs` + `.meta`
  - type grep: only docs/history plus one out-of-scope log string in `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs`; no compile callsites
  - GUID proof (`a93c29780d15bdc499c929c46cc67349`): `0 matches` in `.unity/.prefab/.asset`
- `Modules/SceneFlow/Editor/Validation/TransitionStyleProfileRefMigrator.cs` + `.meta`
  - type grep: self only in `.cs`; no active callsites
  - GUID proof (`8deef1a76e544f4dae8866f3e0afb25b`): `0 matches` in `.unity/.prefab/.asset`
- `Modules/SceneFlow/Editor/Validation/SceneFlowConfigReserializer.cs` + `.meta`
  - type grep: self only in `.cs`; no active callsites
  - GUID proof (`654f7f1cf2b74f2ca15e654c87b247aa`): `0 matches` in `.unity/.prefab/.asset`

## Archives
- `Docs/Reports/Audits/2026-03-06/Archive/EditorQA/Modules_SceneFlow_Editor_Validation_TransitionStyleProfileRefMigrator.cs.txt`
- `Docs/Reports/Audits/2026-03-06/Archive/EditorQA/Modules_SceneFlow_Editor_Validation_SceneFlowConfigReserializer.cs.txt`

## Menu normalization
- Canonical root applied where `MenuItem` remains: `ImmersiveGames/NewScripts/QA/<Module>/<Action>`
- Updated examples:
  - `ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/...`
  - `ImmersiveGames/NewScripts/QA/ContentSwap/...`
  - `ImmersiveGames/NewScripts/QA/Navigation/...`
  - `ImmersiveGames/NewScripts/QA/SceneFlow/Validate Config (DataCleanup v1)`

## Post-checks
### Old menu roots
```text
rg -n "Tools/NewScripts/QA|ImmersiveGames/NewScripts/Config" Modules Editor -g "*.cs"
Result: 0 matches
```

### Leak sweep runtime
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/QA/**" -g "!**/Legacy/**"
./Core/Logging/DebugUtility.cs:62 [RuntimeInitializeOnLoadMethod]
./Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61 [RuntimeInitializeOnLoadMethod]
Result: allowlist only; no UnityEditor leak outside Dev/Editor/QA/Legacy
```

### Gates
```text
PASS Gate A
PASS Gate A2
PASS Gate B
```

### Scope confirmation
```text
No file under Assets/_ImmersiveGames/Scripts/** was touched.
Touched paths are limited to Modules/**/Dev/**, Modules/**/Editor/**, Editor/**, and Docs/**.
```
