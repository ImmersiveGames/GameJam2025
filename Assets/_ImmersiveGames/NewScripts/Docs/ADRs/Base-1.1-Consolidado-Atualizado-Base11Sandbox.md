# Base 1.1 — Consolidado Atualizado: Base11Sandbox / SessionOperationalPipeline

## Status

Congelado como documento consolidado de trabalho após validação do ciclo mínimo `Base11Sandbox`.

Este arquivo substitui, para o recorte atual, os miniADRs e documentos parciais anteriores que estavam desatualizados em relação ao checkpoint validado.

Ele **não substitui** os ADRs normativos da Base 1.1 (`ADR-0060` a `ADR-0067`).

---

## Fontes consolidadas

Foram consolidados e atualizados os seguintes materiais de trabalho:

- `Base-1.1-Matriz-Inicial-de-Migracao.md`
- `Base-1.1-Plano-de-Migracao-Pipeline-Convergence.md`
- `Base-1.1-Fase-3-Debitos.md`
- `MiniADR-Base11Sandbox-Navigation-Transition-v0.md`
- `SessionOperationalPipeline-RouteTransitionPipeline-v0.md`
- `SessionOperationalPipeline-TransitionEnvelope-v0.md`
- `SessionPipeline-Loading-e-ReadyToOpenCurtain-v0.md`
- `Checkpoint-Base11Sandbox-Ciclo-Minimo-Validado.md`

Quando há conflito entre esses documentos, prevalece o estado validado no checkpoint do `Base11Sandbox`.

---

## Fonte normativa

A fonte normativa permanece:

```text
ADR-0060 em diante.
```

ADRs anteriores devem ser tratados como histórico, salvo quando explicitamente citados pelos ADRs vivos da Base 1.1.

---

## Decisão consolidada

A Base 1.1 está sendo validada por um recorte controlado chamado:

```text
Base11Sandbox
```

Esse recorte não tenta preservar o legado funcionando por compatibilidade. O objetivo é validar o novo shape arquitetural mínimo:

```text
Boot
-> Menu
-> SessionActivitySandboxScene
-> Start Activity 01
-> Pause
-> Resume
```

O contrato vivo deste checkpoint é:

```text
SessionOperationalPipeline
-> coordena rota/transição/setup operacional antes de activities

SessionActivityPipeline
-> coordena o ciclo interno da activity

Adapters técnicos
-> executam side-effects comandados pelos pipelines
```

---

## Composition Profile validado

O profile ativo validado é:

```text
Base11Sandbox
```

A composição mínima validada é:

```text
RuntimePolicy
Pooling
Gates
InputModes
SceneFlow
Navigation
SessionOperationalNavigation
SceneComposition
```

Ficam fora do profile mínimo:

```text
GameLoop
SessionIntegration legacy
PhaseDefinition
Gameplay legacy
ActorsSystem
WorldReset
Save
RunEndRail / PostRun services
IntroStage obrigatório
```

Essa exclusão é intencional. Não é débito de composição quebrada; é descontaminação do sandbox.

---

## Ciclo mínimo validado

O smoke validou:

```text
Boot
-> Menu
-> SessionActivitySandboxScene
-> Start Activity 01
-> Pause
-> Resume
```

Validações observadas:

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

---

# 1. SessionOperationalPipeline

## Papel final

`SessionOperationalPipeline` é o pipeline determinístico que coordena a operação de rota/sessão antes de qualquer `Session Activity`.

Ele não é pipeline de activity.

Ele é dono de:

```text
Navigation intention
Route resolution
Transition request
Transition lifecycle observation
Operational setup
ReadyToOpenCurtain
Operational completion
Pipeline Identity da rota/transição
```

Ele não deve:

```text
escolher activity
iniciar SessionActivityPipeline
produzir ActivityHandoff
conhecer activity_01/activity_02 como regra arquitetural
substituir SceneFlow como executor físico
substituir Navigation como catálogo técnico
```

---

## Stages/facts validados

O ciclo atual validado emite:

```text
RouteOperationStarted
NavigationIntentObserved
RouteResolved
TransitionRequested
TransitionStarted
CurtainClosed
PreviousRouteTeardownSkipped
RoutePhysicalApplyObserved
ScenesReadyObserved
SessionOperationalSetupNoOp
InputCapabilityPrepared
InitialInputModePrepared
PauseCapabilityPrepared
ReadyToOpenCurtain
TransitionCompletedObserved
Completed
```

