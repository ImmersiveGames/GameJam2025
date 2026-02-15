# SceneFlow Config Snapshot — DataCleanup v1

## Metadados do snapshot
- Data (UTC): 2026-02-15T19:11:05+00:00
- Commit base: `f6873a78caf94626fedfdeacd78c545209f1ce2f` (`f6873a78`)
- Escopo:
  - `Assets/Resources/NewScriptsBootstrapConfig.asset`
  - `Assets/Resources/Navigation/GameNavigationCatalog.asset`
  - `Assets/Resources/SceneFlow/SceneRouteCatalog.asset`
  - `Assets/Resources/Navigation/TransitionStyleCatalog.asset`
  - `Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset`
  - RouteDefinition assets e SceneKey assets referenciados pelos catálogos acima

---

## 1) Intents do `GameNavigationCatalogAsset`

Fonte: `Assets/Resources/Navigation/GameNavigationCatalog.asset`.

| intentId | route asset (ref) | route asset `routeId` interno | styleId | profileId (via style) | profile asset (via style) |
|---|---|---|---|---|---|
| `to-menu` | `Assets/Resources/Navigation/Route_to-gameplay.asset` | `to-gameplay` | `style.frontend` | `frontend` | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` |
| `to-gameplay` | `Assets/Resources/Navigation/Route_to-menu.asset` | `to-menu` | `style.gameplay` | `gameplay` | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` |

> Observação de evidência: a tabela acima congela exatamente o wiring atual por `routeRef` do catálogo, sem interpretação de regra de negócio.

---

## 2) Rotas resolvidas no `SceneRouteCatalogAsset`

Fonte: `Assets/Resources/SceneFlow/SceneRouteCatalog.asset` (`routeDefinitions`, `routes=[]`).

| routeId | RouteDefinitionAsset | scenesToLoad | scenesToUnload | activeScene |
|---|---|---|---|---|
| `to-gameplay` | `Assets/Resources/Navigation/Route_to-gameplay.asset` | `UIGlobalScene`, `GameplayScene` | `NewBootstrap`, `MenuScene` | `GameplayScene` |
| `to-menu` | `Assets/Resources/Navigation/Route_to-menu.asset` | `UIGlobalScene`, `MenuScene` | `NewBootstrap`, `GameplayScene` | `MenuScene` |
| `level.1` | `Assets/Resources/Navigation/Route_level.1.asset` | `GameplayScene`, `UIGlobalScene` | `NewBootstrap`, `MenuScene` | `GameplayScene` |
| `level.2` | `Assets/Resources/Navigation/Route_level.2.asset` | `GameplayScene`, `UIGlobalScene` | `NewBootstrap`, `MenuScene` | `GameplayScene` |

---

## 3) Estilos no `TransitionStyleCatalogAsset`

Fonte: `Assets/Resources/Navigation/TransitionStyleCatalog.asset`.

| styleId | transitionProfile asset | profileId | useFade |
|---|---|---|---|
| `style.startup` | `DefaultTransitionProfile` (`Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset`) | `startup` | `true` |
| `style.frontend` | `DefaultTransitionProfile` (`Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset`) | `frontend` | `true` |
| `style.gameplay` | `DefaultTransitionProfile` (`Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset`) | `gameplay` | `true` |
| `style.frontend.nofade` | `DefaultTransitionProfile` (`Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset`) | `frontend` | `false` |
| `style.gameplay.nofade` | `DefaultTransitionProfile` (`Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset`) | `gameplay` | `false` |

---

## 4) Profiles no `SceneTransitionProfileCatalogAsset`

Fonte: `Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset`.

| profileId | profile asset |
|---|---|
| `gameplay` | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` |
| `startup` | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` |
| `frontend` | `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` |

---

## 5) Assets & GUIDs (consolidado)

| assetPath | assetName | GUID |
|---|---|---|
| `Assets/Resources/NewScriptsBootstrapConfig.asset` | `NewScriptsBootstrapConfig` | `b3f866decfd85714db622d024b341b0a` |
| `Assets/Resources/Navigation/GameNavigationCatalog.asset` | `GameNavigationCatalog` | `199efb62d31d34e4f8862069f922a7a3` |
| `Assets/Resources/Navigation/TransitionStyleCatalog.asset` | `TransitionStyleCatalog` | `de91983d669cc0641940bf2c0ae602ab` |
| `Assets/Resources/SceneFlow/SceneRouteCatalog.asset` | `SceneRouteCatalog` | `8f3d4c84c7f482241a6978dbc890c1cf` |
| `Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset` | `SceneTransitionProfileCatalog` | `7da079f7b1949914d81899fd4cec6813` |
| `Assets/Resources/Navigation/Route_to-gameplay.asset` | `Route_to-gameplay` | `26fcd85590868ae46a33dd769e1145fc` |
| `Assets/Resources/Navigation/Route_to-menu.asset` | `Route_to-menu` | `6537287d00db3e745bed3369507fad91` |
| `Assets/Resources/Navigation/Route_level.1.asset` | `Route_level.1` | `0b4f973d6e7141159c02b5bb2ecd8364` |
| `Assets/Resources/Navigation/Route_level.2.asset` | `Route_level.2` | `26626e420f9344b2b95d9ccacac64d20` |
| `Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset` | `DefaultTransitionProfile` | `384a994125a175d498624ae0c2407d77` |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneKeys/UIGlobalScene.asset` | `UIGlobalScene` | `c779a1ffa44042768a83865fefeb5893` |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneKeys/GameplayScene.asset` | `GameplayScene` | `3a529321c385413f91f700ea60a5faba` |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneKeys/MenuScene.asset` | `MenuScene` | `9f596f1f913249fda9e8d36a85bd1302` |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneKeys/NewBootstrap.asset` | `NewBootstrap` | `f9cac371e22e64c428a4ad390b70ebc7` |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneKeys/FadeScene.asset` | `FadeScene` | `7983f5c5a711411887b7d73dd12c6702` |

---

## 6) Observações
- Runtime de SceneFlow/Navigation já opera em modo **direct-ref-first** (especialmente para `routeRef` e `transitionProfile`), com IDs tipados para lookup/observabilidade/compatibilidade.
- Este snapshot é um **guardrail de DataCleanup** para comparação futura sem alterar comportamento.
- Snapshot produzido em **Etapa 0 (zero risco)**: somente evidência de configuração + âncora de observabilidade.
