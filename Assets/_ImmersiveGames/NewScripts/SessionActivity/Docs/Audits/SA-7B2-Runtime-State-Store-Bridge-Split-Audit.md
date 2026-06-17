# SA-7B2 — Runtime State Store Bridge Split Audit

## Objetivo

Reduzir o resíduo observável `source='ActivityEntryRuntimeBridge'` nos stores/clears de runtime state executados durante a entry preparation/content load.

O corte não altera lifecycle. O `SessionActivityPipeline` continua como macro lifecycle owner, mas os stores/clears deixam de se apresentar como vindos da bridge agregada genérica.

## Alterações

### Content runtime state

Antes:

```text
source='ActivityEntryRuntimeBridge'
reason='activity_content_loaded_set_ready'
source='ActivityEntryRuntimeBridge'
reason='clear_current_activity_content_loaded_set'
```

Depois:

```text
source='ActivityEntryContentRuntimeBridge'
reason='activity_content_loaded_set_ready'
source='ActivityEntryContentRuntimeBridge'
reason='clear_current_activity_content_loaded_set'
```

Owner factual permanece `ActivityContentReleaseRuntimeState`.

### Object preparation clear

Antes:

```text
source='ActivityEntryRuntimeBridge'
reason='clear_current_activity_object_contributor_discovery_result'
```

Depois:

```text
source='ActivityEntryPreparationRuntimeBridge'
reason='clear_current_activity_object_contributor_discovery_result'
```

Owner factual permanece `ActivityObjectExitRuntimeState`.

### ActivityEntryPipeline marker

`ActivityEntryPipelineStarted` agora adiciona:

```text
runtimeStateStoreSplit='content_preparation_store_sources'
```

## Escopo não alterado

- ActivationWindow
- DeactivationWindow
- Restart
- RouteExit
- Movement
- Camera
- Permission
- ActorPresentation
- ActorAttribute
- ActorParticipation

## Resíduo aceito

Ainda é esperado haver bridges específicas como:

```text
ActivityEntryObjectSetupRuntimeBridge
ActivityEntryActorInventoryRuntimeBridge
```

Essas bridges já são mais específicas que o agregado `ActivityEntryRuntimeBridge` e devem ser reduzidas em cortes futuros apenas quando seus stages/stores forem extraídos com ownership próprio.

## Critério de smoke

```text
sem erro CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido

ActivityEntryPipelineStarted
bridgeReduction='runtime_bridge_domain_split'
stageBridgeSplit='content_object_actor_inventory'
runtimeStateStoreSplit='content_preparation_store_sources'

source='ActivityEntryRuntimeBridge' deve ser 0 no log runtime
source='ActivityEntryContentRuntimeBridge'
source='ActivityEntryPreparationRuntimeBridge'

ActivityEntrySetupReadinessStarted
ActivityEntrySetupReadinessCompleted
ActivityParticipantCommandPlanReady
ActivityParticipantBindApplied
ActivityParticipantPlacementApplied
ActivityParticipantResetApplied

RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```
