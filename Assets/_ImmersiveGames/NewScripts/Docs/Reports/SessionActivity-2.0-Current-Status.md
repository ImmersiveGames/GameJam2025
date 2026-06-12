# SessionActivity Base 2.0 - Current Status

Status do corte: `SA-4-CLOSURE-AUDIT + Docs Consolidation + SA-17A + SA-17B + SA-17C + SA-17D + SA-17D-FIX + SA-18A7-FIX7-DOC + SA-18A8-A9-DOC`

Data de consolidação: 2026-06-11

## Escopo

Este documento é o resumo canônico atual de `SessionActivity` Base 2.0.

Objetivo:

- registrar o baseline funcional atual;
- consolidar os cortes SA-0 a SA-8;
- fechar a leitura objetiva de SA-4 por item;
- evitar duplicação entre planos, ADRs e relatórios transitórios.

Fontes normativas e de evidência:

- `Docs/ADRs/ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md`
- `Docs/ADRs/README.md`
- `SessionActivity/Pipeline/README.md`
- `SessionActivity/Pipeline/ActivityEntryPipeline.cs`
- `SessionActivity/Pipeline/SessionActivityPipeline.cs`
- `SessionActivity/Pipeline/SessionActivityCompositionInstaller.cs`
- `SessionOperational/README.md`

## Baseline funcional atual

- `ActivityEntryPipeline` já existe como owner concreto do lifecycle determinístico de entry.
- `SessionActivityCompositionInstaller` compõe o `ActivityEntryPipeline` por dependência explícita.
- `SessionActivityPipeline` permanece como owner macro de ordem, handoff e freeze.
- `SessionOperational` continua como owner da troca de rota operacional e do handoff para `SessionActivity`.
- `RouteActivitySave` permanece limitado ao scope funcional canônico `CurrentActivityObjectSnapshot`.
- O runtime atual de entry executa os blocos de content, inventory, setup, binding, participation, reset e restore por pipeline/stage dedicado.

## SA-17A - PendingOperation bridge split/reduction

- Status: CLOSED / IMPLEMENTED.
- `IActivityEntryContentPendingOperationRuntimeBridge` perdeu a responsabilidade de despacho assíncrono.
- `ActivityEntryPipeline` continua a registrar o pending operation no owner de estado, mas agora chama o `ISessionActivityPendingOperationRunner` diretamente.
- `SessionActivityPipeline` permanece apenas como callback boundary via `ISessionActivityPendingOperationCallback`.
- Não foi criado owner duplicado para o pending operation.

## SA-17B - LoadedSet bridge cleanup

- Status: CLOSED / IMPLEMENTED.
- `IActivityEntryContentLoadedSetRuntimeBridge` foi removido.
- `ActivityContentLoadedSet` passou a ser escrito e limpo diretamente pelo `ActivityContentRuntimeState`.
- `ActivityEntryPipeline` usa o estado técnico explícito para limpar e persistir `LoadedSet`.
- `ActivityContentReleaseFinalizationStage` limpa o estado diretamente no runtime técnico.
- `SessionActivityPipeline` permanece como leitor técnico do estado, sem atuar como bridge de `LoadedSet`.

## SA-17C - ActivityContent aggregate bridge cleanup

- Status: CLOSED / IMPLEMENTED.
- `IActivityEntryContentRuntimeBridge` não existe mais como aggregate bridge de `ActivityContent`.
- Não foi criada bridge substituta com outro nome.
- `ActivityEntryPipeline` usa os contratos mínimos diretamente.
- `SessionActivityPipeline` permanece apenas como boundary de callback e leitura técnica do estado quando aplicável.
- `ActivityContentRuntimeState` continua como estado técnico único para `LoadedSet`.

## SA-17D - ActivityObject exit correlation observability hygiene

