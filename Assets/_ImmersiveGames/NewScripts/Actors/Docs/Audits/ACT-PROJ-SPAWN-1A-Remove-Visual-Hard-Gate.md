# ACT-PROJ-SPAWN-1A - Remove Visual Hard Gate from Pooled Projectile Spawn

## Status

Applied / Pending manual smoke.

## Objetivo

Remover `Renderer`, `Renderer.enabled` e material válido como hard-gate do spawn lógico em `PooledActorProjectileSpawnAdapter`.

## Corte aplicado

- `TryPrepareSpawnedInstance(...)` continua validando:
  - `PoolDefinitionAsset`;
  - `IPoolService.Rent` não nulo;
  - `RuntimeSpawnedActor` presente;
  - metadata runtime bindada;
  - posição e rotação aplicadas;
  - instância válida e ativa após rent.
- Falta de `Renderer`, `Renderer.enabled` ou material válido deixou de falhar o spawn.
- A observação visual passou a ser explícita no log com `visualContract='optional_for_runtime_spawn'`.

## Contrato preservado

- `ActorProjectileSpawnPoolRentRequested`
- `ActorProjectileSpawnInstancePrepared`
- `ActorProjectileSpawnedFromPool`
- `ActorProjectileSpawnTracked`

## O que não mudou

- motion;
- collision;
- damage;
- lifetime;
- impact;
- audio;
- spawn point;
- presentation pipeline.

## Smoke esperado

```text
ActorProjectileSpawnPoolRentRequested
ActorProjectileSpawnInstancePrepared
ActorProjectileSpawnedFromPool
ActorProjectileSpawnTracked
visualContract='optional_for_runtime_spawn'
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
```
