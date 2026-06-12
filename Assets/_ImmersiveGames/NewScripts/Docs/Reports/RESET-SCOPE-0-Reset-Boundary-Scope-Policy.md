# RESET-SCOPE-0 — Reset Boundary/Scope Policy Passiva

## Status

`APPLIED / PENDING COMPILE + SMOKE`.

Este corte não altera comportamento de reset. Ele adiciona contratos passivos, policy explícita e observabilidade para preparar filtragem futura por boundary/scope.

## Decisão

O comportamento default atual permanece correto:

```text
ActivityEntry / ActivityTransition reexecuta reset da entry atual.
```

O problema arquitetural não era o reset ocorrer na troca de Activity, mas a ausência de uma policy explícita entre:

```text
capability/endpoints declaram grupos de reset suportados
```

e:

```text
stage executa os grupos declarados
```

## Alterações aplicadas

```text
ActivityResetBoundaryKind
ActivityResetTargetScope
ActivityResetScopePlan
ActivityResetBoundaryPolicy
```

`ActivityResetBoundaryPolicy.ResolveForActivityEntry(...)` produz o plano default:

```text
boundaryKind='ActivityEntry'
targetScope='CurrentActivityEntry'
policyId='default_activity_entry_reset_all_declared_groups'
behaviorMode='PassiveNoFiltering'
```

Grupos default de Actor:

```text
Placement
ActivityParticipation
MovementTransient
SpawnedRuntimeObjects
```

Grupos default de ActivityObject:

```text
Placement
ActivityParticipation
TransformState
RuntimeTransient
InteractionState
ObjectiveState
SpawnedRuntimeObjects
```

## Fronteira preservada

```text
ActivityEntryPipeline continua owner da ordem/readiness da entry.
ActivityEntryObjectResetStage continua executor determinístico de object reset.
ActivityEntryParticipantResetStage continua executor determinístico de participant/actor reset.
ActorResetAdapter continua executor técnico por endpoint.
Endpoints locais continuam aplicando reset local.
```

## O que este corte não faz

```text
não filtra grupos ainda;
não muda quais resets executam;
não altera RouteExit;
não altera SessionReset;
não mistura reset com release/dematerialization;
não remove ActivityResetStage de QA;
não cria resolver/manager/coordinator;
não cria fallback;
não cria compat.
```

## Observabilidade esperada

O smoke deve mostrar:

```text
ActivityResetScopePlanResolved
boundaryKind='ActivityEntry'
targetScope='CurrentActivityEntry'
policyId='default_activity_entry_reset_all_declared_groups'
behaviorMode='PassiveNoFiltering'
```

E os logs/facts de reset devem preservar:

```text
ObjectResetStarted
ObjectResetCommandIssued
ObjectResetApplied / ObjectResetCompleted
ActivityEntryParticipantResetStarted
ActorResetInventoryReferencesResolved
ActivityParticipantResetAppliedFromInventory
ActivityEntryParticipantResetCompleted
```

## Critério de smoke

Não aceitar PASS sem smoke manual contendo:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
ActorResetQaApplied preservado
ActivityObjectReset PassedApplied / PassedNoCommands preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
ActivityResetScopePlanResolved observado
behaviorMode='PassiveNoFiltering'
```

## Próximo corte possível

Somente depois de smoke PASS:

```text
RESET-SCOPE-1 — filtrar grupos por ActivityResetScopePlan nos commands
```

Regra para o próximo corte:

```text
filtrar antes de criar command;
não colocar policy no adapter;
não colocar policy no endpoint;
não deixar QA com trilho paralelo de policy.
```
