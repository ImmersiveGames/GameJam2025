# SceneTransitionProfileCatalog Removal

Date: 2026-03-12
Status: applied

## Scope

- Remove `SceneTransitionProfileCatalogAsset` as a structural dependency of runtime/bootstrap.
- Preserve current runtime behavior.
- Keep `TransitionStyleCatalogAsset` as the canonical style binding.

## What changed

- Boot/startup profile now resolves from `NewScriptsBootstrapConfigAsset.startupTransitionProfile`.
- Compatibility fallback remains in `GlobalCompositionRoot.Coordinator` via `TransitionStyleCatalogAsset` using `style.startup` when the direct field is empty.
- `GlobalCompositionRoot.SceneFlow` no longer registers a profile catalog service.
- `SceneFlowConfigValidator` no longer loads or validates `SceneTransitionProfileCatalogAsset`.
- `SceneFlowProfileIdPropertyDrawer` now sources ids from canonical values plus `TransitionStyleCatalogAsset` only.
- `TransitionStyleCatalogAsset` no longer serializes the legacy `transitionProfileCatalog` field.
- `SceneTransitionProfileCatalogAsset` class/file was removed.

## Runtime effect

- `SceneTransitionService` remains unchanged and still consumes direct `SceneTransitionProfile` references.
- Boot `startPlan` continues to use `SceneFlowProfileId.Startup` and `UseFade=true`.
- No runtime path resolves `profileId -> profileRef` through a standalone catalog anymore.

## Remaining ownership

- `NewScriptsBootstrapConfigAsset`: direct startup profile for boot.
- `TransitionStyleCatalogAsset`: canonical `styleId -> profileRef/profileId/useFade`.
- `SceneTransitionProfile`: fade parameters.

## Risks / follow-up

- Existing bootstrap assets should be reserialized to populate `startupTransitionProfile` explicitly and remove legacy orphaned YAML data.
- While fallback to `style.startup` preserves compatibility, H11 should decide whether to keep or remove that fallback after asset migration.
- Unity compilation/playmode was not executed in this session; verification was static by code/reference inspection.
