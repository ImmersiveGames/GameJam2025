# SA-7A — Exit / Release Decomposition Audit

Status: `CLOSED / AUDIT ONLY`

Data: 2026-05-31

## Escopo

Esta auditoria avalia o fluxo atual de saída da `SessionActivity` após os cortes SA-5A1..SA-6D. O objetivo é decidir se o próximo passo deve ser criar um `ActivityExitPipeline` próprio ou extrair stages de saída sob o owner atual.

Não houve alteração runtime neste corte.

## Resumo executivo

Resultado da auditoria:

```text
Não criar ActivityExitPipeline agora.
Manter SessionActivityPipeline como owner macro de lifecycle/rails de saída por enquanto.
Extrair primeiro stages determinísticos de exit/release sob comando do SessionActivityPipeline.
```

Motivo:

```text
Exit ainda mistura CompleteCurrentActivity, RestartCurrentActivity, RouteExit, NavigationExit,
DeactivationWindow, transition blackout, actor teardown, object snapshot/release,
ActivityContent unload, RouteActivitySave payload e route-exit closure.
```

Criar um pipeline novo agora duplicaria o owner do lifecycle e aumentaria o risco de quebrar o ordering já validado por smoke.

## Owner correto por nível

| Área | Owner correto agora | Observação |
|---|---|---|
| Macro rail de saída | `SessionActivityPipeline` | Decide se a saída é ActivityCompletion, Restart, RouteExit ou NavigationExit. |
| DeactivationWindow lifecycle | `SessionActivityPipeline` | Janela de saída ainda é lifecycle macro, não stage de release. |
| Actor presentation release | Stage determinístico de exit | Deve sair do macro pipeline como execução concreta. |
| Actor attribute release | Stage determinístico de exit | Deve sair do macro pipeline como execução concreta. |
| Actor participation exit | Stage determinístico de exit | Deve sair do macro pipeline como execução concreta. |
| Movement permission unbind/control disable | Lifecycle command + PermissionRuntime/receiver | Pipeline decide quando; runtime/receiver executam estado/reação. |
| Object snapshot capture | Stage determinístico de exit | Produz payload para RouteActivitySave; não decide save. |
| Object release | Stage determinístico de exit | Executa release endpoint; não decide continuation/route. |
| ActivityContent unload | Stage/async operation de content release | Precisa cuidado por pending operation. |
| RouteExit closure | `SessionActivityPipeline` | Macro handoff boundary com SessionOperational. |

## Achados principais

### 1. `SessionActivityPipeline` ainda executa release concreto demais

O pipeline macro ainda chama diretamente:

```text
EmitActorPresentationReleaseGenericStage(...)
EmitActorAttributeReleaseFromInventoryStage(...)
EmitActorParticipationExitFromInventoryStage(...)
EmitObjectSnapshotCaptureStage(...)
EmitObjectReleaseStage(...)
TryStartActivityContentReleaseForContinuation(...)
ExecuteNextActivityContentSceneRelease(...)
CompleteRouteExitClosure(...)
```

Isso confirma débito real: a saída está funcional, mas ainda monolítica.

### 2. Existe ordering diferente entre rails

A ordem atual não é única:

```text
CompleteCurrentActivity:
ActivityRunning
-> DeactivationWindowStarted/Ready/Completed
-> ActivityDeactivated
-> transition blackout quando aplicável
-> actor teardown
-> object snapshot/release
-> ActivityContent unload
-> next activity / completed

RestartCurrentActivity:
ActivityRunning
-> ActorPresentation release / Attribute release / Participation exit
-> DeactivationWindowStarted/Ready/Completed
-> ActivityDeactivated
-> object snapshot/release
-> ActivityContent unload
-> same activity new entry

RouteExit from ActivityRunning:
ActivityRunning
-> MovementControl disable
-> ActorPresentation release / Attribute release / Participation exit
-> DeactivationWindowStarted/Ready/Completed
-> ActivityDeactivated
-> object snapshot/release
-> ActivityContent unload
-> ClosedForRouteExit

RouteExit from DeactivationWindowReady:
DeactivationWindowReady
-> DeactivationWindowCompleted
-> ActorPresentation release / Attribute release / Participation exit
-> DeactivationWindow unload
-> ActivityDeactivated
-> object snapshot/release
-> ActivityContent unload
-> ClosedForRouteExit
```

