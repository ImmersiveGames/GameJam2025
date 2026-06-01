# SA-7H2-H1 — ContentRelease Bridge Retirement Audit

Status: `CLOSED / AUDIT ONLY`  
Área: `SessionActivity` / `ActivityContentReleaseRuntimeState` / bridge retirement / content release  
Sem alteração runtime.

---

## 1. Objetivo

Auditar se as bridges transitórias de `ActivityContentRelease` ainda precisam existir depois do `SA-7H2`.

O `SA-7H2` criou o state explícito:

```text
ActivityContentReleaseRuntimeState
```

e moveu para ele o state técnico de content release:

```text
CurrentLoadedSet
PendingActivityContentReleaseContext
IsAwaitingContinuation
```

Agora a questão correta é:

```text
IActivityContentSceneUnloadDispatchRuntimeBridge ainda precisa existir?
IActivityContentReleaseFinalizationRuntimeBridge ainda precisa existir?
Se existir, qual parte ainda é legítima?
O que pode ser removido sem mover lifecycle?
```

---

## 2. Baseline funcional

Baseline validado no smoke do `SA-7H2`:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
```

Checkpoints preservados:

```text
ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

Observabilidade nova validada:

```text
ActivityContentReleaseRuntimeStateLoadedSetStored
ActivityContentReleaseRuntimeStateLoadedSetCleared
ActivityContentReleaseRuntimeStatePendingContextStored
ActivityContentReleaseRuntimeStatePendingContextCleared
ActivityContentReleaseRuntimeStateAwaitingContinuationChanged
owner='ActivityContentReleaseRuntimeState'
```

---

## 3. Bridges auditadas

```text
IActivityContentSceneUnloadDispatchRuntimeBridge
IActivityContentReleaseFinalizationRuntimeBridge
```

---

## 4. Decisão principal

Não remover ambas as bridges de uma vez.

A decisão correta é separar:

```text
SA-7H2-H2 — ActivityContentSceneUnloadDispatchBridgeReduction
SA-7H2-H3 — ActivityContentReleaseFinalizationBridgeReduction
```

Motivo:

```text
dispatch toca pending operation runner;
finalization toca unregister stage e cleanup;
misturar os dois aumenta risco de regressão no release async.
```

A ordem recomendada é:

```text
primeiro dispatch bridge;
depois finalization bridge.
```

Porque o dispatch já deve depender quase só de:

```text
ActivityContentReleaseRuntimeState
SessionActivityPendingOperation endpoint/state
PendingOperationRunner
```

e não deve tocar cleanup nem continuation.

---

## 5. Auditoria da bridge de dispatch

### 5.1 Bridge atual

```text
IActivityContentSceneUnloadDispatchRuntimeBridge
```

Responsabilidade conhecida:

```text
BuildActivityContentReleasePendingOperation
RunActivityContentReleaseOperation
```

Também há uso de endpoint já existente para:

```text
SetPendingOperation
```

### 5.2 Leitura arquitetural

Parte ainda legítima:

```text
BuildActivityContentReleasePendingOperation
RunActivityContentReleaseOperation
```

Mas o nome `RuntimeBridge` ficou amplo demais depois do `ActivityContentReleaseRuntimeState`.

O stage de dispatch deveria receber dependências explícitas:

```text
ActivityContentReleaseRuntimeState
ISessionActivityPendingOperationEndpoint ou equivalente atual
ISessionActivityPendingOperationRunner
```

Sem ponte genérica.

### 5.3 Owner correto

```text
ActivityContentReleaseRuntimeState:
  fornece PendingActivityContentReleaseContext / loaded set / awaiting flag.

SessionActivityPipeline:
  continua owner da callback e continuation macro.

ActivityContentSceneUnloadDispatchStage:
  monta command/pending operation e chama runner.

PendingOperationRunner:
  executa adapter async e retorna callback.

UnityActivityContentSceneReleaseAdapter:
  executa side-effect Unity.
```

### 5.4 O que pode sair da bridge

Pode sair da bridge:

```text
acesso indireto ao pending release context;
acesso indireto ao loaded set;
qualquer helper que só devolva state agora pertencente ao ActivityContentReleaseRuntimeState.
```

### 5.5 O que não deve mudar

