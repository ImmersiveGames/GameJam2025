# ADR-0004 — Checkpoint congelado: SessionActivity ActivityTransition MVP

Status: **PASS estrutural / MVP fechado**  
Data do checkpoint: **2026-05-18**  
Escopo: **Base 1.1 — Session Activity Pipeline**  
Tema: **Activity -> Activity transition, ActivitySetup nominal, NextActivitySetup nominal, fade/loading de ActivityTransition**

---

## 1. Decisão congelada

O `SessionActivityPipeline` possui agora um MVP funcional e validado para troca `Activity -> Activity` dentro da Base 1.1.

A transição entre activities segue a regra canônica:

```text
ActivityRunning
-> CompleteCurrentActivity
-> DeactivationWindow
-> ActivityDeactivated
-> ActivityTransition profile resolution
-> ActivityTransition fade/loading, quando aplicável
-> NextActivitySetup
-> ActivityHandoffPrepared
-> ContinueToNextActivity
-> próxima Activity entra pelo lifecycle local completo
```

O `SessionActivityPipeline` é o owner da decisão de lifecycle, ordem, identidade, setup, handoff, reveal-safe point e rejeição de eventos/comandos `foreign/stale`.

Adapters executam side-effects comandados pelo pipeline:

- fade;
- loading;
- scene loading/unloading das windows aditivas;
- side-effects técnicos futuros.

---

## 2. Fonte única de verdade para transition mode

A duplicidade anterior foi removida como intenção canônica.

A Activity **não define mais diretamente** o modo real da transição como `CutWithCurtain`, `None` ou `Seamless`.

A Activity escolhe apenas a fonte do profile:

```text
ActivityTransitionProfileSource.None
ActivityTransitionProfileSource.OverrideProfile
ActivityTransitionProfileSource.InheritRouteProfile
```

O modo real da transição pertence ao `ActivityTransitionProfile` resolvido:

```text
ActivityTransitionMode.None
ActivityTransitionMode.CutWithCurtain
ActivityTransitionMode.Seamless
```

Regra:

```text
Activity escolhe a fonte.
Profile escolhe o comportamento técnico.
Pipeline resolve e valida.
Adapter executa.
```

Não deve haver duas fontes concorrentes como:

```text
transitionPolicy='CutWithCurtain'
nextActivityTransitionMode='None'
```

---

## 3. Resolução canônica de ActivityTransition

### 3.1 Source=None

Quando a Activity usa:

```text
nextActivityTransitionProfileSource = None
nextActivityTransitionProfileOverride = <none>
```

A resolução deve produzir:

```text
resolvedMode = None
resolvedFadeProfileSource = None
resolvedLoadingProfileSource = None
```

Comportamento:

```text
sem ActivityTransitionFade*
sem ActivityTransitionLoading*
com NextActivitySetup nominal
com ActivityHandoffPrepared após NextActivitySetupCompleted
```

Sequência validada:

```text
ActivityDeactivated
ActivityTransitionProfileSelected
ActivityTransitionProfileResolved mode=None
NextActivitySetupStarted
NextActivitySetupSkippedNoContent
NextActivitySetupCompleted
ActivityHandoffPrepared
ContinueToNextActivity
```

---

### 3.2 Source=OverrideProfile / CutWithCurtain

Quando a Activity usa:

```text
nextActivityTransitionProfileSource = OverrideProfile
nextActivityTransitionProfileOverride = ActivityTransitionProfile válido
```

E o profile resolve:

```text
resolvedMode = CutWithCurtain
resolvedFadeProfileSource = ActivityOverride
resolvedLoadingProfileSource = ActivityOverride
```

Comportamento:

```text
fade animado
loading próprio da ActivityTransition
NextActivitySetup sob cortina fechada
handoff preparado após setup
loading finalizado/ocultado antes do fade out
fade out animado revelando a próxima Activity
```

Sequência validada:

```text
ActivityDeactivated
ActivityTransitionProfileSelected
ActivityTransitionProfileResolved
ActivityTransitionFadeInStarted
ActivityTransitionLoadingStarted
ActivityTransitionLoadingProgress stage=Started progress=0.0
ActivityTransitionFadeInCompleted
ActivityTransitionLoadingProgress stage=FadeInCompleted progress=0.2
NextActivitySetupStarted
NextActivitySetupSkippedNoContent
NextActivitySetupCompleted
ActivityTransitionLoadingProgress stage=NextActivitySetupCompleted progress=0.5
ActivityHandoffPrepared
ActivityTransitionLoadingProgress stage=ActivityHandoffPrepared progress=0.6
ContinueToNextActivity
ContinueAccepted
ActivityTransitionLoadingProgress stage=ContinueAccepted progress=0.7
ActivitySetupStarted da próxima Activity
ActivitySetupSkippedNoContent da próxima Activity
ActivitySetupCompleted da próxima Activity
ActivationWindowStarted da próxima Activity
ActivationWindowSkippedNoContent da próxima Activity
ActivityRunningEntered da próxima Activity
ActivityTransitionLoadingProgress stage=RevealSafePoint progress=1.0
ActivityTransitionLoadingCompleted
ActivityTransitionLoadingHidden
ActivityTransitionFadeOutStarted
ActivityTransitionFadeOutCompleted
```

