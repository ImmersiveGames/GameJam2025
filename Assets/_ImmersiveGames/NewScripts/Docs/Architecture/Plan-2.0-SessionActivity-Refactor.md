# Plano de Refatoração — SessionActivity Base 2.0

> Historical planning document. The active normative boundary is frozen by `ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md` and `ENTRY-BOUNDARY-DOC-0` inside it. Use this plan only as refactor context; do not read inventory-backed setup/binding as an allowed final shape.

> Closure note: `ENTRY-BOUNDARY Closure â€” PASS funcional + PASS arquitetural parcial` is recorded in `ADR-2.0-0002`. The active shape is now frozen: gate binding is explicit, Presentation/Attribute use SetupContributions, Camera uses BindingContributions, and ObjectEmission participates in permission by provider/contribution without returning to inventory.

> ACT-EMIT-2 closure note: `ACT-EMIT-2 â€” ObjectEmission Pool/Rent/Return MVP â€” PASS` is now recorded in `ADR-2.0-0002`. The MVP shape is frozen: FirePrimary is accepted by `ActorObjectEmitterEndpoint`, `ObjectEmissionRuntimeComposer`/`ObjectEmissionPoolRuntimeBridge` wire `IPoolService`, `ObjectEmissionPoolAdapter` owns `Rent`, `ObjectEmissionPoolReturnSink` owns `Return`, and no scanner/inventory/setup/binding/gate reopening is allowed. Next possible cuts start at `ACT-EMIT-3A`, `ACT-EMIT-4A`, `ACT-EMIT-5A` and `ACT-EMIT-6A`.

## Objetivo

Decompor `SessionActivityPipeline` sem criar trilho paralelo, fallback silencioso ou novo owner ambíguo.

O alvo é separar:

```text
SessionActivityPipeline = lifecycle macro / ordem / handoff
ActivityEntryPipeline = entry lifecycle por Activity entry
Stages = passos determinísticos
Policies = classificação
Commands = payload runtime resolvido
Facts = registro
Adapters = side-effects
Endpoints = reação/capacidade local
```

## Premissas

- Base 1.1 atual é baseline funcional seguro.
- Não preservar compatibilidade de shape ruim.
- Não usar `partial` para esconder pipeline monolítico.
- Não criar manager/coordinator genérico.
- Não pedir ao Codex build, compile, tests, smoke, playmode ou batchmode.
- Toda implementação precisa voltar com smoke/log manual antes de PASS.

## Corte SA-0 — Congelamento documental

### Objetivo

Adicionar ADR e plano ao projeto sem alterar runtime.

### Ação

- Criar `ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md`.
- Atualizar índice/README de ADRs se existir.
- Registrar que a auditoria atual é fonte de decisão.

### Aceite

- Sem alteração de código runtime.
- Documento reflete owner de `SessionActivityPipeline`, `ActivityEntryPipeline`, Host, stages, policies, commands, facts, adapters e endpoints.

### Risco

Baixo.

## Corte SA-1 — RouteExit teardown owner unification

### Objetivo

Remover owner duplicado entre `SessionActivityHost` e `SessionActivityPipeline` para `RouteExit teardown`.

### Ação

- Auditar chamadas externas de `ISessionActivityRouteExitTeardownBoundary`.
- Fazer `SessionActivityPipeline` ser owner único de decisão/estado de teardown.
- Fazer `SessionActivityHost` virar delegador/endpoint externo.
- Mover classificação duplicada de stages para policy única ou método único canônico.
- Remover hardcode QA de `activity_01` se estiver acoplado ao lifecycle real.

### Não fazer

- Não criar `ActivityEntryPipeline` ainda.
- Não mexer em input/movement/camera/adapters.
- Não mexer em service locator/global registry ainda, salvo se for necessário para remover decisão de lifecycle.

### Aceite arquitetural

- Host não decide lifecycle de teardown.
- Pipeline decide route-exit teardown.
- Sem duas listas divergentes de route-exit/deactivation stages.
- Sem fallback para caminho antigo.

### Smoke obrigatório

- Boot -> Menu -> Sandbox.
- CompleteActivationWindow.
- BackToMenu / RouteExit.
- Confirmar `RouteExitBackToMenu PASS`.
- Confirmar ausência de `FATAL`, `Exception`, `route_transition_failed`, foreign/stale indevido.

### Risco

Médio. Pode quebrar contrato externo de `SessionOperational` se o Host deixar de responder corretamente.

## Corte SA-2 — ActivityEntryPipeline shell canônico

