# BATCH-CLEANUP-STD-4

Date: 2026-03-10
Source of truth: workspace local (`C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts`)

## Goal
Freeze and revalidate the BootstrapConfig decouple + `LevelCatalogAsset` removal without introducing any new runtime change.

## Discovery
- File inspected: `Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs`
- Serialized field name: `levelCatalog`
- Inspector visibility: `[SerializeField, HideInInspector]`
- Current field type: `UnityEngine.Object`
- Read path inside file: only `OnValidate()` for legacy observability
- Runtime read path outside this file: none found

## Current BootstrapConfig state
```text
rg -n "\blevelCatalog\b" Assets/_ImmersiveGames/NewScripts -g "*.cs"
.\Infrastructure\Config\NewScriptsBootstrapConfigAsset.cs:22:        [SerializeField, HideInInspector] private UnityEngine.Object levelCatalog;
.\Infrastructure\Config\NewScriptsBootstrapConfigAsset.cs:40:            if (!_legacyFieldsWarned && (levelCatalog != null || startGameplayLevelId.IsValid))
.\Infrastructure\Config\NewScriptsBootstrapConfigAsset.cs:44:                    $"[OBS][LEGACY] Bootstrap legacy fields are ignored in canonical flow. asset='{name}' hasLevelCatalog='{(levelCatalog != null)}' hasStartGameplayLevelId='{startGameplayLevelId.IsValid}'.",
```

## Proof before delete / current state
```text
rg -n -w "LevelCatalogAsset" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
0 matches
```

```text
rg -n "7cdacc728aec81746b38bd96d0b26ae3" Assets/_ImmersiveGames/NewScripts -g "*.unity" -g "*.prefab" -g "*.asset" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/QA/**"
0 matches
```

## Post-check
```text
rg -n -w "LevelCatalogAsset" Assets/_ImmersiveGames/NewScripts -g "*.cs"
0 matches
```

```text
Tools/Gates/Run-NewScripts-RgGates.ps1
PASS Gate A
PASS Gate A2
PASS Gate B
```

## Outcome
- No new code change was required beyond the already-applied local cleanup state.
- `NewScriptsBootstrapConfigAsset` remains decoupled from the removed legacy type.
- The serialized field keeps the same name and uses a `UnityEngine.Object` placeholder, with no `#if` on the field.
- Canonical runtime behavior remains unchanged.
