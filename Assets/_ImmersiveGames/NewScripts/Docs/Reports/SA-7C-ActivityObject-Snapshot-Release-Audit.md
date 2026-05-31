# SA-7C — ActivityObject Snapshot / Release / Unregister / Content Release Audit

Status: `CLOSED / AUDIT ONLY`

Data: 2026-05-31

## Escopo

Esta auditoria avalia o bloco de saída de objetos da `SessionActivity` depois do `SA-7B`, com foco em:

```text
ActivityObjectSnapshotCapture
ActivityObjectRelease
ActivityObjectContributorUnregister
ActivityContentRelease / scene unload async
RouteActivitySave payload
```

Não houve alteração runtime neste corte.

## Resumo executivo

Resultado da auditoria:

```text
Não mover snapshot, release, unregister e content unload juntos.
Não criar ActivityExitPipeline.
Não transformar RouteActivitySave em owner de snapshot.
Extrair primeiro ActivityObjectSnapshotCaptureStage em corte próprio.
Depois extrair ActivityObjectReleaseStage.
Depois extrair ActivityObjectContributorUnregisterStage.
Só depois auditar ActivityContentRelease async.
```

Motivo:

```text
O bloco atual mistura captura de payload para save, execução de release endpoint,
limpeza de discovery/runtime state, unload async de cenas e continuação de rail.
Mover tudo junto aumenta risco de quebrar restart, Activity01->Activity02 e RouteExit.
```

## Perguntas obrigatórias respondidas

### Qual pipeline é dono desta decisão?

`SessionActivityPipeline` continua dono do macro lifecycle de saída.

Ele decide quando iniciar dematerialization/release conforme o rail ativo:

```text
CompleteCurrentActivity
RestartCurrentActivity
Activity01 -> Activity02
RouteExit / BackToMenu
```

A execução concreta deve sair para stages determinísticos, mas sem criar pipeline novo.

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

| Item | Classificação correta |
|---|---|
| Decidir quando capturar snapshot | Lifecycle/order do `SessionActivityPipeline` |
| Capturar snapshot de objetos | `ActivityObjectSnapshotCaptureStage` |
| Resultado/payload capturado | Snapshot/runtime payload para `RouteActivitySave` consumir depois |
| Decidir se provider ausente é skip/fail | Policy do stage, baseada em requirement/inventory atual |
| Chamar provider `CaptureSnapshot` | Stage chamando endpoint/provider local |
| Executar release endpoint | `ActivityObjectReleaseStage` |
| Limpar contributor discovery/state | `ActivityObjectContributorUnregisterStage` |
| Unload de content scene | `ActivityContentReleaseStage` / async operation futura |
| Persistir save | `SessionOperational` / `RouteActivitySave` + `SaveRuntime`, não SessionActivity |

### Isso é comportamento final ou bridge transitória?

O nested `ActivityObjectExitStage` atual é **bridge transitória**.

Ele parece stage pelo nome, mas só repassa chamadas para métodos privados do próprio `SessionActivityPipeline`:

```text
CaptureSnapshot -> EmitObjectSnapshotCaptureStageCore
Release -> EmitObjectReleaseStageCore
UnregisterContributors -> EmitObjectContributorUnregisterStageCore
```

Isso não é stage Base 2.0 real, porque não tem contrato próprio, command/result próprio nem ownership visível fora do macro pipeline.

### Essa compatibilidade ainda é necessária?

Não há compatibilidade de produção a preservar.

Mas há **comportamento validado por smoke** que precisa ser preservado:

```text
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RouteActivitySave payload skip/load/save sem regressão
```

Portanto, a restrição não é compatibilidade legada; é preservação do baseline funcional validado.

### O erro está no sintoma ou na fronteira arquitetural errada?

Está na fronteira arquitetural.

O sintoma é o `SessionActivityPipeline` grande. A causa é que ele ainda executa diretamente:

```text
provider snapshot capture
release endpoint dispatch
unregister de contributors
pending operation de unload de content scenes
payload save-on-exit state
```

A correção correta é extrair stages reais por responsabilidade, não criar partial nem mover tudo para outro pipeline.

### Existe owner duplicado para o mesmo lifecycle?

Ainda não, desde que não seja criado `ActivityExitPipeline`.

Risco de owner duplicado aparece se:

