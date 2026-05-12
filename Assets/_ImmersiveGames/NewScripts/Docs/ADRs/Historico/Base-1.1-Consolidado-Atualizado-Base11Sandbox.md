# Base 1.1 - Consolidado Atualizado: Base11Sandbox

## Status

Congelado como documento consolidado do checkpoint validado do `Base11Sandbox`.

Este arquivo nao substitui os ADRs normativos da Base 1.1 (`ADR-0060` em diante). Ele consolida o estado congelado do rail canônico para o sandbox minimo.

## Fonte normativa

A fonte normativa permanece:

```text
ADR-0060 em diante.
```

ADRs anteriores sao historicos, salvo quando explicitamente citados pelos ADRs vivos da Base 1.1.

## Decisao consolidada

O `Base11Sandbox` valida o shape arquitetural minimo e congelado:

```text
Boot
-> Menu
-> SessionActivitySandboxScene
-> Start Activity 01
-> Pause
-> Resume
```

O contrato vivo do checkpoint e:

```text
SessionOperationalPipeline
-> coordena rota/transicao/loading/fade/handoff antes de activities

SessionActivityPipeline
-> coordena o ciclo interno da activity

Adapters tecnicos
-> executam side-effects comandados pelos pipelines
```

## Rail canonico congelado

```text
RuntimeModeConfig
-> RuntimePersistentScenesPolicyAsset
-> SessionOperationalRouteAsset
-> SessionOperationalPipeline
-> Base11SandboxSessionOperationalLoadingAdapter
-> Base11SandboxSessionOperationalFadeAdapter
-> Base11SandboxOperationalRouteTransitionAdapter
-> SceneCompositionExecutor
```

Regras congeladas:

- `RuntimeModeConfig` e a entrada canonica de configuracao do modo.
- `BootstrapConfigAsset` nao e fonte canonica do `Base11Sandbox`.
- `RuntimePersistentScenesPolicyAsset` declara `UIGlobalScene`, `FadeScene` e `LoadingHudScene` como persistent scenes.
- persistent scenes nao sao route-owned.
- `SessionOperationalRouteAsset` usa `SceneKeyAsset` e nao string serializada de cena.
- `ActiveSceneKey` e obrigatorio e entra implicitamente como primeira cena de carga.
- `ScenesToLoad` representa apenas cenas adicionais da rota.
- `ScenesToUnload` permanece apenas para excecoes explicitas.
- `SessionOperationalPipeline` decide quando loading, fade e handoff acontecem.
- `Base11SandboxSessionOperationalLoadingAdapter` executa UI/progresso.
- `Base11SandboxSessionOperationalFadeAdapter` executa o fade.
- `Base11SandboxOperationalRouteTransitionAdapter` executa a aplicacao fisica da rota.
- `SceneCompositionExecutor` executa side-effects fisicos em ordem segura.
- `LoadingHudOrchestrator`, `LoadingProgressOrchestrator` e `LoadingHudService` legados nao participam do rail novo.

## Composition Profile validado

O profile ativo validado e:

```text
RuntimePolicy
Pooling
Gates
InputModes
RuntimePersistentScenes
Base11SandboxOperationalRouting
SceneComposition
```

Ficam fora do profile minimo:

```text
GameLoop
SessionIntegration legacy
PhaseDefinition
Gameplay legacy
ActorsSystem
WorldReset
Save
RunEndRail / PostRun services
IntroStage obrigatorio
SceneFlow / Navigation legacy como owner do Base11Sandbox
```

Essa exclusao e intencional. Nao e debito de composicao quebrada; e descontaminacao do sandbox.

## Ciclo minimo validado

```text
Boot -> Menu -> SessionActivitySandboxScene -> Start Activity 01 -> Pause -> Resume
```

Validacoes observadas:

```text
Boot -> Menu: Completed no SessionOperationalPipeline
Menu -> SessionActivitySandboxScene: Completed no SessionOperationalPipeline
SessionActivityPipeline: PipelineStarted
SessionActivityPipeline: ActivationEntered
SessionActivityPipeline: GameplayRunningEntered
SessionActivityPipeline: PauseResolved
SessionActivityPipeline: ResumeResolved
SimulationGate: BlockActivitySimulation / ReleaseActivitySimulation
```

## Evidencia de smoke do rail canonico

- `RuntimePersistentScenes` carrega `UIGlobalScene`, `FadeScene` e `LoadingHudScene`.
- `Route_MenuToSessionActivitySandbox` usa `LoadingMode=Profile` e `TransitionMode=Profile`.
- `LoadingHidden` ocorre antes de `fadeOutStarted`.
- `fadeOutCompleted` ocorre antes de `OperationalRouteCompleted`.
- `SessionActivityEntryHandoffEmitted` ocorre depois de `OperationalRouteCompleted`.
- `SessionActivityMiniFlowHost` pode inicializar durante o load, mas `autoStart=False` e `stage Unknown` nao significam inicio da activity.

## Pendencias explicitas

- A UI/QA local da `SessionActivitySandboxScene` pode aparecer cedo; isso e visual/local e nao quebra o rail canonico.
- O warning legado `routeId='to-menu'` com `PhaseDefinitionCatalog` ainda existe como residuo de configuracao legado.
- O segundo corte do `SceneFlow/Navigation` legado ainda esta pendente.
- `RouteActorSetRefContext` deve ser extraido de `SceneFlowInstaller` antes da remocao final de `SceneFlowInstaller`/`SceneFlowBootstrap`.