---

### 3.3 Source=InheritRouteProfile / CutWithCurtain

Quando a Activity usa:

```text
nextActivityTransitionProfileSource = InheritRouteProfile
nextActivityTransitionProfileOverride = <none>
```

A transição deve herdar explicitamente o profile da rota, sem fallback silencioso.

Resolução esperada:

```text
resolvedMode = CutWithCurtain
resolvedFadeProfileSource = RouteInherited
resolvedLoadingProfileSource = RouteInherited
```

Comportamento:

```text
igual ao OverrideProfile/CutWithCurtain,
mas usando fade/loading herdados da rota.
```

A herança é explícita e validada. Ausência de profile de rota válido deve falhar de forma explícita, não cair para `None`.

---

## 4. ActivityTransition loading

O loading de `ActivityTransition` pertence ao lifecycle da `SessionActivity`, não ao lifecycle da rota.

Ele não deve esperar:

```text
OperationalRouteCompleted
SceneCompositionCompleted
MaterializationCompleted da rota
```

O `SessionActivityPipeline` decide os milestones de progresso:

```text
Started = 0.0
FadeInCompleted = 0.2
NextActivitySetupCompleted = 0.5
ActivityHandoffPrepared = 0.6
ContinueAccepted = 0.7
RevealSafePoint = 1.0
```

O adapter de loading apenas executa side-effects visuais:

```text
StartAsync
ReportProgressAsync
CompleteAsync
HideAsync
```

Regra de ordem obrigatória:

```text
ActivityTransitionLoadingCompleted
-> ActivityTransitionLoadingHidden
-> ActivityTransitionFadeOutStarted
```

`FadeOutStarted` antes de `LoadingHidden` é blocker.

Se não houver loading profile resolvido e o contrato não marcar loading como obrigatório:

```text
ActivityTransitionLoadingSkippedNoContent
```

Sem fallback silencioso para loading da rota.

---

## 5. Fade de ActivityTransition

`CutWithCurtain` exige fade animado no caminho canônico.

Não é permitido implementar o caminho normal de `CutWithCurtain` usando:

```text
FadeInImmediate
FadeOutImmediate
```

Também não é permitido bloquear a main thread com:

```text
.GetAwaiter().GetResult()
.Result
Task.Wait()
Thread.Sleep()
loop síncrono aguardando fade/loading
```

O caminho canônico deve ser assíncrono/awaitable ou coroutine-safe, sem fire-and-forget de lifecycle.

---

## 6. ActivitySetup e NextActivitySetup

A entrada de qualquer Activity passa por `ActivitySetup` antes de activation:

```text
SessionActivityEntryHandoffAccepted
ActivitySetupStarted
ActivitySetupSkippedNoContent
ActivitySetupCompleted
ActivityActivationStarted
ActivationWindowStarted
ActivationWindowSkippedNoContent ou ActivationWindowReady/Completed
ActivityRunningEntered
```

A troca `Activity -> Activity` passa por `NextActivitySetup` antes de preparar o handoff:

```text
NextActivitySetupStarted
NextActivitySetupSkippedNoContent
NextActivitySetupCompleted
ActivityHandoffPrepared
```

`ActivityHandoffPrepared` antes de `NextActivitySetupCompleted` é blocker.

No modo `CutWithCurtain`, `NextActivitySetupStarted` deve ocorrer somente depois de `ActivityTransitionFadeInCompleted`.

---

## 7. Route-exit / CloseForRouteExit

`CloseForRouteExit` permanece separado do lifecycle de troca interna de Activity.

Ele não deve emitir:

```text
ActivityTransitionProfileSelected
ActivityTransitionProfileResolved
ActivityTransitionFade*
ActivityTransitionLoading*
NextActivitySetup*
ActivityHandoffPrepared
ContinueToNextActivity
```

Route-exit fecha a Activity ativa sem continuação interna de catálogo e sem handoff pendente.

