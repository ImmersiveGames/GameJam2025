# TransitionStyle Direct-Ref Refactor

Date: 2026-03-12
Scope: `Assets/_ImmersiveGames/NewScripts/**`

## Goal

Refactor Navigation/Transition to be direct-ref-first, using `TransitionStyleAsset` as the primary style owner and keeping `TransitionStyleCatalogAsset` only as a short legacy fallback.

## Applied changes

- Introduced `TransitionStyleAsset` as the canonical direct-reference style asset.
- `GameNavigationCatalogAsset` core slots and extra routes now prefer `transitionStyleRef` directly.
- `styleId` remains serialized only as fallback/observability during migration.
- `GameNavigationService` now resolves style by `TransitionStyleAsset` first and only consults `ITransitionStyleCatalog` as a legacy fallback.
- Bootstrap startup transition now prefers `startupTransitionStyleRef`, then `startupTransitionProfile`, and only then legacy `style.startup` from `TransitionStyleCatalogAsset`.
- Editor drawers and validators now source style/profile ids from `TransitionStyleAsset` first, with catalog fallback only when still present.

## Resulting ownership

- `GameNavigationCatalogAsset`: owner of `routeRef + transitionStyleRef`.
- `TransitionStyleAsset`: owner of `profileRef + useFade`.
- `TransitionStyleCatalogAsset`: fallback/editor migration support only.
- `SceneTransitionProfile`: leaf visual profile asset.

## Notes

- Runtime behavior is preserved by keeping legacy fallback through `TransitionStyleCatalogAsset` when direct refs are still absent in serialized assets.
- `StyleId` and `ProfileId` remain available for semantics, logging, signatures, and migration compatibility, but no longer act as the primary structural lookup.
- A Unity reserialize pass is still recommended so existing assets populate `transitionStyleRef` / `startupTransitionStyleRef` explicitly and stop depending on fallback data.
