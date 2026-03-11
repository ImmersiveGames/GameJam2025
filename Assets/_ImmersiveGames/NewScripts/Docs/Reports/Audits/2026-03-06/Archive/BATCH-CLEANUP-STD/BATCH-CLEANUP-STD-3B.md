# BATCH-CLEANUP-STD-3B

Date: 2026-03-10
Source of truth: workspace local (`C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`)

## Goal
Remove the BootstrapConfig compile dependency on the legacy level catalog and delete the remaining LevelFlow legacy catalog stack.

## Discovery
- File inspected: `Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs`
- Serialized field name: `levelCatalog`
- Inspector visibility: `[SerializeField, HideInInspector]`
- Read path inside file: only `OnValidate()` logs whether the legacy field is present.
- Runtime read path outside this file: none found.

## Before / proof
```text
rg -n -w "LevelCatalogAsset" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
.
```
Interpreted result before cleanup: one residual compile dependency in `Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs`.

```text
rg -n "7cdacc728aec81746b38bd96d0b26ae3" Assets/_ImmersiveGames/NewScripts -g "*.unity" -g "*.prefab" -g "*.asset" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**"
0 matches
```

## Changes applied
- `Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs`
  - removed direct `LevelCatalogAsset` type dependency
  - kept serialized field name `levelCatalog`
  - changed field type to `UnityEngine.Object`
  - kept legacy placeholder comment in PT-BR
- Deleted legacy stack files:
  - `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs`
  - `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs.meta`
  - `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs` (already absent from local state after STD-3)
  - `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs.meta` (already absent from local state after STD-3)
  - `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs` (already absent from local state after STD-3)
  - `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs.meta` (already absent from local state after STD-3)
  - `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs` (already absent from local state after STD-3)
  - `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs.meta` (already absent from local state after STD-3)
- Collateral editor cleanup to remove residual type references:
  - deleted `Editor/QA/LevelFlow/NTo1/LevelFlowNTo1QaEditor.cs` + `.meta`
  - deleted `Editor/QA/LevelFlow/Compat/ScenarioB/LevelFlowCompatScenarioBEditor.cs` + `.meta`
  - deleted `Editor/Navigation/LevelDefinitionLegacyCleaner.cs` + `.meta`
  - deleted `Editor/QA/LevelFlow/Compat/ScenarioB/Assets/LevelCatalog_CompatScenarioB.asset` + `.meta`
  - removed legacy level-catalog validation slot from `Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs`

## After / proof
```text
rg -n -w "LevelCatalogAsset|ILevelFlowService|ILevelMacroRouteCatalog|ILevelContentResolver" Assets/_ImmersiveGames/NewScripts -g "*.cs"
0 matches
```

```text
rg -n "7cdacc728aec81746b38bd96d0b26ae3" Assets/_ImmersiveGames/NewScripts -g "*.unity" -g "*.prefab" -g "*.asset"
0 matches
```

```text
Tools/Gates/Run-NewScripts-RgGates.ps1
PASS Gate A
PASS Gate A2
PASS Gate B
```

```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

## Outcome
- Behavior-preserving for the canonical runtime rail.
- No pipeline order change.
- No public contract or event payload change.
- BootstrapConfig keeps only a serialization placeholder for old assets, with no runtime use.