Esses nomes são a superfície observável atual do v0.

---

## Identidade operacional

Cada `SceneTransitionStartedEvent` abre uma nova `Pipeline Identity` ativa.

Identidade mínima validada:

```text
routeId
routeProfileId
activeScene
transitionSequence
transitionId
routeOperationId
source
reason
stage
```

Regra congelada:

```text
Eventos foreign/stale não podem alterar o pipeline ativo.
```

Eventos posteriores da mesma `transitionId` são aceitos pela identidade ativa. `SceneTransitionCompletedEvent` fecha a operação e libera a próxima transição.

---

# 2. SessionOperationalNavigation v0

## Decisão

`SessionOperationalNavigation v0` nasce como rail genérico de navegação/transição da Base 1.1.

Fluxo validado:

```text
Navigation intention
-> NavigateToRoute(routeId)
-> RouteResolved
-> RequestRouteTransition
-> ISessionOperationalTransitionPort
-> SceneFlowSessionOperationalTransitionAdapter
```

Regra central:

```text
Pipeline decide.
Adapter executa.
```

---

## Boot -> Menu

O primeiro producer validado é:

```text
Base11SandboxStartupNavigationProducer
```

Ele observa:

```text
BootStartPlanRequestedEvent
```

e emite:

```text
NavigateToRoute('to-menu')
```

Esse é o único hardcode temporário aceito neste checkpoint.

---

## Menu -> Sandbox

A rota `to-session-activity-sandbox` foi validada fisicamente e observada pelo `SessionOperationalPipeline`.

Observação importante:

```text
Menu -> Sandbox ainda é disparado pelo botão/menu através do caminho técnico atual de Navigation.
```

O `SessionOperationalPipeline` já observa e fecha a transição, mas o producer semântico de intenção de menu ainda deve ser formalizado depois.

Ação futura:

```text
criar producer/adapter de UI para emitir NavigateToRoute(routeId)
em vez de deixar o botão depender diretamente do caminho técnico legado.
```

---

# 3. SceneFlow e Navigation

## Papel atual no checkpoint

`SceneFlow` e `Navigation` continuam existindo no profile mínimo como executores técnicos.

Papel correto:

```text
Pipeline Adapter / executor técnico temporário
```

Eles podem executar:

```text
route catalog lookup
route definition/profile resolution
transition style resolution
fade/load/unload/active scene
SceneTransitionStartedEvent
SceneTransitionScenesReadyEvent
SceneTransitionCompletedEvent
```

Eles não devem decidir lifecycle de sessão, run ou activity.

---

## Adapter validado

O adapter técnico atual é:

```text
SceneFlowSessionOperationalTransitionAdapter
```

Ele implementa a porta semântica:

```text
ISessionOperationalTransitionPort
```

Essa separação é obrigatória: o pipeline não deve depender diretamente de `SceneFlow` concreto.

---

## Resíduos de nomenclatura

Ainda aparecem logs com boundaries antigos:

```text
boundary='Reset'
boundary='SessionIntegration'
```

No `Base11Sandbox`, esses nomes são resíduos observáveis do `SceneFlow` legado e não representam owners ativos.

Ação futura:

```text
higienizar logs/boundaries de SceneFlow para refletir SessionOperationalPipeline,
sem restaurar SessionIntegration/WorldReset.
```

---

# 4. SessionActivityPipeline

## Papel final

`SessionActivityPipeline` é o owner do ciclo interno de activity.

O checkpoint validou:

```text
PipelineStarted
ActivationEntered
GameplayRunningEntered
PauseResolved
ResumeResolved
```

A activation válida do sandbox é:

```text
SessionActivityPipeline.ActivationEntered
```

Não é:

```text
IntroStageCoordinator
IntroStagePresenterHost
IntroStageLifecycle*
```

---

## Pause / Resume

Pause/resume passam pelo caminho canônico:

```text
SessionActivityPipeline
-> SimulationGate
-> SessionActivityPauseOverlayAdapter
-> SessionActivityInputModeAdapter
```

`SimulationGate` executa bloqueio/liberação comandada pelo pipeline.

`SessionActivityInputModeAdapter` ainda opera como:

```text
observed_noop
```

Isso é aceito no checkpoint, mas não é o formato final.

---

# 5. Módulos legados cortados ou inertizados

## GameLoop

