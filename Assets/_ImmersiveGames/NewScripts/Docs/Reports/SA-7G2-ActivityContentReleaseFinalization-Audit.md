# SA-7G2 — ActivityContentReleaseFinalization Audit

Status: `CLOSED / AUDIT ONLY`  
Área: `SessionActivity` / `ActivityContentReleaseCompleted` / finalization / cleanup / continuation / observability  
Sem alteração runtime.

---

## 1. Objetivo

Auditar o fechamento de `ActivityContentRelease` antes de qualquer extração.

Este bloco começa depois que todas as scenes de `ActivityContent` já foram descarregadas ou quando não há content próprio para descarregar.

O foco desta auditoria é:

```text
ActivityContentReleaseCompleted;
ActivityObjectContributorUnregisterStage;
limpeza de CurrentActivityContentLoadedSet;
limpeza de PendingActivityContentReleaseContext;
limpeza de awaiting flags;
continuação para restart, next activity, route-exit ou deactivation;
observabilidade canônica do fechamento.
```

O objetivo não é mover código agora. O objetivo é definir o corte seguro e impedir perda de observabilidade.

---

## 2. Decisão da auditoria

Não mover toda a finalização ainda.

A decisão correta é separar em dois cortes:

```text
SA-7G2A — ActivityContentReleaseFinalizationStage
SA-7G2B — ActivityContentReleaseContinuation audit/extraction
```

`SA-7G2A` deve extrair somente o fechamento determinístico de release:

```text
emitir ActivityContentReleaseCompleted;
emitir SessionActivityDematerializationCompleted;
executar ActivityObjectContributorUnregisterStage;
limpar CurrentActivityContentLoadedSet;
limpar PendingActivityContentReleaseContext;
limpar awaiting flags;
registrar facts/snapshots de finalization.
```

`SA-7G2A` não deve decidir:

```text
StartPendingRestartEntry;
ContinueAfterDeactivationAsync;
CompleteRouteExitClosure;
next activity continuation;
route-exit handoff;
```

Essas decisões ficam no `SessionActivityPipeline` até `SA-7G2B`.

---

## 3. Estado atual observado no fluxo

### 3.1 Finalização nominal com content

Fluxo atual esperado:

```text
ActivityObjectSnapshotCaptureStage
-> ActivityObjectReleaseStage
-> ActivityContentSceneUnloadDispatchStage
-> pendingOperation ActivityContentSceneUnload
-> CompleteActivityContentSceneUnloadOperation
-> FinalizeActivityContentReleaseCompleted
-> ActivityContentReleaseCompleted
-> SessionActivityDematerializationCompleted
-> ActivityObjectContributorUnregisterStage
-> limpar loaded set/context
-> seguir continuation macro
```

### 3.2 Finalização no-content

Fluxo atual esperado:

```text
TryStartActivityContentReleaseForContinuation
-> detecta ausência de loaded content / no content
-> ActivityContentReleaseCompleted status='SkippedNoContent'
-> ActivityObjectContributorUnregisterStage pode rodar com skippedNoContributors='True'
-> limpar contexts/flags
-> seguir continuation macro
```

### 3.3 Continuação macro

A continuação posterior pode ser:

```text
RestartCurrentActivity;
Activity01ToActivity02;
RouteExitBackToMenu;
deactivation completion normal.
```

Essas decisões ainda são macro lifecycle e devem permanecer no `SessionActivityPipeline`.

---

## 4. Matriz de auditoria

