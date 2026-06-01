# SA-7H3 — ActivityObjectExitRuntimeState Audit/Design

Status: `CLOSED / DESIGN ONLY`  
Área: `SessionActivity` / object exit / snapshot capture / object release / contributor unregister / runtime state ownership  
Sem alteração runtime.

---

## 1. Objetivo

Desenhar o runtime state explícito para o bloco de `ActivityObject` durante exit/release.

Os cortes anteriores já isolaram:

```text
ActivityObjectSnapshotCaptureStage
ActivityObjectReleaseStage
ActivityObjectContributorUnregisterStage
```

Mas estes stages ainda dependem de bridges transitórias que expõem state preso no `SessionActivityPipeline`.

Esta auditoria define:

```text
quais dados pertencem ao futuro ActivityObjectExitRuntimeState;
quais bridges ele deve substituir;
qual boundary com RouteActivitySave deve ser preservada;
qual ordem segura de implementação;
quais pontos não podem virar lifecycle owner.
```

---

## 2. Baseline funcional

Último baseline validado:

```text
SA-7H2-H3 — ActivityContentReleaseFinalizationBridgeReduction PASS
```

Estado consolidado:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

Content release já está mais limpo:

```text
ActivityContentReleaseRuntimeState existe.
IActivityContentSceneUnloadDispatchRuntimeBridge saiu do caminho ativo.
IActivityContentReleaseFinalizationRuntimeBridge saiu do caminho ativo.
SessionActivityPipeline continua dono de callback/continuation macro.
```

---

## 3. Problema atual

O bloco de object exit já tem stages dedicados, mas o state usado por eles ainda está distribuído no pipeline e exposto por bridges.

Bridges envolvidas:

```text
IActivityObjectSnapshotCaptureRuntimeBridge
IActivityObjectReleaseRuntimeBridge
IActivityObjectContributorUnregisterRuntimeBridge
```

Dados atualmente expostos por elas:

```text
CurrentActivityObjectContributorDiscoveryResult
CurrentActivityCapabilityInventoryPreview
CurrentActivityCapabilityInventoryPreviewValidation
SnapshotPayloadForSaveOnExit / SetSnapshotPayloadForSaveOnExit
ClearCurrentActivityObjectContributorDiscoveryResult
```

O problema não é mais método grande.  
O problema é:

```text
object exit state ainda não tem owner explícito.
```

---

## 4. Decisão principal

Criar futuramente um runtime state dedicado:

```text
ActivityObjectExitRuntimeState
```

Ele deve ser o owner técnico de:

```text
CurrentActivityObjectContributorDiscoveryResult
CurrentActivityCapabilityInventoryPreview
CurrentActivityCapabilityInventoryPreviewValidation
SessionActivitySnapshotPayloadForSaveOnExit
object exit snapshot/release/unregister observability state mínimo
```

Ele **não** deve ser owner de:

```text
save execution;
RouteActivitySave policy;
ActivityContent unload;
pending operation;
restart;
route-exit closure;
next activity;
deactivation continuation;
actor teardown.
```

---

## 5. Owner correto

### 5.1 `SessionActivityPipeline`

Continua dono de:

```text
ordem macro de exit/release;
quando SnapshotCapture roda;
quando ObjectRelease roda;
quando ContributorUnregister roda;
quando ActivityContentRelease roda;
continuation macro após release;
foreign/stale guards macro;
handoffs.
```

### 5.2 `ActivityObjectExitRuntimeState`

Dono de state técnico de object exit:

```text
discovery result;
capability inventory preview;
capability inventory validation result;
snapshot payload for save-on-exit;
cleanup de discovery/inventory/payload.
```

### 5.3 Stages

Continuam executando passos determinísticos:

```text
ActivityObjectSnapshotCaptureStage
ActivityObjectReleaseStage
ActivityObjectContributorUnregisterStage
```

### 5.4 RouteActivitySave

Continua consumidor externo do payload produzido.

```text
RouteActivitySave não captura snapshot.
RouteActivitySave não decide object release.
RouteActivitySave não escolhe contributor unregister.
RouteActivitySave consome payload por API de leitura controlada.
```

---

## 6. Shape proposto

Arquivo sugerido:

```text
NewScripts/SessionActivity/Pipeline/Runtime/ActivityObjectExitRuntimeState.cs
```

Responsabilidade:

```text
Store técnico de object exit.
Sem lifecycle decision.
Sem save execution.
Sem scene unload.
Sem actor teardown.
```

API inicial sugerida:

