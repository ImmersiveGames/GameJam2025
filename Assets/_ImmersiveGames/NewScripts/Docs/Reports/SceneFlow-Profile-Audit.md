# SceneFlow Profile Audit
- Timestamp (UTC): 2025-12-31T19:48:57.2734250Z
- Scan root: `Assets/_ImmersiveGames/NewScripts`
- Canonical production profiles (tool config): `frontend, gameplay, startup`
- QA profile prefix (tool config): `qa.`
- Resources heuristic enabled: `True`

## Resources profile assets (heurística)
- (nenhum asset encontrado via `AssetDatabase.FindAssets("profile")` dentro de Resources)

### Canonical profiles missing in Resources (heurística)
- `frontend`
- `gameplay`
- `startup`

> Observação: esta checagem é heurística por nome do asset. Se seus profiles não são assets em Resources, ignore este bloco (ou desligue a heurística).

## Code scan summary
- SceneTransitionRequest occurrences: `3`
- SceneTransitionContext direct occurrences: `2`
- SceneFlowProfileNames usages: `10`
- Profile-like string literals captured: `1`

## Unique profile literals found
- `qa.player_move_leak_reset` (qa)

### Unexpected non-canonical profile literals (não QA)
- (nenhum detectado)

## Detail: Profile literal occurrences (por arquivo/linha)
- `qa.player_move_leak_reset` in **SceneTransitionContext** at `Assets/_ImmersiveGames/NewScripts\Infrastructure\QA\PlayerMovementLeakSmokeBootstrap.cs:458:31`

## Detail: SceneTransitionRequest blocks
- `Assets/_ImmersiveGames/NewScripts\Infrastructure\GlobalBootstrap.cs:435:29`
  - snippet: `new SceneTransitionRequest( scenesToLoad: new[] { SceneMenu, SceneUIGlobal }, scenesToUnload: new[] { SceneNewBootstrap }, targetActiveScene: SceneMenu, useFade: true, transitionProfileName: StartProfileName);`
- `Assets/_ImmersiveGames/NewScripts\Infrastructure\Navigation\GameNavigationService.cs:63:20`
  - snippet: `new SceneTransitionRequest( scenesToLoad: scenesToLoad, scenesToUnload: scenesToUnload, targetActiveScene: SceneGameplay, useFade: true, transitionProfileName: SceneFlowProfileNames.Gameplay);`
- `Assets/_ImmersiveGames/NewScripts\Infrastructure\Navigation\GameNavigationService.cs:79:20`
  - snippet: `new SceneTransitionRequest( scenesToLoad: scenesToLoad, scenesToUnload: scenesToUnload, targetActiveScene: SceneMenu, useFade: true, transitionProfileName: SceneFlowProfileNames.Frontend);`

## Detail: SceneTransitionContext blocks (criação direta)
- `Assets/_ImmersiveGames/NewScripts\Infrastructure\QA\PlayerMovementLeakSmokeBootstrap.cs:458:31`
  - string literals: `"qa.player_move_leak_reset"`
  - snippet: `new SceneTransitionContext(new[] { scene }, Array.Empty<string>(), scene, false, "qa.player_move_leak_reset");`
- `Assets/_ImmersiveGames/NewScripts\Infrastructure\Scene\SceneTransitionService.cs:202:20`
  - snippet: `new SceneTransitionContext(loadList, unloadList, request.TargetActiveScene, request.UseFade, request.TransitionProfileName);`

## Detail: SceneFlowProfileNames.* usages
- `SceneFlowProfileNames.IsGameplay` at `Assets/_ImmersiveGames/NewScripts\Gameplay\GameLoop\GameLoopSceneFlowCoordinator.cs:233:20`
- `SceneFlowProfileNames.Startup` at `Assets/_ImmersiveGames/NewScripts\Infrastructure\GlobalBootstrap.cs:49:49`
- `SceneFlowProfileNames.Gameplay` at `Assets/_ImmersiveGames/NewScripts\Infrastructure\InputSystems\InputModeSceneFlowBridge.cs:53:40`
- `SceneFlowProfileNames.Startup` at `Assets/_ImmersiveGames/NewScripts\Infrastructure\InputSystems\InputModeSceneFlowBridge.cs:95:40`
- `SceneFlowProfileNames.Frontend` at `Assets/_ImmersiveGames/NewScripts\Infrastructure\InputSystems\InputModeSceneFlowBridge.cs:96:43`
- `SceneFlowProfileNames.Gameplay` at `Assets/_ImmersiveGames/NewScripts\Infrastructure\Navigation\GameNavigationService.cs:68:40`
- `SceneFlowProfileNames.Frontend` at `Assets/_ImmersiveGames/NewScripts\Infrastructure\Navigation\GameNavigationService.cs:84:40`
- `SceneFlowProfileNames.Normalize` at `Assets/_ImmersiveGames/NewScripts\Infrastructure\SceneFlow\NewScriptsSceneFlowAdapters.cs:153:23`
- `SceneFlowProfileNames.Normalize` at `Assets/_ImmersiveGames/NewScripts\Infrastructure\SceneFlow\SceneFlowProfilePaths.cs:13:22`
- `SceneFlowProfileNames.IsStartupOrFrontend` at `Assets/_ImmersiveGames/NewScripts\Infrastructure\WorldLifecycle\Runtime\WorldLifecycleRuntimeCoordinator.cs:54:33`