O `GameLoop` não participa do ciclo mínimo do `Base11Sandbox`.

Cortados do caminho ativo:

```text
GameLoopInputCommandBridge
GameLoopCommands
IGameLoopCommands
IPauseCommands
LegacyPauseCompatibilityInstaller
GamePauseOverlayController
AudioPauseDuckingBridge
GameLoopInputDriver / ticker
GameLoopService / GameLoopStateMachine como owner/composer do sandbox
```

Decisão congelada:

```text
não preservar GameLoop só porque o legado ainda apontava para ele.
```

---

## IntroStage

`IntroStage` legado não é dependência obrigatória do sandbox.

A ausência de `IntroStage` no `Base11Sandbox` é válida.

A regra Base 1.1 permanece:

```text
IntroStage é Activation Stage / Pipeline Policy,
não owner de ativação.
```

No sandbox atual, a activation está representada por:

```text
SessionActivityPipeline.ActivationEntered
```

---

## ActorsSystem

`ActorsSystem` fica fora do profile mínimo.

Scene scopes sem actor set são explicitamente tratados como:

```text
actors_scope_skipped reason='base11_sandbox_no_actor_set'
```

Isso é `skip/no-content` explícito, não fallback silencioso.

---

## PostRun / RunDecision

`RunEndRail`, `RunDecision` services e `PostRun` services ficam fora do profile mínimo.

`PostRunOverlayController` ainda existe fisicamente em `UIGlobalScene`, mas está inertizado por profile:

```text
post_run_overlay_skipped reason='sandbox_no_run_end'
```

Não foram criados serviços fake e não foi restaurado `RunEndRail`.

Ação futura:

```text
remover fisicamente o resíduo de UI/prefab quando houver variante de cena/profile,
ou substituir por UI própria de activity quando esse escopo for aberto.
```

---

# 6. Débitos atualizados

## D01 — InputMode real comandado pelos pipelines

Status:

```text
Pendente
```

Hoje ainda existem dois pontos temporários:

```text
SceneFlowInputModeBridge direct-event
SessionActivityInputModeAdapter observed_noop
```

Direção correta:

```text
SessionOperationalPipeline
-> emite comando de InitialInputMode / RouteInputMode
-> InputModes executa

SessionActivityPipeline
-> emite comando de ActivityInputMode / PauseOverlay / ActivityGameplay
-> InputModes executa
```

Não criar owner paralelo no `InputModes`.

---

## D02 — Producer semântico para Menu -> Sandbox

Status:

```text
Pendente
```

A rota foi validada, mas a intenção ainda nasce do botão/menu usando o caminho técnico atual de Navigation.

Direção correta:

```text
UI/Menu Adapter
-> NavigateToRoute(routeId)
-> SessionOperationalNavigationService
-> RequestRouteTransition
```

O botão não deve virar owner de rota/transição.

---

## D03 — Remover no-op técnico de Actors/InputModes quando o escopo real existir

Status:

```text
Aceito temporariamente
```

Hoje:

```text
IActorsOperationalBindingQueryPort no-op no InputModesInstaller
```

Aceito apenas porque o `Base11Sandbox` não usa actor bindings.

Quando o escopo de actors/input real for aberto, isso deve ser substituído por contrato explícito ou fail-fast.

---

## D04 — Higienizar logs/boundaries legados do SceneFlow

Status:

```text
Pendente não bloqueante
```

Logs como:

```text
boundary='Reset'
boundary='SessionIntegration'
```

não representam owners ativos no sandbox, mas ainda geram ruído conceitual.

---

## D05 — PostRunOverlayController físico em UIGlobalScene

Status:

```text
Inertizado por profile
```

Não bloqueia o checkpoint, mas deve ser removido fisicamente quando houver cena/variante limpa para o sandbox.

---

## D06 — Base11SandboxTransitionCompletionGate no-content

Status:

```text
Aceito temporariamente
```

Ele libera a completion sem esperar `WorldReset` ou `GameplaySessionFlow`.

Isso está correto para o sandbox mínimo, mas deve evoluir para um `SessionOperationalPipeline` gate/command explícito quando setup real substituir o no-op.

---

# 7. Matriz atualizada de ownership

