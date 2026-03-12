# Transition Direct-Ref Fail-Fast Purge

Date: 2026-03-12
Scope: `Assets/_ImmersiveGames/NewScripts/**` + canonical `Assets/Resources/**` config assets

## Goal

Finish the Navigation/Transition migration by removing every remaining fallback and leaving a single valid direct-reference configuration model.

## Backfill applied

- Created canonical `TransitionStyleAsset` assets in `Assets/Resources/SceneFlow/Styles/`:
  - `TransitionStyle_Startup`
  - `TransitionStyle_Frontend`
  - `TransitionStyle_Gameplay`
  - `TransitionStyle_FrontendNoFade`
  - `TransitionStyle_GameplayNoFade`
- Backfilled `Assets/Resources/Navigation/GameNavigationCatalog.asset` so every core slot now uses `transitionStyleRef` directly.
- Backfilled `Assets/Resources/NewScriptsBootstrapConfig.asset` so startup now resolves only from `startupTransitionStyleRef`.

## Fallbacks removed

- `GameNavigationService` no longer resolves styles through `ITransitionStyleCatalog`.
- `GameNavigationCatalogAsset` no longer accepts `styleId` as an alternative to `transitionStyleRef`.
- `NewScriptsBootstrapConfigAsset` no longer accepts `startupTransitionProfile`.
- `GlobalCompositionRoot.Coordinator` no longer falls back to `style.startup`.
- Editor validation and drawers no longer use `TransitionStyleCatalogAsset` as a source.

## Removed legacy pieces

- `TransitionStyleCatalogAsset` runtime/editor code removed from `NewScripts`.
- Canonical `Assets/Resources/SceneFlow/TransitionStyleCatalog.asset` removed.
- `ITransitionStyleCatalog` removed.

## Final contract

- `GameNavigationCatalogAsset` resolves navigation only by `routeRef + transitionStyleRef`.
- `TransitionStyleAsset` resolves transition only by `profileRef + useFade`.
- `startupTransitionStyleRef` is the only valid startup transition source.
- `styleId` and `profileId` remain semantic/observability fields only.
