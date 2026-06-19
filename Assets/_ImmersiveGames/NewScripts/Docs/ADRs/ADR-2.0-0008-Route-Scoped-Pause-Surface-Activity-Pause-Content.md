# ADR-2.0-0008 — Route-Scoped Pause Surface, Activity Pause Content e SessionActivity Pause Lifecycle

## Status

Aceito como decisão arquitetural da Base 2.0.

Cortes `PAUSE-0` a `PAUSE-6` fechados como baseline funcional validado em 2026-06-19, com compile/runtime exercitado por smoke/log.

Nenhum corte runtime futuro deste ADR deve ser marcado como `PASS` sem compile + smoke/log específico.

Este ADR fecha o desenho canônico para pause sem usar `Time.timeScale = 0` como owner de pausa global e sem colocar o pause como produto da Activity.

---

## Área

```text
SessionOperational
SessionActivity
ActivityEntryPipeline
InputModes
UIGlobal / PauseOverlay
Route Presentation
Activity Content Contributions
SceneComposition / additive UI scenes
```

---

## Fonte normativa local

Este ADR complementa:

```text
ADR-2.0-0001 — SessionOperational Ownership Stabilization e Regra Anti-Deslocamento
ADR-2.0-0002 — SessionActivity Ownership Decomposition e ActivityEntryPipeline
ADR-2.0-0003 — PlayerParticipation, PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary
ADR-2.0-0004 — SA-IDREF Typed Runtime References e PlayerActor Runtime Identity
SessionActivity/Pipeline/README.md — Estado real congelado: pause/resume local existe com SimulationGate
Modularity-Facilitation-of-New-Components.md — CompositionDescriptor, adapters, contracts, stages e explicit ownership
```

Em caso de conflito, este ADR prevalece para a fronteira entre rota, activity e pause UI.

---

## Contexto

O sistema já possui um esqueleto funcional de pause no domínio de `SessionActivity`:

```text
ActivityExecutionState.Paused
SessionActivityCommandKind.PauseRequested
SessionActivityCommandKind.ResumeRequested
SessionActivityFactKind.SimulationPaused
SessionActivityFactKind.SimulationResumed
SessionActivityFactKind.PauseResolved
SessionActivityFactKind.ResumeResolved
SessionActivitySimulationGate
ISessionActivityInputModeAdapter
ISessionActivityPauseOverlayAdapter
PauseOverlayAdapter
InputModeAdapter
```

O caminho atual confirma a decisão estrutural correta:

```text
SessionActivityPipeline decide pause/resume.
SimulationGate bloqueia/libera simulação de activity.
InputModeAdapter aplica modo PauseOverlay/ActivityGameplay.
PauseOverlayAdapter mostra/esconde overlay.
```

O problema restante não é o lifecycle de pause em si. O problema é onde colocar o conteúdo visual e como carregá-lo sem amarrá-lo à Activity.

A intenção funcional é:

```text
A rota traz o palco/surface estável do pause.
A Activity pode fornecer conteúdo contextual opcional para esse palco.
O pause funciona durante qualquer Activity da rota.
A surface de pause não é destruída em troca de Activity.
O menu de pause deve ficar sobre todas as outras surfaces.
O menu precisa continuar funcional durante o pause.
```

---

## Problema

Colocar o pause como conteúdo da Activity é incorreto porque cria estes riscos:

```text
activity_01 carrega pause;
activity_02 recarrega ou perde pause;
restart pode duplicar overlay;
release de ActivityContent pode destruir o menu;
cada Activity passa a declarar uma estrutura que é da rota;
o owner de lifecycle de pause fica ambíguo;
UIGlobal pode virar manager/lifecycle owner por acidente.
```

Colocar o pause inteiro em `SessionOperationalPipeline` também é incorreto porque o Operational não decide eventos interativos durante `ActivityRunning`.

Colocar o pause inteiro em `UIGlobal` é incorreto porque UI surface não decide lifecycle semântico.

A fronteira correta precisa separar:

```text
quem carrega a estrutura persistente;
quem contribui conteúdo contextual;
quem decide pause/resume;
quem executa show/hide;
quem bloqueia gameplay;
quem aplica input mode.
```

---

## Decisão central

Pause será dividido em três camadas:

```text
Route Pause Surface
Activity Pause Content Contribution
SessionActivity Pause Lifecycle
```

### 1. `Route Pause Surface` é route-scoped

