# Hygiene CANON-ONLY Report (2026-03-05)

## Scope
- Applied only under `Assets/_ImmersiveGames/NewScripts/**`.

## A) Dedupe / Canonical compilation path
- Canonical classes confirmed with single implementation in `Modules/**`:
  - `Modules/Navigation/GameNavigationService.cs`
  - `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs`
  - `Modules/LevelFlow/Runtime/LevelSelectedEvent.cs`
  - `Modules/LevelFlow/Runtime/GameplayStartSnapshot.cs`
- Legacy marker strings were removed from active code/docs in scope.

## B) Legacy start entries removed (hard ban)
- Removed from interface + impl:
  - `ILevelFlowRuntimeService.StartGameplayAsync(string levelId, ...)`
  - `IGameNavigationService.StartGameplayAsync(LevelId, ...)`
- Updated call sites to canonical route-only/default:
  - Menu binder uses `StartGameplayDefaultAsync(...)`.
  - QA/Dev gameplay start path updated to `StartGameplayDefaultAsync(...)`.
  - Compat QA editor scenario updated to `StartGameplayDefaultAsync(...)`.

## C) LevelCatalog removal from canonical
- `NewScriptsBootstrapConfigAsset`:
  - `levelCatalog` and `startGameplayLevelId` kept as hidden legacy fields, never exposed/consumed in canonical flow.
  - Added one-time editor log: `[OBS][LEGACY] Bootstrap legacy fields are ignored in canonical flow...`
- `GlobalCompositionRoot.NavigationInputModes`:
  - No requirement/registration of `LevelCatalog` in canonical navigation start path.
  - No `CatalogResolvedVia ... levelCatalog` log.
  - `GameNavigationService` built without `ILevelFlowService/LevelCatalog` dependency.

## D) Assets migration status
- Located target assets (outside `NewScripts/**`):
  - `C:\Projetos\GameJam2025\Assets\Resources\Navigation\Route_to-gameplay.asset`
  - `C:\Projetos\GameJam2025\Assets\Resources\Navigation\Route_to-menu.asset`
  - `C:\Projetos\GameJam2025\Assets\Resources\SceneFlow\LevelCollectionAsset.asset`
- Not edited in this task due explicit scope restriction (`NewScripts/**` only).

## Files changed (key)
- `Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
- `Modules/Navigation/IGameNavigationService.cs`
- `Modules/Navigation/GameNavigationService.cs`
- `Modules/Navigation/Bindings/MenuPlayButtonBinder.cs`
- `Modules/LevelFlow/Runtime/ILevelFlowRuntimeService.cs`
- `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs`
- `Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs`
- `Modules/LevelFlow/Bindings/LevelCatalogAsset.cs`
- `QA/LevelFlow/Compat/ScenarioB/LevelFlowCompatScenarioBEditor.cs`
- `Docs/Reports/Audits/2026-03-05/Hygiene-CanonOnly-Report.md`

## Files removed/moved to Legacy
- None moved/deleted in this pass.
- Legacy usage was blocked/removed from canonical interfaces and call sites.

## Final grep evidence
### Legacy markers (must be 0)
Command:
`rg -n "FailFastMissingLevel|NoLevelSelected|require_explicit_level_or_valid_snapshot|MacroRouteResolvedVia=LevelCatalog" .`

Result:
`0 matches`

### StartGameplayAsync legacy API (must be 0 outside Legacy)
Command:
`rg -n "StartGameplayAsync\(" .`

Result:
`0 matches`

## DoD checklist (code-level)
- [x] No `MenuPlay -> StartGameplayAsync levelId=...` path.
- [x] No `MacroRouteResolvedVia=LevelCatalog` string in active code/docs.
- [x] Present log: `[OBS][Navigation] StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- [x] Present gate log: `MacroLoadingPhase='LevelPrepare'`
- [x] Present prepare log: `LevelDefaultSelected source='catalog_index_0' ...`
- [x] Present apply log: `LevelApplied scenesAdded=... scenesRemoved=...`

## Runtime verification note
- This report validates by source inspection and grep in scope.
- Unity PlayMode execution was not run in this step.
