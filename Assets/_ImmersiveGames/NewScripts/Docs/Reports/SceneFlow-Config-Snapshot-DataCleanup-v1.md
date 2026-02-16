# SceneFlow Config Snapshot — DataCleanup v1

- **Data:** 2026-02-16
- **Objetivo:** congelar snapshot canônico antes do DataCleanup v1.

## Assets canônicos

| Nome | Path |
|---|---|
| GameNavigationIntentCatalog.asset | `Assets/Resources/GameNavigationIntentCatalog.asset` |
| GameNavigationCatalog.asset | `Assets/Resources/Navigation/GameNavigationCatalog.asset` |
| SceneRouteCatalog.asset | `Assets/Resources/SceneFlow/SceneRouteCatalog.asset` |
| TransitionStyleCatalog.asset | `Assets/Resources/Navigation/TransitionStyleCatalog.asset` *(path real no repo)* |
| SceneTransitionProfileCatalog.asset | `Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset` |
| DefaultTransitionProfile.asset | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` *(path real no repo)* |
| LevelCatalog.asset | `Assets/Resources/Navigation/LevelCatalog.asset` *(path real no repo)* |
| RuntimeModeConfig.asset | `Assets/Resources/RuntimeModeConfig.asset` |
| NewScriptsBootstrapConfig.asset | `Assets/Resources/NewScriptsBootstrapConfig.asset` |

## Core mandatory intents

| Intent | routeRef | styleId |
|---|---|---|
| to-menu | `Assets/Resources/Navigation/Route_to-menu.asset` | `style.frontend` |
| to-gameplay | `Assets/Resources/Navigation/Route_to-gameplay.asset` | `style.gameplay` |

## Extras (não-mandatórios)

| Intent | routeRef | styleId |
|---|---|---|
| gameover | `Assets/Resources/Navigation/Route_to-menu.asset` | `style.frontend` |
| victory | `Assets/Resources/Navigation/Route_to-menu.asset` | `style.frontend` |
| defeat | `Assets/Resources/Navigation/Route_to-menu.asset` | `style.frontend` |
| restart | `Assets/Resources/Navigation/Route_to-gameplay.asset` | `style.gameplay` |
| exit-to-menu | `Assets/Resources/Navigation/Route_to-menu.asset` | `style.frontend` |

## Routes (routeDefinitions)

Fonte: `Assets/Resources/SceneFlow/SceneRouteCatalog.asset` -> `routeDefinitions`.

| RouteId | RouteKind | RequiresWorldReset | TargetActiveScene | ScenesToLoad | ScenesToUnload | ActiveScene |
|---|---|---:|---|---|---|---|
| to-menu | Frontend | false | MenuScene | UIGlobalScene, MenuScene | NewBootstrap, GameplayScene | MenuScene |
| to-gameplay | Gameplay | true | GameplayScene | UIGlobalScene, GameplayScene | NewBootstrap, MenuScene | GameplayScene |
| level.1 | Gameplay | true | GameplayScene | GameplayScene, UIGlobalScene | NewBootstrap, MenuScene | GameplayScene |
| level.2 | Gameplay | true | GameplayScene | GameplayScene, UIGlobalScene | NewBootstrap, MenuScene | GameplayScene |

## Transition Styles

Fonte: `Assets/Resources/Navigation/TransitionStyleCatalog.asset`.

| StyleId | useFade | profileRef/profileId usado | observações |
|---|---:|---|---|
| style.startup | true | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` / `startup` | padrão fade |
| style.frontend | true | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` / `frontend` | padrão frontend |
| style.gameplay | true | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` / `gameplay` | padrão gameplay |
| style.frontend.nofade | false | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` / `frontend` | nofade |
| style.gameplay.nofade | false | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` / `gameplay` | nofade |

## Profiles

Fonte: `Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset`.

| ProfileId | AssetRef |
|---|---|
| gameplay | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` |
| startup | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` |
| frontend | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` |

## Observações

- Snapshot desta etapa não introduz mudanças funcionais; serve como baseline de configuração para DataCleanup v1.
- Se algum asset acima não existir em revisões futuras, registrar explicitamente como `NOT FOUND` antes de qualquer migração.
