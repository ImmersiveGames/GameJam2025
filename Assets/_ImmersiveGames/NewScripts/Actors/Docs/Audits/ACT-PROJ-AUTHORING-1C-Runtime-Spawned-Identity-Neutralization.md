# ACT-PROJ-AUTHORING-1C — Runtime Spawned Projectile Identity Neutralization

## Status

Applied / Pending compile + smoke.

## Contexto

O smoke posterior ao `ACT-PROJ-AUTHORING-1B` confirmou que os assets e ids principais de projectile foram neutralizados:

```text
actor.projectile.fire.primary
actor.projectile.spawn.primary
PoolDefinition_PrimaryProjectile
actor.projectile.spawn.adapter.pooled.primary
```

A auditoria do log também mostrou um resíduo em identidade runtime gerada:

```text
spawnedActorId='actor.projectile.actor.player.primary.fire.primary.single.<sequence>'
```

Como `ownerActorId` e `ownerActorInstanceRuntimeId` já são campos explícitos no mesmo payload, embutir o owner no `spawnedActorId` duplica domínio de identidade e reintroduz leitura player-specific no actor gerado.

## Decisão

O `spawnedActorId` gerado para projectile runtime-spawned não embute mais o `ActorId` do owner.

Antes:

```text
actor.projectile.actor.player.primary.fire.primary.single.<sequence>
```

Depois:

```text
actor.projectile.runtime.spawn.<sequence>
```

## Ownership

```text
PooledActorProjectileSpawnAdapter = adapter técnico que cria a identidade runtime do spawned actor no momento do Rent/Spawn.
RuntimeSpawnOriginMetadata = carrega ownerActorId, ownerActorInstanceRuntimeId, spawnProfileId e pool de origem.
ActorInstanceRuntimeId = mantém a correlação com o owner por escopo runtime.
```

## Fronteira preservada

```text
Não altera binding.
Não altera permission.
Não altera pool service.
Não altera reset/release.
Não altera spawn profile.
Não altera fire profile.
Não cria manager/coordinator.
```

## Critério de aceite

```text
Compile limpo.
FirePrimary continua disparando via ActorProjectileFireEndpoint.
ActorProjectileSpawnedFromPool preservado.
ActorProjectileSpawnTracked preservado.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
spawnedActorId não contém actor.player.primary.
ownerActorId e ownerActorInstanceRuntimeId continuam presentes.
```
