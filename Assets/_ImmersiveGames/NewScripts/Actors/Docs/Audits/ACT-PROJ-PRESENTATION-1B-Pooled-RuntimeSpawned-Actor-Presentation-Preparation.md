# ACT-PROJ-PRESENTATION-1B - Pooled RuntimeSpawned Actor Presentation Preparation

Status: implemented / pending manual smoke.

## Objetivo

Materializar a presentation local do projectile runtime-spawned durante o preparo do pool, usando o hook canonico `GameObjectPool -> IPoolableObject.OnPoolCreated()`.

## Ownership fixed

- `GameObjectPool`: owner tecnico do hook de lifecycle do pool.
- `ActorPooledPresentationPreparer`: prepara a presentation local do pooled actor no creation hook.
- `ActorPresentationEndpoint`: fonte local do endpoint e da profile de presentation.
- `ActorPresentationPlanResolver`: resolve o plano sem side-effects de Unity.
- `UnityActorPresentationMaterializationAdapter`: materializa o visual no container local.
- `PooledActorProjectileSpawnAdapter`: continua apenas como adapter tecnico de spawn e observabilidade.

## Corte aplicado

- A presentation do projectile runtime-spawned agora e preparada no pool, nao no tiro.
- O runtime path ativo continua sem trilho paralelo:

```text
GameObjectPool
-> ActorPooledPresentationPreparer.OnPoolCreated()
-> ActorPresentationPlanResolver
-> UnityActorPresentationMaterializationAdapter
```

- `ActivityEntryActorPresentationStage` continua fora do fluxo de tiro.
- `Renderer` / material seguem como observacao, nao como hard-gate.
- Nao houve alteracao de motion, collision, damage, lifetime, impact, audio, spawn point ou presentation pipeline de Activity.

## Logs esperados

```text
ActorPooledPresentationPreparationStarted
ActorPooledPresentationPlanResolved
ActorPooledPresentationMaterialized
ActorPooledPresentationRetained
ActorPooledPresentationAlreadyPrepared
ActorPooledPresentationPreparationCompleted
ActorPooledPresentationPreparationSkipped
ActorPooledPresentationPreparationFailed
```

## Risks

- o prefab precisa manter `ActorPresentationEndpoint` e `VisualRoot` consistentes;
- o componente de preparacao depende do `ActorPresentationProfileAsset` configurado no endpoint;
- o smoke ainda depende de validacao manual no Unity.

## Smoke esperado

```text
ActorPooledPresentationPreparationStarted
ActorPooledPresentationPlanResolved
ActorPooledPresentationMaterialized
ActorPooledPresentationRetained
ActorPooledPresentationPreparationCompleted
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
```