```text
CompleteActivityContentSceneUnloadOperation não move.
FailPendingOperation não move.
StartPendingRestartEntry não move.
CompleteRouteExitClosure não move.
ContinueAfterDeactivationAsync não move.
RouteActivitySave não muda.
```

### 5.6 Corte recomendado

```text
SA-7H2-H2 — ActivityContentSceneUnloadDispatchBridgeReduction
```

Escopo:

```text
Injetar ActivityContentReleaseRuntimeState diretamente no ActivityContentSceneUnloadDispatchStage.
Manter pending operation runner explícito.
Manter pending operation endpoint explícito.
Remover IActivityContentSceneUnloadDispatchRuntimeBridge se ela não tiver mais método necessário.
Ou reduzir a bridge a um port mais específico, se o shape real exigir.
```

Proibido:

```text
Não mover callback.
Não mover continuation.
Não alterar adapter.
Não alterar runner.
Não alterar release finalization.
```

---

## 6. Auditoria da bridge de finalization

### 6.1 Bridge atual

```text
IActivityContentReleaseFinalizationRuntimeBridge
```

Responsabilidades conhecidas antes do runtime state:

```text
HasCurrentActivityContentLoadedSet
HasPendingActivityContentReleaseContext
IsAwaitingContinuationAfterActivityContentRelease
ExecuteActivityObjectContributorUnregister
ClearCurrentActivityContentLoadedSet
ClearPendingActivityContentReleaseContext
SetAwaitingContinuationAfterActivityContentRelease
```

### 6.2 Leitura arquitetural depois do SA-7H2

Essas responsabilidades se dividem:

#### State técnico

Agora deve pertencer a:

```text
ActivityContentReleaseRuntimeState
```

Inclui:

```text
HasCurrentActivityContentLoadedSet
HasPendingActivityContentReleaseContext
IsAwaitingContinuation
ClearCurrentActivityContentLoadedSet
ClearPendingActivityContentReleaseContext
SetAwaitingContinuation(false)
```

#### Execução de unregister

Ainda é chamada de stage:

```text
ActivityObjectContributorUnregisterStage
```

Esse ponto não pertence ao runtime state.

### 6.3 Owner correto

```text
ActivityContentReleaseFinalizationStage:
  decide a ordem interna da finalization determinística.

ActivityContentReleaseRuntimeState:
  executa cleanup técnico de state.

ActivityObjectContributorUnregisterStage:
  executa unregister determinístico.

SessionActivityPipeline:
  decide continuation macro depois da finalization.
```

### 6.4 O que pode sair da bridge

Pode sair:

```text
HasCurrentActivityContentLoadedSet
HasPendingActivityContentReleaseContext
IsAwaitingContinuationAfterActivityContentRelease
ClearCurrentActivityContentLoadedSet
ClearPendingActivityContentReleaseContext
SetAwaitingContinuationAfterActivityContentRelease
```

porque agora existe `ActivityContentReleaseRuntimeState`.

### 6.5 O que precisa de cuidado

```text
ExecuteActivityObjectContributorUnregister
```

Não deve ser movido para o runtime state.

Opções aceitáveis:

```text
1. ActivityContentReleaseFinalizationStage recebe ActivityObjectContributorUnregisterStage diretamente.
2. ActivityContentReleaseFinalizationStage recebe um port específico, ex. IActivityObjectContributorUnregisterExecutor.
```

Opção proibida:

```text
ActivityContentReleaseRuntimeState executar unregister.
```

Runtime state guarda state. Não executa stage nem side-effect.

### 6.6 Corte recomendado

```text
SA-7H2-H3 — ActivityContentReleaseFinalizationBridgeReduction
```

Escopo:

```text
Injetar ActivityContentReleaseRuntimeState diretamente no ActivityContentReleaseFinalizationStage.
Injetar ActivityObjectContributorUnregisterStage ou executor específico diretamente.
Remover/reduzir IActivityContentReleaseFinalizationRuntimeBridge.
Preservar todos os eventos de finalization.
Preservar cleanup ordering.
```

Proibido:

```text
Não mover continuation macro.
Não mover ActivityObjectContributorUnregister para runtime state.
Não alterar object snapshot/release.
Não alterar route-exit.
Não alterar save.
```

---

## 7. Matriz de decisão