- Status: CLOSED / PASS funcional + PASS arquitetural do corte.
- `ActivityObjectExitRuntimeState` é o owner técnico da correlation.
- `SessionActivityPipeline` permanece apenas como owner macro de ordering/freeze.
- `activity_02` preservou `NoActivityContentContributors / no_activity_content_contributors`.
- `SnapshotPayloadExpectedButMissing` não apareceu no smoke validado para o caso no-content.

## SA-17D-FIX - restore activity_02 no-content RouteActivitySave classification

- Status: CLOSED / PASS funcional + PASS arquitetural do corte.
- A classificação de ausência para `activity_02` continua em `NoActivityContentContributors / no_activity_content_contributors`.
- `SnapshotPayloadExpectedButMissing` permanece reservado para contributors esperados que falharam em produzir payload.
- A observabilidade de SA-17D foi preservada: `technicalStateOwner='ActivityObjectExitRuntimeState'`, `discoveryOwner='ActivityObjectExitRuntimeState'`, `inventoryOwner='ActivityObjectExitRuntimeState'`, com reasons `*_by_pipeline_macro`.

## SA-18A7-FIX7-DOC - retained PlayerActor rebind + permission scanner guard closure

- Status: CLOSED / DOC-ONLY.
- Baseline herdado: `SA-18A7-FIX7` `PASS funcional + PASS arquitetural do corte`.
- Causa real registrada: o path retained/no-requirements mantinha `PlayerActorRuntimeHandle` com identity/cycle antigo e `ActivityCapabilityPermissionScanner` descartava targets sem movement/projétil antes de avaliar `ActorPermissionReceiver` direto.
- Correção validada: `ActivityEntryParticipantBindingStage` rebinda o retained `PlayerActor` para a `SessionActivityIdentity` atual antes da promoção/indexação e `ActivityCapabilityPermissionScanner` considera `ActorPermissionReceiver` direto no guard inicial.
- `GateBinding` permaneceu consumidor determinístico.
- Proxies removidos em `SA-18A7` não voltaram.
- `RouteActivitySave`, `LoadedSet`, pending operation e `ActivityObjectExitRuntimeState` não foram tocados por este fechamento documental.
- Smoke de referência preservado: `ActivityGateBindingStarted contributionCount='2'`, `ActivityGateBindingCompleted receivers='2'`, `Activity01ToActivity02 checkpointStatus='Passed'`, `activity02ReachedRunning='true'`, `RouteExitBackToMenu checkpointStatus='Passed'`.
- `activity_02` preservou comportamento no-content: `object discovery skippedNoContent='true'`, `reset PassedNoCommands`, `snapshot restore Skipped`, `RouteActivitySave NoActivityContentContributors / no_activity_content_contributors`.

## SA-18A8-A9-DOC - ParticipantBinding bridge residual cleanup closure

- Status: CLOSED / PASS funcional + PASS arquitetural do corte.
- `SA-18A8` removeu `TryResolvePlacementMarkerFromCurrentEntry` do `IActivityEntryParticipantBindingRuntimeBridge`.
- Causa registrada: lookup de placement marker era lookup técnico de entry/content boundary, não participant binding.
- Owner correto: `ActivityEntryPipeline`.
- `SA-18A9-H1` removeu `StoreActivityParticipationContext` do bridge de participant binding.
- Causa registrada: o método escondia dois stores diferentes atrás do macro bridge.
- Owner correto: `ActivityParticipationRuntimeState` para current participation context e `ActivityActorExitRuntimeState` para exit correlation.
- Resultado: `SessionActivityPipeline` saiu do store path e o compile fix de H1 usou `SessionActivityIdentity.SessionId`, não `SessionStateId`.
- Não houve fallback, compat rail ou novo manager/coordinator.

## SA-18A15 - ParticipantBinding adapter call-through split

