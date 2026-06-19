# Plano de Execução — Route Pause Surface / Activity Pause Content

## Objetivo

Implementar o ADR-2.0-0008 sem criar trilho paralelo de pause, sem `PauseManager` global e sem transformar `UIGlobal` em owner de lifecycle.

O alvo é separar:

```text
RoutePauseSurface = estrutura persistente route-scoped
ActivityPauseContent = contribuição opcional por Activity entry
SessionActivityPipeline = owner de pause/resume lifecycle
SimulationGate/InputModes/PauseOverlayAdapter = side-effects comandados
```

---

## Premissas

```text
SessionActivityPipeline já possui PauseRequested/ResumeRequested.
ActivityExecutionState.Paused já existe.
SessionActivitySimulationGate já bloqueia/libera execução de Activity.
InputModeAdapter e PauseOverlayAdapter existem, mas ainda precisam virar adapters reais.
UIGlobalScene existe como scene reference no pacote atual.
O pause deve funcionar sem Time.timeScale = 0.
```

---

## Respostas anti-deslocamento

### Qual pipeline é dono desta decisão?

```text
SessionOperationalPipeline é dono de preparar/liberar RoutePauseSurface junto com lifecycle de rota.
ActivityEntryPipeline é dono de bindar/liberar ActivityPauseContent na entry.
SessionActivityPipeline é dono de pause/resume runtime durante ActivityRunning.
```

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
RoutePauseSurfaceProfile = authoring data.
RoutePauseSurfaceContext/Handle = runtime handle/snapshot técnico.
OperationalRoutePauseSurfaceStage = stage.
RoutePauseSurfaceAdapter = adapter.
ActivityPauseContentProfile = authoring data.
ActivityEntryPauseContentStage = stage.
ActivityPauseContentBinding = runtime binding/snapshot técnico.
PauseRequested/ResumeRequested = command.
PauseResolved/ResumeResolved = fact.
PauseOverlayAdapter/InputModeAdapter/SimulationGate = side-effect/gate adapters.
```

### Isso é comportamento final ou bridge transitória?

```text
Final como fronteira arquitetural.
A implementação pode usar adapters transitórios estreitos, mas não pode criar bridge agregada permanente.
```

### Essa compatibilidade ainda é necessária?

```text
Não preservar shape em que UIGlobal é carregado como conteúdo comum da rota sem contrato de pause.
Normalizar para RoutePauseSurfaceProfile.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

```text
Na fronteira arquitetural: shell de pause é route-scoped, conteúdo contextual é activity-scoped, lifecycle é SessionActivity-scoped.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
Risco existe se UIGlobal, Operational, Activity ou InputModes decidirem pause/resume.
O plano mantém decisão runtime exclusivamente no SessionActivityPipeline.
```

---

## Inventário inicial observado no pacote atual

```text
SessionActivity/Adapters/PauseOverlayAdapter.cs
SessionActivity/Adapters/InputModeAdapter.cs
SessionActivity/Simulation/SessionActivitySimulationGate.cs
SessionActivity/Contracts/SessionActivityContracts.cs
SessionActivity/Pipeline/SessionActivityPipeline.cs
Resources/SceneReferences/SceneKeys/UIGlobalScene.asset
```

Observação:

```text
PauseOverlayAdapter atual é show/hide por log.
InputModeAdapter atual observa command e registra noop.
SessionActivityPipeline já valida ActivityRunning, identity, already paused/not paused e comanda SimulationGate.
```

---

## Corte PAUSE-0 — ADR e plano

### Objetivo

Congelar documentação normativa antes de mexer em runtime.

### Ação

```text
Criar ADR-2.0-0008.
Criar este plano de execução.
Registrar que pause é RouteSurface + ActivityContribution + SessionActivityLifecycle.
```

### Aceite

```text
Sem alteração runtime.
Decisão de ownership clara.
Plano separado por cortes pequenos.
```

### Smoke

Não requer smoke por ser doc-only.

---

## Corte PAUSE-1 — Auditoria do estado atual

### Objetivo

Mapear o caminho atual de pause e do carregamento de UIGlobal antes da primeira alteração.

### Auditar

```text
Onde UIGlobalScene é carregada hoje.
Se a cena está presa ao route content, persistent scene, camera presentation ou outra surface.
Como PauseRequested chega ao SessionActivityPipeline.
Como SimulationGate bloqueia Movement/ObjectEmission/Projectiles.
Se InputModeAdapter precisa conversar com InputModes real.
Se PauseOverlayAdapter tem acesso a surface/root.
Como RouteExit libera cenas route-scoped atualmente.
```

### Saída esperada

Matriz:

```text
arquivo/classe/método
responsabilidade atual
owner correto
problema
severidade
ação recomendada
risco
evidência
```

### Não fazer

