# SA-ACTOR-1A6 — Actor base ActorId ownership cleanup

Status: IMPLEMENTED / awaiting compile + smoke

## Context

`SA-ACTOR-1A5-H1` corrected the ownership of `ActorScope` and `ActorParticipationPolicy`: both are common `Actor` authoring metadata, not specialization-owned metadata.

After that correction, the remaining inconsistency was `ActorId` ownership:

- `NonPlayerActor` serialized `nonPlayerActorId`;
- `PlayerActor` serialized a hidden `actorId`;
- both values represented the same canonical domain concept: `Actor.ActorId`.

This kept the canonical model visually split by specialization even though the runtime already treats both as `Actor` entries.

## Decision

`ActorId` is now owned by the base `Actor` class.

Specializations keep only their semantic role:

- `PlayerActor` => `ActorRole.PrimaryPlayer`;
- `NonPlayerActor` => `ActorRole.SceneAuthoredNonPlayer`.

The actor selection/grouping metadata remains on base `Actor`:

- `actorId`;
- `actorScope`;
- `participationPolicy`;
- `capabilitySurface`.

## Changed files

- `NewScripts/Actors/Runtime/Actor.cs`
- `NewScripts/Actors/Runtime/NonPlayerActor.cs`
- `NewScripts/GameplayRuntime/Authoring/Actors/Player/PlayerActor.cs`
- `NewScripts/Resources/Actors/NPC_Generic.prefab`
- `NewScripts/Resources/Actors/NPC_Route_Generic.prefab`
- `NewScripts/Resources/Actors/PlayerActor_v0.prefab`

## Runtime shape

### Actor.cs

Owns canonical shared metadata:

```csharp
[SerializeField] private string actorId = string.Empty;
[SerializeField] private ActorScope actorScope = ActorScope.Unknown;
[SerializeField] private ActorParticipationRecord.ActorParticipationPolicy participationPolicy = ActorParticipationRecord.ActorParticipationPolicy.None;
[SerializeField] private ActorCapabilitySurface capabilitySurface;
```

Exposes:

```csharp
public virtual string ActorId => ActorIdValue.ToString();
public ActorId ActorIdValue => new(Normalize(actorId));
```

Provides protected runtime binding for specializations that materialize dynamically:

```csharp
protected void SetActorIdValue(ActorId newActorId, string source)
```

### NonPlayerActor.cs

No longer serializes `nonPlayerActorId`.

It uses base `Actor.ActorId` and only contributes scene-authored specialization semantics:

- `ActorRole.SceneAuthoredNonPlayer`;
- `ISceneAuthoredActor` implementation;
- explicit participating activities when policy requires them.

### PlayerActor.cs

No longer serializes its own hidden `actorId`.

It uses base `Actor.actorId` and exposes the runtime materialization API:

```csharp
public void SetActorId(ActorId newActorId)
```

which delegates to base actor id binding.

## Prefab migration

Known NPC prefabs were migrated from:

```yaml
nonPlayerActorId: npc.generic.01
```

To:

```yaml
actorId: npc.generic.01
```

Updated prefabs:

- `NPC_Generic.prefab`
- `NPC_Route_Generic.prefab`

`PlayerActor_v0.prefab` already used `actorId`; obsolete serialized `displayName` was removed from its `PlayerActor` component block.

## What this does not change

- No lifecycle ownership change.
- No pipeline behavior change.
- No actor materialization behavior change.
- No movement/camera/permission behavior change.
- No Player/NonPlayer branch introduced.

## Architectural classification

| Question | Answer |
|---|---|
| Qual pipeline é dono desta decisão? | Nenhum pipeline. Isto é `authoring/runtime actor metadata`. |
| Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data? | `authoring data` + runtime metadata no `Actor`. |
| Isso é comportamento final ou bridge transitória? | Comportamento final para ownership de `ActorId`. |
| Essa compatibilidade ainda é necessária? | Não. O campo `nonPlayerActorId` foi removido e os prefabs conhecidos foram migrados. |
| O erro está no sintoma ou na fronteira arquitetural errada? | Fronteira de authoring data: `ActorId` estava splitado por especialização. |
| Existe owner duplicado para o mesmo lifecycle? | Não é lifecycle; era owner duplicado de metadata. |

## Acceptance criteria

Compile:

- no `error CS`;
- no new `warning CS`.

Smoke:

- no `FATAL`;
- no `Exception`;
- no `route_transition_failed`;
- no invalid foreign/stale;
- no `checkpointStatus='Failed'`.

Expected markers:

- `ActivityEntryActorSceneDiscoveryCompleted`;
- `ActivityEntryActorInventoryFeedCompleted`;
- `ActorPresentationSetupCompleted`;
- `ActorPresentationReady` for `npc.generic.01`;
- `ActorPresentationReady` for `npc.route.generic.01`;
- `ActorPresentationReady` for `actor.player.primary`;
- `ActorParticipationEntered` for all three actors;
- `RouteExitBackToMenu PASS`.