A rota declara e materializa a estrutura estável do pause.

Exemplos:

```text
UIGlobal / PauseSurface additive scene
Canvas base
EventSystem / UI input surface quando aplicável
sorting/camera overlay root
slots para conteúdo contextual
containers de menu
managers locais de UI do pause, desde que não decidam lifecycle
```

Owner correto:

```text
SessionOperationalPipeline decide quando preparar/liberar a surface da rota.
Operational stage executa o passo determinístico.
Adapter carrega/descarrega a cena additive ou surface técnica.
```

A surface não decide pause/resume.

### 2. `Activity Pause Content Contribution` é activity-scoped opcional

A Activity pode preencher slots da surface de pause.

Exemplos:

```text
mapa da Activity
objetivos da Activity
dicas específicas
inventário contextual
painel de boss
submenu especial opcional
```

Owner correto:

```text
ActivityEntryPipeline decide setup da contribuição da entry.
ActivityEntryPauseContentStage executa bind/load determinístico.
Activity exit/release remove a contribuição da Activity atual.
```

A Activity não carrega a shell do pause.

### 3. `SessionActivityPipeline` decide pause/resume

O pause runtime continua pertencendo ao lifecycle macro da Activity.

Permitido ao `SessionActivityPipeline`:

```text
validar ActivityRunning;
validar SessionActivityIdentity;
validar route pause policy resolvida;
rejeitar stale/foreign;
bloquear/liberar SimulationGate;
alterar ActivityExecutionState Running/Paused;
comandar InputModeAdapter;
comandar PauseOverlayAdapter;
emitir facts/snapshots.
```

Proibido ao `SessionActivityPipeline`:

```text
carregar cena UIGlobal diretamente;
materializar conteúdo visual de pause;
procurar slots por cena;
executar side-effects visuais;
reconstruir RoutePauseSurfaceHandle por lookup textual.
```

---

## Modelo canônico

```text
RouteDefinition
-> RoutePauseSurfaceProfile
-> SessionOperationalPipeline
-> OperationalRoutePauseSurfaceStage
-> RoutePauseSurfaceAdapter
-> RoutePauseSurfaceHandle
-> SessionActivityEntryHandoff
-> SessionActivityPipeline

ActivityDefinition / ActivityContent
-> ActivityPauseContentProfile optional
-> ActivityEntryPipeline
-> ActivityEntryPauseContentStage
-> ActivityPauseContentBinding
-> RoutePauseSurface slots

PauseRequested
-> SessionActivityPipeline
-> SimulationGate BlockActivityExecution
-> InputModeAdapter PauseOverlay
-> PauseOverlayAdapter Show(RoutePauseSurfaceHandle)

ResumeRequested
-> SessionActivityPipeline
-> PauseOverlayAdapter Hide(RoutePauseSurfaceHandle)
-> InputModeAdapter ActivityGameplay
-> SimulationGate ReleaseActivityExecution
```

---

## Conceitos normativos

### `RoutePauseSurfaceProfile`

Authoring data da rota.

Contém a declaração da surface persistente de pause:

```text
pauseEnabled
surfaceMode: None | AdditiveScene
surfaceSceneRef
preloadPolicy: WithRoute
overlayRootId ou root marker
inputModeOnPause
inputModeOnResume
slotDefinitions[]
required
```

Regra:

```text
Se pauseEnabled=true e a surface obrigatória estiver ausente, falhar explicitamente.
Se pauseEnabled=false, comandos de pause devem ser rejeitados com pause_disabled_by_route_policy.
```

### `RoutePauseSurfaceHandle`

Runtime handle técnico produzido pela rota.

Contém referência runtime resolvida, não lookup textual solto:

```text
RouteIdentity
surfaceId
surfaceSceneRuntimeReference
root endpoint/marker resolvido
slot registry técnico
isLoaded
isVisible
```

O handle é snapshot/estado técnico. Não decide lifecycle.

### `ActivityPauseContentProfile`

Authoring data opcional da Activity.

Exemplos:

```text
contentMode: None | AdditiveScene | Prefab | Provider
sceneRef ou prefabRef
targetSlotId
required
releasePolicy: OnActivityExit
```

Regra:

```text
Activity sem conteúdo de pause emite skip explícito.
Activity com conteúdo opcional ausente emite skip explícito.
Activity com conteúdo obrigatório ausente falha a entry.
```

### `ActivityPauseContentBinding`

