# POOLABLE-ORIGIN-1B - ActorProjectileFireEndpoint consumes Presentation-backed Origin Surface

Status: implemented / pending manual smoke.

## Objetivo

Fazer o `ActorProjectileFireEndpoint` consumir a surface de origem materializada por `ActorPresentationEndpoint`, sem mover ownership para o adapter técnico de spawn.

## Audit curto

- `ActorPresentationEndpoint` continua sendo o owner local do índice de `PoolableSpawnOriginAnchor`.
- `ActorProjectileFireEndpoint` é o consumer correto do origin surface no caminho de tiro.
- `PooledActorProjectileSpawnAdapter` continua técnico: recebe `origin` e `direction` já resolvidos.
- `spawnOriginId` e `spawnOriginResolutionMode` passam a ser authoring do `ActorProjectileFireProfileAsset`.
- `ActorProjectileFireEndpoint` resolve origin antes do build do comando e rejeita explicitamente quando a surface não está disponível.
- Ausência de renderer/material continua sendo observação; não é hard-gate do spawn lógico.

## Shape final escolhido

```text
PoolableSpawnOriginId
PoolableSpawnOriginResolutionMode
PoolableSpawnOriginResolved
IPoolableSpawnOriginSurface
ActorProjectileFireProfileAsset.spawnOriginId
ActorProjectileFireProfileAsset.spawnOriginResolutionMode
```

### Owner da resolução

`ActorProjectileFireEndpoint` chama `ActorCapabilitySurface -> ActorPresentationEndpoint -> IPoolableSpawnOriginSurface` antes de montar `ActorProjectileFireCommand`.

### Como a resolução funciona

- se o anchor typed existir, o endpoint usa a surface materializada;
- se o modo permitir fallback, o `visual.root` materializado pode ser usado explicitamente;
- se a presentation surface não existir ou a origem não for resolvida, o tiro falha de forma observável;
- o adapter de spawn não reconstrói origin, não procura anchor e não decide fallback.

## Logs esperados

```text
ActorProjectileFireOriginResolutionStarted
ActorProjectileFireOriginResolved
ActorProjectileFireOriginFallbackApplied
ActorProjectileFireOriginMissing
ActorProjectileFireCommandBuilt
ActorProjectileSpawnedFromPool
ActorProjectileSpawnInstancePrepared
ActorProjectileSpawnTracked
```

## Fora do corte

- alterar motion, collision, damage, lifetime, impact ou audio;
- chamar `ActivityEntryActorPresentationStage` pelo tiro;
- usar `PlayerMovementController`;
- criar novo spawner, manager ou pipeline paralelo;
- mover ownership da surface para o adapter técnico.

## Smoke esperado

```text
ActorProjectileSpawnPoolRentRequested
ActorProjectileFireOriginResolutionStarted
ActorProjectileFireOriginResolved
ActorProjectileFireCommandBuilt
ActorProjectileSpawnedFromPool
ActorProjectileSpawnInstancePrepared
ActorProjectileSpawnTracked
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
```