```text
Não implementar.
Não criar manager.
Não alterar assets.
Não mudar pause order.
```

---

## Corte PAUSE-2 — Contratos passivos e authoring data

### Objetivo

Criar os contratos mínimos sem side-effects.

### Escopo permitido

```text
RoutePauseSurfaceProfile / RoutePauseSurfacePolicy
RoutePauseSurfaceId
RoutePauseSurfaceSlotId
RoutePauseSurfaceHandle ou RoutePauseSurfaceContext
ActivityPauseContentProfile
ActivityPauseContentContribution
ActivityPauseContentBinding
Route pause enabled/disabled policy
```

### Regras

```text
Sem scene load ainda.
Sem show/hide real ainda.
Sem alteração no pause runtime além de aceitar payload resolvido, se necessário.
Sem fallback para string solta.
IDs textuais só para log/debug/asset serialization.
```

### Aceite

```text
Contratos compilam.
Nenhum side-effect novo.
Nenhum owner novo de lifecycle.
```

### Smoke

Compile já é obrigatório. Smoke funcional pode ser simples porque não há comportamento real novo.

---

## Corte PAUSE-3 — Operational RoutePauseSurface preload/release

### Objetivo

Fazer a rota preparar a shell/surface de pause como recurso route-scoped.

### Escopo permitido

```text
OperationalRoutePauseSurfaceStage
IRoutePauseSurfacePort / IRoutePauseSurfaceAdapter
RoutePauseSurfaceLoadCommand
RoutePauseSurfaceLoadResult
RoutePauseSurfaceReleaseCommand
RoutePauseSurfaceReleaseResult
RoutePauseSurfaceHandle storage no runtime state correto
Handoff de RoutePauseSurfaceContext para SessionActivity
```

### Ordem conceitual

```text
Route materialization/reveal prepara surface antes de ActivityRunning.
RouteExit/release descarrega surface depois que Activity finalizou/route exit iniciou.
```

### Não fazer

```text
Não mostrar overlay.
Não bindar conteúdo de Activity.
Não alterar PauseRequested/ResumeRequested ainda.
Não carregar surface pela Activity.
```

### Aceite arquitetural

```text
SessionOperationalPipeline mantém ordem.
Stage executa passo determinístico.
Adapter carrega/descarrega additive scene.
Adapter não decide pauseEnabled.
Surface obrigatória ausente falha explicitamente.
Surface opcional ausente gera skip explícito.
```

### Smoke obrigatório

```text
Boot -> Menu -> Sandbox
RoutePauseSurfaceLoadStarted/Loaded ou SkippedDisabled
CompleteActivationWindow
BackToMenu / RouteExit
RoutePauseSurfaceReleased quando carregada
sem FATAL/Exception/route_transition_failed
```

---

## Corte PAUSE-4 — Handoff e runtime context no SessionActivity

### Objetivo

Entregar ao `SessionActivityPipeline` o contexto de pause resolvido da rota.

### Escopo permitido

```text
SessionActivityEntryHandoff inclui RoutePauseSurfaceContext ou referência equivalente.
SessionActivityRuntimeState guarda contexto passivo.
SessionActivityPipeline valida pauseEnabled e surface disponível antes de aceitar pause.
Novas rejeições: pause_disabled_by_route_policy, pause_surface_missing_required.
```

### Não fazer

```text
Não carregar scene.
Não procurar UIGlobal por nome.
Não criar fallback se contexto ausente.
Não mudar ActivityEntry content.
```

### Aceite

```text
Pause em rota sem pause habilitado é rejeitado explicitamente.
Pause em rota com policy habilitada e surface carregada mantém comportamento atual.
Foreign/stale continua rejeitado.
```

### Smoke obrigatório

```text
PauseRequested em ActivityRunning com policy habilitada -> PauseResolved.
PauseRequested fora de ActivityRunning -> unexpected_stage.
PauseRequested em rota desabilitada -> pause_disabled_by_route_policy.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
```

---

## Corte PAUSE-5 — PauseOverlayAdapter real e InputModeAdapter real

### Objetivo

Transformar adapters de log/no-op em execução real comandada.

### Escopo permitido

```text
PauseOverlayAdapter recebe/resolve RoutePauseSurfaceHandle.
Show/Hide habilita root visual, canvas group, marker ou endpoint da surface.
InputModeAdapter chama InputModes real para PauseOverlay/ActivityGameplay.
Logs/facts preservados.
```

### Não fazer

```text
Não decidir pause/resume dentro do adapter.
Não carregar/descarregar cena no Show/Hide.
Não buscar por FindObjectOfType como contrato canônico.
Não usar Time.timeScale.
```

### Aceite

```text
PauseOverlayShown/Hidden observado.
InputModeApplied PauseOverlay/ActivityGameplay observado.
SimulationGate continua bloqueando/liberando gameplay.
UI continua funcional durante pause.
```

