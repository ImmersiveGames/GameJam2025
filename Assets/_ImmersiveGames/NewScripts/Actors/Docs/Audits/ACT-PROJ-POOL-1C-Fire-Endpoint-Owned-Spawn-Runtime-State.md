# ACT-PROJ-POOL-1C — Fire Endpoint Owned Spawn Runtime State

## Status

Applied / Pending compile + smoke.

## Objetivo

Remover `ActorProjectileSpawnRuntimeTracker` como `MonoBehaviour` separado e manter o tracking de spawned projectiles por endpoint de fire.

## Decisão

`ActorProjectileFireEndpoint` passa a ser a borda Unity da capability de projectile fire:

- command sink de `FirePrimary`;
- provider de reset/release da capability de spawned runtime objects;
- owner de um `ActorProjectileSpawnRuntimeState` puro;
- ponto de configuração explícita de `IPoolService` vindo do binding/composition.

`ActorProjectileSpawnRuntimeState` é classe C# pura:

- não herda de `MonoBehaviour`;
- não é descoberto por scanner;
- não serializa authoring data;
- não consulta `DependencyManager.Provider`;
- rastreia spawned runtime objects daquele endpoint;
- retorna spawned runtime objects ao pool em reset/release.

## Shape anterior

```text
PlayerActor GameObject
├── ActorProjectileFireEndpoint
└── ActorProjectileSpawnRuntimeTracker
```

## Shape novo

```text
PlayerActor GameObject
└── ActorProjectileFireEndpoint
      └── ActorProjectileSpawnRuntimeState
```

## Ownership

| Responsabilidade | Owner correto |
|---|---|
| Descoberta por scanner | `ActorProjectileFireEndpoint` |
| Authoring de boundary eligibility | `ActorProjectileFireEndpoint` |
| Command sink | `ActorProjectileFireEndpoint` |
| Tracking de spawned runtime objects | `ActorProjectileSpawnRuntimeState` por endpoint |
| Retorno ao pool | `ActorProjectileSpawnRuntimeState` usando `IPoolService` injetado |
| Ordem de reset/release | Activity pipeline/stages canônicos |

## Compatibilidade

Não foi mantido componente de compatibilidade para `ActorProjectileSpawnRuntimeTracker`.

Como não há produção dependente, manter o componente separado seria preservar ownership errado por conveniência.

## Multiplayer / múltiplos endpoints

O corte preserva a opção A discutida: tracking por endpoint.

Se um Actor tiver múltiplos endpoints de fire no futuro, cada endpoint mantém seu próprio `ActorProjectileSpawnRuntimeState`:

```text
PrimaryFireEndpoint -> Primary SpawnRuntimeState
SecondaryFireEndpoint -> Secondary SpawnRuntimeState
SpecialFireEndpoint -> Special SpawnRuntimeState
```

Não foi criado state compartilhado por Actor porque isso ainda seria falso genérico sem necessidade concreta.

## Arquivos alterados

- `Actors/Projectile/Runtime/ActorProjectileFireEndpoint.cs`
- `Actors/Projectile/Runtime/ActorProjectileSpawnRuntimeState.cs`
- `Actors/Projectile/Contracts/ActorProjectileEndpointContracts.cs`
- `Actors/Projectile/Binding/ActorProjectileFireCommandBindingExecutor.cs`
- `Resources/Actors/PlayerActor_v0.prefab`
- `Docs/ADRs/ADR-2.0-0005-ActorCommandHub-ActorProjectileCapability-PoolingAudio.md`
- `REMOVED_FILES.txt`

## Arquivos removidos

- `Actors/Projectile/Runtime/ActorProjectileSpawnRuntimeTracker.cs`
- `Actors/Projectile/Runtime/ActorProjectileSpawnRuntimeTracker.cs.meta`

## Critério de smoke

PASS exige:

- sem `FATAL`;
- sem `Exception`;
- sem `route_transition_failed`;
- sem `checkpointStatus='Failed'`;
- `ActorProjectileSpawnRuntimeStatePoolServiceConfigured`;
- `ActorProjectileSpawnedFromPool`;
- `ActorProjectileSpawnTracked`;
- reset/restart retornando tracked spawns ao pool;
- provider de reset/release vindo de `ActorProjectileFireEndpoint`, não de `ActorProjectileSpawnRuntimeTracker`.