### Objetivo

Criar `ActivityEntryPipeline` concreto como owner real de entry, sem ainda migrar todos os stages.

### Ação

- Criar `ActivityEntryPipeline` com `ExecuteAsync(ActivityEntryCommand)`.
- Criar `ActivityEntryCommand`, `ActivityEntryResult`, `ActivityEntryResultKind` se os contratos atuais forem insuficientes.
- Compor o pipeline por DI explícita, não por service locator.
- Fazer `SessionActivityPipeline` chamar `ActivityEntryPipeline` no ponto de entry.
- O primeiro corte pode migrar apenas um bloco pequeno e coeso de entry, removendo o caminho antigo equivalente.

### Não fazer

- Não criar fallback para o fluxo antigo.
- Não criar boundary que chama sub-stages como mini-pipeline.
- Não passar delegates/Func/stages dentro de command.
- Não mover ActivationWindow completion para `ActivityEntryPipeline`.

### Aceite arquitetural

- Existe classe concreta `ActivityEntryPipeline`.
- O path migrado tem owner único.
- O caminho antigo equivalente foi removido ou deixou de ser chamado.
- Logs mostram início/fim de `ActivityEntryPipeline`.

### Smoke obrigatório

- Boot -> Menu -> Sandbox.
- Activity entry inicial.
- CompleteActivationWindow.
- Confirmar chegada em `ActivityRunning`.
- Confirmar ausência de `FATAL`, `Exception`, foreign/stale indevido.

### Risco

Alto. O corte define a costura principal da decomposição.

## Corte SA-3 — ActivityContent + Inventory para ActivityEntryPipeline

### Objetivo

Mover prepare/load/readiness de `ActivityContent` e preview/resolution de `ActivityCapabilityInventory` para `ActivityEntryPipeline`.

### Ação

- Migrar stages de content load/prepare para entry pipeline.
- Migrar inventory preview/resolution para entry pipeline.
- Garantir que content obrigatório ausente falhe explicitamente.
- Garantir skip explícito para content opcional/no-content.
- Rejeitar completions stale/foreign por `SessionActivityIdentity + ActivityId + EntrySequence`.

### Não fazer

- Não mover Actor setup inteiro no mesmo corte.
- Não alterar adapters já fail-fast.
- Não criar inventário universal em `ActivityContentProfileAsset`.

### Aceite arquitetural

- `ActivityEntryPipeline` é owner de content/inventory para a entry.
- `SessionActivityPipeline` não executa diretamente esses passos.
- Facts preservam nomes/checkpoints relevantes ou mapeiam novo owner claramente.

### Smoke obrigatório

- Entry com `activity_01` content.
- Transição para `activity_02` no-content/skip.
- `Activity01ToActivity02 PASS`.

### Risco

Alto. Content/inventory alimentam quase todos os stages posteriores.

## Corte SA-4 — Actor/Object setup para ActivityEntryPipeline

### Objetivo

Mover setup/readiness de actors, objects e capabilities para `ActivityEntryPipeline`.

### Ação

Migrar em subcortes, não em bloco único, mas sem transformar especializações concretas em trilhos arquiteturais:

1. ActorDiscovery / ActorInventoryFeed audit.
2. ActorReadiness genérico por `ActorScanTarget` / `ActorCapabilitySurface`.
3. ActorPresentation setup.
4. ActorAttributes setup.
5. ActorParticipation enter readiness.
6. ActivityObject reset/restore.
7. Input binding quando for capacidade de actor/participante, não rail de player.
8. Movement binding quando for capacidade/endpoints do actor, não branch global de player.
9. Camera binding por capability target, não por tipo concreto de actor.
10. Permission target preparation com identidades separadas.

### Não fazer

- Não recriar rails `PlayerActor` vs `NonPlayerActor` como lifecycle global.
- Não criar subcorte/stage canônico nomeado por especialização concreta (`PlayerActorReadiness`, `NonPlayerActorDiscovery`) quando o domínio correto é `Actor`.
- Não alterar reaction local de permission.
- Não mover Deactivation/Release neste corte.
- Não misturar `ActorId`, `PlayerActorId`, `PlayerSlotId`, `ActorInstanceRuntimeId` e receiver técnico.

### Aceite arquitetural

- Cada subcorte tem stage owner explícito.
- Capabilities usam `ActorCapabilitySurface`/inventory quando já migradas.
- Ausência obrigatória é fail-fast.
- Ausência opcional é skip explícito.
- Sem fallback silencioso.

