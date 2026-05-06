# Base11Sandbox - Operational Routing minimo e Pipeline Handoff para SessionActivityPipeline

## Status
- Estado: Accepted
- Marco: Frozen checkpoint
- Escopo: Base 1.1 / Base11Sandbox

## Contexto

O Base11Sandbox foi criado para isolar o ciclo minimo da Base 1.1 sem depender do legado contaminado.

O trilho validado e:

```text
Boot -> Menu -> Sandbox -> Activity 01 -> Pause -> Resume
```

## Decisao

- Criar e usar `SessionOperationalRouteAsset` minimo.
- Nao usar `RouteKind`, `RouteProfile`, `routeClass`, `NavigationCatalog`, `GameNavigationService` ou `SceneTransitionService` no trilho ativo do Base11Sandbox.
- `RuntimeModeConfig` aponta diretamente para `startupRouteDefinition`.
- A UI usa `SessionOperationalRouteButtonBinder` generico com referencia direta ao asset.
- Rota nao e tipo/policy. Rota e dado/configuracao.
- `routeIdentity` e apenas `Pipeline Identity` / log / guard.
- `completionHandoff` e explicito:
  - `NoHandoff`
  - `SessionActivityEntry`
- `SessionOperationalPipeline` decide `OperationalRouteCommand`, `OperationalRouteCompleted` e `SessionActivityEntryHandoff`.
- `Base11SandboxOperationalRouteTransitionAdapter` e `Pipeline Adapter` e so executa `load/unload/set-active`.
- `SceneCompositionExecutor` executa side-effect fisico em ordem segura:
  - `load -> set active -> unload`
- `SessionActivityPipeline` decide a primeira activity via catalogo proprio e aloca `entrySequence`.
- `SessionOperationalPipeline` nao conhece `activity_01`.
- `DebugDirectStart` e QA/tooling e deve rejeitar com `pipeline_already_started` apos start canonico.
- `SimulationGate` executa `Pipeline Command`; nao decide lifecycle.
- `InputMode` permanece `observed_noop` / `deferred` neste checkpoint.

## Contratos fail-fast

- `routeIdentity` obrigatorio.
- `activeScene` obrigatorio.
- `activeScene` nao pode aparecer em `scenesToUnload`.
- `scenesToLoad` sem duplicatas.
- `scenesToUnload` sem duplicatas.
- `completionHandoff` invalido = fail-fast.
- `completionHandoff=SessionActivityEntry` exige `handoffSessionStateId`.
- config obrigatoria ausente nao pode ter fallback silencioso.

## Evidencia de smoke

Evidencia textual do checkpoint:

- `Route_BootToMenu completionHandoff='NoHandoff'`
- `OperationalRouteCompleted Route_BootToMenu`
- `Route_MenuToSessionActivitySandbox completionHandoff='SessionActivityEntry'`
- `SessionActivityEntryHandoffAccepted`
- `PipelineStarted source='SessionOperationalRouteButtonBinder'`
- `GameplayRunningEntered activity='activity_01'`
- `DebugDirectStart rejected reason='pipeline_already_started'`
- `PauseResolved`
- `ResumeResolved`
- `InputMode PauseOverlay/ActivityGameplay observed_noop`

## Classificacao dos componentes

| Componente | Classificacao | Observacao |
| --- | --- | --- |
| `SessionOperationalRouteAsset` | Canonical | contrato minimo de rota operacional |
| `SessionOperationalStartupRouteEmitter` | Canonical command emitter | traduz boot para rota inicial |
| `SessionOperationalRouteButtonBinder` | Canonical UI command producer | binder generico por asset |
| `SessionOperationalPipeline` | Canonical owner | decide comando, completion e handoff |
| `Base11SandboxOperationalRouteTransitionAdapter` | Pipeline Adapter | executor fisico |
| `SceneCompositionExecutor` | executor fisico | ordem segura de composicao de cena |
| `SessionActivityPipeline` | Canonical owner do ciclo interno de activity | aloca `entrySequence` e decide primeira activity |
| `SessionActivityMiniFlowHost` | QA/tooling | nao owner |
| `SimulationGateService` | executor | sempre comandado por pipeline |
| `InputMode adapters` | observed_noop/deferred | fora da decisao canonica |
| `NavigationCatalog/GameNavigationService/SceneTransitionService` | fora do trilho ativo Base11Sandbox | legado removido do ciclo minimo |

## Consequencias

- O sandbox passa a ter rota minima no shape Base 1.1.
- A navegacao legada deixa de ser fonte de policy no ciclo validado.
- Rotas futuras podem ser `Boot -> Test`, `Gameplay -> Menu` etc. sem depender de `RouteKind`.
- `Transitions` / `fade` / `loading` continuam fora deste checkpoint.
- `InputMode` real continua adiado.
- `ActorsSystem`, `GameLoop`, `IntroStage`, `RunEndRail`, `WorldReset` e `Save` permanecem fora do profile minimo.
