# ACT-PROJ-BIND-1A — Actor-generic projectile command binding

## Objetivo

Separar a parte genérica de projectile fire da área de Player setup.

Em termos simples:

```text
PlayerInput aperta o botão.
ActorCommandHub emite FirePrimary.
ActorProjectileFireCommandBindingExecutor liga FirePrimary ao ActorProjectileFireEndpoint.
ActorProjectileFireEndpoint executa o disparo.
```

## Dono correto

- `ActivityEntryPipeline`: orquestra setup/readiness/binding da entrada.
- `ActorCommandBindingAdapter`: resolve Actor ativo a partir do participante da Activity.
- `ActorProjectileFireCommandBindingExecutor`: faz o binding específico de projectile fire.
- `ActorProjectileFireEndpoint`: executa a capability local.
- `PooledActorProjectileSpawnAdapter`: executa spawn/pool técnico.

## O que mudou

Novo arquivo:

```text
Actors/Projectile/Binding/ActorProjectileFireCommandBindingExecutor.cs
```

`ActorCommandBindingAdapter` deixou de conter a lógica detalhada de projectile:

```text
FirePrimary binding
ActorProjectileFireEndpoint readiness
PooledActorProjectileSpawnAdapter creation
BindCommandSink(FirePrimary, endpoint)
```

Essa lógica agora fica no executor de projectile.

## O que não mudou

```text
PlayerInput continua sendo a source atual.
ActivityPlayerActorRegistry ainda é usado para achar o Actor ativo.
Permission/gate não foi alterado.
Pool service não foi alterado.
Spawn/runtime não foi alterado.
```

## Critério de smoke

Esperado no log:

```text
ActorCommandBindingReadinessObserved source='ActorProjectileFireCommandBindingExecutor'
ActorCommandSinkBound source='ActorProjectileFireCommandBindingExecutor'
ActorProjectileSpawnAdapterConfigured source='ActorProjectileFireCommandBindingExecutor'
ActorProjectileSpawnedFromPool
ActorProjectileSpawnTracked
```

Sem:

```text
FATAL
Exception
route_transition_failed
checkpointStatus='Failed'
```