Essa variação é exatamente por que não devemos criar pipeline novo antes de consolidar os stage boundaries.

### 3. `ActorPresentationRelease` mistura policy, stage, adapter e state

O método atual faz mais de uma responsabilidade:

```text
seleciona active states
classifica rail/policy mismatch
executa adapter Release
emite facts/snapshots/logs
remove ou retém handle
sincroniza registry de NonPlayer quando aplicável
lança FATAL em falha
```

Owner correto:

```text
Policy: classificar release/retain/skip/fail por rail e releasePolicy.
Stage: iterar comandos determinísticos e emitir result/facts.
Adapter: executar release da presentation.
Runtime state: writer único para active handles.
```

### 4. `ActorAttributeRelease` mistura endpoint local e lifecycle macro

O método atual lê `_activeActorAttributeCapabilitiesByActorInstanceId`, chama `Endpoint.TryRelease(...)`, remove state e emite facts.

Owner correto:

```text
Stage de exit chama endpoints locais.
Endpoint executa release local.
Pipeline macro não deve iterar capability state diretamente.
```

### 5. `ActorParticipationExit` já tem executor, mas o wrapper ainda está no macro pipeline

Existe `ActorParticipationExitStageExecutor`, mas o `SessionActivityPipeline` ainda controla o wrapper, logs agregados, player exit e state updates.

Owner correto imediato:

```text
ActivityExitActorParticipationStage ou ActivityExitActorTeardownStage
```

Sem criar rail `Player` vs `NonPlayer`; player-specific exit continua subefeito local quando o participante exige player actor.

### 6. `ActivityObjectExitStage` existe, mas é apenas nested wrapper

O código tem uma classe interna `ActivityObjectExitStage`, porém ela só chama métodos core do próprio `SessionActivityPipeline`.

Conclusão:

```text
Não conta como stage real Base 2.0.
É uma bridge interna/transitória.
```

O próximo estágio real para objetos deve nascer em arquivo próprio e receber command/result explícito.

### 7. `ActivityContentRelease` é o corte mais arriscado

`ActivityContentRelease` envolve pending operation, unload async e continuação de rail. Ele não deve ser movido no primeiro patch.

Motivo:

```text
TryStartActivityContentReleaseForContinuation
_pendingActivityContentReleaseContext
_awaitingContinuationAfterActivityContentRelease
ExecuteNextActivityContentSceneRelease
CompleteActivityContentSceneUnloadOperation
FinalizeActivityContentReleaseCompleted
```

Esse bloco controla continuação async. Mover antes dos actor/object teardown stages aumentaria risco de regressão.

### 8. `RouteActivitySave` depende do snapshot payload, mas não deve virar owner de snapshot capture

O payload de snapshot para save-on-exit é produzido no exit da activity. `RouteActivitySave` no operational consome depois.

Owner correto:

```text
ObjectSnapshotCaptureStage produz payload.
SessionOperational/RouteActivitySave decide save-on-exit.
SaveRuntime persiste.
```

Não mover save junto com release.

## Matriz de auditoria