### Smoke obrigatório

```text
ActivityRunning
PauseRequested
SimulationPaused
InputMode PauseOverlay aplicado
PauseOverlayShown
ResumeRequested
PauseOverlayHidden
InputMode ActivityGameplay aplicado
SimulationResumed
sem FATAL/Exception/route_transition_failed
```

---

## Corte PAUSE-6 — ActivityPauseContentContribution opcional

### Objetivo

Permitir que Activities preencham slots da RoutePauseSurface com conteúdo contextual opcional.

### Escopo permitido

```text
ActivityPauseContentProfile em Activity authoring/content.
ActivityEntryPauseContentStage.
ActivityPauseContentResolver/Policy estreita.
ActivityPauseContentAdapter para load/bind/release de conteúdo.
ActivityPauseContentBinding runtime state.
Skip explícito para no-content.
```

### Exemplo funcional

```text
activity_01 -> MapPanel com PauseMap_Activity01.
activity_02 -> no pause content, skip explícito.
boss_activity -> BossHintsPanel.
```

### Não fazer

```text
Não carregar shell UIGlobal pela Activity.
Não exigir conteúdo de pause por padrão.
Não usar ActivityPauseContent como gate de pause.
Não criar inventário universal novo.
```

### Aceite

```text
Activity com contribution binda no slot correto.
Activity sem contribution emite ActivityPauseContentSkippedNoContribution.
Slot obrigatório ausente falha explicitamente.
Activity transition libera contribution anterior e mantém surface.
```

### Smoke obrigatório

```text
activity_01 ActivityPauseContentBound ou skipped explícito.
Pause/Resume funciona com conteúdo ativo.
CompleteCurrentActivity -> activity_02.
ActivityPauseContentReleased da activity_01.
RoutePauseSurface permanece carregada.
activity_02 ActivityPauseContentSkippedNoContribution ou Bound próprio.
Pause/Resume funciona na activity_02.
Activity01ToActivity02 PASS.
```

---

## Corte PAUSE-7 — Fonte canônica do comando de pause

### Objetivo

Sair do comando QA/manual como único produtor de pause.

### Opções aceitáveis após auditoria

```text
Global pause input source route-scoped.
PlayerActorCommandInputHub emitindo PauseRequested para SessionActivity command surface.
UIGlobal button emitindo ResumeRequested.
```

### Decisão inicial recomendada

```text
Começar com route-scoped pause input endpoint para comando PauseRequested.
Manter ResumeRequested também disponível por botão na PauseSurface.
Não prender pause a uma Activity específica.
```

### Não fazer

```text
Não criar PlayerPausePipeline.
Não criar PauseManager.
Não usar UnityEvent como contrato canônico.
Não deixar botão de UI alterar State.CurrentExecutionState diretamente.
```

### Aceite

```text
Input producer apenas emite command.
SessionActivityPipeline continua decidindo.
Button resume apenas emite ResumeRequested.
Commands carregam SessionActivityIdentity resolvida ou passam por boundary canônico para construir command.
```

### Smoke obrigatório

```text
Apertar input de pause em ActivityRunning pausa.
Botão Resume retorna para ActivityGameplay.
Input de pause fora de ActivityRunning é rejeitado.
Sem foreign/stale indevido.
```

---

## Corte PAUSE-8 — Release/RouteExit hardening

### Objetivo

Garantir limpeza correta em restart, activity transition e route exit.

### Escopo permitido

```text
ActivityPauseContentReleaseStage se necessário.
RoutePauseSurfaceRelease no Operational route exit/release.
Hide antes de unload se estiver visível.
Limpeza de bindings stale.
Facts de release.
```

### Não fazer

```text
Não criar ActivityExitPipeline só por simetria.
Não mover release macro sem auditoria.
Não descarregar RoutePauseSurface em Activity transition.
```

### Aceite

```text
RestartCurrentActivity não duplica PauseSurface.
Activity01ToActivity02 não destrói PauseSurface.
RouteExitBackToMenu descarrega PauseSurface.
Pause aberto durante RouteExit é escondido/liberado de modo determinístico.
```

### Smoke obrigatório

```text
Pause aberto -> Resume -> RestartCurrentActivity PASS.
Pause aberto -> RouteExitBackToMenu PASS ou fluxo rejeita/fecha pause explicitamente.
Activity01ToActivity02 PASS com surface retida.
RoutePauseSurfaceReleased no route exit.
sem fallback silencioso.
```

---

## Ordem recomendada