| Bridge | Estado após SA-7H2 | Pode remover agora? | Próximo corte | Risco | Decisão |
|---|---|---:|---|---|---|
| `IActivityContentSceneUnloadDispatchRuntimeBridge` | Parte substituível por `ActivityContentReleaseRuntimeState`, parte ainda é runner/pending op | Sim, mas em corte isolado | `SA-7H2-H2` | Médio | Reduzir/remover primeiro |
| `IActivityContentReleaseFinalizationRuntimeBridge` | State substituível por runtime state; unregister precisa port/stage explícito | Sim, mas depois do dispatch | `SA-7H2-H3` | Médio | Reduzir/remover segundo |

---

## 8. Respostas obrigatórias

### Qual pipeline é dono desta decisão?

```text
SessionActivityPipeline continua dono do macro lifecycle e da continuation.
```

A decisão de retirar bridge é estrutural, mas não muda o owner do lifecycle.

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
ActivityContentReleaseRuntimeState = runtime state.
ActivityContentSceneUnloadDispatchStage = stage.
ActivityContentReleaseFinalizationStage = stage.
IActivityContentSceneUnloadDispatchRuntimeBridge = bridge transitória.
IActivityContentReleaseFinalizationRuntimeBridge = bridge transitória.
PendingOperationRunner = bridge técnica async/runner.
UnityActivityContentSceneReleaseAdapter = adapter.
```

### Isso é comportamento final ou bridge transitória?

```text
As duas interfaces RuntimeBridge são transitórias.
ActivityContentReleaseRuntimeState é o caminho final provável para state técnico.
```

### Essa compatibilidade ainda é necessária?

```text
Compatibilidade de produção não é necessária.
A remoção deve ser gradual para preservar baseline funcional.
Não deve haver fallback paralelo.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

```text
O sintoma era bridge permanecendo após mover state.
A fronteira correta é stages dependerem de runtime state/ports explícitos, não de bridge genérica para pipeline.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
Não atualmente.
O risco surgiria se a redução das bridges movesse callback/continuation para stage ou runtime state.
```

---

## 9. Próximo corte recomendado

```text
SA-7H2-H2 — ActivityContentSceneUnloadDispatchBridgeReduction
```

### Escopo permitido

```text
Auditar o construtor real de ActivityContentSceneUnloadDispatchStage.
Substituir acesso de state via IActivityContentSceneUnloadDispatchRuntimeBridge por ActivityContentReleaseRuntimeState.
Manter runner/pending operation como dependências explícitas.
Remover a bridge se ela ficar vazia.
Preservar ActivityContentSceneUnloadDispatched.
Preservar pendingOperation ActivityContentSceneUnload.
Preservar callback no SessionActivityPipeline.
```

### Escopo proibido

```text
Não mover CompleteActivityContentSceneUnloadOperation.
Não mover FailPendingOperation.
Não mover StartPendingRestartEntry.
Não mover CompleteRouteExitClosure.
Não mover ContinueAfterDeactivationAsync.
Não alterar ActivityContentReleaseFinalizationStage.
Não alterar ActivityObjectContributorUnregisterStage.
Não alterar RouteActivitySave.
Não criar ActivityContentReleasePipeline.
Não criar ActivityExitPipeline.
```

---

## 10. Critério de smoke para SA-7H2-H2

Rodar:

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
QA Reset Current Player Actor
RestartCurrentActivity
CompleteDeactivationWindow
CompleteActivationWindow
CompleteCurrentActivity
Activity01ToActivity02
BackToMenu / RouteExit
```

Critérios:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityContentReleaseRuntimeStateLoadedSetStored observado
ActivityContentReleaseRuntimeStatePendingContextStored observado
ActivityContentReleaseRuntimeStateAwaitingContinuationChanged observado
ActivityContentSceneUnloadDispatchStage preservado
pendingOperation ActivityContentSceneUnload preservado
ActivityContentReleaseFinalizationStage preservado
ActivityContentReleaseContinuationResolved/Started/Completed preservado
ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

---

## 11. Conclusão

`SA-7H2-H1` fecha como auditoria.

A remoção das bridges de content release é viável, mas deve ser em dois cortes:

```text
SA-7H2-H2 — dispatch bridge reduction
SA-7H2-H3 — finalization bridge reduction
```

Regra central:

```text
Trocar bridge por runtime state/port explícito.
Não mover lifecycle.
```
