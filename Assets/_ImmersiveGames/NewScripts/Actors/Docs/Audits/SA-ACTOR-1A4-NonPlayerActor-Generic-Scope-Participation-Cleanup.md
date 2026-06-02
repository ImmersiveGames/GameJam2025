# SA-ACTOR-1A4 — NonPlayerActor generic scope/participation cleanup

## Status

`SA-ACTOR-1A4` removes the remaining NonPlayer-specific authoring enums from `NonPlayerActor` and points the component directly at canonical actor contracts.

## Owner decision

- `Actor` remains the process/runtime type.
- `NonPlayerActor` remains only a concrete scene-authored actor specialization.
- Scope is canonical `ActorScope`.
- Participation policy is canonical `ActorParticipationRecord.ActorParticipationPolicy`.
- No pipeline owns this authoring choice; `ActivityEntryPipeline` only consumes the resolved actor inventory.

## Changed runtime component

`NonPlayerActor` no longer serializes:

- `NonPlayerActorScope`;
- `NonPlayerActorParticipationPolicy`.

It now serializes:

- `ActorScope actorScope`;
- `ActorParticipationRecord.ActorParticipationPolicy participationPolicy`.

The public scene-authored surface remains:

- `ActorId`;
- `ActorRoleMetadata`;
- `ActorScopeMetadata`;
- `ISceneAuthoredActor.SceneActorScope`;
- `ISceneAuthoredActor.SceneActorParticipationPolicy`;
- `ISceneAuthoredActor.ResolveExplicitParticipationActivityIdsOrFail`;
- `ISceneAuthoredActor.ValidateSceneAuthoredConfigurationOrThrow`.

## Prefab serialization migration

The canonical participation enum uses this order:

```text
None = 0
AllActivitiesInRoute = 1
ExplicitActivityIds = 2
```

The former NonPlayer-specific enum used this order:

```text
Unknown = 0
ExplicitActivityIds = 1
AllActivitiesInRoute = 2
Disabled = 3
```

Because of that, the two known NonPlayer prefabs were updated from:

```text
participationPolicy: 1
```

to:

```text
participationPolicy: 2
```

This preserves the current explicit activity binding for:

- `NPC_Generic.prefab`;
- `NPC_Route_Generic.prefab`.

The obsolete serialized presentation fields were also removed from the prefab YAML because `SA-ACTOR-1A3` made `ActorCapabilitySurface.PresentationEndpoint` the only presentation source.

## Deletion required

`NonPlayerActorSetupContracts.cs` should be deleted because it only contains obsolete NonPlayer-specific enums after this cut.

It is not a compatibility contract and must not remain as a parallel vocabulary.

## Not changed

- `ActorSceneDiscoveryStage`;
- `ActivityEntryPipeline`;
- actor presentation stages;
- actor participation stages;
- player materialization;
- movement/camera bindings;
- route-exit ordering.

## Expected smoke

Hard criteria:

```text
sem erro CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
```

Actor criteria:

```text
ActivityEntryActorSceneDiscoveryCompleted
ActivityEntryActorInventoryFeedCompleted
ActorPresentationSetupCompleted
ActorPresentationReady actorId='npc.generic.01'
ActorPresentationReady actorId='npc.route.generic.01'
ActorPresentationReady actorId='actor.player.primary'
ActorParticipationEntered actorId='npc.generic.01'
ActorParticipationEntered actorId='npc.route.generic.01'
RouteExitBackToMenu checkpointStatus='Passed'
```