```text
RouteActivitySave começar a decidir snapshot capture;
ActivityContentReleaseStage decidir continuation/route;
ActivityObjectReleaseStage decidir Activity completion/restart/route-exit;
ActivityExitPipeline duplicar SessionActivityPipeline.
```

Esses caminhos devem ser evitados.

## Fluxo atual observado

O fluxo atual de dematerialization/release é:

```text
SessionActivityPipeline
-> TryStartActivityContentReleaseForContinuation
   -> SessionActivityDematerializationStarted
   -> EmitObjectSnapshotCaptureStage
      -> ActivityObjectSnapshotCapture provider(s)
      -> _lastSnapshotPayloadForSaveOnExit
   -> EmitObjectReleaseStage
      -> ReleaseEndpoint(s)
   -> if no content scenes:
      -> ActivityContentReleaseSkippedNoContent
      -> EmitObjectContributorUnregisterStage
      -> ActivityContentReleaseCompleted
   -> if content scenes:
      -> ActivityContentReleaseStarted
      -> ActivityContentRetentionPlanResolved
      -> _pendingActivityContentReleaseContext
      -> ExecuteNextActivityContentSceneRelease
         -> pending ActivityContentSceneUnload operation
         -> CompleteActivityContentSceneUnloadOperation
         -> FinalizeActivityContentReleaseCompleted
            -> ActivityContentReleaseCompleted
            -> EmitObjectContributorUnregisterStage
            -> ClearCurrentActivityContentLoadedSet
```

## Achados principais

### 1. `ActivityObjectExitStage` interno não é stage real

Existe uma classe interna:

```text
SessionActivityPipeline.ActivityObjectExitStage
```

Ela contém métodos de fachada:

```text
CaptureSnapshot(...)
Release(...)
UnregisterContributors(...)
```

Mas esses métodos chamam de volta o owner:

```text
_owner.EmitObjectSnapshotCaptureStageCore(...)
_owner.EmitObjectReleaseStageCore(...)
_owner.EmitObjectContributorUnregisterStageCore(...)
```

Conclusão:

```text
Isso é bridge nominal/transitória, não stage decomposto.
```

### 2. Snapshot capture mistura stage, policy, endpoint e payload de save

`EmitObjectSnapshotCaptureStageCore` faz tudo no mesmo bloco:

```text
lê CurrentActivityObjectContributorDiscoveryResult
filtra providers
classifica skip/fail
monta ActivityObjectSnapshotCaptureCommand
chama provider.CaptureSnapshot(...)
valida stale/foreign result
agrega captured objects
preenche _lastSnapshotPayloadForSaveOnExit
marca _lastSnapshotCaptureFailedForSaveOnExit
emite facts/snapshots
```

Owner correto:

```text
ActivityObjectSnapshotCaptureStage executa captura.
Snapshot providers capturam estado local.
SessionActivityPipeline mantém apenas a decisão de quando chamar o stage.
RouteActivitySave consome payload depois, sem decidir captura.
```

### 3. `TryGetSnapshotPayloadForSaveOnExit` é API correta, mas o payload writer ainda está preso no macro pipeline

O método público:

```text
TryGetSnapshotPayloadForSaveOnExit(sessionStateId, out payload, out failureReason)
```

é uma fronteira útil para `RouteActivitySave` consumir o resultado.

O problema é o writer do payload:

```text
_lastSnapshotPayloadForSaveOnExit
_lastSnapshotCaptureFailedForSaveOnExit
_lastSnapshotCaptureFailureDetail
```

ainda é preenchido dentro do método core do macro pipeline.

Corte recomendado:

```text
ActivityObjectSnapshotCaptureStage produz ActivityObjectSnapshotCaptureStageResult.
SessionActivityPipeline/bridge registra o payload em um writer mínimo.
TryGetSnapshotPayloadForSaveOnExit permanece como API pública por enquanto.
```

### 4. Object release mistura discovery, endpoint dispatch, policy e facts

`EmitObjectReleaseStageCore` ainda:

```text
lê discovery result
classifica no contributors / stale / valid
monta release commands
chama release endpoints
agrega applied/skipped/failed
emite QACheckpoint/facts/snapshots
```

Owner correto:

```text
ActivityObjectReleaseStage executa release determinístico.
Release endpoints aplicam side-effects locais.
Pipeline macro não itera endpoints diretamente.
```

### 5. Contributor unregister deve ser stage separado, não detalhe de content unload

`EmitObjectContributorUnregisterStageCore` atualmente roda:

```text
quando content release é skipped no-content;
ou após FinalizeActivityContentReleaseCompleted.
```

Ele também limpa:

```text
CurrentActivityObjectContributorDiscoveryResult
```

Esse comportamento deve ser preservado, mas o owner deve ficar mais claro:

```text
ActivityObjectContributorUnregisterStage executa unregister/cleanup.
SessionActivityPipeline decide o ponto exato de chamada conforme content release path.
```

### 6. ActivityContentRelease async é o bloco mais sensível e não deve entrar no próximo patch

Este bloco inclui:

```text
_pendingActivityContentReleaseContext
_awaitingContinuationAfterActivityContentRelease
BuildActivityContentReleasePendingOperation
ExecuteNextActivityContentSceneRelease
_pendingOperationRunner.RunActivityContentReleaseOperation
CompleteActivityContentSceneUnloadOperation
FinalizeActivityContentReleaseCompleted
```

Ele decide continuação após unload assíncrono. Mover agora junto com snapshot/release seria arriscado.

Decisão:

```text
Manter ActivityContentRelease async no SessionActivityPipeline até snapshot/release/unregister estarem extraídos e validados.
```

### 7. ActivityObject snapshot/release não deve ser um único stage permanente

Embora a auditoria tenha sido chamada de `SnapshotAndRelease`, o desenho final não deve colapsar tudo em um stage único.

Recomendação:

```text
SA-7D: ActivityObjectSnapshotCaptureStage
SA-7E: ActivityObjectReleaseStage
SA-7F: ActivityObjectContributorUnregisterStage
SA-7G: ActivityContentReleaseAsync audit/extraction
```

Motivo:

```text
Snapshot produz payload.
Release executa side-effects.
Unregister limpa registro/runtime state.
Content unload gerencia pending async operation.
```

São responsabilidades diferentes.

## Matriz de auditoria

| Arquivo / classe / método | Responsabilidade atual | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---|---|---|---|
| `SessionActivityPipeline.TryStartActivityContentReleaseForContinuation` | Inicia dematerialization, chama snapshot, release, content release/unload e continuation. | `SessionActivityPipeline` macro lifecycle + stages dedicados. | Método orquestra corretamente, mas ainda executa detalhes concretos por helpers internos. | Alta | Manter macro; substituir helpers por stages reais em cortes pequenos. | Alto | Controla restart, completion, route-exit e pending context. |
| `SessionActivityPipeline.EmitObjectSnapshotCaptureStage` | Fachada para capture. | Stage dedicado. | Chama nested bridge que volta para o owner. | Média | Substituir por `ActivityObjectSnapshotCaptureStage`. | Médio | Não deve mudar ordering. |
| `SessionActivityPipeline.EmitObjectSnapshotCaptureStageCore` | Captura snapshot, classifica providers, monta payload save-on-exit. | `ActivityObjectSnapshotCaptureStage` + payload writer bridge. | Mistura policy, endpoint calls, payload state e fact emission. | Alta | Próximo patch: extrair apenas snapshot capture. | Alto | `RouteActivitySave` depende do payload. |
| `SessionActivityPipeline.ExecuteObjectSnapshotCaptureCommand` | Chama provider `CaptureSnapshot`. | Stage ou adapter local de snapshot capture. | Execução concreta no macro pipeline. | Média/Alta | Mover junto com snapshot stage. | Médio | Deve preservar stale/foreign result validation. |
| `SessionActivityPipeline.TryGetSnapshotPayloadForSaveOnExit` | API para Operational/RouteActivitySave obter payload. | API pública do provider de payload, mantida por enquanto. | API é correta; writer do payload ainda é acoplado ao macro method. | Média | Manter API; mover writer para bridge/field update comandado pelo stage. | Médio | Evita mexer em RouteActivitySave agora. |
| `_lastSnapshotPayloadForSaveOnExit` e flags de falha | Estado do último payload capturado para save-on-exit. | Snapshot payload state/writer dentro de SessionActivity até extração posterior. | Estado técnico preso no macro pipeline. | Média | Criar bridge mínima para stage registrar result. | Médio | Não criar Save owner novo. |
| `SessionActivityPipeline.EmitObjectReleaseStage` | Fachada para release. | `ActivityObjectReleaseStage`. | Chama nested bridge que volta ao owner. | Média | Extrair depois do snapshot stage. | Médio/Alto | Release endpoint pode ter side-effects. |
| `SessionActivityPipeline.EmitObjectReleaseStageCore` | Executa release endpoints e emite checkpoint/facts. | `ActivityObjectReleaseStage` + endpoints locais. | Macro pipeline itera endpoints diretamente. | Alta | SA-7E após snapshot PASS. | Alto | Precisa preservar ActivityObjectRelease PASS. |
| `SessionActivityPipeline.EmitObjectContributorUnregisterStage` | Fachada para unregister. | `ActivityObjectContributorUnregisterStage`. | Chamado em dois paths de content release; timing é sensível. | Média/Alta | Extrair depois do release stage; preservar pontos de chamada. | Médio/Alto | Limpa discovery result. |
| `SessionActivityPipeline.EmitObjectContributorUnregisterStageCore` | Unregister/cleanup de contributors. | `ActivityObjectContributorUnregisterStage`. | Mistura cleanup state e facts dentro do macro pipeline. | Média | SA-7F. | Médio | Deve preservar no-content e content-unload paths. |
| `ActivityObjectExitStage` nested | Wrapper interno para capture/release/unregister. | Remover/substituir por stages reais. | Bridge nominal; não altera ownership real. | Média | Remover gradualmente quando stages reais existirem. | Baixo/Médio | Não deve ficar como solução final. |
| `ExecuteNextActivityContentSceneRelease` | Dispara unload async da próxima content scene. | Futuro `ActivityContentReleaseStage`; por enquanto macro pipeline. | Sensível a pending operation. | Alta | Não mover no próximo patch. Auditar depois. | Alto | Já causou regressões de pending em histórico do projeto. |
| `FinalizeActivityContentReleaseCompleted` | Completa content release, emite facts e chama unregister. | Macro lifecycle + future content release stage. | Mistura completion async com unregister. | Alta | Não mover agora; separar depois de unregister stage. | Alto | Determina continuação do rail. |
| `RouteActivitySave` consumer | Consome payload para save-on-exit. | SessionOperational/RouteActivitySave + SaveRuntime. | Não é problema se continuar consumidor. | Baixa | Não alterar nos próximos cortes. | Alto se alterado | Save não deve decidir snapshot capture. |