```csharp
internal sealed class ActivityObjectExitRuntimeState
{
    public ActivityObjectContributorDiscoveryResult CurrentContributorDiscoveryResult { get; }
    public ActivityCapabilityInventoryPreview CurrentInventoryPreview { get; }
    public ActivityCapabilityInventoryValidationResult CurrentInventoryPreviewValidation { get; }
    public SessionActivitySnapshotPayload SnapshotPayloadForSaveOnExit { get; }

    public bool HasContributorDiscoveryResult { get; }
    public bool HasInventoryPreview { get; }
    public bool HasInventoryPreviewValidation { get; }
    public bool HasSnapshotPayloadForSaveOnExit { get; }

    public void SetContributorDiscoveryResult(
        ActivityObjectContributorDiscoveryResult result,
        string activityId,
        int entrySequence,
        string source,
        string reason);

    public void ClearContributorDiscoveryResult(
        string activityId,
        int entrySequence,
        string source,
        string reason);

    public void SetInventoryPreview(
        ActivityCapabilityInventoryPreview preview,
        string activityId,
        int entrySequence,
        string source,
        string reason);

    public void SetInventoryPreviewValidation(
        ActivityCapabilityInventoryValidationResult validation,
        string activityId,
        int entrySequence,
        string source,
        string reason);

    public void ClearInventoryState(
        string activityId,
        int entrySequence,
        string source,
        string reason);

    public void SetSnapshotPayloadForSaveOnExit(
        SessionActivitySnapshotPayload payload,
        string activityId,
        int entrySequence,
        string source,
        string reason);

    public bool TryGetSnapshotPayloadForSaveOnExit(
        string activityIdentity,
        out SessionActivitySnapshotPayload payload,
        out string reason);

    public void ClearSnapshotPayloadForSaveOnExit(
        string activityId,
        int entrySequence,
        string source,
        string reason);
}
```

Notas:

```text
A API deve ser ajustada aos nomes reais dos contratos existentes.
Não criar DTO novo se os contratos atuais já servirem.
Não fazer save.
Não chamar RouteActivitySave.
Não chamar ActivityContentReleaseRuntimeState.
Não chamar Actor teardown.
```

---

## 7. Bridge retirement por etapas

Não remover as três bridges de uma vez.

### SA-7H3A — ActivityObjectExitRuntimeState implementation

Escopo:

```text
criar ActivityObjectExitRuntimeState;
mover storage de CurrentActivityObjectContributorDiscoveryResult;
mover storage de CurrentActivityCapabilityInventoryPreview;
mover storage de CurrentActivityCapabilityInventoryPreviewValidation;
mover storage de SnapshotPayloadForSaveOnExit;
manter bridges funcionando como facade fina para o runtime state;
não alterar stages ainda.
```

Motivo:

```text
Primeiro cria owner explícito sem trocar consumers.
Baixo risco funcional.
Permite smoke validar que state moveu sem mexer em stage behavior.
```

### SA-7H3B — ActivityObjectSnapshotCaptureBridgeReduction

Escopo:

```text
ActivityObjectSnapshotCaptureStage passa a depender de ActivityObjectExitRuntimeState diretamente.
Remove/reduz IActivityObjectSnapshotCaptureRuntimeBridge.
Preserva SetSnapshotPayloadForSaveOnExit via runtime state.
Não altera RouteActivitySave.
```

### SA-7H3C — ActivityObjectReleaseBridgeReduction

Escopo:

```text
ActivityObjectReleaseStage passa a depender de ActivityObjectExitRuntimeState diretamente.
Remove/reduz IActivityObjectReleaseRuntimeBridge.
Preserva object release facts/checkpoints.
```

### SA-7H3D — ActivityObjectContributorUnregisterBridgeReduction

Escopo:

```text
ActivityObjectContributorUnregisterStage passa a depender de ActivityObjectExitRuntimeState diretamente.
Remove/reduz IActivityObjectContributorUnregisterRuntimeBridge.
Preserva cleanup de discovery result.
```

---

## 8. Matriz de bridges

| Bridge atual | Dados expostos | Owner final | Severidade | Próximo corte |
|---|---|---|---|---|
| `IActivityObjectSnapshotCaptureRuntimeBridge` | discovery, inventory, validation, snapshot payload write | `ActivityObjectExitRuntimeState` | Média/alta | SA-7H3B |
| `IActivityObjectReleaseRuntimeBridge` | discovery, inventory, validation | `ActivityObjectExitRuntimeState` | Média | SA-7H3C |
| `IActivityObjectContributorUnregisterRuntimeBridge` | discovery read/clear | `ActivityObjectExitRuntimeState` | Baixa/média | SA-7H3D |

---

## 9. Boundary com RouteActivitySave

Este é o ponto mais sensível do object exit.

### Regra

```text
ActivityObjectSnapshotCaptureStage produz payload.
ActivityObjectExitRuntimeState guarda payload.
SessionActivityPipeline / payload provider expõe leitura controlada.
RouteActivitySave consome payload.
RouteActivitySave não captura e não decide lifecycle.
```