| Arquivo / classe / método | Responsabilidade atual | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---|---|---|---|
| `SessionActivityPipeline.EmitCompleteAsync` | Inicia completion, desabilita movement, abre DeactivationWindow. | `SessionActivityPipeline` macro lifecycle. | Correto como macro, mas ainda encadeia para release indireto depois. | Média | Manter por enquanto. | Médio | Completion precisa preservar DeactivationWindow. |
| `SessionActivityPipeline.EmitRestartCurrentActivityAsync` | Inicia restart e executa actor teardown antes da DeactivationWindow. | Macro rail + stage de actor teardown. | Mistura restart policy com release concreto. | Alta | Extrair actor teardown para stage, preservando posição atual. | Alto | Smoke passa; ordering não deve mudar no primeiro patch. |
| `SessionActivityPipeline.EmitCloseForRouteExit` | Executa route-exit a partir de Running ou DeactivationWindowReady. | `SessionActivityPipeline` macro route-exit. | Também executa actor teardown concreto. | Alta | Extrair actor teardown para stage chamado pelo macro pipeline. | Alto | RouteExit ordering é crítico. |
| `SessionActivityPipeline.FinalizeDeactivationAndContinuation` | Fecha deactivation e continua para next/completed/navigation. | `SessionActivityPipeline` macro continuation. | Correto como macro. | Média | Não mover ainda. | Alto | Decide next activity/handoff. |
| `EnsureContinuationExitTeardownAfterBlackoutOrStartRelease` | Garante blackout antes de teardown e inicia release/content unload. | Macro ordering + stage calls. | Bom guard macro, mas executa release concreto. | Alta | Manter guard; delegar actor/object teardown para stages. | Alto | CutWithCurtain depende desse ordering. |
| `EmitActorPresentationReleaseGenericStage` | Release/retain presentation por rail/policy. | Stage + policy + adapter. | Mistura policy, adapter side-effect, state e fact. | Alta | Próximo corte deve extrair para `ActivityExitActorTeardownStage` ou `ActivityExitActorPresentationReleaseStage`. | Médio/Alto | Release policy precisa permanecer idêntica. |
| `EmitActorAttributeReleaseFromInventoryStage` | Release de ActorAttributes ativos. | Stage + endpoint local. | Macro pipeline chama endpoints diretamente. | Alta | Extrair junto ou logo após actor presentation release. | Médio | Attribute release é local e determinístico. |
| `EmitActorParticipationExitFromInventoryStage` | Exita participations ativos e player actor participation. | Stage de participation exit. | Já tem executor, mas wrapper/state/log ainda no macro pipeline. | Alta | Extrair para stage próprio; não criar rail player/nonplayer. | Alto | Permission unbound/player exit precisa ser preservado. |
| `EmitMovementControlDisableForCurrentTargets` | Publica bloqueio/unbound de controle nos pontos de lifecycle. | Macro lifecycle + PermissionRuntime. | Aceitável por enquanto. | Média | Não mover no próximo patch; auditar depois da permission identity cleanup. | Médio | Permission runtime já aplica facts/reaction. |
| `EmitObjectSnapshotCaptureStage/Core` | Captura snapshots e preenche payload save-on-exit. | Object snapshot exit stage. | Macro pipeline executa providers diretamente e guarda payload. | Alta | Extrair após actor teardown, não junto. | Alto | RouteActivitySave depende do payload. |
| `EmitObjectReleaseStage/Core` | Executa release endpoints e unregister posterior. | Object release exit stage. | Nested stage atual não é stage real. | Alta | Extrair em corte próprio depois de snapshot capture. | Alto | Content unload depende de release. |
| `ActivityObjectExitStage` nested | Wrapper interno para capture/release/unregister. | Stage real em arquivo próprio. | Bridge nominal; chama métodos do owner. | Média/Alta | Substituir por stage real futuramente. | Médio | Não altera ownership real hoje. |
| `TryStartActivityContentReleaseForContinuation` | Inicia snapshot/release/content unload e continuação async. | Content release stage/async boundary. | Mistura object exit + content unload + pending state. | Alta | Não mover primeiro; preparar depois que actor/object stages existirem. | Alto | Pending operation é sensível. |
| `CompleteRouteExitClosure` | Marca ClosedForRouteExit e completa handoff. | `SessionActivityPipeline` macro route-exit. | Correto ficar no macro pipeline por enquanto. | Baixa/Média | Não mover agora. | Alto se movido cedo | SessionOperational depende desse fechamento. |

## Respostas obrigatórias

### Qual pipeline é dono desta decisão?

