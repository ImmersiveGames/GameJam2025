# ACT-PROJ-BIND-1B — Projectile Spawn Adapter Id Neutralization

## Status

Applied / Pending compile + smoke.

## Objetivo

Remover o `adapterId` fixo `actor.projectile.spawn.adapter.pooled.primary` do binding de projectile.

O id fixo não quebrava o smoke single-player, mas carregava um resíduo semântico de `primary` em um ponto que deve funcionar por Actor/endpoint.

## Decisão

`ActorProjectileFireCommandBindingExecutor` passa a montar o `adapterId` a partir do endpoint de projectile resolvido no Actor.

Ordem de derivação:

1. `ActorProjectileFireEndpoint.EndpointId`;
2. `ActorProjectileFireEndpoint.ProfileId`;
3. `ActorId`;
4. fallback técnico `actor.projectile.spawn.adapter.pooled`.

Shape esperado no caso atual:

```text
actor.projectile.spawn.adapter.pooled.actor.projectile.fire.endpoint.primary
```

## Ownership

- `ActivityEntryPipeline`: continua dona da fase de binding.
- `ActorCommandBindingAdapter`: continua resolvendo participante -> Actor ativo.
- `ActorProjectileFireCommandBindingExecutor`: passa a decidir o id técnico do adapter a partir do endpoint resolvido.
- `PooledActorProjectileSpawnAdapter`: continua sendo adapter técnico de pool/spawn.

## O que não mudou

- Não mudou pool service.
- Não mudou spawn runtime.
- Não mudou reset/release.
- Não mudou permission/gate.
- Não mudou authoring asset.
- Não criou manager/coordinator.
- Não criou trilho Player/Enemy/NonPlayer.

## Critério de aceite

Smoke simples deve confirmar:

```text
ActorProjectileSpawnAdapterConfigured adapter='PooledActorProjectileSpawnAdapter'
ActorCommandSinkBound adapter='PooledActorProjectileSpawnAdapter'
ActorProjectileSpawnPoolRentRequested adapterId='actor.projectile.spawn.adapter.pooled.<endpoint-or-profile>'
ActorProjectileSpawnedFromPool
ActorProjectileSpawnTracked
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

O smoke multiplayer completo continua fora do escopo até existir cenário com dois participantes jogáveis.