Registro runtime da contribuição ativa da Activity.

Contém:

```text
SessionActivityIdentity
ActivityId
EntrySequence
RoutePauseSurfaceHandle reference
slotId
contentRuntimeReference
source
reason
```

A binding é removida no exit/release da Activity ou no restart conforme policy.

---

## Ownership por categoria

| Item | Categoria | Owner correto |
|---|---|---|
| `RoutePauseSurfaceProfile` | Authoring data | Route authoring / Operational resolution |
| `RoutePauseSurfaceHandle` | Runtime handle/snapshot técnico | Operational route runtime state |
| Carregar scene `UIGlobal/PauseSurface` | Adapter side-effect | `RoutePauseSurfaceAdapter` comandado por Operational stage |
| Ordem de preload/release da surface | Pipeline/stage order | `SessionOperationalPipeline` |
| `ActivityPauseContentProfile` | Authoring data opcional | Activity authoring/content |
| Bind/load de conteúdo contextual | Entry stage | `ActivityEntryPipeline` -> `ActivityEntryPauseContentStage` |
| `PauseRequested`/`ResumeRequested` | Runtime command | Producer emite; `SessionActivityPipeline` decide |
| `ActivityExecutionState.Paused` | Macro lifecycle state | `SessionActivityPipeline` |
| Bloqueio de gameplay | Gate side-effect | `SessionActivitySimulationGate` |
| Aplicar `PauseOverlay`/`ActivityGameplay` | Input side-effect | `InputModeAdapter` / InputModes |
| Mostrar/esconder overlay | Visual side-effect | `PauseOverlayAdapter` |
| UI buttons de pause/resume | Endpoint/input producer | UI emite command; não decide lifecycle |
| UIGlobal/PauseSurface | Surface/endpoint visual | Não é lifecycle owner |

---

## Regras de lifecycle

### Entrada de rota com Activity

```text
1. SessionOperationalPipeline resolve RoutePauseSurfaceProfile.
2. OperationalRoutePauseSurfaceStage prepara a surface se pauseEnabled=true.
3. RoutePauseSurfaceAdapter carrega additive scene/surface e retorna handle.
4. Handoff para SessionActivity carrega RoutePauseSurfaceHandle ou RoutePauseContext.
```

### Entrada de Activity

```text
1. ActivityEntryPipeline recebe contexto de pause resolvido.
2. ActivityEntryPauseContentStage avalia ActivityPauseContentProfile.
3. Se não houver conteúdo, emite skip explícito.
4. Se houver conteúdo, carrega/binda no slot da RoutePauseSurface.
5. Binding fica correlacionada com SessionActivityIdentity + EntrySequence.
```

### Pause

```text
1. Command chega ao SessionActivityPipeline.
2. Pipeline valida ActivityRunning, policy, identity e state.
3. Pipeline comanda SimulationGate.BlockActivityExecution.
4. Pipeline muda ActivityExecutionState para Paused.
5. Pipeline comanda InputModeAdapter.PauseOverlay.
6. Pipeline comanda PauseOverlayAdapter.Show.
7. Pipeline emite facts/snapshots.
```

### Resume

```text
1. Command chega ao SessionActivityPipeline.
2. Pipeline valida Paused, policy e identity.
3. Pipeline comanda PauseOverlayAdapter.Hide.
4. Pipeline comanda InputModeAdapter.ActivityGameplay.
5. Pipeline muda ActivityExecutionState para Running.
6. Pipeline comanda SimulationGate.ReleaseActivityExecution.
7. Pipeline emite facts/snapshots.
```

### Troca de Activity

```text
RoutePauseSurface permanece carregada.
ActivityPauseContentBinding da Activity anterior é liberada.
Nova Activity pode registrar nova contribuição opcional.
Se a nova Activity não tiver contribuição, a surface continua com shell padrão.
```

### RouteExit

```text
ActivityPauseContentBinding ativa é liberada.
RoutePauseSurface é escondida se visível.
RoutePauseSurface é descarregada pelo Operational/route release owner.
```

---

## Não objetivos

Este ADR não implementa:

```text
novo sistema genérico de UI;
PauseManager global;
Time.timeScale como mecanismo canônico;
runtime join/rebind de players durante pause;
settings complexos;
save/load no pause menu;
multiplayer split-screen pause policy avançada;
PauseWindowTemplateLibrary genérica;
conteúdo de pause obrigatório para toda Activity.
```