```text
SessionActivityPipeline é dono do macro lifecycle de saída:
- ActivityCompletion
- RestartCurrentActivity
- RouteExit
- NavigationExit
- DeactivationWindow
- ClosedForRouteExit

Stages de exit/release devem executar passos determinísticos sob comando do owner macro.
```

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
DeactivationWindow = lifecycle macro.
ActorPresentation release = stage + policy + adapter.
ActorAttribute release = stage + endpoint local.
ActorParticipation exit = stage + endpoint/executor.
Movement disable/unbound = command/fact via PermissionRuntime + receiver reaction.
ObjectSnapshotCapture = stage + snapshot payload.
ObjectRelease = stage + endpoint.
ActivityContentUnload = async stage/adapter boundary.
RouteExitClosed = fact/handoff macro.
```

### Isso é comportamento final ou bridge transitória?

```text
SessionActivityPipeline como macro exit owner = comportamento final provável.
ActivityExitPipeline novo agora = não aprovado.
Métodos Emit*Release dentro do macro pipeline = transitórios.
ActivityObjectExitStage nested = bridge transitória.
Runtime bridges dos stages de entry = transitórias.
```

### Essa compatibilidade ainda é necessária?

Não como compatibilidade final.

O que deve ser preservado por comportamento validado:

```text
ordering atual de Restart/RouteExit/Activity01ToActivity02;
facts/checkpoints existentes;
RouteActivitySave payload;
permission Blocked/Allowed/Unbound;
activity_02 como cenário negativo/no-content.
```

O que não precisa ser preservado como shape:

```text
release concreto dentro de SessionActivityPipeline;
ActivityObjectExitStage nested;
helpers gigantes de release no macro pipeline.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

Na fronteira.

O sintoma é o tamanho do `SessionActivityPipeline`; a causa é que exit/release ainda não foi separado em stages determinísticos.

### Existe owner duplicado para o mesmo lifecycle?

Ainda não há owner duplicado, e por isso não devemos criar `ActivityExitPipeline` agora.

O risco é criar duplicidade se adicionarmos pipeline novo antes de separar os stages.

## Decisão SA-7A

```text
Não criar ActivityExitPipeline agora.
Criar primeiro stages reais de exit/release chamados pelo SessionActivityPipeline.
```

## Próximo corte recomendado

### `SA-7B — ActivityExitActorTeardownStage`

Objetivo:

```text
Extrair actor teardown de saída para stage dedicado:
- ActorPresentation release/retain por rail;
- ActorAttribute release;
- ActorParticipation exit;
- player participation exit quando aplicável;
- facts/logs preservados;
- sem mover object snapshot/release;
- sem mover ActivityContent unload;
- sem mover DeactivationWindow;
- sem mover RouteExit closure.
```

Regra principal:

```text
SessionActivityPipeline decide quando o teardown roda.
ActivityExitActorTeardownStage executa o teardown determinístico.
```

Escopo proibido:

```text
Não criar ActivityExitPipeline.
Não mexer em ActivityContentRelease async.
Não mexer em ObjectSnapshot/ObjectRelease.
Não alterar ordering dos rails.
Não mover DeactivationWindow.
Não mover CompleteRouteExitClosure.
Não alterar CameraPresentation release operacional.
Não alterar RouteActivitySave.
Não alterar PermissionRuntime/reaction local.
Não criar branch global player/nonplayer.
```

Critério de aceite:

```text
ActivityExitActorTeardownStarted/Completed com owner='ActivityExitActorTeardownStage' ou owner='SessionActivityPipeline/ExitStage'.
ActorPresentationReleased/Skipped preservado.
ActorAttributeReleased preservado.
ActorParticipationExited preservado.
ActivityObjectSnapshotCapture preservado.
ActivityObjectRelease preservado.
ActivityContentRelease preservado.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
sem FATAL / Exception / route_transition_failed / foreign/stale.
```

## Smoke exigido após SA-7B

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
QA Reset Current Player Actor
RestartCurrentActivity
CompleteActivationWindow
CompleteCurrentActivity
Activity01ToActivity02
BackToMenu / RouteExit
```

Critérios mínimos:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActorPresentationReleaseStarted/Completed preservado
ActorAttributeReleaseStarted/Completed preservado
ActorParticipationExitStarted/Completed preservado
ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```
