# DataCleanup v1 — Step 03 (Inline Routes Removal)

## What changed

- `SceneRouteCatalogAsset` no longer accepts inline routes from `routes[]`.
- Cache build now uses only `routeDefinitions` (`SceneRouteDefinitionAsset` references).
- If `routes[]` contains any item, the system now raises **FATAL** in both Editor validation path (`OnValidate`) and cache/runtime usage path (`EnsureCache`).

## Why

- DataCleanup v1 removes legacy fallback behavior to prevent ambiguous configuration sources.
- A single canonical source (`routeDefinitions`) improves determinism, maintainability, and fail-fast diagnostics.

## How to validate

1) Check code references and enforcement:

```bash
rg -n "routes\b" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs
```

2) Manual validation in Unity Editor:

- Run menu item: `ImmersiveGames/NewScripts/Config/Validate SceneFlow Config (DataCleanup v1)`.
- Confirm report is written to:
  - `Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- Confirm behavior:
  - `routes[]` empty => PASS
  - `routes[]` with at least 1 item => FAIL + exception (fail-fast)