- Status: CLOSED / PASS funcional + PASS arquitetural do corte.
- `IActivityEntryParticipantBindingRuntimeBridge` foi removido do runtime ativo.
- `SessionActivityPipeline` deixou de ser proxy de `ExecutePlayerActorMaterialization`, `ExecutePlayerActorParticipationEnter` e `ExecuteActorReset`.
- `ActivityEntryPipeline` passou a injetar dependências explícitas no `ActivityEntryParticipantBindingStage`.
- Critério de aceite observado: `RestartCurrentActivity`, `Activity01ToActivity02` e `RouteExitBackToMenu` passaram sem `error CS`, `FATAL`, `Exception`, `route_transition_failed`, `checkpointStatus='Failed'`, `ActivityGateBindingFailed` ou `Duplicate player participant registration`.
- A frente `IActivityEntryParticipantBindingRuntimeBridge` está fechada; o próximo foco volta aos resíduos gerais de ownership do `SessionActivityPipeline`.

## Matriz consolidada SA-0 a SA-8

| Corte | Status | Leitura objetiva | Evidência principal |
|---|---|---|---|
| SA-0 | Closed | Congelamento documental concluído. | ADR `ADR-2.0-0002` existe e registra a fronteira Base 2.0; os planos e READMEs já apontam para a matriz consolidada. |
| SA-1 | Closed | `RouteExit teardown` tem owner único no `SessionActivityPipeline`. | `SessionActivityPipeline` continua como boundary macro; `SessionActivityHost` funciona como delegador/endpoint. |
| SA-2 | Closed | `ActivityEntryPipeline` concreto foi criado e passou a ser o owner de entry. | `ActivityEntryPipeline.cs` e `SessionActivityCompositionInstaller.cs`. |
| SA-3 | Superseded | O corte original foi refinado em subcortes de content/inventory/ownership. | `ActivityEntryPipeline.cs`, `ActivityEntryActorInventoryStage.cs`, ADR com SA-3A/SA-3B0/SA-3B1. |
| SA-4 | Partial | Setup/binding/restore estão implementados, mas ainda há bridge técnico e projeções residuais no macro pipeline. | `ActivityEntryPipeline.cs`, `ActivityEntryObjectSetupStages.cs`, `SessionActivityPipeline.cs`. |
| SA-5 | Superseded | A frente de exit/release foi decomposta em auditorias e subcortes posteriores. | ADR SA-7A..SA-7H3C-D, `SessionActivityPipeline.cs`. |
| SA-6 | Closed | Identity cleanup de input/permission/movement/camera foi encerrado em subcortes fechados. | ADR SA-6A..SA-6D, `ActivityEntryPlayerInputBindingStage.cs`, `ActivityEntryMovementBindingStage.cs`, `ActivityEntryCameraBindingStage.cs`. |
| SA-7 | Superseded | A leitura atual de handoff/entry readiness foi absorvida por cortes posteriores e pelo pipeline de entry já concreto. | `ActivityEntryPipeline.cs`, ADR SA-11A/SA-14B1 e posteriores. |
| SA-8 | Superseded | Exit/release/state hygiene foi decomposto em auditorias e cortes posteriores. | ADR SA-8A..SA-8C, SA-13D, SA-14C, SA-14D, SA-16A/B/C. |

## SA-4 - auditoria detalhada

Leitura conservadora:

- os itens de setup/binding/restore já têm owner operacional explícito no `ActivityEntryPipeline`;
- o corte ainda é `Partial` porque a fronteira macro conserva bridges e estado técnico que não são owner final do setup;
- não há subitem sem evidência local.

