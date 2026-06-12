# RESET-SCOPE-2 — Reset Scope Plan Filtering

## Status

Closed / PASS.

## Objetivo

Ativar filtragem real de grupos de reset por `ActivityResetScopePlan` antes da emissão dos commands, sem mover decisão para adapter/endpoint e sem criar trilho paralelo.

## Decisão

`ActivityResetBoundaryPolicy` continua classificando boundary/scope/policy. Os stages passam a usar o `ActivityResetScopePlan` como filtro canônico antes de emitir commands.

O filtro acontece antes dos side-effects:

- object reset de entry: `ActivityEntryObjectResetStage` filtra `ActivityStateResetGroup` antes de criar `ActivityObjectResetCommand`;
- QA object reset: `ActivityResetStage` filtra `ActivityStateResetGroup` antes de criar `ActivityObjectResetCommand`;
- participant/actor reset: `ActivityEntryParticipantResetStage` filtra `ActorCapabilityResetEndpointReference` antes de criar `ActivityParticipantResetCommand`.

## Owner

- `ActivityResetBoundaryPolicy`: owner da decisão de boundary/scope/policy.
- `ActivityEntryPipeline`: owner da ordem de entry e handoff para stages.
- `ActivityEntryObjectResetStage`: executor determinístico do object reset de entry.
- `ActivityEntryParticipantResetStage`: executor determinístico do participant/actor reset de entry.
- `ActivityResetStage`: executor técnico do QA object reset com plano resolvido.
- `ActorResetAdapter` e `ActivityObjectResetEndpoint`: executam side-effects comandados; não decidem policy.

## Alterações aplicadas

- `behaviorMode` mudou de `PassiveNoFiltering` para `FilteringByScopePlan` nos logs do corte ativo.
- `ActivityEntryObjectResetStage` agora ignora `ActivityStateResetGroup` não permitido pelo plano.
- `ActivityResetStage` agora ignora `ActivityStateResetGroup` não permitido pelo plano no QA object reset.
- `ActivityEntryParticipantResetStage` agora filtra referências de actor reset por `AllowedActorResetGroups`.
- Quando grupo de objeto é filtrado, o stage registra `ObjectResetSkippedOptional` com `reason='reset_group_filtered_by_scope_policy'`.
- Quando todas as referências de participant/actor reset são filtradas, o stage registra skip com `reason='reset_references_filtered_by_scope_policy'`.
- Se uma única `ActorCapabilityResetEndpointReference` misturar grupos permitidos e filtrados, o stage falha explicitamente com `actor_reset_reference_mixes_allowed_and_filtered_groups`.

## Não alterado

- Não foi criada policy nova.
- Não foi criado manager/coordinator.
- Não foi alterado `ActorResetAdapter` para decidir policy.
- Não foi alterado endpoint para decidir boundary/scope.
- Não foi alterado RouteExit.
- Não foi alterado SessionReset.
- Não foi criada compat/alias/trilho paralelo.

## Risco controlado

O default atual de policy ainda permite os grupos já observados nos smokes:

- actor reset:
  - `Placement`
  - `ActivityParticipation`
  - `MovementTransient`
  - `SpawnedRuntimeObjects`
- activity object reset:
  - `Placement`
  - `ActivityParticipation`
  - `TransformState`
  - `RuntimeTransient`
  - `InteractionState`
  - `ObjectiveState`
  - `SpawnedRuntimeObjects`

Portanto, o comportamento visual esperado deve permanecer igual nos cenários atuais. A diferença arquitetural é que grupos fora do plano deixam de gerar command.

## Smoke esperado

Executar smoke mínimo:

1. Boot -> Menu -> Sandbox.
2. CompleteActivationWindow.
3. Acionar QA Object Reset em `activity_01`.
4. CompleteCurrentActivity -> CompleteDeactivationWindow.
5. Validar entrada em `activity_02`.
6. Acionar QA Object Reset em `activity_02`.
7. RouteExitBackToMenu.

Critérios:

- sem `FATAL`;
- sem `Exception`;
- sem `route_transition_failed`;
- sem `error CS`;
- sem `checkpointStatus='Failed'`;
- `ActivityResetScopePlanResolved` observado com `behaviorMode='FilteringByScopePlan'`;
- `ActivityObjectResetQaScopePlanResolved` observado com `behaviorMode='FilteringByScopePlan'`;
- `ActivityObjectResetQaApplied` preservado em `activity_01`;
- `ActivityObjectResetQaSkipped` preservado em `activity_02` no-content;
- `ActivityObjectReset checkpointStatus='PassedApplied'` preservado em `activity_01`;
- `ActivityObjectReset checkpointStatus='PassedNoCommands'` preservado em `activity_02`;
- `ActivityParticipantResetAppliedFromInventory` preservado;
- `MovementBindingCompleted` preservado;
- `CameraBindingCompleted` preservado;
- `Activity01ToActivity02 checkpointStatus='Passed'` preservado;
- `RouteExitBackToMenu checkpointStatus='Passed'` preservado.

## Critério de PASS

Só aceitar PASS com smoke/log confirmando os critérios acima.


## Fechamento

`RESET-SCOPE-2` foi aceito como PASS após smoke específico validar:

- `behaviorMode='FilteringByScopePlan'`;
- `ActivityObjectResetQaScopePlanResolved` em `activity_02`;
- `ActivityObjectResetQaSkipped` em `activity_02` com `completionKind='NoCommands'`;
- `Activity01ToActivity02 checkpointStatus='Passed'`;
- `ActivityObjectReset checkpointStatus='PassedNoCommands'` preservado em `activity_02`;
- ausência de `FATAL`, `Exception`, `route_transition_failed`, `error CS` e `checkpointStatus='Failed'`.

Classificação final: `CLOSED / PASS`.
