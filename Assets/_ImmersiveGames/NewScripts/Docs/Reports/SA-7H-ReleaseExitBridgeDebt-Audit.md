# SA-7H — Release/Exit Bridge Debt Audit

Status: `CLOSED / AUDIT ONLY`  
Área: `SessionActivity` / release-exit decomposition / transient bridges / runtime state ownership  
Sem alteração runtime.

---

## 1. Objetivo

Auditar as bridges transitórias criadas durante os cortes de release/exit da `SessionActivity`.

Os cortes anteriores reduziram o corpo concreto do `SessionActivityPipeline`, mas criaram bridges mínimas para permitir que stages dedicados acessassem state ainda preso no pipeline.

Esta auditoria responde:

```text
quais bridges existem;
qual responsabilidade cada uma expõe;
se a bridge é aceitável temporariamente;
qual owner final provável;
qual o próximo corte seguro;
quais bridges não devem virar manager/coordinator.
```

---

## 2. Baseline funcional

Baseline funcional imediatamente anterior:

```text
SA-7B  — ActivityExitActorTeardownStage PASS
SA-7D  — ActivityObjectSnapshotCaptureStage PASS
SA-7E  — ActivityObjectReleaseStage PASS
SA-7F  — ActivityObjectContributorUnregisterStage PASS
SA-7G1 — ActivityContentSceneUnloadDispatchStage PASS parcial
SA-7G2A-H1 — ActivityContentReleaseFinalizationStage + observability PASS
SA-7G2B-H1 — ActivityContentReleaseContinuationObservability PASS
```

O último smoke validou:

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

---

## 3. Decisão principal

Não remover todas as bridges agora.

A decisão correta é separar as bridges em três grupos:

```text
Grupo A — Bridges de state técnico ainda aceitáveis temporariamente.
Grupo B — Bridges que devem virar runtime state/port explícito em corte futuro.
Grupo C — Bridges que não são débito grave porque representam adapter/runner técnico.
```

O próximo corte recomendado não deve implementar remoção ampla.

Próximo passo seguro:

```text
SA-7H1 — SessionActivityExitRuntimeState audit/design
```

Objetivo do H1:

```text
desenhar um runtime state explícito para release/exit;
mapear que campos saem do SessionActivityPipeline;
não mover código ainda;
não alterar smoke path.
```

---

## 4. Bridges transitórias encontradas

### 4.1 `IActivityExitActorTeardownRuntimeBridge`

Criada no SA-7B.

Expõe:

```text
active ActorPresentation handles;
active ActorAttribute capabilities;
active ActorParticipation records;
player participant binding resolution;
player actor participation adapter/registry;
actor inventory feed for exit.
```

Leitura:

```text
É a bridge mais larga.
Ela permite que ActivityExitActorTeardownStage execute teardown sem mover ainda os stores internos.
É aceitável como transição, mas não deve virar manager.
```

Owner final provável:

```text
Actor participation/presentation/attributes devem convergir para ActivityActorRuntimeState ou ActivityParticipationRuntimeState.
Pipeline continua decidindo quando teardown roda.
Stage continua executando teardown.
Adapters/endpoints continuam side-effects.
```

Risco:

```text
Médio/alto.
Se expandir, vira coordinator de actors dentro de exit.
```

Ação recomendada:

```text
Congelar interface.
Não adicionar novos métodos.
Projetar runtime state explícito antes de mexer.
```

---

### 4.2 `IActivityObjectSnapshotCaptureRuntimeBridge`

Criada no SA-7D.

Expõe:

```text
CurrentActivityObjectContributorDiscoveryResult;
CurrentActivityCapabilityInventoryPreview;
CurrentActivityCapabilityInventoryPreviewValidation;
SetSnapshotPayloadForSaveOnExit.
```

Leitura:

```text
Bridge de leitura de inventory/discovery e escrita de payload.
Aceitável temporariamente.
O ponto sensível é SetSnapshotPayloadForSaveOnExit, porque toca payload consumido por RouteActivitySave.
```

Owner final provável:

```text
ActivityObjectRuntimeState ou ActivityExitRuntimeState deve reter discovery/inventory snapshot.
RouteActivitySave continua consumidor do payload; não vira owner de capture.
```

Risco:

```text
Médio.
Pode misturar snapshot capture com save se crescer.
```

Ação recomendada:

```text
Manter até existir runtime state explícito para object exit.
Não expor SaveRuntime ou RouteActivitySave pela bridge.
```