| Arquivo / classe / método | Responsabilidade atual | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---|---|---|---|
| `SessionActivityPipeline.FinalizeActivityContentReleaseCompleted` | Emite completed, dematerialization completed, unregister e limpa contexts. | Stage dedicado futuro para finalization; macro owner continua pipeline. | Concentra execução determinística de finalization no macro pipeline. | Média | Extrair para `ActivityContentReleaseFinalizationStage`. | Médio | Fecha release e limpa state técnico. |
| `SessionActivityPipeline.CompleteActivityContentSceneUnloadOperation` | Callback de unload, itera scenes e chama finalization. | `SessionActivityPipeline` por enquanto. | Mistura callback async e continuação macro; não deve ser movido junto. | Alta | Manter; chamar stage de finalization no ponto final. | Alto | Decide se há mais scenes e quando chegou ao fim. |
| `ActivityObjectContributorUnregisterStage` | Unregister determinístico de contributors. | Stage dedicado já correto. | Já extraído; deve ser chamado pela finalization. | Baixa | Manter, mas chamada pode migrar para finalization stage. | Baixo | SA-7F PASS. |
| `CurrentActivityContentLoadedSet` cleanup | Limpa referência de content carregado após release. | Finalization stage via bridge mínima. | State cleanup ainda está no macro pipeline. | Média | Stage deve limpar via bridge explícita. | Médio | Necessário antes de nova entry/restart. |
| `PendingActivityContentReleaseContext` cleanup | Limpa contexto de release async. | Finalization stage via bridge mínima. | State cleanup acoplado ao pipeline. | Média | Stage deve limpar via bridge explícita, sem assumir continuation. | Médio | Evita stale release context. |
| `_awaitingContinuationAfterActivityContentRelease` cleanup | Flag de bloqueio/continuação. | Finalization stage via bridge mínima. | Se limpar cedo/tarde, quebra restart/transition/route-exit. | Alta | Limpar somente no stage e validar ordem no smoke. | Alto | Flag coordena release async. |
| `StartPendingRestartEntry` | Continuação macro de restart. | `SessionActivityPipeline`. | Não é finalization determinística. | Alta | Não mover em SA-7G2A. | Alto | Cria nova entrySequence. |
| `CompleteRouteExitClosure` | Continuação macro de route-exit. | `SessionActivityPipeline`. | Não é finalization determinística. | Alta | Não mover em SA-7G2A. | Alto | Handoff com SessionOperational. |
| `ContinueAfterDeactivationAsync` | Continuação macro pós deactivation. | `SessionActivityPipeline`. | Não é finalization determinística. | Alta | Não mover em SA-7G2A. | Alto | Decide next activity/completion. |

---

## 5. Observabilidade obrigatória

`SA-7G1` mostrou que um corte pode passar funcionalmente, mas perder nomes literais usados por análise humana ou automática.

Portanto, `SA-7G2A` deve preservar ou introduzir observabilidade explícita, com nomes estáveis.

### 5.1 Eventos obrigatórios do stage

O stage futuro deve emitir:

```text
ActivityContentReleaseFinalizationStarted
ActivityContentReleaseCompleted
SessionActivityDematerializationCompleted
ActivityContentReleaseFinalizationCleanupStarted
ActivityContentReleaseFinalizationCleanupCompleted
ActivityContentReleaseFinalizationCompleted
```

### 5.2 Campos obrigatórios

Cada evento deve conter:

```text
owner='ActivityContentReleaseFinalizationStage'
pipelineId
sessionStateId
activityId
entrySequence
stage
source
reason
completionKind
status
loadedSceneCount
releasedSceneCount
skippedNoContent
pendingReleaseContextPresentBefore
pendingReleaseContextPresentAfter
loadedSetPresentBefore
loadedSetPresentAfter
awaitingContinuationBefore
awaitingContinuationAfter
continuationKind
```

### 5.3 Continuação explícita, mas sem decidir continuation

O stage pode registrar qual continuation está pendente, mas não pode executá-la.

Valores recomendados:

```text
RestartCurrentActivity
NextActivity
RouteExit
CompleteActivity
None
Unknown
```

Campo:

```text
continuationKind='<value>'
```

### 5.4 Eventos canônicos legados a preservar

Não perder os nomes atuais:

```text
ActivityContentReleaseCompleted
SessionActivityDematerializationCompleted
ActivityObjectContributorUnregisterStarted
ActivityObjectContributorUnregistered
ActivityObjectContributorUnregisterCompleted
```

Se o stage novo emitir eventos adicionais, eles são complementares, não substitutos silenciosos.

### 5.5 Checkpoints obrigatórios após SA-7G2A

O smoke deve preservar:

```text
ActivityObjectSnapshotCapture checkpointStatus='Passed'
ActivityObjectRelease checkpointStatus='Passed'
ActivityObjectContributorUnregister checkpointStatus='Passed'
RestartCurrentActivity checkpointStatus='Passed'
Activity01ToActivity02 checkpointStatus='Passed'
RouteExitBackToMenu checkpointStatus='Passed'
```

### 5.6 Regras de higiene

```text
Não substituir nome canônico por nome novo sem alias/fact equivalente.
Não registrar apenas evento genérico "Completed".
Não esconder cleanup em logs de debug não OBS.
Não usar fallback silencioso se context estiver ausente.
No-content deve ser skip explícito, não ausência de log.
Foreign/stale context deve ser rejeitado ou fail-fast conforme contrato.
```

