# Actor Projectile — active authoring shape

Status: `ACT-PROJ-3G / CLOSED`.

This folder owns the current projectile/fire capability shape. Projectile fire is an Actor capability, not an ObjectEmission bridge.

## Active runtime path

```text
PlayerActorCommandInputHub
-> ActorProjectileFireEndpoint
-> PooledActorProjectileSpawnAdapter
-> IPoolService
-> RuntimeSpawnedActor
```

## Pool preparation

`ActivityEntryPoolPreparationStage` consulta `IActorRuntimePoolDependencyProvider` nos actors descobertos na Activity Entry e chama `IPoolService.EnsureRegistered(...)` apenas para pools com `registrationMode == ActivityEntry` antes de `ActivityRunning`.

`ActorProjectileFireEndpoint` declara suas dependências de pool a partir do `ActorProjectileFireProfileAsset` e do `ActorProjectileSpawnProfileAsset` dos fire modes. O pool primário do projectile fica em `registrationMode=ActivityEntry` com `prewarm=true`.

## Active authoring assets

```text
ActorProjectileFireProfileAsset
-> ActorProjectileSpawnProfileAsset
-> PoolDefinitionAsset
```

### ActorProjectileFireProfileAsset

Owner: fire behavior.

Use it for:

- fire mode identity;
- spawn pattern;
- muzzle policy;
- spread policy;
- cooldown;
- reference to `ActorProjectileSpawnProfileAsset`.

Do not use it for:

- pool capacity;
- prefab identity;
- spawned actor lifecycle execution;
- damage/collision/VFX/audio.

### ActorProjectileSpawnProfileAsset

Owner: projectile spawn authoring.

Use it for:

- typed `PoolDefinitionAsset` reference;
- materialization kind;
- lifetime/reset/snapshot policies for the spawned actor;
- role/scope applied to `RuntimeSpawnedActor` during rent.

Do not use it for:

- input binding;
- fire pattern;
- movement/collision/damage behavior;
- direct spawn side-effects.

### PoolDefinitionAsset

Owner: technical pool.

Use it for:

- pooled prefab;
- initial capacity;
- expansion limits;
- prewarm;
- technical pool label.

Do not use it for:

- gameplay actor identity;
- command routing;
- fire policy;
- save/reset policy decisions.

## RuntimeSpawnedActor rule

`RuntimeSpawnedActor` must remain prefab-neutral. ActorId, ActorInstanceRuntimeId, ActorRole and ActorScope are applied by `BindRuntimeMetadata(...)` after rent.

The prefab must not carry a concrete gameplay actor id for a specific pool/source.

## Removed path

`ActorSpawnabilityProfileAsset` is not part of the active projectile path after `ACT-PROJ-3F`.

Do not reintroduce:

```text
ActorProjectileFireProfileAsset -> ActorSpawnabilityProfileAsset -> PoolDefinitionAsset
```

## Smoke evidence expected

```text
ActorCommandSinkBound commandId='FirePrimary' state='Executable'
FirePrimary outside ActivityRunning -> projectile_fire_endpoint_inactive
RuntimeSpawnedActorMetadataBound
ActorProjectileSpawnVisualObserved
ActorProjectileSpawnInstancePrepared
ActorProjectileSpawnedFromPool
ActorCommandDispatchAccepted dispatchReason='projectile_spawned_from_pool'
```

## Open debts

- `ACT-CMD-CONFIG-1`: configurable ActorCommand definitions instead of command enum/hardcoded factory.
- `ACT-PROJ-LIFE-1`: projectile lifetime/return-to-pool by spawned actor policy/object, not global pool coroutine.
- `ACT-PROJ-MOTION-1`: projectile motion.
- `ACT-PROJ-COLLISION-1`: collision/hit detection.
- `ACT-PROJ-DAMAGE-1`: damage/effects.
- `ACT-PROJ-VFX-AUDIO-1`: fire/impact VFX and audio.
- `ACT-PROJ-SPREAD-DET-1`: deterministic spread/random when a seed/context exists.

## ACT-PROJ-4A — Command ownership cleanup

`ActorProjectileFireProfileAsset` no longer declares an accepted command. The fire profile describes projectile behavior; command/input ownership remains in ActorCommand binding.

## ACT-PROJ-4B1 — Runtime spawned origin metadata

`RuntimeSpawnedActor` stores runtime origin metadata after pool rent.

Stored origin:

```text
ownerActorId
ownerActorInstanceRuntimeId
spawnProfileId
originPoolDefinition
commandSequence
```

The metadata is created by `PooledActorProjectileSpawnAdapter` and passed into `RuntimeSpawnedActor.BindRuntimeMetadata(...)`.

Do not infer owner or pool by parsing `ActorInstanceRuntimeId`. Use `RuntimeSpawnedActor.SpawnOrigin` / convenience properties instead.

## ACT-PROJ-4B2-FIX1 — cooldown e auto-return canônico

O projectile não possui componente próprio de lifetime/return.

- Cooldown de disparo: `ActorProjectileFireEndpoint`, timer local por `fireModeId`.
- Return ao pool: `PoolDefinitionAsset.autoReturnSeconds` + módulo canônico de pooling.
- `PooledActorProjectileSpawnAdapter`: aluga, aplica metadata, valida materialização lógica do actor runtime-spawned e observa visual/presentation como contrato opcional.
- `RuntimeSpawnedActor`: guarda origem (`ownerActorId`, `ownerActorInstanceRuntimeId`, `spawnProfileId`, `originPoolDefinition`, `commandSequence`).

Não criar `RuntimeSpawnedActorLifetime`, `RuntimeSpawnedActorPoolReturnAdapter` ou trilho paralelo de return-to-pool dentro de projectile.

## ACT-PROJ-PRESENTATION-1A — Runtime-spawned projectile local presentation

`ProjectileActor_Primary.prefab` declara `ActorPresentationEndpoint` no root do Actor e um `VisualRoot` local (`VisualRoot` / `visual.root`) como capability authoring do projectile.

- O shot path continua sendo:

```text
PlayerActorCommandInputHub
-> ActorProjectileFireEndpoint
-> PooledActorProjectileSpawnAdapter
-> IPoolService
-> RuntimeSpawnedActor
```

- `ActivityEntryActorPresentationStage` não faz parte do fluxo de tiro.
- `PooledActorProjectileSpawnAdapter` só observa a presença de presentation e visual.
- `Renderer` / material continuam observáveis, não são hard-gate de spawn lógico.