| Item | Owner atual | Owner correto | Status | Evidência | Pendência |
|---|---|---|---|---|---|
| Content load | `ActivityEntryPipeline` | `ActivityEntryPipeline` | Closed | `PrepareEntry`, `ExecuteSetupAndReadiness`, `ActivityContent` load/prepare/readiness no entry pipeline. | Nenhuma pendência funcional conhecida neste corte. |
| Actor scene discovery | `ActivityEntryPipeline` via `ActivityEntryActorInventoryStage` | `ActivityEntryPipeline` | Closed | `ActivityEntryActorInventoryStage.ExecuteSceneDiscovery`. | Nenhuma pendência funcional conhecida neste corte. |
| Object contributor discovery | `ActivityEntryPipeline` via `ActivityEntryObjectContributorDiscoveryStage` | `ActivityEntryPipeline` | Closed | `ActivityEntryObjectContributorDiscoveryStage.Execute`. | Nenhuma pendência funcional conhecida neste corte. |
| Participant binding | `ActivityEntryPipeline` via `ActivityEntryParticipantBindingStage` | `ActivityEntryPipeline` | Closed | `ExecuteParticipantBinding`. | Nenhuma pendência funcional conhecida neste corte. |
| Actor inventory feed | `ActivityEntryPipeline` via `ActivityEntryActorInventoryStage` | `ActivityEntryPipeline` | Closed | `ExecuteActorInventoryFeed`. | Nenhuma pendência funcional conhecida neste corte. |
| Actor presentation setup | `ActivityEntryPipeline` via `ActivityEntryActorPresentationStage` | `ActivityEntryPipeline` | Closed | `ExecuteActorPresentationSetup`; logs `ActivityEntryActorPresentationSetupStarted/Completed`. | Bridge de apresentação ainda existe como contrato técnico, mas não como owner de fluxo. |
| Actor attribute setup | `ActivityEntryPipeline` via `ActivityEntryActorAttributeStage` | `ActivityEntryPipeline` | Closed | `ExecuteActorAttributeSetup`; logs `ActivityEntryActorAttributeSetupStarted/Completed`. | Bridge de atributos ainda existe como contrato técnico, mas não como owner de fluxo. |
| Actor participation enter | `ActivityEntryPipeline` via `ActivityEntryActorParticipationStage` | `ActivityEntryPipeline` | Closed | `ExecuteActorParticipationEnter`; logs `ActivityEntryActorParticipationEnterStarted/Completed`. | Nenhuma pendência funcional conhecida neste corte. |
| Player input binding | `ActivityEntryPipeline` via `ActivityEntryPlayerInputBindingStage` | `ActivityEntryPipeline` | Closed | `ExecutePlayerInputBinding`; `PlayerInputBindingAdapter` é dependência explícita. | Nenhuma pendência funcional conhecida neste corte. |
| Actor command binding | `ActivityEntryPipeline` via `ExecuteActorCommandBinding` | `ActivityEntryPipeline` | Closed | Método `ExecuteActorCommandBinding` e logs `ActivityEntryActorCommandBindingStarted/Completed`. | Nenhuma pendência funcional conhecida neste corte. |
| Gate / permission binding | `ActivityEntryPipeline` via `ActivityGateBindingStage` | `ActivityEntryPipeline` | Closed | `ActivityGateBindingStage.Execute`; `PermissionTargetPreparationStarted/Completed`. | Há bridge de permissão no macro pipeline, mas a decisão de binding está explicitamente no entry pipeline. |
| Movement binding | `ActivityEntryPipeline` via `ActivityEntryMovementBindingStage` | `ActivityEntryPipeline` | Closed | `ExecuteMovementBinding`; logs `ActivityEntryMovementBindingStarted/Completed`; `MovementBindingCompleted` preservado nos cortes posteriores. | A reação local de movement continua fora do entry pipeline, como esperado. |
| Camera binding | `ActivityEntryPipeline` via `ActivityEntryCameraBindingStage` | `ActivityEntryPipeline` | Closed | `ExecuteCameraBinding`; logs `ActivityEntryCameraBindingStarted/Completed`; `CameraBindingCompleted` preservado. | A resolução usa bridge técnico para handle/participante, mas não cria owner alternativo. |
| Object reset | `ActivityEntryPipeline` via `ActivityEntryObjectResetStage` | `ActivityEntryPipeline` | Closed | `ActivityEntryObjectSetupStages.ExecuteObjectReset`. | Nenhuma pendência funcional conhecida neste corte. |
| Object snapshot restore | `ActivityEntryPipeline` via `ActivityEntryObjectSnapshotRestoreStage` | `ActivityEntryPipeline` | Closed | `ActivityEntryObjectSnapshotRestoreStage.Execute`; `ResolveObjectSnapshotRestoreEndpointsFromInventory`. | Nenhuma pendência funcional conhecida neste corte. |