Antes de unload de cena, o `SessionOperationalPipeline` deve garantir teardown canônico via boundary da `SessionActivity`.

---

## 8. Smokes validados neste checkpoint

### 8.1 Source=None

Status: **PASS**

Validado:

```text
sem fade de ActivityTransition
sem loading de ActivityTransition
NextActivitySetup antes de ActivityHandoffPrepared
ContinueToNextActivity aceito em NextActivitySetupCompleted
activity_02 entrou pelo lifecycle local
PipelineCompleted ao completar activity_02
BackToMenu sem handoff pendente
```

---

### 8.2 OverrideProfile / CutWithCurtain

Status: **PASS**

Validado:

```text
source=OverrideProfile
resolvedMode=CutWithCurtain
resolvedFadeProfileSource=ActivityOverride
resolvedLoadingProfileSource=ActivityOverride
fade animado
loading milestones próprios
NextActivitySetup sob cortina fechada
ActivityHandoffPrepared após NextActivitySetupCompleted
LoadingHidden antes de FadeOutStarted
FadeOutCompleted após reveal-safe point
```

---

### 8.3 InheritRouteProfile / CutWithCurtain

Status: **PASS**

Validado:

```text
source=InheritRouteProfile
resolvedMode=CutWithCurtain
resolvedFadeProfileSource=RouteInherited
resolvedLoadingProfileSource=RouteInherited
fade animado herdado da rota
loading herdado da rota, mas controlado pela SessionActivity
NextActivitySetup sob cortina fechada
LoadingHidden antes de FadeOutStarted
route-exit limpo após Completed
```

---

## 9. Critérios de regressão

Marcar como BLOCKER se qualquer item abaixo aparecer:

```text
ActivityHandoffPrepared antes de NextActivitySetupCompleted
NextActivitySetupStarted antes de ActivityTransitionFadeInCompleted em CutWithCurtain
ContinueToNextActivity aceito sem handoff pendente
ContinueToNextActivity habilitado fora de NextActivitySetupCompleted
LoadingHidden depois de ActivityTransitionFadeOutStarted
Loading ainda visível após ActivityTransitionFadeOutCompleted
Source=None emitindo ActivityTransitionFade*
Source=None emitindo ActivityTransitionLoading*
CloseForRouteExit emitindo ActivityTransitionProfile*
CloseForRouteExit emitindo ActivityTransitionFade*
CloseForRouteExit emitindo ActivityTransitionLoading*
CloseForRouteExit emitindo NextActivitySetup*
CloseForRouteExit emitindo ActivityHandoffPrepared
fallback silencioso de OverrideProfile/InheritRouteProfile para None
CutWithCurtain usando FadeImmediate como caminho canônico
bloqueio síncrono de async na main thread
```

---

## 10. Estado final do MVP

Com este checkpoint, o MVP local da `SessionActivity` está fechado para:

```text
Activity lifecycle nominal
ActivationWindow None/AdditiveScene
DeactivationWindow None/AdditiveScene
ActivitySetup nominal
NextActivitySetup nominal
Source=None transition
OverrideProfile/CutWithCurtain transition
InheritRouteProfile/CutWithCurtain transition
ActivityTransition fade animado
ActivityTransition loading próprio
Route-exit teardown sem handoff
QA guiado por stage
```

Ainda não faz parte deste checkpoint:

```text
ObjectEntry real
ActivitySceneContract real
NPC/HUD binding
pool/spawn/release homogêneo
scene inventory/discovery
retention policy real de cenas/activities
Seamless transition funcional
Progression Save real da Activity
Run Pipeline completo
```

Esses itens devem ser tratados em fases posteriores, sem reabrir o ownership já congelado neste checkpoint.

---

## 11. Checkpoint congelado - Smoke canonico SessionActivityEntry + ActivityTransition (2026-05-18)

Status: **PASS estrutural**

Escopo validado por smoke:

```text
Boot -> Menu -> route-menu-gameplay
```

Resultado congelado:

```text
RouteCamera (SessionActivityEntry) -> skip explicito reason='activity_camera_has_priority'
SessionOperationalPipeline -> OperationalRouteCompleted -> SessionActivityEntryHandoff
SessionActivityPipeline -> entrada inicial aceita sem ActivityTransitionCompleted indevido
activity_01 com ActivationWindow/DeactivationWindow AdditiveScene
activity_02 com ActivationWindow=None e DeactivationWindow=None
activity_01 -> activity_02 com OverrideProfile/CutWithCurtain
facts finais canonicos observaveis no Host StateFacts:
  ActivityTransitionLoadingCompleted
  ActivityTransitionLoadingHidden
  ActivityTransitionFadeOutStarted
  ActivityTransitionFadeOutCompleted
  ActivityTransitionCompleted
activity_02 completa sem proxima activity -> PipelineCompleted
```

