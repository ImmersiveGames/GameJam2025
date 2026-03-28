# Blueprint - Baseline 4.0 Ideal Architecture

Status: Canonical architecture reference for Baseline 4.0
Date: 2026-03-28

This blueprint is the primary architecture reference for Baseline 4.0 and is consolidated by `ADR-0044`.

## 1. Executive Summary

Este documento define a arquitetura ideal do Baseline 4.0 a partir do ADR-0001 e do ADR-0043. O codigo atual e usado apenas como evidencia e inventario de reaproveitamento.

A direcao central e:

- `Gameplay` e o `Contexto Macro`.
- `Level` e o `Contexto Local de Conteudo`.
- `EnterStage` e `ExitStage` sao `Estagios Locais`.
- `Playing` e o `Estado de Fluxo`.
- `Victory` / `Defeat` sao `Resultado da Run`.
- `PostRunMenu` e `Contexto Local Visual`.
- `Restart` / `ExitToMenu` sao `Intencoes Derivadas`.
- `Pause` e `Estado Transversal`.

O Baseline 4.0 ideal nao preserva nomes ou splits atuais por compatibilidade automatica. O que nao encaixar na espinha conceitual deve ser substituido.

## 2. Conceptual Backbone

### 2.1 Domain Spine

| Concept | Role in ideal architecture | Notes |
|---|---|---|
| `Contexto Macro` | onde a aplicacao esta | define palco, entrada, saida e regras maiores |
| `Contexto Local de Conteudo` | o conteudo ativo dentro do macro | no gameplay, o `Level` |
| `Contexto Local Visual` | a camada visual/interativa sobreposta | exemplo: `PauseMenu`, `PostRunMenu` |
| `Estagio Local` | fase delimitada do conteudo local | `EnterStage` e `ExitStage` |
| `Estado de Fluxo` | fase principal da experiencia | `Playing` |
| `Resultado da Run` | conclusao consolidada da run | `Victory` / `Defeat` |
| `Intencao Derivada` | decisao emitida apos resultado ou estado consolidado | `Restart` / `ExitToMenu` |
| `Estado Transversal` | condicao que suspende ou modifica outro fluxo | `Pause` |

### 2.2 Canonical Reading

- `Gameplay` identifica o macro-contexto.
- `Level` identifica o conteudo local.
- `EnterStage` prepara a entrada.
- `Playing` e a fase principal.
- `ExitStage` fecha o conteudo local.
- `RunResult` consolida a run.
- `PostRunMenu` aparece depois do resultado.
- `Restart` e `ExitToMenu` nascem do pos-run.
- `Pause` modifica o fluxo sem virar contexto macro.

## 3. Runtime Backbone

### 3.1 Ideal Runtime Sequence

```text
Gameplay
-> Level
-> EnterStage
-> Playing
-> ExitStage
-> RunResult
-> PostRunMenu
-> Restart / ExitToMenu
-> Navigation primary dispatch
-> Audio contextual reactions
```

### 3.2 Runtime Semantics

- `Playing` e o unico estado de fluxo canonicamente ativo.
- `RunResult` nasce no dominio de gameplay quando a run termina.
- `PostRunMenu` nao descobre o resultado; apenas o apresenta.
- `Restart` e `ExitToMenu` sao comandos derivados do pos-run, nao estados de fluxo.
- `Navigation` resolve e despacha a mudanca primaria, mas nao define o significado do resultado.
- `Audio` reage ao contexto ja consolidado e nao o define.

### 3.3 What Must Persist

- start da gameplay somente apos readiness canonico.
- transicao de run para resultado sem ambiguidade.
- pausa como estado transversal real, com menu visual separado.
- retorno para menu e restart com intents claras.
- audio contextual por precedente e sem ownership de navigation.

## 4. Target Domain Architecture

### 4.1 `GameLoop`

#### Ideal role

Owner da maquina de estados de fluxo, da run e da pausa.

#### Main services

- flow state machine
- run start/end signaling
- pause/resume signaling
- activity telemetry
- run-end guard

#### Main events

- `GameRunStarted`
- `GameRunEnded`
- `PauseStateChanged`
- `PauseWillEnter`
- `PauseWillExit`
- `GameLoopActivityChanged`

#### Main assets/config

- minimal bootstrap config for state startup
- optional debug/QA hooks

#### Must not own

- post-run overlay
- result presentation
- route dispatch
- audio precedence
- post-run menu logic

### 4.2 `PostGame`

#### Ideal role

Owner do pos-run: ownership de input/gate, apresentacao do resultado e contexto visual local de pos-run.

#### Main services

- post-run ownership service
- post-run result projection service
- post-run stage coordinator
- presenter registry/scope resolver
- post-run control service

#### Main events

- `PostStageStartRequested`
- `PostStageStarted`
- `PostStageCompleted`
- `PostGameEntered`
- `PostGameExited`

#### Main assets/config

- post-run visual config
- optional presenter config
- post-run text/config sources

#### Must not own

- gameplay state machine
- route resolution
- run outcome decision
- primary navigation policy

### 4.3 `LevelFlow`

#### Ideal role

