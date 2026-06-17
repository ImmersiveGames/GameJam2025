# ACT-PROJ-POOL-1A — Injected Pool Service for Projectile Spawn Adapter

## Status

Applied / Pending compile + smoke.

## Objetivo

Remover a resolução tardia de `IPoolService` de dentro de `PooledActorProjectileSpawnAdapter`.

O adapter continua sendo o executor técnico do side-effect de pool/spawn, mas não conhece mais `DependencyManager.Provider`. A dependência obrigatória é resolvida uma vez no composition root e passada por construtor pelo caminho de binding.

## Fronteira correta

```text
SessionActivityCompositionInstaller
= resolve dependência obrigatória `IPoolService` no composition root.

ActivityEntryPipeline
= recebe dependência já resolvida e cria o adapter de binding.

ActorCommandBindingAdapter
= resolve participante -> Actor ativo e delega binding de projectile.

ActorProjectileFireCommandBindingExecutor
= cria `PooledActorProjectileSpawnAdapter` com `IPoolService` explícito.

PooledActorProjectileSpawnAdapter
= executa Rent/Spawn técnico via `IPoolService` injetado.
```

## Alteração aplicada

- `PooledActorProjectileSpawnAdapter` passou a receber `IPoolService` no construtor.
- Removido `DependencyManager.Provider.TryGetGlobal<IPoolService>(...)` do adapter de spawn.
- `ActorProjectileFireCommandBindingExecutor` passou a receber `IPoolService` no construtor.
- `ActorCommandBindingAdapter` passou a receber `IPoolService` no construtor.
- `ActivityEntryPipeline` passou a receber `IPoolService` no construtor.
- `SessionActivityCompositionInstaller` resolve `IPoolService` com erro fatal se a dependência obrigatória estiver ausente.

## O que não mudou

- Não mudou `PoolService`.
- Não mudou `IPoolService`.
- Não mudou reset/release.
- Não mudou permission/gate.
- Não mudou spawn profile.
- Não mudou pool definition.
- Não criou manager/coordinator novo.
- Não criou fallback silencioso.

## Critério de aceite

Smoke deve confirmar:

```text
ActorCommandSinkBound
ActorProjectileSpawnPoolRentRequested
ActorProjectileSpawnedFromPool
ActorProjectileFireSpawnAdapterCompleted
ActorProjectileSpawnTracked
```

E deve permanecer sem:

```text
FATAL
Exception
route_transition_failed
checkpointStatus='Failed'
```

## Observação

`ActorProjectileSpawnRuntimeTracker` ainda resolve `IPoolService` diretamente para retorno de objetos spawned. Isso fica fora deste corte porque o objetivo aqui é só remover a resolução tardia do adapter de spawn. A normalização do tracker deve ser um corte separado, para não misturar spawn binding com lifecycle/reset/release.
