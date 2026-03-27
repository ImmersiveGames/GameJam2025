# SceneFlow

## Precedência canônica
- Fonte de verdade operacional: `ADR-0030`, `ADR-0031`, `ADR-0032`, `ADR-0033`, `ADR-0039`.
- Em conflito, esta ordem prevalece sobre baseline histórica (`ADR-0009..ADR-0027`, pasta `Obsolete/`).

## Estado atual
- `SceneTransitionService` é owner da timeline macro de transição.
- `SceneRouteDefinitionAsset` é a definição canônica da rota.
- `SceneTransitionRequest.ResolvedRouteDefinition` é a entrada operacional preferencial.
- `GameNavigationCatalogAsset` resolve `intent -> routeRef + transitionStyleRef`.
- `TransitionStyleAsset` resolve `profileRef + useFade`.
- `SceneTransitionProfile` permanece asset leaf visual.
- `LoadingHudScene` e a HUD canônica fazem parte do macro flow.

## Ownership
- `SceneTransitionService`: fases da transição, sequencing macro e eventos canônicos.
- `SceneRouteDefinitionAsset`: definição de rota, `RouteKind`, target scene e policy macro de reset.
- `GameNavigationCatalogAsset`: intents canônicos de navegação.
- `TransitionStyleAsset`: style estrutural da transição.
- `SceneFlowFadeAdapter`: aplicação do style no fade.
- `WorldResetCompletionGate` (em `ResetInterop`) + `MacroLevelPrepareCompletionGate`: gate composto entre `ScenesReady` e `BeforeFadeOut`.
- `ILoadingPresentationService` + `LoadingHudService`: apresentação visual de loading.
- `LoadingHudOrchestrator` + `LoadingProgressOrchestrator`: ponte de apresentação; não são owners da transição.

## Regras práticas
- Não existe semântica de fluxo em style ou profile.
- `startup` não passa por navigation.
- Rota `Gameplay` exige reset macro e `LevelCollection` válida.
- Rota `Frontend` não pode exigir reset de mundo nem carregar `LevelCollection`.
- Loading e Fade são apresentação; ownership do fluxo permanece em `SceneFlow`.
- `set-active` permanece no trilho macro do `SceneFlow`.
- `load/unload` técnico deve convergir para executor técnico (`SceneComposition`) sem mover ownership de timeline para fora de `SceneFlow`.

## Policy
- Falha estrutural obrigatória de rota/style/profile continua fail-fast.
- Falha operacional de apresentação (loading/fade) pode degradar com observabilidade explícita.
- Não há fallback silencioso novo no trilho macro.

## Integração com GameLoop e InputModes
- `SceneFlowInputModeBridge` publica requests de input orientados por evento de transição.
- Ownership de estado do loop permanece no `GameLoop`.
- Ownership de aplicação de input permanece em `InputModes` (`InputModeCoordinator` + `IInputModeService`).

## Ordem canônica resumida
1. `SceneTransitionStarted`
2. `FadeIn` quando habilitado
3. abertura de loading
4. composição macro (`load/unload`) + `set-active`
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
