# SA-7B3 — Object/Actor inventory store source ownership cleanup

## Objetivo

Reduzir o resíduo observável em runtime-state stores que ainda registravam `source` como bridge específica, mesmo quando a decisão de store vinha de stages canônicos do `ActivityEntryPipeline`.

## Escopo

Alterado apenas o ownership observável dos stores/clears abaixo:

- `ActivityObjectExitRuntimeStateContributorDiscoveryStored`
- `ActivityObjectExitRuntimeStateInventoryPreviewStored`
- `ActivityObjectExitRuntimeStateInventoryCleared`
- `ActivityActorExitRuntimeStateInventoryFeedStored`
- `ActivityActorExitRuntimeStateInventoryFeedCleared`

## Resultado esperado

Antes:

```text
source='ActivityEntryObjectSetupRuntimeBridge'
source='ActivityEntryActorInventoryRuntimeBridge'
```

Depois:

```text
source='ActivityEntryObjectContributorDiscoveryStage'
source='ActivityEntryCapabilityInventoryPreviewStage'
source='ActivityEntryActorInventoryStage'
```

## Ownership

- `ActivityEntryPipeline` continua dono da ordem de setup/readiness.
- `ActivityEntryObjectContributorDiscoveryStage` é o source do contributor discovery result.
- `ActivityEntryCapabilityInventoryPreviewStage` é o source do capability inventory preview/clear.
- `ActivityEntryActorInventoryStage` é o source do actor inventory feed/clear.
- `ActivityObjectExitRuntimeState` e `ActivityActorExitRuntimeState` continuam owners factuais dos stores.

## Não alterado

- ActivationWindow.
- DeactivationWindow.
- Restart.
- RouteExit.
- Movement.
- Camera.
- Permission.
- ActorPresentation.
- ActorAttribute.
- ActorParticipation.
- ActivityContent load/unload behavior.

## Resíduo aceito

As interfaces `IActivityEntryObjectSetupRuntimeBridge` e `IActivityEntryActorInventoryRuntimeBridge` ainda existem como bridges técnicas transitórias. Este corte remove apenas o `source` observável de bridge nos runtime-state stores, sem mudar assinatura pública das bridges.

## Critério de smoke

- Sem `FATAL`.
- Sem `Exception`.
- Sem `route_transition_failed`.
- Sem `foreign/stale` indevido.
- Sem `error CS`.
- `source='ActivityEntryObjectSetupRuntimeBridge'` deve ser 0.
- `source='ActivityEntryActorInventoryRuntimeBridge'` deve ser 0.
- `source='ActivityEntryObjectContributorDiscoveryStage'` presente.
- `source='ActivityEntryCapabilityInventoryPreviewStage'` presente quando houver inventory preview/clear.
- `source='ActivityEntryActorInventoryStage'` presente.
- `ActivityEntryPipelineStarted` deve conter `objectActorStoreSourceSplit='stage_owned_store_sources'`.
- `RestartCurrentActivity` PASS.
- `Activity01ToActivity02` PASS.
- `RouteExitBackToMenu` PASS.
