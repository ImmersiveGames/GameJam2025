# SA-7G2B — ActivityContentReleaseContinuation Audit

Status: `CLOSED / AUDIT ONLY`  
Área: `SessionActivity` / `ActivityContentRelease` / macro continuation / restart / next activity / route-exit / deactivation  
Sem alteração runtime.

---

## 1. Objetivo

Auditar a continuação macro que acontece depois de `ActivityContentReleaseFinalizationStage`.

Os cortes anteriores já separaram:

```text
SA-7G1  -> dispatch de unload da scene de ActivityContent
SA-7G2A -> finalização determinística do release
SA-7G2A-H1 -> observabilidade literal de ActivityContentReleaseCompleted
```

Agora o ponto sensível é o que acontece **depois** do release estar finalizado:

```text
RestartCurrentActivity;
Activity01ToActivity02 / next activity;
RouteExitBackToMenu;
CompleteActivity / deactivation continuation.
```

Esta auditoria não move código. Ela define se há base para extração futura e quais limites não podem ser cruzados.

---

## 2. Decisão da auditoria

Não criar `ActivityContentReleaseContinuationStage` ainda.

A continuação pós-release ainda é **macro lifecycle** do `SessionActivityPipeline`.

A próxima melhoria segura deve ser **observabilidade e classificação**, não extração de ownership.

Recomendação:

```text
SA-7G2B-H1 — ActivityContentReleaseContinuationObservability
```

Objetivo do H1:

```text
emitir um fact/OBS explícito antes de executar a continuação macro;
classificar continuationKind;
registrar target activity / route-exit / restart;
preservar owner SessionActivityPipeline;
não mover execução.
```

Só depois de H1 e smoke podemos decidir se existe algum subtrecho extraível.

---

## 3. Estado atual após SA-7G2A-H1

O release fecha corretamente no stage:

```text
ActivityContentReleaseFinalizationStage
-> ActivityContentReleaseFinalizationStarted
-> ActivityContentReleaseCompleted
-> SessionActivityDematerializationCompleted
-> ActivityContentReleaseFinalizationCleanupStarted
-> ActivityObjectContributorUnregisterStage
-> cleanup
-> ActivityContentReleaseFinalizationCleanupCompleted
-> ActivityContentReleaseFinalizationCompleted
```

Depois disso, o `SessionActivityPipeline` segue para a continuação macro.

Caminhos observados/esperados:

```text
RestartCurrentActivity:
  release activity_01 entrySequence N
  start new activity_01 entrySequence N+1

Activity01ToActivity02:
  release activity_01
  prepare/enter activity_02

RouteExitBackToMenu:
  release current activity
  close route-exit handoff
  return control to SessionOperational

CompleteActivity:
  complete current activity or continue after deactivation
```

---

## 4. Matriz de auditoria

| Arquivo / classe / método | Responsabilidade atual | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---|---|---|---|
| `SessionActivityPipeline.CompleteActivityContentSceneUnloadOperation` | Recebe callback de unload, itera scenes, aciona finalization e depois continuation. | `SessionActivityPipeline` | Mistura callback async com decisão macro, mas ainda é owner correto. | Alta | Não mover agora; adicionar observabilidade explícita da continuation. | Alto | Decide quando não há mais scenes e cai no fluxo macro. |
| `SessionActivityPipeline.FinalizeActivityContentReleaseCompleted` após SA-7G2A | Deve delegar finalization para stage e retornar para continuation macro. | `SessionActivityPipeline` para continuation; stage para finalization. | Agora a fronteira está mais clara, mas continuation ainda não tem fact próprio. | Média | Adicionar fact `ActivityContentReleaseContinuationResolved/Started`. | Médio | H1 corrigiu observabilidade de release completed. |
| `StartPendingRestartEntry` | Inicia nova entry da mesma activity após restart. | `SessionActivityPipeline`. | Não é stage de release; cria novo lifecycle. | Alta | Não mover. Observar com continuationKind='RestartCurrentActivity'. | Alto | Cria nova entrySequence. |
| `CompleteRouteExitClosure` | Fecha route-exit e handoff com SessionOperational. | `SessionActivityPipeline` + SessionOperational handoff boundary. | Não é release; é fronteira macro com Operational. | Alta | Não mover. Observar com continuationKind='RouteExit'. | Alto | Ordering route-exit já foi checkpoint PASS. |
| `ContinueAfterDeactivationAsync` | Decide completion/next activity após deactivation. | `SessionActivityPipeline`. | Mistura decisões de next/completion; ainda é macro owner. | Alta | Não mover antes de auditoria própria. | Alto | Pode disparar Activity01->Activity02. |
| Pending restart state | Guarda intenção de restart durante deactivation/release. | `SessionActivityPipeline`. | State de macro lifecycle; se movido para stage cria owner duplicado. | Alta | Não expor por bridge de stage além de leitura observável. | Alto | Controla restart. |
| Pending route-exit state/handoff | Guarda intenção de route-exit. | `SessionActivityPipeline` / Operational handoff. | Fronteira sensível entre pipelines. | Alta | Não mover no SA-7G2B. | Alto | RouteExitBackToMenu depende disso. |
| Next activity handoff | Decide transição activity_01 -> activity_02. | `SessionActivityPipeline`. | Ainda acoplado à completion/deactivation policy. | Alta | Auditar depois, se necessário. | Alto | Activity01ToActivity02 PASS atual. |

---

## 5. Por que não extrair agora

