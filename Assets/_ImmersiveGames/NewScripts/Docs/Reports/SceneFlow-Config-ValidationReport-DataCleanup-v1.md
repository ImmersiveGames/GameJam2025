# SceneFlow Config Validation Report (DataCleanup v1)

- Timestamp: 2026-02-17 18:52:03 UTC
- Unity version: 6000.0.67f1

## Assets canônicos

| Path | Status |
|---|---|
| `Assets/Resources/GameNavigationIntentCatalog.asset` | OK |
| `Assets/Resources/Navigation/GameNavigationCatalog.asset` | OK |
| `Assets/Resources/SceneFlow/SceneRouteCatalog.asset` | OK |
| `Assets/Resources/Navigation/TransitionStyleCatalog.asset` | OK |
| `Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset` | OK |
| `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` | OK |
| `Assets/Resources/Navigation/LevelCatalog.asset` | OK |
| `Assets/Resources/RuntimeModeConfig.asset` | OK |
| `Assets/Resources/NewScriptsBootstrapConfig.asset` | OK |

## Core mandatory intents

| intentId | routeRef | routeId | status |
|---|---|---|---|
| `to-menu` | `Route_to-menu` | `to-menu` | OK |
| `to-gameplay` | `Route_to-gameplay` | `to-gameplay` | OK |

## Core slots

| slot | routeId | styleId | status |
|---|---|---|---|
| `menu` | `to-menu` | `style.frontend` | OK |
| `gameplay` | `to-gameplay` | `style.gameplay` | OK |

## Transition styles

| styleId | useFade | profileId | profileRef | status |
|---|---|---|---|---|
| `style.startup` | `True` | `startup` | `DefaultTransitionProfile` | OK |
| `style.frontend` | `True` | `frontend` | `DefaultTransitionProfile` | OK |
| `style.gameplay` | `True` | `gameplay` | `DefaultTransitionProfile` | OK |
| `style.frontend.nofade` | `False` | `frontend` | `DefaultTransitionProfile` | OK |
| `style.gameplay.nofade` | `False` | `gameplay` | `DefaultTransitionProfile` | OK |

## Problems

### FATAL
- None

### WARN
- None

VERDICT: PASS