### Smoke obrigatório

- RestartCurrentActivity PASS.
- Activity01ToActivity02 PASS.
- RouteExitBackToMenu PASS.
- Checkpoints de Presentation/Attribute/Participation/Input/Movement/Camera preservados.

### Risco

Muito alto. Deve ser dividido em subcortes pequenos.

## Corte SA-5 — Exit/Release decomposition audit

### Objetivo

Auditar e decidir se saída/release/snapshot continuam como stages chamados pelo `SessionActivityPipeline` ou se exigem um `ActivityExitPipeline` próprio.

### Ação

- Auditar release de ActorPresentation, ActorParticipation exit, ActivityObject snapshot/release, ActivityContent unload, DeactivationWindow ordering.
- Separar o que é macro lifecycle do que é stage determinístico.
- Só criar `ActivityExitPipeline` se houver owner final claro.

### Não fazer

- Não criar `ActivityExitPipeline` apenas por simetria com entry.
- Não mover release antes de confirmar ordering de DeactivationWindow.
- Não quebrar RouteExit ordering congelado.

### Aceite arquitetural

- Decisão explícita: stages de exit sob `SessionActivityPipeline` ou pipeline próprio.
- Sem owner duplicado.
- Sem release antes da janela correta.

### Smoke obrigatório

- CompleteCurrentActivity.
- Activity01ToActivity02.
- RestartCurrentActivity.
- BackToMenu / RouteExit.

### Risco

Alto. Exit mistura deactivation, snapshot, release e route-exit.

## Corte SA-6 — Permission identity cleanup

### Objetivo

Separar domínio de identidade em permission target/receiver.

### Ação

- Auditar `PlayerMovementPermissionReceiver.TargetsCurrentActor`.
- Separar `PlayerActorId`, `PlayerSlotId`, `ReceiverId` e futuro `PermissionTargetId`.
- Remover comparação cruzada como fallback.
- Manter receiver player-specific enquanto movement for player-specific.

### Não fazer

- Não generalizar movement para todos os actors neste corte.
- Não criar fallback por `ActorId`.
- Não alterar local reaction.

### Aceite arquitetural

- Nenhum domínio de identidade é comparado como equivalente.
- Missing identity é observável.
- Permission runtime continua command/fact, sem side-effect direto.

### Smoke obrigatório

- Movement control bloqueado antes de ActivityRunning.
- Movement control liberado em ActivityRunning.
- Movement control bloqueado em completion/route-exit.
- Sem `PermissionTargetIdentityUnresolved` em cenário válido.

### Risco

Médio. Pode quebrar receivers existentes se a separação for agressiva.

## Corte SA-7 — Host/composition boundary cleanup

### Objetivo

Remover `DependencyManager.Provider` e registros globais do Host quando a fronteira de lifecycle já estiver estabilizada.

### Ação

- Auditar `RegisterGlobal*` / `UnregisterGlobal*`.
- Mover composição/global registry para composition root/installer correto.
- Host fica boundary/endpoint externo.

### Não fazer

- Não iniciar antes de SA-1 e SA-2.
- Não alterar bootstrap global sem auditoria de dependentes.

### Aceite arquitetural

- Host não usa service locator para lifecycle/composition ativa.
- Composition root registra dependências.
- Sem regressão de acesso externo ao boundary.

### Smoke obrigatório

- Full smoke de rota/activity.

### Risco

Médio/alto. Pode quebrar bootstrap.

## Corte SA-8 — State/fact hygiene

### Objetivo

Reduzir state mutável e fact emission dentro do pipeline macro após os owners estarem corretos.

### Ação

- Auditar runtime state usado por SessionActivity.
- Separar fact recording de decisão.
- Garantir que recorder não decide lifecycle/policy.

### Não fazer

- Não repetir erro do recorder crescendo além de fact/trace.
- Não mover result building para recorder.

### Aceite arquitetural

- State é técnico e explícito.
- Facts não executam side-effects.
- Recorder não decide stage order.

### Smoke obrigatório

- Full smoke.

### Risco

Médio. Só deve ocorrer depois da decomposição principal.

## Ordem recomendada

```text
SA-0 -> SA-1 -> SA-2 -> SA-3 -> SA-4 subcortes -> SA-5 audit -> SA-6 -> SA-7 -> SA-8
```

## Primeiro prompt recomendado para Codex

