# NAVIGATION-INTENTCATALOG-SIMPLIFY

Date: 2026-03-11

## Scope

- Simplify `GameNavigationIntentCatalogAsset` to its canonical minimum role.
- Preserve runtime behavior.
- Keep `GameNavigationCatalogAsset` as the only owner of `routeRef` and `styleId`.
- Do not structurally change `SceneTransitionProfileCatalogAsset` in this phase.

## Changes applied

- Removed parallel serialized shape from `GameNavigationIntentCatalogAsset`:
  - `routeRef`
  - `styleId`
  - `criticalRequired`
- Kept `GameNavigationIntentCatalogAsset` as owner of:
  - canonical core intent ids
  - official aliases (`defeat`, etc.)
  - editor id source for `NavigationIntentId`
- Updated `GameNavigationCatalogNormalizer`:
  - intent catalog normalization now ensures ids only
  - navigation catalog defaults now come from `SceneRouteCatalogAsset` plus canonical style constants
- Updated `SceneFlowConfigValidator`:
  - intent catalog validation now checks only canonical intent presence
  - route/style ownership remains validated on `GameNavigationCatalogAsset`
- Updated ADR-0019 to match the current owner split.

## Resulting ownership

- `GameNavigationIntentCatalogAsset`
  - owner of canonical navigation ids/aliases
  - not owner of route/style anymore
- `GameNavigationCatalogAsset`
  - owner of navigation `routeRef` and `styleId`
- `SceneRouteCatalogAsset`
  - source of canonical route assets used by the normalizer

## Runtime impact

- No intended runtime behavior change.
- `GameNavigationService` still depends on `GameNavigationIntentCatalogAsset` for core intent ids.
- Runtime route/style resolution remains in `GameNavigationCatalogAsset` + `TransitionStyleCatalogAsset`.

## Residual risks / H11 follow-up

- Existing serialized intent-catalog assets still need a Unity reserialize cycle to fully drop orphaned legacy fields from YAML.
- `SceneTransitionProfileCatalogAsset` still duplicates part of the style/profile mapping and remains for a later migration.
- Some docs still describe `GameNavigationIntentCatalogAsset` as residual/editor-serialized; that remains directionally true, but the remaining role is now narrower and explicit.