Invariante reforcada:

```text
ActivityTransitionCompleted existe apenas para transicao interna Activity -> Activity.
Nao existe na entrada inicial via SessionActivityEntryHandoff.
```

---

## 12. Pendencias abertas (fora do freeze deste checkpoint)

1. Remover inferencia por `reason.Contains("route_exit")` e substituir por contrato explicito de route-exit.
2. Limpar/restringir APIs legadas (`GoTo*`, `Restart*`, `DebugStartActivity`) para evitar trilhos paralelos.
3. Tratar o trilho `fire-and-forget async` como divida separada, com contrato de observabilidade e erro explicito.
4. Tratar idempotencia de `scene already loaded` com fact explicito dedicado em fase futura.

---

## 13. Checkpoint congelado - SessionActivity MVP + RestartCurrentActivity local (2026-05-18)

Status: **CLOSED / PASS estrutural**

Leitura can�nica consolidada:

`	ext
SessionOperationalPipeline emite SessionActivityEntryHandoff.
SessionActivityPipeline e owner do lifecycle local apos o handoff.
Entrada inicial via SessionActivityEntryHandoff nao e ActivityTransition.
ActivityTransition e somente troca Activity -> Activity dentro da mesma SessionActivity.
`

Contrato de ActivityTransition validado:

`	ext
ActivityTransitionCompleted so existe em transicao real Activity -> Activity.
Source=None: no reveal-safe point da proxima activity.
CutWithCurtain: somente depois de ActivityTransitionFadeOutCompleted.
`

Windows validadas no smoke:

`	ext
ActivationWindow: None e AdditiveScene.
DeactivationWindow: None e AdditiveScene.
activity_01: ActivationWindow AdditiveScene + DeactivationWindow AdditiveScene.
activity_02: ActivationWindow None + DeactivationWindow None.
`

ActivityTransition validada em activity_01 -> activity_02:

`	ext
source=OverrideProfile
mode=CutWithCurtain
loading/fade em ordem canonica
ActivityTransitionCompleted canonico observavel em StateFacts
`

Policy de continuidade de transicao:

`	ext
ActivityTransitionContinuePolicy: AutoContinue | ManualContinue.
Unknown: fail-fast quando aplicavel.
Sandbox atual: AutoContinue.
Nao ha pausa QA entre ActivityHandoffPrepared e inicio da proxima activity.
ContinueToNextActivity manual so quando policy=ManualContinue.
`

Observabilidade/ownership de tooling:

`	ext
Host/QA pode observar estado assincrono para tooling, sem decidir lifecycle.
Dump do Host deve exibir facts canonicos acumulados em State.Facts.
`

RestartCurrentActivity local (can�nico):

`	ext
rail proprio de SessionActivityPipeline
reinicia a mesma activity atual
aceito somente em ActivityRunning
gera nova entrySequence
respeita DeactivationWindow da execucao antiga
respeita ActivationWindow da nova execucao
nao usa GoTo*, navigation, DebugStartActivity ou ActivityTransition
`

Facts canonicos minimos de restart:

`	ext
ActivityRestartRequested
ActivityRestartAccepted
ActivityRestartTeardownStarted
ActivityRestartSetupStarted
ActivityRestartCompleted
ActivityRestartRejected
`

Regra de fechamento de restart:

`	ext
ActivityRestartCompleted so pode ser emitido apos a nova execucao voltar para ActivityRunning.
`

Invariante de pending operation em callback async:

`	ext
Pending operation validada deve ser consumida sem apagar pending operation nova criada no mesmo callback.
`

Foreign/stale guard:

`	ext
Callbacks/completions foreign ou stale nao podem alterar a nova execucao apos restart.
`

Cenarios de restart validados:

`	ext
activity com ActivationWindow/DeactivationWindow AdditiveScene
activity sem ActivationWindow/DeactivationWindow
`

---

## 14. Pendencias fora deste checkpoint

1. RestartActivitySession.
2. RestartRoute.
3. RestartRun.
4. ObjectRelease/ObjectReset real.
5. Reset de objetos/actors/props/HUD/bindings/pools/subscriptions.
6. Restore de checkpoint/save.
7. ActivityWindowProfileAsset (scene + presentation/camera policy + completion policy).
8. Remover inferencia por reason.Contains("route_exit").
9. Limpar/restringir APIs legadas GoTo*/DebugStartActivity se ainda expostas.
10. Tratar fire-and-forget async como divida separada, se ainda existir.