```text
Audite e implemente somente o Corte SA-1 de SessionActivity Base 2.0.

Objetivo: unificar o owner de RouteExit teardown.

Regras:
- Não rode build, compile, tests, smoke, playmode ou batchmode.
- Antes de alterar, audite chamadas de ISessionActivityRouteExitTeardownBoundary, SessionActivityHost.RequestRouteExitTeardown/AwaitRouteExitTeardownAsync e os métodos equivalentes no SessionActivityPipeline.
- SessionActivityPipeline deve ser o único owner de decisão/estado de RouteExit teardown.
- SessionActivityHost deve virar delegador/endpoint externo, sem classificar lifecycle/stages como owner final.
- Remova ou consolide listas duplicadas de stage policy se estiverem no mesmo domínio de decisão.
- Não crie ActivityEntryPipeline ainda.
- Não altere input/movement/camera/adapters.
- Não crie fallback para caminho antigo.

Entregue:
1. arquivos alterados;
2. resumo do novo ownership;
3. confirmação de que não há dois owners de RouteExit teardown;
4. como gerar smoke manual para Boot -> Menu -> Sandbox -> CompleteActivationWindow -> BackToMenu.
```

## Evidência exigida após cada implementação

Não aceitar PASS sem log/smoke contendo:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
RestartCurrentActivity PASS quando aplicável
Activity01ToActivity02 PASS quando aplicável
RouteExitBackToMenu PASS
owner correto visível nos logs
sem fallback silencioso
sem trilho paralelo novo
```


## Addendum SA-13C — Module Port Ownership / Object Correlation

### SA-13C1 — ActorAttribute command execution owner extraction

Status: CLOSED.

Ganho:

```text
Remove execução concreta de ActorAttributes do SessionActivityPipeline.
Reusa ActorAttributeEndpoint.TryApplyCommand(...).
```

Risco residual:

```text
Pipeline ainda resolve capability ativa por actorId via correlation state.
Aceito como passo intermediário.
```

### SA-13C-OBJ1-FIX — ActivityObject exit correlation mirror

Status: CLOSED.

Ganho:

```text
ActivityObjectSnapshotCapture/Release/Unregister voltam a consumir test_object_01 em activity_01.
ActivityObjectExitRuntimeState passa a receber discovery + inventory preview + validation antes do exit.
```

Risco residual:

```text
Freeze entry->exit ainda é feito no SessionActivityPipeline como boundary macro.
Shape futuro desejado: ActivityEntryPipeline congelar a correlação diretamente.
RouteActivitySave payload útil ainda exige smoke próprio.
```

### Próximos cortes recomendados

```text
SA-13C2 — ActorPresentation registry projection ownership audit/fix.
SA-13C3 — Movement retained/control bridge audit.
SA-13C4 — Permission runtime surface audit.
SA-13C5 — ActivityObject correlation localization cleanup, se necessário.
SA-13C-SAVE1 — RouteActivitySave useful payload smoke/audit.
```

Regra para próximos cortes:

```text
Não remover mais carriers método-a-método sem classificar módulo, lifetime, owner e risco.
Não mexer em RouteActivitySave sem smoke que exercite payload útil.
Não mover continuation macro para runtime state/stage.
```


## SA-13D - fechamento documental

Status: CLOSED / AUDITED.

Resumo:

- SA-13D1 a SA-13D7 foram concluidos em auditoria documental.
- Nao ha patch imediato recomendado para PlayerInput, Permission, Movement, Camera, ActivityContent ou RouteActivitySave.
- PlayerInput ainda tem debito futuro de lookup tardio de RuntimeConfigRegistry no adapter.
- Camera explicit composition para ActivityCameraAnchorHost foi fechado em SA-16E1.
- Movement retained/control e ActivityContent release/continuation permanecem por alto risco.
- RouteActivitySave atual salva/skipa com base na activity/rota imediatamente anterior concluida.
- Se o produto quiser preservar o last useful payload, a policy precisa ser adicionada explicitamente antes de qualquer mudanca funcional.
- Nao criar fallback silencioso para payload antigo.

## SA-14B1 - fechamento documental

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

Resumo:

- ActivityObject exit correlation agora é carrier explícito produzido pela entry.
- SessionActivityPipeline continua apenas como commit boundary do bundle no ActivityObjectExitRuntimeState.
- ActivityObjectExitRuntimeState continua sink técnico.
- Stages de exit continuam determinísticos.
- Sem fallback ou reconstruction no exit.

## SA-14C - residual bridge / carrier matrix

Status: CLOSED / AUDITED.

Resumo:

- No new wrong owner, duplicate owner, fallback silencioso, or new lookup tardio was found inside SessionActivity.
- No bridge documented as removed was still active in the audited path.
- No immediate runtime patch is recommended.
- Main future bridge candidate remains `IActivityEntryContentPendingOperationRuntimeBridge` and `RunActivityContentOperation(..., this)`.
- `IActivityEntryParticipantBindingRuntimeBridge` remains a possible future split candidate.
- `IActivityEntryContentLoadedSetRuntimeBridge` remains a future cleanup candidate.
- `Movement retained/control` and `ActivityContentReleaseRuntimeState` remain high risk.

## SA-14D - ActivityContent pending-operation bridge audit

Status: CLOSED / AUDITED.

Resumo:

- `IActivityEntryContentPendingOperationRuntimeBridge` remains a technical residual and is deferred.
- `RunActivityContentOperation(..., this)` is a technical callback, not a wrong owner.
- `SessionActivityPipeline` remains the callback boundary through `ISessionActivityPendingOperationCallback`.
- No fallback silencioso, no new lookup tardio, and no duplicate owner were found in the audited path.
- No immediate runtime patch is recommended.
- Any future change in this path requires full smoke validation.

Backlog futuro:

```text
IActivityEntryContentPendingOperationRuntimeBridge split/reduction
IActivityEntryContentLoadedSetRuntimeBridge cleanup
IActivityEntryContentRuntimeBridge aggregate cleanup
```

## SA-14A - status normalization

- Consolidated current status: `SA-14E - SessionActivity decomposition closure matrix CLOSED / AUDITED`.
- Treat older roadmap blocks as historical traceability, not active status.
- Keep current future debts limited to: RouteActivitySave policy gap, Movement high risk, ActivityContent high risk, and optional ActivityObject exit correlation observability hygiene only.
- Do not reopen Movement or ActivityContent without regression evidence.
- Do not create fallback for old payloads or add `ActivityExitPipeline` / `ActivityContentReleasePipeline` only for symmetry.

## SA-14E - SessionActivity decomposition closure matrix

Status: CLOSED / AUDITED.

Resumo:

- The current `SessionActivity` runtime decomposition is frozen as a temporary checkpoint.
- `SA-14B1` remains `CLOSED / PASS funcional + PASS arquitetural do corte`.
- `SA-13D`, `SA-14C` and `SA-14D` remain closed as audits.
- Remaining debts are classified as `DEFER_HIGH_RISK`, `POLICY_GAP`, `FUTURE_CLEANUP_LOW`, `FUTURE_CLEANUP_MEDIUM` and `DO_NOT_REOPEN_WITHOUT_REGRESSION`.

Matrix:

```text
CLOSED_PASS:
  SA-14B1 - ActivityObject exit correlation explicit entry result

