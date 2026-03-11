# BATCH-CLEANUP-STD-3

Date: 2026-03-10
Source of truth: workspace local (`C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`)

## Goal
Prune dead legacy LevelFlow interfaces with proof-first cleanup, keeping `LevelCatalogAsset` because it still has a compile reference outside `Legacy/**`.

## Final decision table

| FilePath | TypeName | MetaGuid | RuntimeCallsitesOutsideLegacyDevEditorQA | AssetRefsOutsideEditorDevQA | Decision | Reason |
|---|---|---|---:|---:|---|---|
| `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs` | `LevelCatalogAsset` | `7cdacc728aec81746b38bd96d0b26ae3` | 1 | 0 | KEEP | Referenced by `Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs`. |
| `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs` | `ILevelFlowService` | `021e0027743bdc5499f5318220b8dd7c` | 0 | 0 | DELETE | No runtime callsites outside excluded scopes; no asset refs; only remaining dependency was `LevelCatalogAsset` implementing it. |
| `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs` | `ILevelMacroRouteCatalog` | `3be1c1a52fd36bd45b996fea4d2c5f04` | 0 | 0 | DELETE | Same proof as above. |
| `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs` | `ILevelContentResolver` | `5eb572e0700f4d6f822620626d4ac869` | 0 | 0 | DELETE | Same proof as above. |

## Changes applied

### Deleted
- `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs.meta`
- `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs.meta`
- `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs.meta`

### Kept
- `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs`
- `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs.meta`

### Minimal refactor
- `LevelCatalogAsset` no longer implements the deleted legacy interfaces.
- No method signatures or runtime behavior were changed.

## Evidence / rg

### Pre-delete proof collected from local workspace
```text
rg -n "LevelCatalogAsset" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
.\Infrastructure\Config\NewScriptsBootstrapConfigAsset.cs:21:        [SerializeField, HideInInspector] private LevelCatalogAsset levelCatalog;
```

```text
rg -n "ILevelFlowService" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "ILevelMacroRouteCatalog" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "ILevelContentResolver" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "7cdacc728aec81746b38bd96d0b26ae3" . -g "*.unity" -g "*.prefab" -g "*.asset" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "021e0027743bdc5499f5318220b8dd7c" . -g "*.unity" -g "*.prefab" -g "*.asset" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "3be1c1a52fd36bd45b996fea4d2c5f04" . -g "*.unity" -g "*.prefab" -g "*.asset" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "5eb572e0700f4d6f822620626d4ac869" . -g "*.unity" -g "*.prefab" -g "*.asset" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**"
0 matches
```

### Post-apply proof
```text
rg -n "ILevelFlowService|ILevelMacroRouteCatalog|ILevelContentResolver" . -g "*.cs"
0 matches
```

```text
rg -n "LevelCatalogAsset" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
.\Infrastructure\Config\NewScriptsBootstrapConfigAsset.cs:21:        [SerializeField, HideInInspector] private LevelCatalogAsset levelCatalog;
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
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

```text
Tools/Gates/Run-NewScripts-RgGates.ps1
PASS Gate A
PASS Gate A2
PASS Gate B
```

## Outcome
- Behavior-preserving.
- No pipeline order changes.
- No event payload or public contract changes.
- No new shims/no-ops created.
