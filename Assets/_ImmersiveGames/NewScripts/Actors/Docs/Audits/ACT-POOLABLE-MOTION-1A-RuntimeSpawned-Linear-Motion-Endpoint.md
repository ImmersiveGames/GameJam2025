# ACT-POOLABLE-MOTION-1A - RuntimeSpawned Linear Motion Endpoint

Status: implemented / pending manual smoke.

## Objetivo

Adicionar movimento linear ao projectile runtime-spawned sem transferir ownership para o adapter técnico ou para o fire endpoint.

## Audit curto

- `ActorProjectileFireProfileAsset.FireMode` é o owner correto de `motionStrategy=Linear` e `linearSpeed`.
- `ActorProjectileFireEndpoint` resolve `direction + speed + strategy` e monta o bootstrap runtime.
- `PooledActorProjectileSpawnAdapter` continua técnico: entrega bootstrap depois de rent, `SetPositionAndRotation` e `RuntimeSpawnedActor.BindRuntimeMetadata(...)`.
- `ActorProjectileMotionEndpoint` é o owner do movimento por frame no próprio projectile prefab.
- `ActorProjectileMotionTickAdvanced` foi throttled para o primeiro tick após bootstrap; o endpoint continua sendo o owner do movimento.
- `GameObjectPool` já chama todos os `IPoolableObject` do prefab, então o novo endpoint pode limpar estado no return/reset sem alterar o pool.

## Shape final escolhido

```text
ActorProjectileFireProfileAsset.FireMode.motionStrategy
ActorProjectileFireProfileAsset.FireMode.linearSpeed
ActorProjectileMotionBootstrap
ActorProjectileMotionEndpoint
```

### Owner do movimento

`ActorProjectileMotionEndpoint` move o próprio objeto em `Update()`, apenas quando bootstrapado.

### Como o bootstrap é entregue

- fire endpoint resolve bootstrap a partir do fire mode;
- adapter aplica `RuntimeSpawnedActor` metadata primeiro;
- depois injeta bootstrap no motion endpoint do spawned object;
- o adapter não ticka movimento.

### Como o endpoint limpa estado

- `OnPoolCreated()` limpa estado inicial;
- `OnPoolReturn()` limpa estado e loga `ActorProjectileMotionStateCleared`;
- `OnPoolDestroyed()` limpa estado e loga `ActorProjectileMotionStateCleared`.

## Logs esperados

```text
ActorProjectileSpawnMotionBootstrapConfigured
ActorProjectileMotionConfigured
ActorProjectileMotionTickAdvanced
ActorProjectileMotionStateCleared
```

`ActorProjectileMotionTickAdvanced` deve aparecer poucas vezes, idealmente uma por projectile após bootstrap.

## Fora do corte

- arco;
- lifetime gameplay;
- collision;
- impact;
- damage;
- auto-return gameplay;
- `PlayerMovementController`;
- movimento no adapter ou no fire endpoint;
- manager/registry genérico.

## Smoke esperado

```text
ActorProjectileFireCommandBuilt
ActorProjectileSpawnPoolRentRequested
RuntimeSpawnedActorMetadataBound
ActorProjectileSpawnMotionBootstrapConfigured
ActorProjectileMotionConfigured
ActorProjectileSpawnedFromPool
ActorProjectileSpawnInstancePrepared
ActorProjectileMotionTickAdvanced
ActorProjectileSpawnTracked
ActorProjectileMotionStateCleared
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
```

`ActorProjectileMotionTickAdvanced` deve ser suficiente para provar movimento sem repetir por frame.