```text
PAUSE-0  ADR + plano
PAUSE-1  Auditoria do estado atual
PAUSE-2  Contratos passivos
PAUSE-3  RoutePauseSurface preload/release no Operational
PAUSE-4  Handoff/context no SessionActivity
PAUSE-5  PauseOverlayAdapter/InputModeAdapter reais
PAUSE-6  ActivityPauseContentContribution opcional
PAUSE-7  Fonte canônica de comando de pause
PAUSE-8  Release/RouteExit hardening
```

---

## Critério de PASS global do ADR

```text
sem error CS
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem foreign/stale indevido
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
RoutePauseSurface carregada pela rota
RoutePauseSurface não destruída na troca de Activity
ActivityPauseContent opcional por Activity
Pause/Resume funcional em activity_01 e activity_02
SimulationGate bloqueando gameplay
UI de pause continua funcional
InputModes alternando PauseOverlay/ActivityGameplay
PauseOverlayAdapter executando show/hide real
RouteExit libera surface
sem Time.timeScale como owner de pause
sem PauseManager/Coordinator
sem fallback silencioso
```

---

## Fechamento — PAUSE-0 a PAUSE-6

### Status final

```text
PAUSE-0..PAUSE-6 — CLOSED / PASS funcional
Baseline: PAUSE-6
Data: 2026-06-19
```

### Matriz de fechamento

| Corte | Status | Evidência / aceite |
|---|---|---|
| `PAUSE-0` | CLOSED | ADR e plano registrados. |
| `PAUSE-1` | CLOSED / AUDITED | Auditoria confirmou que o lifecycle canônico de pause já pertencia ao `SessionActivityPipeline`; o problema restante era surface/load/context. |
| `PAUSE-2` | CLOSED | Contratos passivos e authoring data criados sem side-effects. |
| `PAUSE-3` | CLOSED / PASS funcional | Pause surface entrou em `scenesToLoad` da rota e saiu em `scenesToUnload` no BackToMenu. |
| `PAUSE-4` | CLOSED / PASS funcional | `RoutePauseSurfaceContext` chegou ao `SessionActivityPipeline` e foi aceito. |
| `PAUSE-5` | CLOSED / PASS funcional | Overlay e input mode passaram a executar side-effects comandados. |
| `PAUSE-5A` | CLOSED / PASS funcional | `PauseToggle` passou a existir via QA GUI e input action. |
| `PAUSE-5A-FIX1` | CLOSED / PASS funcional | `activityContentRoot` deixou de bloquear `Show/Hide`, permanecendo obrigatório apenas para conteúdo. |
| `PAUSE-6` | CLOSED / PASS funcional | Activity content foi bindado/liberado em slot da surface; activity sem profile registrou skip explícito. |
| `PAUSE-6-FIX1` | CLOSED | Assinatura de `ActivityEntryPipeline.ExecuteCapabilityObjectSetup(...)` voltou a cumprir `IActivityEntryPipeline`. |

### Smoke final aceito

O último smoke aceito validou:

```text
ActivityPauseContentBound activity_01 requestedCount='1' boundCount='1'.
ActivityPauseContentReleased ao sair da contribution anterior.
ActivityPauseContentSkippedNoContribution para activity_02.
PauseOverlayShown / PauseOverlayHidden.
InputModeApplied PauseOverlay / Gameplay.
PauseToggleRequested por input action.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
UnloadSceneCompleted scene='UIGlobal_PauseSurfaceScene'.
```

Critérios negativos aceitos:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem foreign/stale indevido
sem SessionActivityRouteExitWithoutCanonicalDeactivation
```

### Owners congelados

```text
SessionOperationalPipeline: load/release da RoutePauseSurface com lifecycle de rota.
ActivityEntryPipeline: bind/release de ActivityPauseContent por entry.
SessionActivityPipeline: decisão pause/resume runtime.
SessionActivitySimulationGate: bloqueio/liberação da execução de Activity.
PauseOverlayAdapter/RoutePauseSurfaceEndpoint: show/hide visual comandado.
ActivityPauseContentAdapter/RoutePauseSurfaceEndpoint: instanciação/destruição do conteúdo contextual comandado.
InputModeAdapter/InputModes: aplicação de input mode comandada.
QA/InputAction: producer de comando, não owner de lifecycle.
```

### Fora do fechamento

```text
PAUSE-7 e PAUSE-8 permanecem como cortes futuros, não necessários para aceitar PAUSE-6.
Não há decisão final ainda sobre PauseOverlay trocar action map para UI ou continuar state_only com action dedicada.
Não há necessidade de novo manager/coordinator para pause.
```

### Próximo corte recomendado

Antes de abrir feature nova, escolher uma das opções:

```text
Opção A: PAUSE-7 — Hardening da fonte canônica de comando de pause/input mode.
Opção B: Observability hygiene do bloco Pause, preservando eventos canônicos.
Opção C: Retomar ADR0007/Actors depois de congelar este baseline como fonte atual.
```

