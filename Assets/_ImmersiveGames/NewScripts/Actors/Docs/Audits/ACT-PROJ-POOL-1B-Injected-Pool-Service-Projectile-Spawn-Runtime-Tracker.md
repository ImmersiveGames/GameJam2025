# ACT-PROJ-POOL-1B — Injected Pool Service for Projectile Spawn Runtime Tracker

## Status

Applied / Pending compile + smoke.

## Objetivo

Remover a resolução tardia de `IPoolService` de dentro de `ActorProjectileSpawnRuntimeTracker`.

O tracker continua sendo o endpoint técnico de reset/release para `SpawnedRuntimeObjects`, mas não pode mais consultar `DependencyManager.Provider` para devolver objetos ao pool.

## Fronteira correta

```text
SessionActivityCompositionInstaller
= resolve `IPoolService` obrigatório no composition root.

ActivityEntryPipeline
= recebe a dependência já resolvida e mantém ownership da fase de binding.

ActorCommandBindingAdapter
= resolve participante -> Actor ativo.

ActorProjectileFireCommandBindingExecutor
= configura a capability de projectile resolvida no Actor.

ActorProjectileFireEndpoint
= conhece seu tracker local e repassa a dependência técnica.

ActorProjectileSpawnRuntimeTracker
= retorna spawned runtime objects ao pool em reset/release.
```

## Alteração aplicada

- `ActorProjectileSpawnRuntimeTracker` ganhou `ConfigurePoolService(...)` explícito.
- Removido `DependencyManager.Provider.TryGetGlobal<IPoolService>(...)` do tracker.
- `ActorProjectileFireEndpoint` ganhou `ConfigureSpawnRuntimeTrackerPoolService(...)`.
- `IActorProjectileFireEndpoint` expõe a configuração do tracker como parte do binding técnico da capability.
- `ActorProjectileFireCommandBindingExecutor` configura o tracker antes de configurar o spawn adapter.

## O que não mudou

- Não mudou `PoolService`.
- Não mudou `IPoolService`.
- Não mudou spawn profile.
- Não mudou pool definition.
- Não mudou permission/gate.
- Não mudou policy de reset.
- Não criou manager/coordinator.
- Não criou fallback silencioso.

## Critério de aceite

Smoke deve confirmar:

```text
ActorProjectileSpawnRuntimeTrackerPoolServiceConfigured
ActorProjectileSpawnedFromPool
ActorProjectileSpawnTracked
ActorProjectileSpawnedRuntimeObjectsStateProfileApplied
```

E deve permanecer sem:

```text
FATAL
Exception
route_transition_failed
checkpointStatus='Failed'
pool_service_not_configured
pool_service_unavailable
```

## Observação

Este corte fecha o par iniciado em `ACT-PROJ-POOL-1A`:

```text
Spawn adapter = IPoolService injetado por construtor.
Spawn runtime tracker = IPoolService configurado explicitamente durante binding.
```

A diferença existe porque o adapter é objeto runtime comum, enquanto o tracker é `MonoBehaviour` existente no prefab do Actor.
