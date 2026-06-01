# SA-7H1 — SessionActivityExitRuntimeState Audit/Design

Status: `CLOSED / DESIGN ONLY`  
Área: `SessionActivity` / release-exit decomposition / runtime state ownership / bridge retirement planning  
Sem alteração runtime.

---

## 1. Objetivo

Desenhar o state explícito que deve substituir parte das bridges transitórias criadas nos cortes SA-7B até SA-7G2B-H1.

Este corte não implementa código.

Ele define:

```text
quais estados de exit/release ainda estão presos no SessionActivityPipeline;
quais runtime states devem existir;
quais bridges eles substituem;
quais boundaries continuam no pipeline;
quais dados continuam sendo command/fact/snapshot;
qual ordem segura de implementação futura.
```

---

## 2. Diagnóstico

A decomposição de release/exit já reduziu o monólito:

```text
ActivityExitActorTeardownStage
ActivityObjectSnapshotCaptureStage
ActivityObjectReleaseStage
ActivityObjectContributorUnregisterStage
ActivityContentSceneUnloadDispatchStage
ActivityContentReleaseFinalizationStage
ActivityContentReleaseContinuationObservability
```

Mas o state consumido por esses stages ainda está preso no `SessionActivityPipeline` e foi exposto por bridges.

O problema atual não é mais “mover método grande”.  
O problema correto agora é:

```text
state ownership
```

Sem um runtime state explícito, qualquer remoção de bridge tende a virar:

```text
manager escondido;
coordinator novo;
pipeline paralelo;
stage com acesso excessivo;
ou bridge ainda maior.
```

---

## 3. Decisão principal

Não criar um único `SessionActivityExitRuntimeState` gigante.

A proposta correta é separar em três states pequenos, com fronteiras explícitas:

```text
ActivityActorExitRuntimeState
ActivityObjectExitRuntimeState
ActivityContentReleaseRuntimeState
```

E manter o macro lifecycle no:

```text
SessionActivityPipeline
```

Desenho:

```text
SessionActivityPipeline
  -> decide ordem, lifecycle e continuation

ActivityActorExitRuntimeState
  -> guarda runtime records de actor presentation/attributes/participation usados no teardown

ActivityObjectExitRuntimeState
  -> guarda discovery/inventory/snapshot payload usados em object exit

ActivityContentReleaseRuntimeState
  -> guarda loaded set, pending release context e awaiting flag usados no content release async/finalization
```

---

## 4. Proposta de ownership

### 4.1 `ActivityActorExitRuntimeState`

Responsável por state de actor durante exit/teardown.

Deve conter ou mediar acesso a:

```text
active actor presentation handles;
active actor attribute capabilities;
active actor participation records;
player participant bindings necessários ao exit;
actor inventory feed snapshot para exit.
```

Não deve decidir:

```text
quando teardown roda;
qual rail está ativo;
quando restart/route-exit ocorre;
quando next activity entra.
```

Consumidores futuros:

```text
ActivityExitActorTeardownStage
```

Substitui gradualmente:

```text
IActivityExitActorTeardownRuntimeBridge
```

Risco:

```text
Alto, porque a bridge atual é a mais larga.
```

Regra:

```text
Migrar por leitura primeiro.
Não mover adapters de player/actor antes de mapear se são side-effect endpoints ou runtime stores.
```

---

### 4.2 `ActivityObjectExitRuntimeState`

Responsável por state de object exit.

Deve conter:

```text
CurrentActivityObjectContributorDiscoveryResult;
CurrentActivityCapabilityInventoryPreview;
CurrentActivityCapabilityInventoryPreviewValidation;
snapshot payload for save-on-exit;
snapshot capture failure flags se existirem;
object release outcome temporário se necessário para checkpoints.
```

Não deve conter:

```text
SaveRuntime;
RouteActivitySave adapter;
Scene unload operation;
pending operation callback;
route-exit continuation.
```

Consumidores futuros:

```text
ActivityObjectSnapshotCaptureStage
ActivityObjectReleaseStage
ActivityObjectContributorUnregisterStage
RouteActivitySave payload provider por API de leitura controlada
```

Substitui gradualmente:

