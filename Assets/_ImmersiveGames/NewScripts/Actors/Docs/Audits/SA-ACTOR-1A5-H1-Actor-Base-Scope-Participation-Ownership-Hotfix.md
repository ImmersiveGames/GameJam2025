# SA-ACTOR-1A5-H1 — Actor Base Scope/Participation Ownership Hotfix

Status: IMPLEMENTED / requires compile + smoke.

## Context

`SA-ACTOR-1A5` removed serialized base actor metadata too aggressively.
That was incorrect for `scope` and `participationPolicy`.

`PlayerActor` and `NonPlayerActor` are concrete specializations, but specialization must not own the actor participation group semantics.
The canonical selection metadata belongs to `Actor`:

- `ActorScope`;
- `ActorParticipationRecord.ActorParticipationPolicy`.

The specialization still owns role semantics:

- `PlayerActor` -> `ActorRole.PrimaryPlayer`;
- `NonPlayerActor` -> `ActorRole.SceneAuthoredNonPlayer`.

## Decision

`Actor` is the owner of serialized scope/participation metadata.
`PlayerActor` and `NonPlayerActor` consume that metadata instead of duplicating it.

This is not a compatibility bridge. It is the canonical authoring shape.

## Files changed

- `Actors/Runtime/Actor.cs`
- `Actors/Runtime/IActor.cs`
- `Actors/Runtime/NonPlayerActor.cs`
- `GameplayRuntime/Authoring/Actors/Player/PlayerActor.cs`
- `Actors/Players/ActivitySetup/PlayerActorInstanceSource.cs`
- `Resources/Actors/PlayerActor_v0.prefab`

## Shape after hotfix

### Actor

Serialized common metadata:

- `actorScope`;
- `participationPolicy`;
- `capabilitySurface`.

Abstract specialization metadata:

- `ActorRoleMetadata`.

### NonPlayerActor

Keeps only concrete scene-authored data:

- `nonPlayerActorId`;
- `participatingActivities`.

Uses base metadata for:

- `SceneActorScope`;
- `SceneActorParticipationPolicy`.

### PlayerActor

Keeps only runtime-bound actor identity:

- `actorId`.

Uses base metadata for:

- `ActorScopeMetadata`;
- `ActorParticipationPolicy`.

Keeps specialization role:

- `ActorRole.PrimaryPlayer`.

### PlayerActorInstanceSource

No longer hardcodes `AllActivitiesInRoute`.
It reads `runtimeActor.ActorParticipationPolicy` and `runtimeActor.ActorScopeMetadata` from the actor contract.

## Prefab migration

`PlayerActor_v0.prefab` now contains:

```yaml
actorScope: 2
participationPolicy: 1
```

This preserves current behavior:

- `ActorScope.RouteScoped`;
- `ActorParticipationPolicy.AllActivitiesInRoute`.

NPC prefabs already had the same serialized field names from `NonPlayerActor`, so the data is preserved under the inherited `Actor` fields.

## Acceptance criteria

Compile:

- no `error CS`;
- no new `warning CS`.

Smoke:

- no `FATAL`;
- no `Exception`;
- no `route_transition_failed`;
- no foreign/stale invalid state;
- no `checkpointStatus='Failed'`.

Expected facts:

- `ActivityEntryActorSceneDiscoveryCompleted`;
- `ActivityEntryActorInventoryFeedCompleted`;
- `ActorPresentationSetupCompleted`;
- `ActorPresentationReady` for `npc.generic.01`;
- `ActorPresentationReady` for `npc.route.generic.01`;
- `ActorPresentationReady` for `actor.player.primary`;
- `RouteExitBackToMenu PASS`.