Uma extração direta agora provavelmente criaria um destes problemas:

```text
1. stage decidindo restart;
2. stage decidindo next activity;
3. stage fechando route-exit;
4. stage chamando Operational handoff;
5. stage virando ActivityContentReleasePipeline disfarçado;
6. dois owners para o mesmo lifecycle.
```

Esse é o ponto onde a regra da Base 2.0 precisa ser aplicada com rigor:

```text
Stages executam passos determinísticos.
Pipelines decidem lifecycle macro.
```

A continuação pós-release ainda não é passo determinístico simples. Ela é decisão macro.

---

## 6. Observabilidade obrigatória para H1

Antes de qualquer extração, adicionar eventos explícitos.

### 6.1 Eventos recomendados

```text
ActivityContentReleaseContinuationResolved
ActivityContentReleaseContinuationStarted
ActivityContentReleaseContinuationCompleted
```

Se a continuação for assíncrona ou delegada, pode haver:

```text
ActivityContentReleaseContinuationDelegated
```

### 6.2 Owner obrigatório

Como a continuação ainda é macro lifecycle:

```text
owner='SessionActivityPipeline'
```

Não usar `ActivityContentReleaseFinalizationStage` como owner da continuação.

### 6.3 Campos obrigatórios

```text
pipelineId
sessionStateId
activityId
entrySequence
stage
source
reason
continuationKind
continuationTargetActivityId
continuationTargetEntrySequence
hasPendingRestartTransition
hasPendingRouteExit
hasNextActivity
routeExitRequested
deactivationCompleted
activityCompletionRequested
releaseStatus
skippedNoContent
loadedSceneCount
releasedSceneCount
previousStage
nextStage
```

### 6.4 Valores de `continuationKind`

```text
RestartCurrentActivity
NextActivity
RouteExit
CompleteActivity
None
Unknown
```

### 6.5 Regras de higiene

```text
Não esconder continuation em logs genéricos.
Não substituir checkpoints existentes.
Não registrar `Unknown` quando há sinal claro de restart/route-exit/next.
Não mover execução junto com observabilidade.
Não emitir owner de stage para decisão macro.
Não criar fallback para continuation se o state estiver ausente.
State obrigatório ausente deve ser fail-fast ou failure explícito conforme contrato atual.
```

---

## 7. Respostas obrigatórias

### Qual pipeline é dono desta decisão?

```text
SessionActivityPipeline.
```

A decisão de continuação após release pertence ao macro lifecycle da SessionActivity.

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
A continuação pós-release é macro lifecycle/policy do pipeline.
A observabilidade proposta é fact/OBS.
Não é adapter, endpoint, snapshot nem authoring data.
```

### Isso é comportamento final ou bridge transitória?

```text
A continuação no SessionActivityPipeline é comportamento correto neste momento.
A falta de fact explícito é déficit de observabilidade.
Uma futura extração só será válida se separar classificação de execução sem duplicar owner.
```

### Essa compatibilidade ainda é necessária?

```text
Compatibilidade de produção não é necessária.
Compatibilidade de observabilidade/checkpoints é necessária.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

```text
O sintoma é a continuação estar pouco observável.
A fronteira arquitetural correta ainda é: pipeline decide continuation; stages executam release/finalization.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
Hoje não.
Criar stage/pipeline de continuation que execute restart/route-exit/next activity criaria owner duplicado.
```

---

## 8. Próximo corte recomendado

```text
SA-7G2B-H1 — ActivityContentReleaseContinuationObservability
```

### Escopo permitido

```text
Adicionar OBS/fact explícito para continuation resolved/started/completed.
Classificar continuationKind.
Registrar target activity/entry quando houver.
Registrar pending restart/route-exit/next flags.
Preservar owner='SessionActivityPipeline'.
Não mover execução.
Atualizar ADR.
```

### Escopo proibido

```text
Não criar ActivityContentReleaseContinuationStage.
Não criar ActivityContentReleasePipeline.
Não criar ActivityExitPipeline.
Não mover StartPendingRestartEntry.
Não mover CompleteRouteExitClosure.
Não mover ContinueAfterDeactivationAsync.
Não alterar route-exit handoff.
Não alterar restart lifecycle.
Não alterar next activity lifecycle.
Não alterar ActivityContentReleaseFinalizationStage.
Não alterar unload adapter/runner.
```

---

## 9. Critério de smoke para H1

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

ActivityContentReleaseContinuationResolved aparece
ActivityContentReleaseContinuationStarted aparece
ActivityContentReleaseContinuationCompleted aparece quando aplicável

RestartCurrentActivity:
  continuationKind='RestartCurrentActivity'
  owner='SessionActivityPipeline'
  targetActivityId='activity_01'
  targetEntrySequence='2' ou valor correto do smoke

Activity01ToActivity02:
  continuationKind='NextActivity' ou 'CompleteActivity' conforme classificação final escolhida
  owner='SessionActivityPipeline'
  targetActivityId='activity_02'

RouteExitBackToMenu:
  continuationKind='RouteExit'
  owner='SessionActivityPipeline'
  routeExitRequested='true'

ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

---

## 10. Conclusão

`SA-7G2B` fecha apenas auditoria.

A próxima ação correta não é extração. É observabilidade:

```text
SA-7G2B-H1 — ActivityContentReleaseContinuationObservability
```

Regra central:

```text
Pipeline decide a continuação.
Logs tornam a decisão verificável.
```