```text
IActivityObjectSnapshotCaptureRuntimeBridge
IActivityObjectReleaseRuntimeBridge
IActivityObjectContributorUnregisterRuntimeBridge
```

Risco:

```text
Médio.
```

Regra:

```text
Snapshot capture produz payload.
RouteActivitySave consome payload.
Nenhum stage de object exit executa save.
```

---

### 4.3 `ActivityContentReleaseRuntimeState`

Responsável por state técnico de content release async.

Deve conter:

```text
CurrentActivityContentLoadedSet;
PendingActivityContentReleaseContext;
awaitingContinuationAfterActivityContentRelease flag;
last release status;
loaded/released scene counts;
skip/no-content classification for finalization observability.
```

Não deve decidir:

```text
StartPendingRestartEntry;
CompleteRouteExitClosure;
ContinueAfterDeactivationAsync;
NextActivity continuation.
```

Consumidores futuros:

```text
ActivityContentSceneUnloadDispatchStage
ActivityContentReleaseFinalizationStage
SessionActivityPipeline continuation observability
```

Substitui gradualmente:

```text
IActivityContentSceneUnloadDispatchRuntimeBridge
IActivityContentReleaseFinalizationRuntimeBridge
```

Risco:

```text
Médio/alto por causa de pending operation e async callback.
```

Regra:

```text
Dispatch stage pode ler/escrever pending operation state técnico.
Callback e continuation macro permanecem no SessionActivityPipeline até auditoria própria.
```

---

## 5. O que continua no `SessionActivityPipeline`

Mesmo com runtime states explícitos, o pipeline continua dono de:

```text
entry/exit macro lifecycle;
release ordering;
deactivation window lifecycle;
restart continuation;
next activity continuation;
route-exit closure;
foreign/stale guards;
policy/handoff macro;
checkpoint orchestration quando for cross-stage.
```

Exemplo de ordem preservada:

```text
SessionActivityPipeline
-> ActivityExitActorTeardownStage
-> DeactivationWindow quando aplicável
-> ActivityObjectSnapshotCaptureStage
-> ActivityObjectReleaseStage
-> ActivityContentSceneUnloadDispatchStage
-> ActivityContentReleaseFinalizationStage
-> ActivityContentReleaseContinuationResolved/Started/Completed
-> Restart / NextActivity / RouteExit / CompleteActivity
```

---

## 6. O que continua sendo stage

```text
ActivityExitActorTeardownStage
ActivityObjectSnapshotCaptureStage
ActivityObjectReleaseStage
ActivityObjectContributorUnregisterStage
ActivityContentSceneUnloadDispatchStage
ActivityContentReleaseFinalizationStage
```

Stages executam passos determinísticos.

Eles não devem:

```text
escolher next activity;
abrir route-exit;
criar restart;
chamar SessionOperational;
executar save;
inventar fallback se state obrigatório faltar.
```

---

## 7. O que continua sendo command/fact/adapter

### Commands

```text
ActorParticipationExitCommand
ActivityObjectSnapshotCaptureCommand se existir ou equivalente
ActivityObjectReleaseCommand
ActivityContentSceneUnloadCommand
```

Commands carregam payload runtime resolvido.  
Não carregam stage, adapter, delegate ou ScriptableObject autoral inteiro.

### Facts / OBS

```text
ActorPresentationReleased / ReleaseCompleted
ActorAttributeReleased / ReleaseCompleted
ActorParticipationExited / ExitCompleted
ActivityObjectSnapshotCaptureCompleted
ActivityObjectReleaseCompleted
ActivityObjectContributorUnregisterCompleted
ActivityContentReleaseCompleted
ActivityContentReleaseContinuationResolved/Started/Completed
```

Facts registram o ocorrido.  
Não executam side-effects.

### Adapters

```text
UnityActivityContentSceneReleaseAdapter
Player/Actor presentation release endpoints/adapters
Object release endpoints
SaveRuntime backend via RouteActivitySave consumer
```

Adapters executam side-effects comandados.  
Não decidem lifecycle.

---

## 8. Matriz de bridges para runtime state