---

### 4.3 `IActivityObjectReleaseRuntimeBridge`

Criada no SA-7E.

Expõe:

```text
CurrentActivityObjectContributorDiscoveryResult;
CurrentActivityCapabilityInventoryPreview;
CurrentActivityCapabilityInventoryPreviewValidation.
```

Leitura:

```text
Bridge estreita.
É aceitável temporariamente.
Só fornece contexto para o stage resolver release endpoints.
```

Owner final provável:

```text
ActivityObjectRuntimeState.
```

Risco:

```text
Baixo/médio.
```

Ação recomendada:

```text
Manter até a unificação de object exit state.
Não adicionar métodos de unload scene, save ou continuation.
```

---

### 4.4 `IActivityObjectContributorUnregisterRuntimeBridge`

Criada no SA-7F.

Expõe:

```text
GetCurrentActivityObjectContributorDiscoveryResult;
ClearCurrentActivityObjectContributorDiscoveryResult.
```

Leitura:

```text
Bridge estreita.
O cleanup é legítimo para finalization, mas o state ainda está no pipeline.
```

Owner final provável:

```text
ActivityObjectRuntimeState.
```

Risco:

```text
Baixo.
```

Ação recomendada:

```text
Manter por enquanto.
Migrar junto com discovery/result state, não isoladamente.
```

---

### 4.5 `IActivityContentSceneUnloadDispatchRuntimeBridge`

Criada no SA-7G1.

Expõe:

```text
BuildActivityContentReleasePendingOperation;
RunActivityContentReleaseOperation.
```

Usa também endpoint existente para:

```text
SetPendingOperation.
```

Leitura:

```text
Bridge de dispatch async.
É aceitável temporariamente, mas está próxima da fronteira de pending operation.
```

Owner final provável:

```text
PendingOperationRuntime ou ActivityContentReleaseRuntimeState no futuro.
O pipeline deve continuar owner da continuation macro.
```

Risco:

```text
Médio/alto.
Se crescer para callback/failure/continuation, vira ActivityContentReleasePipeline disfarçado.
```

Ação recomendada:

```text
Congelar.
Não mover CompleteActivityContentSceneUnloadOperation para essa bridge.
Não expor route-exit/restart/deactivation pela bridge.
```

---

### 4.6 `IActivityContentReleaseFinalizationRuntimeBridge`

Criada no SA-7G2A.

Expõe:

```text
HasCurrentActivityContentLoadedSet;
HasPendingActivityContentReleaseContext;
IsAwaitingContinuationAfterActivityContentRelease;
ExecuteActivityObjectContributorUnregister;
ClearCurrentActivityContentLoadedSet;
ClearPendingActivityContentReleaseContext;
SetAwaitingContinuationAfterActivityContentRelease.
```

Leitura:

```text
Bridge de finalization/cleanup.
Aceitável temporariamente.
É uma candidata forte para virar ActivityContentReleaseRuntimeState.
```

Owner final provável:

```text
ActivityContentReleaseRuntimeState para loaded set/context/awaiting flag.
ActivityObjectContributorUnregisterStage continua stage dedicado.
SessionActivityPipeline continua owner da continuation.
```

Risco:

```text
Médio.
Se ganhar StartPendingRestartEntry ou CompleteRouteExitClosure, quebra fronteira.
```

Ação recomendada:

```text
Não adicionar métodos de continuation.
Migrar state de content release em conjunto, não método por método.
```

---

### 4.7 Observability de continuation sem bridge

Criada no SA-7G2B-H1.

Leitura:

```text
Não criou stage nem bridge.
Correto: owner='SessionActivityPipeline'.
A continuação macro ficou verificável sem mover lifecycle.
```

Risco:

```text
Baixo.
```

Ação recomendada:

```text
Manter.
Qualquer extração futura exige nova auditoria.
```

---

## 5. Matriz consolidada

