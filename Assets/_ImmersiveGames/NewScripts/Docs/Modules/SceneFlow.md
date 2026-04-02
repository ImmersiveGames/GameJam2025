# SceneFlow

## Precedencia canonica
- Fonte de verdade operacional: `ADR-0030`, `ADR-0031`, `ADR-0032`, `ADR-0033`, `ADR-0039`.
- Em conflito, esta ordem prevalece sobre baseline historica.

## Estado atual
- `SceneTransitionService` e owner da timeline macro de transicao.
- `SceneRouteDefinitionAsset` e a definicao canonica da rota.
- `SceneTransitionRequest.ResolvedRouteDefinition` e a entrada operacional preferencial.
- `GameNavigationCatalogAsset` resolve `intent -> routeRef + transitionStyleRef`.
- `TransitionStyleAsset` resolve `profileRef + useFade`.
- `LoadingHudScene` e a HUD canonica fazem parte do macro flow.
- `LevelCollection` e `LevelDefinition` nao sao owners daqui; vivem em `Game/Content/Definitions/Levels`.

## Ownership
- `SceneTransitionService`: fases da transicao, sequencing macro e eventos canonicos.
- `SceneRouteDefinitionAsset`: definicao de rota, `RouteKind`, target scene e policy macro de reset.
- `GameNavigationCatalogAsset`: intents canonicos de navegacao.
- `TransitionStyleAsset`: style estrutural da transicao.
- `SceneFlowFadeAdapter`: aplicacao do style no fade.
- `WorldResetCompletionGate` (em `ResetInterop`) + `MacroLevelPrepareCompletionGate`: gate composto entre `ScenesReady` e `BeforeFadeOut`.
- `ILoadingPresentationService` + `LoadingHudService`: apresentacao visual de loading.
- `LoadingHudOrchestrator` + `LoadingProgressOrchestrator`: ponte de apresentacao; nao sao owners da transicao.
- `SceneFlowInputModeBridge`: ponte de input, nao owner do modo.

## Regras praticas
- Nao existe semantica de fluxo em style ou profile.
- `startup` nao passa por navigation.
- Rota `Gameplay` exige reset macro e `LevelCollection` valida.
- Rota `Frontend` nao pode exigir reset de mundo nem carregar `LevelCollection`.
- Loading e Fade sao apresentacao; ownership do fluxo permanece em `SceneFlow`.
- `set-active` permanece no trilho macro do `SceneFlow`.
- `load/unload` tecnico deve convergir para executor tecnico (`SceneComposition`) sem mover ownership de timeline para fora de `SceneFlow`.
- `LevelLifecycle` resolve o lifecycle local depois que a rota macro chega.

## Policy
- Falha estrutural obrigatoria de rota/style/profile continua fail-fast.
- Falha operacional de apresentacao (loading/fade) pode degradar com observabilidade explicita.
- Nao ha fallback silencioso novo no trilho macro.

## Integracao com GameLoop e InputModes
- `SceneFlowInputModeBridge` publica requests de input orientados por evento de transicao.
- Ownership de estado do loop permanece no `GameLoop`.
- Ownership de aplicacao de input permanece em `InputModes` (`InputModeCoordinator` + `IInputModeService`).
- `GameLoop` apenas consome o resultado da transicao e nao substitui `SceneFlow`.

## Ordem canonica resumida
1. `SceneTransitionStarted`
2. `FadeIn` quando habilitado
3. abertura de loading
4. composicao macro (`load/unload`) + `set-active`
5. `ScenesReady`
6. completion gate (`WorldResetCompletionGate` + `LevelPrepare/Clear`)
7. `BeforeFadeOut`
8. fechamento de loading no fim real
9. `FadeOut` quando habilitado
10. `SceneTransitionCompleted`

## Leitura cruzada
- `Docs/Modules/Navigation.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/WorldReset.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/ResetInterop.md`