| Bridge atual | Problema | Runtime state proposto | Severidade | Ação futura |
|---|---|---|---|---|
| `IActivityExitActorTeardownRuntimeBridge` | Bridge larga para actor stores/adapters/feed | `ActivityActorExitRuntimeState` | Alta | Migrar por leitura primeiro; não expandir |
| `IActivityObjectSnapshotCaptureRuntimeBridge` | Discovery/inventory + snapshot payload preso no pipeline | `ActivityObjectExitRuntimeState` | Média | Migrar snapshot payload e discovery juntos |
| `IActivityObjectReleaseRuntimeBridge` | Release lê inventory/discovery via pipeline | `ActivityObjectExitRuntimeState` | Baixa/média | Migrar após snapshot state existir |
| `IActivityObjectContributorUnregisterRuntimeBridge` | Cleanup de discovery preso no pipeline | `ActivityObjectExitRuntimeState` | Baixa | Migrar junto com contributor discovery |
| `IActivityContentSceneUnloadDispatchRuntimeBridge` | Dispatch async depende de pending operation state | `ActivityContentReleaseRuntimeState` | Média/alta | Migrar sem mover callback/continuation |
| `IActivityContentReleaseFinalizationRuntimeBridge` | Cleanup de loaded set/context/awaiting no pipeline | `ActivityContentReleaseRuntimeState` | Média | Migrar finalization state junto |

---

## 9. Ordem futura recomendada

Não implementar todos os states de uma vez.

### SA-7H2 — ActivityContentReleaseRuntimeState implementation

Motivo:

```text
É o state mais autocontido depois da observabilidade.
Cobre loaded set, pending release context e awaiting flag.
Substitui duas bridges relacionadas.
Não toca actor stores nem snapshot payload/save.
```

Escopo futuro:

```text
criar ActivityContentReleaseRuntimeState;
mover CurrentActivityContentLoadedSet;
mover PendingActivityContentReleaseContext;
mover awaitingContinuationAfterActivityContentRelease;
adaptar ActivityContentSceneUnloadDispatchStage;
adaptar ActivityContentReleaseFinalizationStage;
preservar callback e continuation no SessionActivityPipeline.
```

### SA-7H3 — ActivityObjectExitRuntimeState implementation

Motivo:

```text
Agrupa discovery/inventory/snapshot payload.
Reduz três bridges de object exit.
Toca payload de RouteActivitySave, então vem depois.
```

### SA-7H4 — ActivityActorExitRuntimeState audit/design ou implementation

Motivo:

```text
Bridge mais larga.
Envolve presentation, attributes, participation e player exit.
Deve ser auditada com mais cuidado antes de migrar.
```

---

## 10. Proposta de shape inicial

### 10.1 `ActivityContentReleaseRuntimeState`

Arquivo sugerido:

```text
NewScripts/SessionActivity/Pipeline/Runtime/ActivityContentReleaseRuntimeState.cs
```

Responsabilidades:

```text
Store técnico de release async de ActivityContent.
Sem lifecycle decision.
Sem continuation.
Sem adapter Unity direto.
```

API sugerida:

```csharp
internal sealed class ActivityContentReleaseRuntimeState
{
    public ActivityContentLoadedSet CurrentLoadedSet { get; }
    public PendingActivityContentReleaseContext PendingReleaseContext { get; }
    public bool IsAwaitingContinuation { get; }

    public bool HasCurrentLoadedSet { get; }
    public bool HasPendingReleaseContext { get; }

    public void SetCurrentLoadedSet(ActivityContentLoadedSet loadedSet);
    public void ClearCurrentLoadedSet();

    public void SetPendingReleaseContext(PendingActivityContentReleaseContext context);
    public void ClearPendingReleaseContext();

    public void SetAwaitingContinuation(bool value);

    public ActivityContentReleaseFinalizationTelemetry CreateFinalizationTelemetry(...);
}
```

Notas:

```text
Os tipos acima devem ser ajustados ao shape real do código.
Não criar DTOs novos se os contratos atuais já existem e são adequados.
Não expor StartPendingRestartEntry, CompleteRouteExitClosure ou ContinueAfterDeactivationAsync.
```

### 10.2 `ActivityObjectExitRuntimeState`