| Bridge | Stage consumidor | Tipo de acoplamento | Severidade | Aceitável agora? | Owner final provável | Ação |
|---|---|---:|---|---|---|---|
| `IActivityExitActorTeardownRuntimeBridge` | `ActivityExitActorTeardownStage` | Actor stores + adapters + feed | Alta | Sim, congelada | `ActivityActorRuntimeState` / `ActivityParticipationRuntimeState` | Projetar state explícito antes de remover |
| `IActivityObjectSnapshotCaptureRuntimeBridge` | `ActivityObjectSnapshotCaptureStage` | Discovery/inventory + snapshot payload | Média | Sim | `ActivityObjectRuntimeState` / `ActivityExitRuntimeState` | Migrar com object exit state |
| `IActivityObjectReleaseRuntimeBridge` | `ActivityObjectReleaseStage` | Discovery/inventory | Baixa/média | Sim | `ActivityObjectRuntimeState` | Manter estreita |
| `IActivityObjectContributorUnregisterRuntimeBridge` | `ActivityObjectContributorUnregisterStage` | Discovery cleanup | Baixa | Sim | `ActivityObjectRuntimeState` | Migrar junto com discovery |
| `IActivityContentSceneUnloadDispatchRuntimeBridge` | `ActivityContentSceneUnloadDispatchStage` | Pending operation dispatch | Média/alta | Sim, congelada | `PendingOperationRuntime` / `ActivityContentReleaseRuntimeState` | Não expandir para callback/continuation |
| `IActivityContentReleaseFinalizationRuntimeBridge` | `ActivityContentReleaseFinalizationStage` | Release state cleanup + unregister call | Média | Sim | `ActivityContentReleaseRuntimeState` | Não expor continuation |
| Nenhuma bridge | Continuation observability | Fact/OBS | Baixa | Sim | `SessionActivityPipeline` | Manter owner macro |

---

## 6. Respostas obrigatórias

### Qual pipeline é dono desta decisão?

```text
SessionActivityPipeline continua dono do macro lifecycle de saída, dematerialization, release ordering e continuation.
```

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
As bridges são infraestrutura transitória entre stages e state ainda preso no pipeline.
Stages executam passos determinísticos.
Facts/logs registram o ocorrido.
Adapters/endpoints executam side-effects.
As bridges não devem virar owner, policy, manager ou pipeline.
```

### Isso é comportamento final ou bridge transitória?

```text
Todas as interfaces listadas são bridges transitórias.
Nenhuma deve ser tratada como contrato final de runtime.
```

### Essa compatibilidade ainda é necessária?

```text
Compatibilidade de produção não é necessária.
Mas a transição é necessária para preservar baseline funcional enquanto o state ainda não foi movido.
Não deve existir fallback paralelo.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

```text
O sintoma era o pipeline grande.
A fronteira agora está melhor, mas o state ainda está no pipeline.
O próximo problema real não é mais mover métodos; é definir runtime state explícito para não depender de bridge.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
No estado atual, não.
O risco surgiria se uma bridge passasse a decidir continuation, route-exit, restart ou next activity.
```

---

## 7. Decisão de próximo passo

Não implementar remoção de bridge agora.

Próximo corte recomendado:

```text
SA-7H1 — SessionActivityExitRuntimeState audit/design
```

### Objetivo

Desenhar o state explícito que substituirá parte das bridges.

Escopo:

```text
mapear fields internos do SessionActivityPipeline usados por release/exit;
propor ActivityExitRuntimeState / ActivityObjectRuntimeState / ActivityContentReleaseRuntimeState;
definir ownership de loaded set, pending release context, discovery result, inventory preview, snapshot payload;
definir quais stages recebem state direto e quais continuam recebendo command;
definir smoke e observabilidade.
```

### Proibido no H1

```text
Não mover código runtime.
Não alterar lifecycle.
Não remover bridge ainda.
Não criar manager/coordinator.
Não criar ActivityExitPipeline.
Não criar ActivityContentReleasePipeline.
Não mover continuation macro.
```

---

## 8. Critério para futura implementação pós-H1

Uma futura remoção de bridges só pode ser aceita se preservar:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
ActivityContentReleaseCompleted observável
ActivityContentReleaseContinuationResolved/Started/Completed observável
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

E ainda:

```text
sem fallback silencioso;
sem stage decidindo continuation macro;
sem duplicar owner de lifecycle;
sem expor SaveRuntime/Operational adapters dentro de object/content stages;
sem mover pending operation callback para stage de dispatch.
```

---

## 9. Conclusão

`SA-7H` fecha como auditoria.

A decomposição de release/exit reduziu o monólito, mas criou bridges transitórias aceitáveis.

O próximo problema correto não é “remover tudo”, mas desenhar o state explícito:

```text
SA-7H1 — SessionActivityExitRuntimeState audit/design
```

Regra central:

```text
Não expandir bridges.
Desenhar runtime state.
Só então remover bridges por cortes pequenos.
```
