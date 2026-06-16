# ACT-PROJ-PRESENTATION-1A - RuntimeSpawned Projectile Actor Presentation

Status: implemented / pending manual smoke.

## Objective

Normalizar `ProjectileActor_Primary.prefab` para declarar presentation como capability local do Actor runtime-spawned, sem passar por `ActivityEntryActorPresentationStage` no tiro e sem reintroduzir renderer/material como hard-gate de spawn.

## Files touched

- `Resources/Actors/ProjectileActor_Primary.prefab`
- `Resources/Actors/ActorPresentationProfile_ProjectilePrimary.asset`
- `Resources/Actors/ActorPresentationProfile_ProjectilePrimary.asset.meta`
- `Actors/Projectile/Runtime/PooledActorProjectileSpawnAdapter.cs`
- `Actors/Projectile/README.md`
- `Docs/ADRs/ADR-2.0-0005-ActorCommandHub-ActorProjectileCapability-PoolingAudio.md`

## Ownership fixed

- `RuntimeSpawnedActor`: runtime identity and metadata.
- `ActorCapabilitySurface`: local capability surface on the Actor root.
- `ActorPresentationEndpoint`: local presentation capability/authoring on the Actor.
- `ActorPresentationContainer`: visual container, not spawn point.
- `PooledActorProjectileSpawnAdapter`: technical rent/spawn and presentation observation only.

## Not changed

- no `SpawnPoint` extension;
- no `ActivityEntryActorPresentationStage` call from shot flow;
- no new pipeline;
- no collision, damage, motion, lifetime, audio or permission changes.

## Expected smoke evidence

```text
ActorProjectileSpawnPoolRentRequested
RuntimeSpawnedActorMetadataBound
ActorProjectileSpawnVisualObserved
visualContract='optional_for_runtime_spawn'
presentationEndpointPresent='True'
presentationProfileId='actor.presentation.projectile.primary'
presentationVisualRootPresent='True'
ActorProjectileSpawnInstancePrepared
ActorProjectileSpawnedFromPool
ActorProjectileSpawnTracked
ActorProjectileFireSpawnAdapterCompleted
```

## Risks

- prefab YAML must keep `VisualRoot` and `Sphere` hierarchy valid;
- if the projectile prefab loses `ActorPresentationEndpoint`, the adapter still spawns but logs `presentationEndpointPresent='False'`;
- smoke still needs manual confirmation in Unity.
