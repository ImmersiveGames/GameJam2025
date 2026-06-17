# ACT-SPAWN-LAYER-1A - FireMode layer override via RuntimeSpawnedActor restore

Status: implemented / pending manual smoke.

## Resumo

- `ActorProjectileFireProfileAsset.FireMode` é o owner autoral do override de layer via `LayerMask`.
- `ActorProjectileFireEndpoint` resolve o `LayerMask` em runtime e monta `LayerBootstrap.Override` apenas quando houver exatamente uma layer válida; mask vazio ou inválido vira skip observável, não bloqueio.
- `PooledActorProjectileSpawnAdapter` apenas aplica o bootstrap depois de rent, transform e metadata bind.
- `RuntimeSpawnedActor` captura o baseline de layer da hierarquia antes do primeiro override e restaura no retorno ao pool.

## Observabilidade

- `ActorProjectileSpawnLayerResolved`
- `ActorProjectileSpawnLayerOverrideSkipped`
- `RuntimeSpawnedActorLayerBaselineCaptured`
- `RuntimeSpawnedActorLayerOverrideApplied`
- `RuntimeSpawnedActorLayerBaselineRestored`

## Smoke esperado

```text
ActorProjectileFireCommandBuilt
ActorProjectileSpawnLayerResolved
ActorProjectileSpawnLayerOverrideSkipped
RuntimeSpawnedActorLayerBaselineCaptured
RuntimeSpawnedActorLayerOverrideApplied
ActorProjectileSpawnMotionBootstrapConfigured
ActorProjectileMotionConfigured
ActorProjectileSpawnedFromPool
ActorProjectileSpawnInstancePrepared
RuntimeSpawnedActorLayerBaselineRestored
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
```
