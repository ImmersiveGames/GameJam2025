# EDITOR-QA-1

## Summary
- Audit date dir: `2026-03-06`
- Editable scope executed: `Editor/**`, `Dev/**`, `Docs/**`
- `QA/**` at top level does not exist in this workspace
- No safe `.cs` prune or move was available inside the allowed edit scope
- Canonical menu normalization was a no-op in the allowed edit scope because no editable file exposes `[MenuItem]`

## Inventory
| FilePath | Kind | MenuItemPaths | UsesUnityEditor | RuntimeTouch | DependsOnRemovedTypes | Recommendation |
|---|---|---|---|---|---|---|
| `Editor/Core/Events/EventBusUtil.Editor.cs` | `EditorTool` | `-` | `true` | `false` | `false` | Keep in place; already canonical under `Editor/**` |
| `Editor/Core/Logging/HardFailFastH1.Editor.cs` | `EditorTool` | `-` | `true` | `false` | `false` | Keep in place; already canonical under `Editor/**` |
| `Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs` | `EditorTool` | `-` | `true` | `false` | `false` | Keep in place; already canonical under `Editor/**` |

## Required command evidence
### MenuItems
```text
rg -n "\[MenuItem\(" Editor -g "*.cs"
Result: 0 matches

QA top-level directory:
Test-Path QA
Result: False
```

### UnityEditor usage
```text
rg -n "using\s+UnityEditor;|UnityEditor\." . -g "*.cs"
Findings in editable scope:
- Editor/Core/Events/EventBusUtil.Editor.cs
- Editor/Core/Logging/HardFailFastH1.Editor.cs
- Editor/Infrastructure/Composition/GlobalCompositionRoot.Editor.cs

Additional out-of-scope findings observed under `Modules/**` were not modified in this pass.
```

### Removed/obsolete type probe
```text
rg -n "LevelCatalogAsset|ILevelFlowService|ILevelMacroRouteCatalog|ILevelContentResolver" Editor Dev -g "*.cs"
Result: 0 matches in editable scope
```

## Layout normalization
- No move was needed inside `Editor/**`, `QA/**`, or top-level `Dev/**`.
- `QA/**` top-level is absent, so there was nothing to relocate into `Editor/QA/**`.
- Blocked by strict scope: several `UnityEditor`-bearing files still live under `Modules/**/Dev` and `Modules/**/Editor`, but those paths were explicitly out of scope for editing in this run.

## Menu normalization
- No editable file in scope defines `[MenuItem]`, so no menu path rewrite was applied.
- Recommended canonical target remains `ImmersiveGames/NewScripts/QA/<Module>/<Action>` for a future pass that includes the out-of-scope editor/dev files under `Modules/**`.

## Moves
- None.

## Deletes
- None.
- No file met the prune criteria inside the allowed edit scope.

## Archives
- None.

## Out-of-scope findings
- `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs`
- `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevMenuItems.cs`
- `Modules/Navigation/Dev/Editor/GameNavigationCatalogNormalizer.cs`
- `Modules/SceneFlow/Editor/**`
- Multiple `Modules/**/Dev/*.DevQA.cs`

These remain candidates for a follow-up pass, but editing them would violate the strict scope given for this run.

## Post-checks
### Leak sweep outside `Dev/Editor/QA/Legacy`
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

### Scope check
```text
Touched files:
- Docs/Reports/Audits/2026-03-06/Modules/EDITOR-QA-1.md
- Docs/Reports/Audits/2026-03-06/Audit-Index.md
- Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md

No file under Assets/_ImmersiveGames/Scripts/** was touched.
No file outside Editor/**, Dev/**, QA/**, Docs/** was modified.
```

## Conclusion
- Net-negative `.cs` was not achievable inside the allowed edit scope because there were no editable obsolete tools to prune.
- Baseline-preserving result: no code changes, documentation updated, required checks passed, and out-of-scope editor/dev debt was documented for follow-up.