CLOSED_AUDITED:
  SA-13D - Runtime surface audits
  SA-14C - residual bridge / carrier matrix
  SA-14D - ActivityContent pending-operation bridge audit

DEFER_HIGH_RISK:
  Movement retained/control surface
  ActivityContent release/continuation surface
  ActivityContentReleaseRuntimeState

POLICY_GAP:
  RouteActivitySave policy gap: current completed activity vs last useful snapshot payload

FUTURE_CLEANUP_LOW:
  IActivityEntryContentLoadedSetRuntimeBridge cleanup
  IActivityEntryContentRuntimeBridge aggregate cleanup
  ActivityObject exit correlation observability hygiene

FUTURE_CLEANUP_MEDIUM:
  IActivityEntryContentPendingOperationRuntimeBridge split/reduction
  IActivityEntryParticipantBindingRuntimeBridge possible split
  ActivityCameraAnchorHost explicit scene-scope composition: CLOSED / PASS funcional + PASS arquitetural do corte

DO_NOT_REOPEN_WITHOUT_REGRESSION:
  Movement
  ActivityContent
  RouteActivitySave
  SA-14B1 exit correlation production
  ActivityContent release/continuation
  ActivityContent pending-operation callback path
```

Smoke baseline:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
RestartCurrentActivity
Activity01ToActivity02
RouteExitBackToMenu
ActivityObjectSnapshotCapture, ActivityObjectRelease, ActivityObjectContributorUnregister quando aplicavel
```

Closure rules:

```text
DEFER_HIGH_RISK items must not be reopened without concrete regression evidence.
RouteActivitySave last useful payload is a new policy, not a local bug.
Any future change in the pending-operation callback path requires full smoke validation.
```