### Proibido

```text
Não injetar SaveRuntime no ActivityObjectSnapshotCaptureStage.
Não chamar RouteActivitySave dentro do ActivityObjectExitRuntimeState.
Não transformar SnapshotCaptureStage em save adapter.
Não apagar payload antes de RouteActivitySave consumir no route-exit.
Não criar fallback se payload ausente quando deveria existir.
```

### Permitido

```text
TryGetSnapshotPayloadForSaveOnExit pode ler do ActivityObjectExitRuntimeState.
Snapshot payload pode ser limpo por lifecycle owner em momento explícito.
Ausência de payload pode ser skip explícito quando não havia conteúdo.
```

---

## 10. Observabilidade obrigatória para SA-7H3A

Eventos do runtime state:

```text
ActivityObjectExitRuntimeStateCreated
ActivityObjectExitRuntimeStateContributorDiscoveryStored
ActivityObjectExitRuntimeStateContributorDiscoveryCleared
ActivityObjectExitRuntimeStateInventoryPreviewStored
ActivityObjectExitRuntimeStateInventoryValidationStored
ActivityObjectExitRuntimeStateInventoryCleared
ActivityObjectExitRuntimeStateSnapshotPayloadStored
ActivityObjectExitRuntimeStateSnapshotPayloadRead
ActivityObjectExitRuntimeStateSnapshotPayloadCleared
```

Campos mínimos:

```text
owner='ActivityObjectExitRuntimeState'
activityId
entrySequence
hasDiscoveryBefore
hasDiscoveryAfter
hasInventoryPreviewBefore
hasInventoryPreviewAfter
hasInventoryValidationBefore
hasInventoryValidationAfter
hasSnapshotPayloadBefore
hasSnapshotPayloadAfter
discoveredCount
inventoryOwnerCount
inventoryCapabilityCount
validationStatus
snapshotObjectCount
source
reason
```

Regra:

```text
Esses eventos são complementares.
Não substituir eventos canônicos dos stages.
```

Eventos canônicos que devem permanecer:

```text
ActivityObjectSnapshotCaptureStarted
ActivityObjectSnapshotCaptureCompleted
ActivityObjectReleaseStarted
ActivityObjectReleaseCompleted
ActivityObjectContributorUnregisterStarted
ActivityObjectContributorUnregistered
ActivityObjectContributorUnregisterCompleted
```

---

## 11. Critério de smoke para SA-7H3A

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

ActivityObjectExitRuntimeState* observado
ActivityObjectSnapshotCaptureStage preservado
ActivityObjectReleaseStage preservado
ActivityObjectContributorUnregisterStage preservado
ActivityContentReleaseRuntimeState preservado
ActivityContentReleaseFinalizationStage preservado
ActivityContentReleaseContinuationResolved/Started/Completed preservado

ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed

activity_01:
  discovery count > 0
  snapshot payload stored/read quando aplicável
  unregister count > 0

activity_02:
  no-content/negação preservado
  unregister count = 0
  skippedNoContributors = true
```

---

## 12. Respostas obrigatórias

### Qual pipeline é dono desta decisão?

```text
SessionActivityPipeline continua dono do macro lifecycle de object exit.
```

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
ActivityObjectExitRuntimeState = runtime state.
ActivityObjectSnapshotCaptureStage = stage.
ActivityObjectReleaseStage = stage.
ActivityObjectContributorUnregisterStage = stage.
Snapshot payload = snapshot/payload.
RouteActivitySave = consumer/adapter externo de save-on-exit.
Bridges atuais = bridges transitórias.
```

### Isso é comportamento final ou bridge transitória?

```text
ActivityObjectExitRuntimeState é caminho final provável.
As bridges atuais são transitórias.
```

### Essa compatibilidade ainda é necessária?

```text
Compatibilidade de produção não é necessária.
A manutenção temporária das bridges como facade fina é necessária para reduzir risco no primeiro corte.
Não deve haver fallback paralelo.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

```text
O sintoma é bridge de object exit ainda expor state do pipeline.
A fronteira correta é mover state para ActivityObjectExitRuntimeState sem mover lifecycle nem save.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
Não no desenho proposto.
O pipeline decide lifecycle.
O runtime state guarda dados.
Stages executam passos.
RouteActivitySave consome payload.
```

---

## 13. Conclusão

`SA-7H3` fecha como design.

Próximo corte recomendado:

```text
SA-7H3A — ActivityObjectExitRuntimeState implementation
```

Regra central:

```text
Mover state de object exit.
Não mover lifecycle.
Não mover save.
Não mover content release.
```
