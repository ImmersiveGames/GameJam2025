# Navigation Intent Catalog Removal

Date: 2026-03-12
Status: applied

## Scope

- Remove `GameNavigationIntentCatalogAsset` as a structural dependency of runtime and bootstrap.
- Preserve runtime behavior.
- Keep `GameNavigationCatalogAsset` as the single canonical navigation asset.
- Keep `TransitionStyleCatalogAsset` unchanged as the style owner.

## What changed

- Introduced `Modules/Navigation/GameNavigationIntents.cs` as the canonical code source for core ids and official aliases.
- `GameNavigationService` now resolves `GameNavigationIntentKind -> NavigationIntentId` via code.
- `GameNavigationCatalogAsset` no longer references or depends on an intent catalog asset.
- `NewScriptsBootstrapConfigAsset` no longer serializes or validates `navigationIntentCatalog`.
- `GlobalCompositionRoot.Navigation` no longer requires or injects a navigation intent catalog.
- `NavigationIntentIdPropertyDrawer` now sources ids from code plus `GameNavigationCatalogAsset.routes`.
- `GameNavigationCatalogNormalizer` now normalizes only the navigation catalog.
- `SceneFlowConfigValidator` now validates mandatory navigation intents from code and material navigation data from the canonical assets.
- `GameNavigationIntentCatalogAsset` and its DevQA partial were removed.

## Resulting architecture

- `GameNavigationCatalogAsset`
  - owner of navigation `routeRef + styleId`
  - owner of extra/custom material intents via `routes`
- `GameNavigationIntents.cs`
  - owner of core ids and official aliases (`defeat`, etc.)
- `TransitionStyleCatalogAsset`
  - owner of `styleId -> profileRef/profileId/useFade`

## Runtime impact

- No intended behavior change.
- `defeat` remains an official alias resolved to the gameplay core path.
- Custom/extras continue to live in `GameNavigationCatalogAsset.routes`; no parallel custom-intent block remains.

## Risks / follow-up

- Existing bootstrap and navigation assets should be reserialized in Unity to remove orphaned YAML from removed serialized fields.
- Historical docs still mention `GameNavigationIntentCatalogAsset` as residual/editor-only; they should be cleaned up opportunistically if they are used as current guidance.
- Unity compilation/playmode was not executed in this session; verification was static by code/reference inspection.