Owner do conteudo local do gameplay e das acoes pos-level.

#### Main services

- level selection/runtime service
- macro prepare service
- local swap service
- restart context service
- post-level actions service
- level post-game hook service

#### Main events

- `LevelSelected`
- `LevelIntroCompleted`
- `LevelSwapLocalApplied`
- `LevelPostGameHook...` events, if retained

#### Main assets/config

- `LevelDefinition`
- `LevelCollection`
- restart snapshot/context
- local content metadata

#### Must not own

- resultado terminal da run
- post-run ownership
- global route dispatch

### 4.4 `Navigation`

#### Ideal role

Owner da resolucao de intent para rota/estilo e do dispatch primario.

#### Main services

- navigation service
- intent catalog
- route catalog
- dispatch runner

#### Main events

- navigation intents only
- route dispatch lifecycle events, if needed

#### Main assets/config

- intent catalog
- route definitions
- transition styles

#### Must not own

- result semantics
- post-run semantics
- pause semantics
- audio catalog ownership

### 4.5 `Audio`

#### Ideal role

Domino standalone de playback global e entity-bound, com precedencia contextual propria.

#### Main services

- global audio service
- BGM service
- entity audio service
- settings service

#### Main events

- audio request / apply / stop events
- contextual audio reaction events

#### Main assets/config

- cue assets
- voice profiles
- defaults asset
- semantic maps, when scoped

#### Must not own

- navigation intent resolution
- gameplay result ownership
- post-run ownership

### 4.6 `SceneFlow`

#### Ideal role

Pipeline tecnico de transicao de cenas e readiness.

#### Main services

- scene transition service
- loading/fade orchestration
- readiness coordination

#### Main events

- transition started/completed events
- readiness completion events

#### Main assets/config

- route definitions
- transition styles
- loading/fade configs

#### Must not own

- gameplay semantics
- post-run semantics
- audio precedence

### 4.7 `Frontend/UI`

#### Ideal role

Camada de contextos visuais locais e emissores de intents.

#### Main services

- panel controller
- overlay controller
- button binders
- local presenter wiring

#### Main events

- UI action events
- derived intent emission events

#### Main assets/config

- panel config
- button/config bindings
- overlay presentation config

#### Must not own

- run outcome
- navigation policy
- gameplay flow state

## 5. Current Code Reuse Map

### 5.1 `GameLoop`

| Current piece | Ideal use | Verdict |
|---|---|---|
| `GameLoopStateMachine` | flow state core | reuse with adjustment |
| `GameLoopService` | coordination facade | reuse with adjustment |
| `GameRunOutcomeService` | run-end owner/guard | reuse with adjustment |
| `GameRunResultSnapshotService` | projection only, if kept | reuse with adjustment |
| `GameLoopSceneFlowSyncCoordinator` | technical sync bridge | reuse with adjustment |
| `GamePauseOverlayController` | should move to UI layer | replace |
| `GameLoopCommands` | intent bridge facade | reuse with adjustment |
| `GameResetRequestedEvent` / `GameExitToMenuRequestedEvent` | intents only | reuse as concept, but may move ownership |

### 5.2 `PostGame`

| Current piece | Ideal use | Verdict |
|---|---|---|
| `PostStageCoordinator` | technical post-run stage owner | reuse with adjustment |
| `PostStageControlService` | stage control state | reuse with adjustment |
| `PostStagePresenterRegistry` | presenter adoption | reuse with adjustment |
| `PostStagePresenterScopeResolver` | presenter discovery seam | reuse with adjustment |
| `PostGameOwnershipService` | post-run ownership/gate/input | reuse with adjustment |
| `PostGameResultService` | result projection | reuse with adjustment |
| `PostGameOverlayController` | visual context local | reuse with adjustment or move to UI-owned presentation layer |
| `GameRunEndedEventBridge` | handoff bridge from gameplay to post-run | reuse with adjustment |
| `LevelPostStageMockPresenter` | QA/prototyping only | reuse for QA only or replace |

### 5.3 `LevelFlow`

| Current piece | Ideal use | Verdict |
|---|---|---|
| `LevelFlowRuntimeService` | level lifecycle runtime | reuse with adjustment |
| `PostLevelActionsService` | post-level intent execution | reuse with adjustment |
| `RestartContextService` | restart context/snapshot | reuse with adjustment |
| `LevelSelectedRestartSnapshotBridge` | snapshot sync bridge | reuse with adjustment |
| `LevelPostGameHookService` | optional level reaction | reuse with adjustment |
| `GameplayStartSnapshot` | restart/start context | reuse as is or with small adjustment |

### 5.4 `Navigation`

| Current piece | Ideal use | Verdict |
|---|---|---|
| `GameNavigationService` | intent resolution and dispatch | reuse with adjustment |
| `GameNavigationIntents` | canonical intent catalog | reuse with adjustment |
| `GameNavigationEntry` | route/style resolution result | reuse as is |
| `MenuPlayButtonBinder` | frontend intent emitter | reuse with adjustment |
| `MenuQuitButtonBinder` | frontend exit emitter | reuse with adjustment |
| `FrontendPanelsController` | visual context controller | reuse with adjustment |