---

## Proibido

```text
Activity carregar/destruir UIGlobal ou shell do pause.
UIGlobal decidir se o jogo está pausado.
Operational decidir PauseRequested em ActivityRunning.
InputModes decidir lifecycle de pause.
Adapter decidir pauseEnabled/disabled.
Criar GlobalPauseManager ou PauseCoordinator como owner de lifecycle.
Criar fallback silencioso quando surface obrigatória estiver ausente.
Comparar route/activity/surface ids por string para decidir ownership runtime.
Recriar RoutePauseSurface a cada Activity.
Destruir pause surface no release de ActivityContent.
```

---

## Permitido

```text
Route declarar surface de pause.
Route carregar additive scene de UIGlobal/PauseSurface.
Activity contribuir mapa/painel/objetivos opcionais.
ActivityEntryPipeline bindar contribuição em slot tipado.
SessionActivityPipeline rejeitar pause fora de ActivityRunning.
SimulationGate bloquear gameplay sem congelar UI.
InputModes aplicar PauseOverlay.
PauseOverlayAdapter mostrar/esconder root visual já carregado.
```

---

## Facts/logs esperados

Nomes finais podem variar, mas o smoke deve observar eventos equivalentes:

```text
RoutePauseSurfaceResolveStarted
RoutePauseSurfaceResolved
RoutePauseSurfaceLoadStarted
RoutePauseSurfaceLoaded
RoutePauseSurfaceRegistered
ActivityPauseContentResolveStarted
ActivityPauseContentSkippedNoContribution
ActivityPauseContentLoadStarted
ActivityPauseContentBound
ActivityPauseContentReleased
PauseResolved
ResumeResolved
SimulationPaused
SimulationResumed
InputModeApplied mode='PauseOverlay'
InputModeApplied mode='ActivityGameplay'
PauseOverlayShown
PauseOverlayHidden
RoutePauseSurfaceReleased
```

Rejeições esperadas:

```text
pause_disabled_by_route_policy
pause_surface_missing_required
unexpected_stage
stale_or_foreign_command
simulation_already_paused
simulation_not_paused
pause_content_required_missing
pause_surface_slot_missing
```

---

## Critério de aceite arquitetural

Um corte deste ADR só pode ser aceito como `PASS arquitetural` quando:

```text
RoutePauseSurface é route-scoped.
ActivityPauseContent é contribution opcional da entry.
SessionActivityPipeline continua owner de pause/resume lifecycle.
Operational apenas prepara/libera surface de rota.
Activity não carrega shell do pause.
UIGlobal não decide lifecycle.
Adapters não decidem policy.
SimulationGate bloqueia gameplay sem parar UI global.
Sem fallback silencioso.
Sem manager/coordinator genérico.
Sem string lookup cross-domain como owner runtime.
Logs mostram owner correto.
```

---

## Smoke mínimo

```text
Boot -> Menu
Menu -> Sandbox route com RoutePauseSurfaceProfile habilitado
Operational carrega RoutePauseSurface
Activity 01 entry
ActivityPauseContentBound ou ActivityPauseContentSkippedNoContribution
CompleteActivationWindow
ActivityRunning
PauseRequested
SimulationPaused
InputMode PauseOverlay aplicado
PauseOverlayShown
ResumeRequested
SimulationResumed
InputMode ActivityGameplay aplicado
PauseOverlayHidden
CompleteCurrentActivity / activity_01 -> activity_02
RoutePauseSurface permanece carregada
ActivityPauseContent da activity_01 é liberado
Activity 02 registra contribuição ou skip explícito
Pause/Resume funciona em activity_02
BackToMenu / RouteExit
RoutePauseSurfaceReleased
```

