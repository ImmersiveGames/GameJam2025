# ADR-0006 - Route, Scene Composition, Fade, Loading e Audio Adapters

## Status
- Estado: Accepted / Frozen Checkpoint
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A auditoria de Navigation/Routes/SceneFlow mostrou que `routeKind` deixou de ser apenas classificação de rota e passou a influenciar decisões de várias camadas. Além disso, o checkpoint do Base11Sandbox validou um rail canônico de rotas, loading, fade e handoff sem depender do rail legado de SceneFlow como owner do fluxo canônico.

Base 1.1 exige que:
1. Lifecycle, policy e handoff vivam nos pipelines.
2. SceneFlow/Navigation execute side-effects como adapters, não decida lifecycle.
3. Route define topologia; RouteProfile define policies; Pipeline decide lifecycle.

## Decisão

### 1. RouteDefinition, SceneRouteProfile e Route Policy

#### RouteDefinition

`RouteDefinition` descreve a topologia da rota:

- `routeId`
- `scenesToLoad`
- `scenesToUnload`
- `targetActiveScene`

#### SceneRouteProfile

`SceneRouteProfile` descreve capacidades e policies operacionais da rota:

- Route classification (gameplay, menu, overlay, etc.)
- Gameplay participation
- World reset requirement
- Payload eligibility
- Input behavior
- Actor set behavior
- Save behavior
- Loading behavior
- Fade behavior

#### Decisão de Lifecycle

- Session Pipeline e Run Pipeline são donos de:
  - Lifecycle
  - Pipeline policy
  - Pipeline handoff
- `SceneFlow/Navigation` passam a ser tratados como `Pipeline Adapter`.

#### Invariante sobre routeKind

- `routeKind` não deve continuar sendo a fonte central de policy.
- Pode permanecer temporariamente como compatibilidade durante a migração.
- Consumidores que usam `routeKind` como policy devem migrar gradualmente para `SceneRouteProfile`.

### 2. Scene Composition Executor

Regras:

- `SceneCompositionExecutor` é executor puramente físico.
- Executa carregamento, descarregamento e ativação de cenas.
- Não decide o que carregar; recebe comando do pipeline via adapter.
- Reporta completion ou erro ao adapter.

### 3. Fade Adapter

Regras:

- `FadeAdapter` executa fade/unfade operacional.
- `Base11SandboxSessionOperationalFadeAdapter` é implementação canônica do sandbox.
- Executa o que foi comandado pelo pipeline.
- Não decide timing ou transição.

### 4. Loading Adapter

Regras:

- `LoadingAdapter` executa UI e progresso de loading.
- `Base11SandboxSessionOperationalLoadingAdapter` é implementação canônica do sandbox.
- Mostra progresso conforme `SessionOperationalPipeline` avança.
- Reporta completion para o pipeline continuar.

#### Ordem Canônica do Loading / Fade / Scene Composition no Base11Sandbox

```text
RuntimePersistentScenes guaranteed
-> LoadingPlanReady
-> OperationalRouteCommand
-> TransitionPlanReady
-> LoadingStarted/showCompleted
-> fadeInStarted/fadeInCompleted, se transition ativa
-> SceneComposition
-> SceneCompositionCompleted
-> MaterializationCompleted
-> progress=1
-> progressVisualSettled
-> finalProgressHoldStarted/finalProgressHoldCompleted
-> LoadingCompleted
-> hideStarted/hideCompleted
-> LoadingHidden
-> fadeOutStarted/fadeOutCompleted, se transition ativa
-> OperationalRouteCompleted
-> SessionActivityEntryHandoffEmitted, se houver
```

### 5. Audio Adapter

Regras:

- `AudioAdapter` executa playback, pause, stop de áudio.
- Não é owner de mix/ducking; apenas executa o comandado.
- Reporta completion quando necessário.

### 5.1 Checkpoint Congelado - Audio Operacional de Rota (Base 1.1)

Checkpoint congelado do trilho canônico de áudio operacional de rota:

```text
OperationalRouteAsset
-> routeAudioMode/routeAudioCue/routeAudioTiming/stopPreviousRouteAudio
-> SessionOperationalPipeline
-> RouteAudioPlanReady
-> RouteRevealAudioStarted
-> AudioAdapter playStarted/playSubmitted
-> RouteRevealAudioSubmitted
```

Decisões normativas congeladas:

1. A rota declara `routeAudioMode`, `routeAudioCue`, `routeAudioTiming` e `stopPreviousRouteAudio`.
2. `SessionOperationalPipeline` decide ordem e timing de execução do `RouteAudio`.
3. `RouteAudioPlanReady` é plano/observabilidade; não executa side-effect.
4. `RouteRevealAudioStarted` é o ponto de execução comandado.
5. `AudioAdapter` executa side-effect e não decide policy/lifecycle.
6. `RouteRevealAudioSubmitted` confirma submissão do comando de áudio.
7. `AudioRuntime` técnico não decide rota, scene, handoff, timing ou lifecycle.
8. Não existe fallback silencioso no trilho canônico.
9. `routeAudioMode=None` exige `routeAudioCue` nulo (asset inválido se houver cue preenchido).
10. `routeAudioMode=Cue` exige `routeAudioCue` válido.
11. Timing validado no MVP: `BeforeFadeOut`.

Evidência funcional validada no smoke:

- Boot -> Menu: `AudioBgmCue_Startup`.
- Menu -> SessionActivitySandboxScene: `AudioBgmCue_Alternate`.
- SessionActivitySandboxScene -> Menu: retorno para `AudioBgmCue_Startup`.
- Rota `None` com skip explícito permanece pendente de validação de smoke dedicada e não bloqueia o fechamento do trilho `Cue`.

### 6. Rail Canônico do Base11Sandbox

O rail canônico é:

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

#### Regras Canônicas

1. `RuntimeModeConfig` é a entrada de configuração do modo.
2. `RuntimePersistentScenesPolicyAsset` declara cenas persistentes; persistent scenes não são route-owned.
3. `UIGlobalScene`, `FadeScene` e `LoadingHudScene` são persistent scenes do modo.
4. `SessionOperationalRouteAsset` usa `SceneKeyAsset`, não string serializada de cena.
5. `ActiveSceneKey` é obrigatório e entra implicitamente como primeira cena a carregar.
6. `ScenesToLoad` representa apenas cenas adicionais da rota.
7. `ScenesToUnload` permanece apenas para exceções explícitas.
8. `UnloadPreviousRouteOwnedScenes` descarrega somente cenas owned pela última rota operacional concluída.
9. `SessionOperationalPipeline` decide quando loading, fade e handoff acontecem.
10. `SceneTransitionService` não é owner do fade/loading no Base11Sandbox.
11. O rail canônico deve falhar cedo quando houver conflito entre persistent scene obrigatória e rota.

## Invariantes

- Não criar Base 2.0.
- Não reorganizar fisicamente a arquitetura.
- Não criar compat paralelo.
- Não criar fallback silencioso.
- Não permitir que rotas carreguem, descarreguem ou ativem persistent scenes.
- Não reativar `SceneFlow` legado como owner do loading/fade.
- Pipeline decide lifecycle; SceneFlow/Navigation executa side-effects.
- Foreign/stale events não podem alterar o pipeline ativo.
- `routeKind` não deve mais ser o ponto onde decisões transversais são inferidas por conveniência.

## Consequências

- Consumidores que hoje usam `routeKind` como policy devem migrar gradualmente para `SceneRouteProfile`.
- `GameNavigationService` não decide lifecycle de gameplay por `routeKind`.
- `SceneFlow/Navigation` são rebaixados a Pipeline Adapter.
- A compatibilidade com `routeKind` atual pode existir por um período de migração apenas.
- O rail do Base11Sandbox fica congelado e rastreável.
- O loading/fade não voltam a depender do owner legado.
- `SessionActivityPipeline` começa somente após `SessionActivityEntryHandoff`.

## Materializacao Base11Sandbox - Checkpoint Congelado

O checkpoint `Base11Sandbox Minimal Route + Session Activity Cycle - PASS` congelou o rail:

- `RuntimeModeConfig` é a entrada canônica de configuração do modo.
- `SessionOperationalRouteAsset` descreve a rota operacional sem usar string serializada de cena.
- `SessionOperationalPipeline` decide lifecycle, completion e handoff.
- `Base11SandboxSessionOperationalLoadingAdapter` executa a UI de loading.
- `Base11SandboxSessionOperationalFadeAdapter` executa o fade.
- `Base11SandboxOperationalRouteTransitionAdapter` executa a aplicação física da rota.
- `SceneCompositionExecutor` executa os side-effects de cena.
- `LoadingHudOrchestrator`, `LoadingProgressOrchestrator` e `LoadingHudService` legados saíram do caminho ativo.
- `SessionOperationalRouteTransitionBridge` saiu do caminho ativo.
- `SceneFlowBootstrap`, `SceneFlowInstaller`, `SceneTransitionService` permanecem apenas como legado isolado ou seam técnico não canônico.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 permitiu que a decisão de rota carregasse política demais.
- Base 1.1 formaliza a separação entre topologia, profile, intenção e lifecycle.
- Base 2.0 futura só deve nascer se essa separação continuar válida na prática.