## Baseline de smokes aceitos por corte

Últimos smokes aceitos que sustentam o estado atual:

- `SA-18A7-FIX7` - retained PlayerActor rebind + permission scanner guard - `CLOSED / PASS funcional + PASS arquitetural do corte`
- `SA-18A8` - placement marker lookup split from participant binding bridge - `CLOSED / PASS funcional + PASS arquitetural do corte`
- `SA-18A9-H1` - ActivityParticipationContext store boundary + compile fix - `CLOSED / PASS funcional + PASS arquitetural do corte`
- `SA-17D-FIX` - restore activity_02 no-content RouteActivitySave classification
- `SA-17D` - ActivityObject exit correlation observability hygiene
- `SA-16D` - PlayerInput canonical actions explicit composition
- `SA-16C1` - PendingOperation window unload callback boundary cleanup
- `SA-16C2` - PendingOperation kind contract cleanup
- `SA-16B1` - ActivityContent unload callback boundary cleanup
- `SA-16A1` - Initial Movement Blocked state ownership cleanup
- `SA-16A2` - MovementTransient reset endpoint support
- `SA-14B1` - ActivityObject exit correlation explicit entry result
- `SA-7B` - ActivityExitActorTeardownStage
- `SA-6D` - Camera binding stage
- `SA-6C` - Movement binding stage
- `SA-5D` - ActorParticipation enter stage
- `SA-5C` - ActorAttributes setup stage

Leituras funcionais preservadas nesses smokes:

- `RestartCurrentActivity PASS`
- `Activity01ToActivity02 PASS`
- `RouteExitBackToMenu PASS`
- sem `FATAL`
- sem `Exception`
- sem `route_transition_failed`
- sem `foreign/stale indevido`

## Próximos cortes recomendados

Prioridade alta:

1. `RouteActivitySave` policy gap caso o produto passe a exigir "last useful payload".
2. Qualquer ajuste futuro em `ActivityObjectExitRuntimeState` deve continuar preservando `NoActivityContentContributors / no_activity_content_contributors` para activity sem content.

## Docs consolidados nesta frente

- Este relatório passa a ser o resumo canônico de status atual.
- O espelho `Docs/Architecture/Plan-2.0-SessionActivity-Refactor.md` foi removido.
- Os relatórios transitórios `ACTIVITY-OBJECT-CAPACITY-1A-FIX1-Compile-Fix`, `ACTIVITY-OBJECT-CAPACITY-1A-FIX2-Restore-Runtime-References`, `ACTIVITY-OBJECT-CAPACITY-1A-Object-Lifecycle-Contribution-Provider` e `ACTOR-CAPACITY-3A-Passive-Actor-Snapshot-Restore-Release-Contracts` foram removidos.
- `SessionActivity/Pipeline/README.md` e `Docs/ADRs/README.md` passam a apontar para este documento em vez de repetir a matriz inteira.

## Docs mantidos e motivo

- `Docs/Plans/Plan-2.0-SessionActivity-Refactor.md`: histórico do plano original e trilha de decisão.
- `Docs/ADRs/ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md`: fonte normativa viva da fronteira Base 2.0.
- `SessionActivity/Pipeline/README.md`: documentação de módulo, agora reduzida a apontamento canônico.
- `Docs/ADRs/README.md`: índice de ADRs e checkpoints, agora reduzido a apontamento canônico.

## Nota de consolidação

Este corte não altera runtime C#.
O objetivo aqui é reduzir duplicação documental, manter um único resumo atual e deixar os históricos como rastreabilidade.