Critérios gerais:

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
sem fallback silencioso
sem destruição da RoutePauseSurface durante Activity transition
```

---

## Registro de implementação — PAUSE-0 a PAUSE-6

### Baseline congelado

```text
Baseline PAUSE-6 — Route-scoped Pause Surface + Activity Pause Content
Data: 2026-06-19
Status: CLOSED / PASS funcional
```

### Cortes fechados

| Corte | Status | Resultado |
|---|---|---|
| `PAUSE-0` | CLOSED | ADR e plano criados. |
| `PAUSE-1` | CLOSED / AUDITED | Auditoria confirmou lifecycle de pause no `SessionActivityPipeline` e surface/load ainda global/route composition. |
| `PAUSE-2` | CLOSED | Contratos passivos e authoring data criados. |
| `PAUSE-3` | CLOSED / PASS funcional | `UIGlobal_PauseSurfaceScene` passou a ser carregada/descarregada como route-owned scene via composition operacional. |
| `PAUSE-4` | CLOSED / PASS funcional | `RoutePauseSurfaceContext` passou pelo handoff e foi aceito pelo `SessionActivityPipeline`. |
| `PAUSE-5` | CLOSED / PASS funcional | `PauseOverlayAdapter` e `InputModeAdapter` deixaram de ser no-op para o fluxo exercitado. |
| `PAUSE-5A` | CLOSED / PASS funcional | `PauseToggle` canônico foi exposto por QA GUI e input action `PauseToggle`. |
| `PAUSE-5A-FIX1` | CLOSED / PASS funcional | `activityContentRoot` ficou opcional para `Show/Hide` do overlay. |
| `PAUSE-6` | CLOSED / PASS funcional | `ActivityPauseContentContribution` foi bindado/liberado pela `ActivityEntryPipeline` sem mover ownership do shell. |
| `PAUSE-6-FIX1` | CLOSED | Correção de assinatura de `ActivityEntryPipeline` contra `IActivityEntryPipeline`. |

### Evidência do smoke final

Smoke final validou:

```text
ActivityPauseContentBound para activity_01.
ActivityPauseContentReleased ao sair/reiniciar/trocar de activity.
ActivityPauseContentSkippedNoContribution para activity_02.
PauseOverlayShown e PauseOverlayHidden em ActivityRunning.
InputMode PauseOverlay aplicado no pause.
InputMode Gameplay aplicado no resume.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
UIGlobal_PauseSurfaceScene descarregada no BackToMenu.
```

Critérios de rejeição observados no fechamento:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem RejectedForeign
sem RejectedStale
sem SessionActivityRouteExitWithoutCanonicalDeactivation
```

### Owners confirmados

| Responsabilidade | Owner fechado |
|---|---|
| Load/release da `UIGlobal_PauseSurfaceScene` | `SessionOperationalPipeline` via route scene composition. |
| Contexto runtime da surface | Handoff Operational -> SessionActivity, aceito pelo `SessionActivityPipeline`. |
| Bind/release de conteúdo contextual | `ActivityEntryPipeline` / `ActivityEntryPauseContentStage`. |
| Instanciar/destruir conteúdo em slots | `ActivityPauseContentAdapter` + `RoutePauseSurfaceEndpoint`. |
| Pause/resume runtime | `SessionActivityPipeline`. |
| Bloqueio/liberação de gameplay | `SessionActivitySimulationGate`. |
| Show/hide visual | `PauseOverlayAdapter` + `RoutePauseSurfaceEndpoint`. |
| Input mode | `InputModeAdapter` -> `InputModeCoordinator` -> `IInputModeService`. |
| Produção do comando de pause | QA GUI/input action como producer; não owner de lifecycle. |

### Decisão consolidada

O shape final aceito para esta fase é:

```text
Route owns pause shell.
Activity optionally contributes pause content.
SessionActivity owns pause lifecycle.
Adapters execute commanded side-effects.
```

Não foi introduzido:

```text
PauseManager
PauseCoordinator
Time.timeScale como owner
UIGlobal lifecycle owner
Activity carregando/destruindo shell de pause
fallback silencioso para surface/content obrigatório
owner duplicado de pause/resume
```

### Débitos explícitos pós-PAUSE-6

```text
InputMode PauseOverlay ainda aparece como state_only em Base 1.1 operational scope.
Decidir em corte futuro se PauseOverlay deve trocar action map para UI de forma completa ou permanecer state-only com input action dedicada.
Hardening futuro pode validar pause aberto durante RouteExit, desde que sem mover lifecycle para UI/Operational.
Observabilidade pode ser reduzida depois que os eventos canônicos forem estabilizados.
```

---

## Conclusão

O pause canônico da Base 2.0 é:

```text
Route-scoped surface
+ Activity-scoped optional content
+ SessionActivity-scoped pause lifecycle
+ adapters/gates/input modes executando side-effects comandados
```

A rota carrega o palco.
A Activity pode preencher o palco com conteúdo contextual.
O `SessionActivityPipeline` decide quando o jogo pausa ou volta.
O UIGlobal/PauseSurface apenas apresenta a interface.
