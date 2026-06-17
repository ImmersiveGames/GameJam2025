# POOL-PREP-1A-1B - Activity Pool Preparation and Pool Object Hooks

## Status

Applied / Pending manual smoke.

## Audit summary

O repositório ja possuía os mecanismos tecnicos corretos no owner de pool:

- `PoolService.EnsureRegistered(PoolDefinitionAsset)` ja era idempotente e disparava `GameObjectPool.Prewarm()` quando `prewarm=true`.
- `GameObjectPool` ja chamava `IPoolableObject.OnPoolCreated()` na criacao da instancia.
- `GameObjectPool` ja chamava `IPoolableObject.OnPoolRent()` no rent.

O que faltava era o owner correto da preparacao antecipada:

- nenhum stage da `ActivityEntryPipeline` registrava/preaquecia pools declarados por capabilities/endpoints antes do primeiro tiro;
- `ActorProjectileFireEndpoint` nao expunha a dependencia de pool como contrato de capability;
- faltava observabilidade minima do ciclo do objeto pooled.

## Corte aplicado

- Adicionado `IActorRuntimePoolDependencyProvider`.
- `ActorProjectileFireEndpoint` passou a declarar os pools derivados de seus fire modes.
- Adicionado `ActivityEntryPoolPreparationStage` na `ActivityEntryPipeline`, depois do discovery/preview e antes de `ActivityRunning`.
- A stage chama somente `IPoolService.EnsureRegistered(poolDefinition)` e deduplica por `PoolDefinitionAsset`.
- `GameObjectPool` passou a logar:
  - `PoolObjectCreated`;
  - `PoolObjectPrepared`;
  - `PoolObjectRentPrepared`.

## Contrato preservado

- `PooledActorProjectileSpawnAdapter` continua executando apenas o rent/spawn tecnico no tiro.
- `ActivityEntryActorPresentationStage` nao foi tocada.
- motion, collision, damage, lifetime, impact, audio e spawn point nao foram alterados.

## Smoke esperado

```text
ActivityEntryPoolPreparationStarted
ActivityEntryPoolDependencyResolved
ActivityEntryPoolPrepared
ActivityEntryPoolPreparationCompleted
GameObjectPool Prewarm complete
PoolService Ensure registered
PoolObjectCreated
PoolObjectPrepared
PoolObjectRentPrepared
ActorProjectileSpawnedFromPool
ActorProjectileSpawnTracked
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
```