## Plano recomendado

### SA-7D — ActivityObjectSnapshotCaptureStage

Escopo:

```text
Extrair captura de snapshot para stage dedicado.
Manter TryStartActivityContentReleaseForContinuation no SessionActivityPipeline.
Manter ActivityObjectRelease no caminho atual.
Manter ContentRelease async no caminho atual.
Manter TryGetSnapshotPayloadForSaveOnExit como API pública.
```

Aceite arquitetural:

```text
SessionActivityPipeline decide quando capturar.
ActivityObjectSnapshotCaptureStage captura.
Snapshot providers executam captura local.
Stage retorna result/payload.
Bridge mínima registra _lastSnapshotPayloadForSaveOnExit e failure flags.
Nenhum save é executado pelo stage.
```

Escopo proibido:

```text
Não mover ObjectRelease.
Não mover ContributorUnregister.
Não mover ActivityContentRelease async.
Não alterar RouteActivitySave.
Não criar ActivityExitPipeline.
Não criar fallback silencioso.
```

Smoke requerido:

```text
ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
RouteActivitySave save/load behavior preservado
sem FATAL / Exception / route_transition_failed / foreign/stale
```

### SA-7E — ActivityObjectReleaseStage

Só depois do SA-7D PASS.

Escopo:

```text
Mover release endpoint dispatch e aggregation.
Preservar ordering depois de snapshot capture e antes de content unload.
```

### SA-7F — ActivityObjectContributorUnregisterStage

Só depois do SA-7E PASS.

Escopo:

```text
Mover unregister/cleanup dos contributors.
Preservar chamada em no-content release e after-content-unload release.
```

### SA-7G — ActivityContentReleaseAsync audit

Só depois de snapshot/release/unregister estarem estáveis.

Escopo:

```text
Auditar pending context e scene unload async.
Decidir se vira stage dedicado ou permanece macro lifecycle por mais tempo.
```

## Decisão congelada

```text
SA-7C fecha como auditoria somente.
O próximo patch deve ser SA-7D ActivityObjectSnapshotCaptureStage.
Não implementar Snapshot+Release juntos.
Não mover ActivityContentRelease async ainda.
```
