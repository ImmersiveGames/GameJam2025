# SceneFlow

## A) Status atual (Baseline 3.1 — NAO MEXER SEM NOVA EVIDENCIA)

- Pipeline canonico centralizado em `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`.
- O serviço publica os eventos de fase (`TransitionStarted`, `ScenesReady`, `BeforeFadeOut`, `TransitionCompleted`) e aplica `load/unload/active` antes de liberar gates.
- O gate de completion e composto em cadeia para garantir:
  - Gate de WorldLifecycle V1 (`WorldLifecycleResetCompletionGate`), depois
  - Gate de LevelPrepare (`MacroLevelPrepareCompletionGate`).
- Integracoes observadas:
  - `WorldLifecycleSceneFlowResetDriver` consome `SceneTransitionScenesReadyEvent`.
  - `SceneFlowInputModeBridge`, `SceneFlowSignatureCache` e `LoadingHudOrchestrator` consomem eventos de transicao para sincronizacao auxiliar.

## B) Ownership map

| Componente | Owner de | Nao-owner de | Logs ancora |
|---|---|---|---|
| `SceneTransitionService` | Timeline de transicao e publicacao de fases do SceneFlow | Reset world/level e regras internas dos gates | `[SceneFlow] TransitionStarted`, `[SceneFlow] ScenesReady`, `[SceneFlow] TransitionCompleted`, `[OBS][SceneFlow] RouteExecutionPlan` |
| `SceneManagerLoaderAdapter` | Operacoes de `Load/Unload/SetActive` no adapter | Ordem de fases | `[OBS][SceneFlow] UnloadAttempt` |
| `SceneFlowFadeAdapter` | Tradução de profile para fade e execucao via `IFadeService` | Orquestracao de timeline | `[SceneFlow] Profile ... aplicado` |
| `WorldLifecycleResetCompletionGate` | Correlacao V1 de completion para liberar SceneFlow | Execucao do reset do mundo | `[SceneFlowGate] WorldLifecycleResetCompletedEvent recebido` |
| `MacroLevelPrepareCompletionGate` | Etapa `LevelPrepare` antes do FadeOut | Route resolve/apply de cenas | `[OBS][SceneFlow] MacroLoadingPhase='LevelPrepare'` |
| `SceneFlowSignatureCache` | Cache de ultima assinatura/profile/cena de transicao | Dedupe de fluxo | (sem log proprio; consome `Started/Completed`) |
| `SceneRouteCatalogAsset` + `SceneRouteCatalogResolver` + `SceneRouteDefinitionAsset` | Fonte/validacao/resolucao de dados de rota | Execucao runtime de transicao | `[OBS][Config] RouteResolvedVia=AssetRef`, `[OBS][SceneFlow] RouteResolvedVia=RouteId` |
| `LoadingHudOrchestrator` | Visibilidade da HUD por fase de transicao | Carregamento tecnico da cena HUD | `[LoadingStart]`, `[LoadingCompleted]` |
| `LoadingHudService` | Ensure/Show/Hide tecnico de LoadingHudScene | Decisao de fase da HUD | `[LoadingHudEnsure]`, `[LoadingHudShow]`, `[LoadingHudHide]` |
| `WorldLifecycleSceneFlowResetDriver` (integracao) | Handoff `ScenesReady -> WorldResetService` e fallback completion V1 | Eventos V2 de commands/telemetria | `[WorldLifecycle] Driver registrado: SceneFlow ScenesReady -> ResetWorld -> ResetCompleted` |
| `SceneFlowInputModeBridge` (integracao) | Sincronizacao InputMode/GameLoop no `Started/Completed` | Pipeline de transicao e gates | `[OBS][InputMode] Requested ...` |

## C) Pipeline de fases (timeline canonico)

1. `TransitionStarted`
2. `FadeIn` (se `useFade`)
3. `LoadedScenesSnapshot(before_apply_route)`
4. `RouteExecutionPlan`
5. `ApplyRoute` (loads/unloads/active)
6. `LoadedScenesSnapshot(after_apply_route)`
7. `ScenesReady`
8. Gate: `WorldLifecycleResetCompletionGate` (macro reset / skip)
9. Gate: `MacroLevelPrepareCompletionGate` (LevelPrepare / clear)
10. `BeforeFadeOut`
11. `FadeOut`
12. `TransitionCompleted`

## D) Gates e contratos

- Gate-critical (V1):
  - `WorldLifecycleResetCompletionGate` aguarda `WorldLifecycleResetCompletedEvent` para liberar fluxo antes do `FadeOut`.
  - `MacroLevelPrepareCompletionGate` executa `ILevelMacroPrepareService.PrepareOrClearAsync` no mesmo ponto de gate.
- Contrato de composição observado:
  - `GlobalCompositionRoot.SceneFlowWorldLifecycle` garante wrapping para que o gate final seja `MacroLevelPrepareCompletionGate(inner=WorldLifecycleResetCompletionGate)`.
- Telemetria/commands V2 no escopo:
  - Eventos V2 (`WorldLifecycleResetRequestedV2Event`, `WorldLifecycleResetCompletedV2Event`) são publicados por `WorldResetCommands` em `Modules/WorldLifecycle`.
  - Esses V2 nao substituem o gate-critical V1 do SceneFlow.

## E) LEGACY/Compat (sem remocao nesta etapa)

- `Modules/SceneFlow/Transition/Adapters/NoOpTransitionCompletionGate.cs`: implementacao compat/no-op do gate.
- `Modules/SceneFlow/Transition/Adapters/NoFadeAdapter.cs`: fallback compat para transicoes sem fade.
- Fluxo inline de dados de cena em `SceneTransitionService` e explicitamente legado/desativado (fail-fast sem `RouteId` valido).
- Caminhos de degrade do completion gate (`Completion gate falhou/abortou -> prosseguindo`) permanecem por contrato de robustez do baseline.