Arquivo sugerido:

```text
NewScripts/SessionActivity/Pipeline/Runtime/ActivityObjectExitRuntimeState.cs
```

Responsabilidades:

```text
Store de discovery/inventory/snapshot payload de object exit.
Sem save execution.
Sem scene unload.
Sem continuation.
```

API sugerida:

```csharp
internal sealed class ActivityObjectExitRuntimeState
{
    public ActivityObjectContributorDiscoveryResult CurrentContributorDiscoveryResult { get; }
    public ActivityCapabilityInventoryPreview CurrentInventoryPreview { get; }
    public ActivityCapabilityInventoryValidationResult CurrentInventoryPreviewValidation { get; }
    public SessionActivitySnapshotPayload SnapshotPayloadForSaveOnExit { get; }

    public void SetContributorDiscoveryResult(...);
    public void ClearContributorDiscoveryResult();

    public void SetInventoryPreview(...);
    public void ClearInventoryPreview();

    public void SetSnapshotPayloadForSaveOnExit(...);
    public bool TryGetSnapshotPayloadForSaveOnExit(...);
}
```

### 10.3 `ActivityActorExitRuntimeState`

Arquivo sugerido:

```text
NewScripts/SessionActivity/Pipeline/Runtime/ActivityActorExitRuntimeState.cs
```

Responsabilidades:

```text
Store de runtime records de actor exit.
Sem decidir rail.
Sem chamar route-exit.
Sem continuation.
```

API deve ser desenhada após auditoria específica da bridge larga.

---

## 11. Observabilidade obrigatória para implementação futura

### SA-7H2

Eventos recomendados:

```text
ActivityContentReleaseRuntimeStateCreated
ActivityContentReleaseRuntimeStateLoadedSetStored
ActivityContentReleaseRuntimeStateLoadedSetCleared
ActivityContentReleaseRuntimeStatePendingContextStored
ActivityContentReleaseRuntimeStatePendingContextCleared
ActivityContentReleaseRuntimeStateAwaitingContinuationChanged
```

Campos mínimos:

```text
owner='ActivityContentReleaseRuntimeState'
activityId
entrySequence
hasLoadedSetBefore
hasLoadedSetAfter
hasPendingContextBefore
hasPendingContextAfter
awaitingBefore
awaitingAfter
source
reason
```

### Regras

```text
Não substituir eventos canônicos de release/finalization.
Runtime state logs são complementares.
Manter ActivityContentReleaseCompleted e ActivityContentReleaseContinuation*.
```

---

## 12. Critério de smoke para implementação futura

Para SA-7H2, rodar:

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

ActivityContentSceneUnloadDispatchStage preservado
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

## 13. Respostas obrigatórias

### Qual pipeline é dono desta decisão?

```text
SessionActivityPipeline continua dono do macro lifecycle.
```

Runtime states não decidem lifecycle; eles armazenam state técnico usado por stages.

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
ActivityActorExitRuntimeState / ActivityObjectExitRuntimeState / ActivityContentReleaseRuntimeState são runtime state.
Stages continuam stages.
Commands continuam payloads resolvidos.
Facts continuam observabilidade.
Adapters continuam side-effects.
```

### Isso é comportamento final ou bridge transitória?

```text
Os runtime states propostos são caminho final provável.
As bridges atuais são transitórias.
```

### Essa compatibilidade ainda é necessária?

```text
Compatibilidade de produção não é necessária.
A transição gradual é necessária para não quebrar baseline funcional.
Não deve existir fallback paralelo.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

```text
O sintoma era bridge transitória.
A fronteira arquitetural correta é mover state para runtime state explícito, não criar manager nem pipeline novo.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
Não no desenho proposto.
O pipeline continua dono do lifecycle.
Runtime states não executam continuation.
```

---

## 14. Conclusão

`SA-7H1` fecha como design.

A recomendação é implementar primeiro:

```text
SA-7H2 — ActivityContentReleaseRuntimeState implementation
```

Porque é o menor state coeso e reduz duas bridges sem tocar actor exit nem save payload.

Regra central:

```text
Mover state, não lifecycle.
Reduzir bridges, não criar managers.
```