| Área | Situação anterior | Situação atual no Base11Sandbox | Owner correto | Status |
|---|---|---|---|---|
| Boot/start route | Ponte legada / startup route | `Base11SandboxStartupNavigationProducer` emite `NavigateToRoute('to-menu')` | `SessionOperationalPipeline` | Validado |
| Navigation | Serviço técnico podia parecer owner | Usado como executor técnico/catálogo | `SessionOperationalPipeline` decide; Navigation executa | Parcial / adapter |
| SceneFlow | Transição física + handshakes legados | Executa transição via `ISessionOperationalTransitionPort` | `SessionOperationalPipeline` decide; SceneFlow executa | Validado como adapter temporário |
| Route transition identity | Sequência/eventos stale quebravam rota seguinte | `SessionOperationalRouteTransitionBridge` abre/fecha identidade por transition | `SessionOperationalPipeline` | Validado |
| Session Activity | Sandbox scene-local | `SessionActivityPipeline` executa activity | `SessionActivityPipeline` | Validado |
| Pause/Resume | GameLoop/InputCommands/PauseOverlay legado | `SessionActivityPipeline -> SimulationGate` | `SessionActivityPipeline` decide; Gate executa | Validado v0 |
| InputMode | SceneFlow/InputModes e observed_noop | Temporário | Pipelines emitem comando; InputModes executa | Pendente |
| GameLoop | Owner/ticker/estado legado | Fora do profile | Nenhum no sandbox; pipelines substituem lifecycle | Cortado |
| IntroStage | Activation legado obrigatório | Fora do sandbox obrigatório | `SessionActivityPipeline` no sandbox; IntroStage só policy futura | Cortado/inertizado |
| ActorsSystem | Registro obrigatório causava fatal | `actors_scope_skipped` | Futuro Pipeline Adapter quando necessário | No-content explícito |
| RunEnd/PostRun | UI/serviços legados vazavam | Services fora; UI inertizada | Futuro Run Pipeline / Activity Outcome | Inertizado |
| WorldReset/Save | Rails globais legados | Fora do profile | Adapters futuros sob comando de pipeline | Cortado |

---

# 8. Próximo bloco recomendado

O próximo bloco natural é:

```text
InputMode real comandado pelos pipelines
```

Objetivo:

```text
remover SceneFlowInputModeBridge direct-event
remover observed_noop do SessionActivityInputModeAdapter
fazer InputModes aplicar comandos vindos de SessionOperationalPipeline e SessionActivityPipeline
```

Ordem sugerida:

1. MiniADR curto para `InputMode Pipeline Commands v0`.
2. `SessionOperationalPipeline` emite comando de input inicial por rota.
3. `SessionActivityPipeline` emite comandos reais de `PauseOverlay` e `ActivityGameplay`.
4. `InputModes` executa via adapter/port.
5. Remover ou inertizar `SceneFlowInputModeBridge`.

---

# 9. Regra de continuidade

Antes de qualquer novo patch, perguntar:

```text
Qual pipeline é dono desta decisão?
```

Se a resposta for `SceneFlow`, `Navigation`, `GameLoop`, `InputModes`, `Gate`, `PostRunOverlay`, `Menu Button` ou `Bootstrap`, a decisão está provavelmente no lugar errado.

Forma correta:

```text
Pipeline decide.
Module produz fact/command.
Adapter executa side-effect.
Executor técnico aplica estado/efeito.
```

---

## Checkpoint congelado

Estado congelado:

```text
Base11Sandbox profile: PASS
SessionOperationalNavigation v0: PASS
SessionOperationalRouteTransitionBridge identity: PASS
Boot -> Menu: PASS
Menu -> SessionActivitySandboxScene: PASS
SessionActivityPipeline Start: PASS
Pause/Resume: PASS
InputMode real: próximo bloco
```

---

## Checkpoint Base11Sandbox Minimal Route + Session Activity Cycle - PASS

O checkpoint validado e congelado da Base 1.1 para este recorte e:

```text
Base11Sandbox Minimal Route + Session Activity Cycle - PASS
```

Leitura executiva do checkpoint:

- `SessionOperationalRouteAsset` e o contrato minimo de rota operacional;
- `SessionOperationalPipeline` decide comando, completion e handoff;
- `Base11SandboxOperationalRouteTransitionAdapter` executa somente side-effects fisicos;
- `SceneCompositionExecutor` aplica a composicao em ordem segura;
- `SessionActivityPipeline` resolve a primeira activity e aloca `entrySequence`.

Este documento permanece como consolidacao de trabalho e evidencia textual do checkpoint congelado.
