# SA-7B1 — Stage Bridge Injection Split Audit

## Objetivo

Reduzir o uso direto do agregado transitório `IActivityEntryRuntimeBridge` em stages de `ActivityEntryPipeline`, começando pelos resíduos mais visíveis no smoke do SA-7B0-H1.

## Escopo aplicado

Este corte migrou os seguintes pontos para bridges menores explícitas:

- `ActivityEntryActorInventoryStage.ExecuteSceneDiscovery`
  - `IActivityEntryIdentityRuntimeBridge`
  - `IActivityEntryFactRuntimeBridge`
  - `IActivityEntryLogRuntimeBridge`
- `ActivityEntryActorInventoryStage.ExecuteActorInventoryFeed`
  - `IActivityEntryLogRuntimeBridge`
  - `IActivityEntryActorInventoryRuntimeBridge`
- `ActivityEntryObjectContributorDiscoveryStage.Execute`
  - `IActivityEntryIdentityRuntimeBridge`
  - `IActivityEntryFactRuntimeBridge`
  - `IActivityEntryLogRuntimeBridge`
  - `IActivityEntryPreparationRuntimeBridge`
  - `IActivityEntryObjectSetupRuntimeBridge`

## Fora do escopo

Não altera:

- ActivationWindow
- DeactivationWindow
- Restart
- RouteExit
- Activity transition
- Player input behavior
- Movement behavior
- Camera behavior
- ActorPresentation behavior
- ActorAttribute behavior
- ActorParticipation behavior

## Resíduo aceito

Ainda existem stages recebendo `IActivityEntryRuntimeBridge`. Isso permanece transitório e deve ser reduzido em cortes seguintes.

Resíduos principais ainda esperados:

- `ActivityEntrySetupInventoryStage`
- `ActivityEntryObjectSnapshotContractValidationStage`
- `ActivityEntryCapabilityInventoryPreviewStage`
- `ActivityEntryObjectResetStage`
- `ActivityEntryObjectSnapshotRestoreStage`
- Player/Input/Permission/Movement/Camera/Exit stages

## Critério de aceite

O smoke deve confirmar:

- sem erro CS;
- sem FATAL;
- sem Exception;
- sem `route_transition_failed`;
- sem foreign/stale indevido;
- `ActivityEntryPipelineStarted` com `stageBridgeSplit='content_object_actor_inventory'`;
- `ActivityEntrySetupReadinessStarted` e `ActivityEntrySetupReadinessCompleted` preservados;
- `ActivityEntryActorSceneDiscoveryCompleted` preservado;
- `ActivityEntryObjectContributorDiscoveryCompleted` preservado;
- `ActivityEntryActorInventoryFeedCompleted` preservado;
- `ActivityParticipantCommandPlanReady` preservado;
- `RestartCurrentActivity`, `Activity01ToActivity02` e `RouteExitBackToMenu` PASS.