### 5.5 `Audio`

| Current piece | Ideal use | Verdict |
|---|---|---|
| `Audio` baseline contracts and runtime | standalone audio domain | reuse with adjustment |
| contextual BGM precedence assets | audio-owned precedence | reuse with adjustment |
| `EntityAudioSemanticMapAsset` | entity-only semantic map or split asset | substitute or split |

### 5.6 `SceneFlow`

| Current piece | Ideal use | Verdict |
|---|---|---|
| transition pipeline and readiness hooks | technical runtime pipeline | reuse with adjustment |
| route definitions / style assets | dispatch infrastructure | reuse as is |

### 5.7 `Frontend/UI`

| Current piece | Ideal use | Verdict |
|---|---|---|
| overlay controllers | local visual presentation | reuse with adjustment |
| button binders | intent emitters | reuse with adjustment |
| panel controllers | local context switching | reuse with adjustment |

## 6. Structural Backlog

### 6.1 Conceptual renames

- `PostPlay` should be treated as technical retention, not domain phase.
- `PostGame` should mean post-run ownership, not macro context.
- `PostRunMenu` should be the conceptual label for the visual layer, even if the runtime name differs.

### 6.2 Service extraction / substitution

- separate post-run ownership from result projection if they remain coupled.
- move any post-run overlay ownership out of flow state owners.
- isolate audio precedence from navigation.
- keep scene-flow strictly technical.

### 6.3 Asset remodeling

- reduce or split `EntityAudioSemanticMapAsset` if it still carries non-entity semantics.
- keep route/style assets in navigation or scene-flow only.
- keep post-run presentation assets out of gameplay flow ownership.

### 6.4 Duplication removal

- collapse duplicate result projection surfaces.
- choose one canonical exit-to-menu execution path for post-run.
- avoid duplicated post-run gate/input ownership.

### 6.5 Temporary bridges only when unavoidable

- gameplay to post-run handoff bridge.
- post-run visual adoption bridge.
- audio contextual bridge from consumer domains.

## 7. Implementation Phases

### Phase 1 - Spine and vocabulary freeze

#### What is born

- canonical blueprint for the target architecture
- boundary map for each domain

#### What can be reused

- the current audited code as evidence and temporary runtime source
- existing ADR glossary

#### What must be replaced

- any conceptual assumption that current module boundaries are final

#### Runtime invariants

- gameplay start still works
- run end still produces a result
- pause still suspends gameplay

### Phase 2 - Gameplay core and post-run separation

#### What is born

- clean ownership split between `GameLoop` and `PostGame`
- single post-run handoff semantics

#### What can be reused

- `GameLoopStateMachine`
- `GameRunOutcomeService`
- `PostStage` control flow

#### What must be replaced

- post-run ownership inside flow state owners
- direct visual ownership inside gameplay core

#### Runtime invariants

- `Playing` remains the active flow state
- `Victory` / `Defeat` still reach post-run
- restart and exit-to-menu still function

### Phase 3 - LevelFlow and navigation normalization

#### What is born

- one canonical restart path
- one canonical intent-to-route dispatch path

#### What can be reused

- `LevelFlowRuntimeService`
- `GameNavigationService`
- route/intents catalogs

#### What must be replaced

- duplicated restart semantics
- duplicated exit semantics

#### Runtime invariants

- level selection still works
- gameplay can restart
- menu return still works

### Phase 4 - Audio boundary independence

#### What is born

- audio-owned contextual precedence
- explicit consumer bridges

#### What can be reused

- existing audio runtime and asset model where already canonical

#### What must be replaced

- any navigation-owned audio policy
- any cross-domain audio ownership inside gameplay flow

#### Runtime invariants

- BGM continuity
- contextual cues
- entity audio playback

### Phase 5 - UI and presenter cleanup

#### What is born

- local visual contexts with intent emission only

#### What can be reused

- overlay/panel controllers as UI surfaces

#### What must be replaced

- UI as owner of gameplay/post-run semantics

#### Runtime invariants

- buttons still emit the same intents
- overlays still show/hide correctly
- focus and gate behavior remain stable

## 8. Reuse / Recreate / Adapter / Discard Rules

### Reuse when

- the current piece already expresses the correct domain role.
- the current piece can be simplified without changing semantics.
- the current piece is a thin bridge or projection.

### Recreate when

- the current piece mixes two domain roles that cannot be untangled safely.
- the current piece is conceptually wrong even if it works.
- the current piece exists only to preserve legacy shape.

### Adapters are forbidden when

- they would preserve a wrong ownership boundary indefinitely.
- they would hide a domain mismatch behind compatibility.
- they would become the permanent answer to a conceptual bug.

### Old names must be discarded when

- they encode a legacy technical phase as if it were a domain concept.
- they obscure whether something is result, intent, context or bridge.
- they force consumers to reason about implementation details instead of domain meaning.

## 9. Final Verdict

Blueprint pronto para execucao planejada.

Este documento define a arquitetura ideal do Baseline 4.0 sem depender da organizacao atual como contrato. O legado continua util como evidencia e como fonte de reaproveitamento, mas nao define o desenho final.
