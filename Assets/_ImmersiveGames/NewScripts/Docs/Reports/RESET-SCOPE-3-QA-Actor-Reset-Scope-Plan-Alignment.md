# RESET-SCOPE-3 — QA Actor Reset Scope Plan Alignment

## Status

Applied / pending compile + smoke.

## Objetivo

Alinhar o QA/manual reset do actor/player ao mesmo `ActivityResetScopePlan` usado pelo reset de entry e pelo QA object reset.

O corte não cria feature nova. Ele remove o último caminho de reset manual que ainda montava `ActivityParticipantResetCommand` diretamente a partir do inventory sem registrar boundary/scope/policy resolvidos.

## Decisão

`SessionActivityPipeline.TryQaResetCurrentPlayerActor(...)` continua sendo o owner do comando QA, mas não decide a policy de reset sozinho.

O fluxo passa a ser:

```text
SessionActivityPipeline QA command
-> ActivityResetBoundaryPolicy.ResolveForQaManual(...)
-> ActivityResetBoundaryPolicy.FilterActorResetReferencesByScopePlan(...)
-> ActivityParticipantResetCommand com referências filtradas
-> ActorResetAdapter executa side-effects
```

## Owner

- `SessionActivityPipeline`: owner do comando QA e validação de ciclo ativo.
- `ActivityResetBoundaryPolicy`: owner de boundary/scope/policy e do filtro canônico de referências de actor reset.
- `ActivityEntryParticipantResetStage`: executor determinístico de reset de participant/actor durante entry.
- `ActorResetAdapter`: executor técnico de side-effects; não decide policy.

## Alterações aplicadas

- `ActivityResetBoundaryPolicy` ganhou `FilterActorResetReferencesByScopePlan(...)` como filtro canônico reutilizável.
- `ActivityEntryParticipantResetStage` deixou de manter filtro privado duplicado e passou a chamar a policy.
- `SessionActivityPipeline.TryQaResetCurrentPlayerActor(...)` agora resolve `ActivityResetScopePlan` com `ResolveForQaManual(...)`.
- O QA actor reset agora registra `ActorResetQaScopePlanResolved`.
- O QA actor reset filtra `ActorCapabilityResetEndpointReference` antes de criar `ActivityParticipantResetCommand`.
- O log `ActorResetQaAppliedFromInventory` agora inclui `resetBoundaryKind`, `resetTargetScope`, `resetPolicyId` e `sourceReferenceCount`.
- Se todas as referências forem filtradas, o QA registra `ActorResetQaSkipped` com `actor_reset_qa_references_filtered_by_scope_policy`.

## Não alterado

- Não foi criado manager/coordinator.
- Não foi criado trilho paralelo de QA.
- Não foi alterado `ActorResetAdapter` para decidir policy.
- Não foi alterado endpoint de actor reset.
- Não foi alterado object reset.
- Não foi alterado RouteExit/SessionReset.
- Não foi criada compat/alias/fallback.

## Risco controlado

A policy `qa_manual_reset_all_declared_groups` ainda permite os grupos já usados no fluxo atual:

- `Placement`
- `ActivityParticipation`
- `MovementTransient`
- `SpawnedRuntimeObjects`

Portanto, o comportamento esperado do QA actor reset deve permanecer igual. A diferença arquitetural é que o caminho manual agora usa o mesmo owner de policy e o mesmo filtro canônico de referências.

## Smoke esperado

Executar smoke mínimo:

1. Boot -> Menu -> Sandbox.
2. CompleteActivationWindow.
3. Acionar QA Reset Current Player Actor em `activity_01`.
4. Acionar QA Object Reset em `activity_01` opcionalmente.
5. CompleteCurrentActivity -> CompleteDeactivationWindow.
6. Validar entrada em `activity_02`.
7. Acionar QA Reset Current Player Actor em `activity_02`.
8. RouteExitBackToMenu.

Critérios:

- sem `FATAL`;
- sem `Exception`;
- sem `route_transition_failed`;
- sem `error CS`;
- sem `checkpointStatus='Failed'`;
- `ActorResetQaScopePlanResolved` observado;
- `resetBoundaryKind='QaManual'`;
- `resetTargetScope='CurrentActivityEntry'`;
- `resetPolicyId='qa_manual_reset_all_declared_groups'`;
- `behaviorMode='FilteringByScopePlan'`;
- `ActorResetQaAppliedFromInventory` preservado;
- `appliedGroups='4'` nos cenários atuais, salvo mudança autoral explícita;
- `Activity01ToActivity02 checkpointStatus='Passed'` preservado;
- `ActivityObjectReset checkpointStatus='PassedNoCommands'` preservado em `activity_02`;
- `RouteExitBackToMenu checkpointStatus='Passed'` preservado.

## Critério de PASS

Só aceitar PASS com smoke/log confirmando o QA actor reset em `activity_01` e, idealmente, em `activity_02` após transição.
