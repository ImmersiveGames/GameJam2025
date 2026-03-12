# SceneFlow

## A) Status atual (Baseline 3.1 -- NAO MEXER SEM NOVA EVIDENCIA)

- Pipeline canonico centralizado em `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`.
- O servico publica os eventos de fase (`TransitionStarted`, `ScenesReady`, `BeforeFadeOut`, `TransitionCompleted`) e aplica `load/unload/active` antes de liberar gates.
- O gate de completion e composto em cadeia para garantir:
  - Gate de WorldLifecycle V1 (`WorldLifecycleResetCompletionGate`), depois
  - Gate de LevelPrepare (`MacroLevelPrepareCompletionGate`).
- Integracoes observadas:
  - `WorldLifecycleSceneFlowResetDriver` consome `SceneTransitionScenesReadyEvent`.
  - `SceneFlowInputModeBridge`, `SceneFlowSignatureCache` e `LoadingHudOrchestrator` consomem eventos de transicao para sincronizacao auxiliar.
- Boot/startup transition:
  - owner unico: `NewScriptsBootstrapConfigAsset.startupTransitionStyleRef`.
  - nao existe mais fallback para `startupTransitionProfile` nem para catalogo nominal.
- Semantica final de fluxo:
  - `startup` pertence exclusivamente ao bootstrap.
  - `frontend` e `gameplay` pertencem exclusivamente a `SceneRouteKind`.
  - labels de `style`/`profile` permanecem apenas como observabilidade.
- Navigation/Transition agora e direct-ref-first e fail-fast:
  - `GameNavigationCatalogAsset` resolve por `routeRef + transitionStyleRef`.
  - `TransitionStyleAsset` resolve por `profileRef + useFade`.
  - `style` e `profile` em logs/signatures sao apenas labels descritivos derivados dos assets.

## B) Ownership map

| Componente | Owner de | Nao-owner de | Logs ancora |
|---|---|---|---|
| `SceneTransitionService` | Timeline de transicao e publicacao de fases do SceneFlow | Reset world/level e regras internas dos gates | `[SceneFlow] TransitionStarted`, `[SceneFlow] ScenesReady`, `[SceneFlow] TransitionCompleted`, `[OBS][SceneFlow] RouteExecutionPlan` |
| `SceneManagerLoaderAdapter` | Operacoes de `Load/Unload/SetActive` no adapter | Ordem de fases | `[OBS][SceneFlow] UnloadAttempt` |
| `SceneFlowFadeAdapter` | Traducao de profile para fade e execucao via `IFadeService` | Orquestracao de timeline | `[SceneFlow] Profile ... aplicado` |
| `WorldLifecycleResetCompletionGate` | Correlacao V1 de completion para liberar SceneFlow | Execucao do reset do mundo | `[SceneFlowGate] WorldLifecycleResetCompletedEvent recebido` |
| `MacroLevelPrepareCompletionGate` | Etapa `LevelPrepare` antes do FadeOut | Route resolve/apply de cenas | `[OBS][SceneFlow] MacroLoadingPhase='LevelPrepare'` |
| `SceneFlowSignatureCache` | Cache da ultima assinatura/profile/cena observada | Dedupe de fluxo | (sem log proprio; consome `Started/Completed`) |
| `SceneRouteCatalogAsset` + `SceneRouteCatalogResolver` + `SceneRouteDefinitionAsset` | Fonte/validacao/resolucao de dados de rota | Execucao runtime de transicao | `[OBS][Config] RouteResolvedVia=AssetRef`, `[OBS][SceneFlow] RouteResolvedVia=RouteId` |
| `TransitionStyleAsset` | Owner canonico direto de `profileRef + useFade` | Lookup estrutural por id/catalogo | `[OBS][SceneFlow] Startup transition resolved via bootstrap direct style reference` |
| `NewScriptsBootstrapConfigAsset` | Referencia direta da startup transition do boot | Resolucao runtime de styles gerais | `[OBS][SceneFlow] Startup transition resolved via bootstrap direct style reference` |
| `LoadingHudOrchestrator` | Visibilidade da HUD por fase de transicao | Carregamento tecnico da cena HUD | `[LoadingStart]`, `[LoadingCompleted]` |
| `LoadingHudService` | Ensure/Show/Hide tecnico de LoadingHudScene | Decisao de fase da HUD | `[LoadingHudEnsure]`, `[LoadingHudShow]`, `[LoadingHudHide]` |
| `WorldLifecycleSceneFlowResetDriver` (integracao) | Handoff `ScenesReady -> WorldResetService` e fallback completion V1 | Eventos V2 de commands/telemetria | `[WorldLifecycle] Driver registrado: SceneFlow ScenesReady -> ResetWorld -> ResetCompleted` |
| `SceneFlowInputModeBridge` (integracao) | Sincronizacao InputMode/GameLoop no `Started/Completed` orientada por `RouteKind` | Pipeline de transicao e gates | `[OBS][InputMode] Requested ...` |