---

## 6. Respostas obrigatórias

### Qual pipeline é dono desta decisão?

```text
SessionActivityPipeline continua dono do macro lifecycle.
Ele decide quando a finalization roda e qual continuation macro acontece depois.
```

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
ActivityContentReleaseFinalizationStage = stage determinístico futuro.
ActivityContentReleaseCompleted = fact/checkpoint observável.
SessionActivityDematerializationCompleted = fact de fase.
Cleanup de loaded set/context = state cleanup comandado pelo stage via bridge.
Continuation restart/route-exit/next/deactivation = macro lifecycle do SessionActivityPipeline.
```

### Isso é comportamento final ou bridge transitória?

```text
A extração do finalization stage pode ser comportamento final.
A bridge para limpar state do SessionActivityPipeline será transitória.
Continuation ainda dentro do SessionActivityPipeline é correta neste corte.
```

### Essa compatibilidade ainda é necessária?

```text
Compatibilidade de produção não é necessária.
Compatibilidade de observabilidade é necessária para evitar perder evidência.
Nomes canônicos de logs/checkpoints devem ser preservados.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

```text
O sintoma é o pipeline executar cleanup/finalization diretamente.
A fronteira correta é mover finalization determinística para stage, mas manter continuation macro no pipeline.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
Não deve existir.
Se o stage executar restart/route-exit/next activity, passaria a existir owner duplicado.
Por isso SA-7G2A deve finalizar release, não continuar lifecycle.
```

---

## 7. Próximo corte recomendado

```text
SA-7G2A — ActivityContentReleaseFinalizationStage
```

### Escopo permitido

```text
Criar ActivityContentReleaseFinalizationStage.
Criar IActivityContentReleaseFinalizationRuntimeBridge.
Mover emissão de ActivityContentReleaseCompleted.
Mover emissão de SessionActivityDematerializationCompleted.
Mover chamada para ActivityObjectContributorUnregisterStage.
Mover cleanup de CurrentActivityContentLoadedSet.
Mover cleanup de PendingActivityContentReleaseContext.
Mover cleanup de awaiting continuation flag.
Emitir observabilidade nova e preservar nomes canônicos antigos.
Substituir o corpo concreto de FinalizeActivityContentReleaseCompleted por chamada ao stage.
```

### Escopo proibido

```text
Não mover CompleteActivityContentSceneUnloadOperation.
Não mover FailPendingOperation.
Não mover StartPendingRestartEntry.
Não mover CompleteRouteExitClosure.
Não mover ContinueAfterDeactivationAsync.
Não mover NextActivity continuation.
Não criar ActivityContentReleasePipeline.
Não criar ActivityExitPipeline.
Não alterar UnityActivityContentSceneReleaseAdapter.
Não alterar UnitySessionActivityPendingOperationRunner.
Não alterar RouteActivitySave.
```

### Bridge mínima

A bridge deve expor somente:

```text
EmitFact / AppendSnapshot se o projeto exigir;
ExecuteActivityObjectContributorUnregister;
ClearCurrentActivityContentLoadedSet;
ClearPendingActivityContentReleaseContext;
SetAwaitingContinuationAfterActivityContentRelease(false);
GetFinalizationStateForObservability;
```

Não expor:

```text
StartPendingRestartEntry;
CompleteRouteExitClosure;
ContinueAfterDeactivationAsync;
Current route command;
Operational adapters;
Save services.
```

---

## 8. Critério de smoke para SA-7G2A

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

ActivityContentReleaseFinalizationStarted aparece
ActivityContentReleaseCompleted preservado
SessionActivityDematerializationCompleted preservado
ActivityContentReleaseFinalizationCleanupStarted aparece
ActivityContentReleaseFinalizationCleanupCompleted aparece
ActivityContentReleaseFinalizationCompleted aparece
ActivityObjectContributorUnregisterStarted/Completed preservado
pendingReleaseContextPresentBefore/After observado
loadedSetPresentBefore/After observado
awaitingContinuationBefore/After observado
continuationKind observado

ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS

activity_02 no-content preserva skip explícito
```

---

## 9. Conclusão

`SA-7G2` fecha a auditoria.

A extração segura é `SA-7G2A`:

```text
extrair finalization determinística;
preservar observabilidade canônica;
não mover continuation macro.
```

A regra principal é:

```text
Stage fecha o release.
Pipeline decide o que vem depois.
```
