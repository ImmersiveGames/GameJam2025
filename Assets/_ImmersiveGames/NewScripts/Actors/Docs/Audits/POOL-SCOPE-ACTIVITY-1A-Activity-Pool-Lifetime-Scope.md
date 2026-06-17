# POOL-SCOPE-ACTIVITY-1A — Activity Pool Lifetime Scope

Status: Applied / Pending compile + smoke
Date: 2026-06-15

## Objetivo

Introduzir uma policy simples de escopo para pools:

- `Global`: comportamento atual; pool permanece vivo até `PoolService.Shutdown()`.
- `Activity`: pool só deve permanecer materializado enquanto existir uma rota `SessionActivity` ativa.

O corte não implementa `OwnerScoped`, não vincula pool a `ActorId` e não altera ownership dos objetos spawnados.

## Perguntas obrigatórias

| Pergunta | Resposta |
|---|---|
| Qual pipeline é dono desta decisão? | `SessionOperationalPipeline`, porque ele sabe quando a rota anterior tinha Activity e a próxima rota não possui handoff para Activity. |
| Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data? | `PoolLifetimeScope` é authoring data no `PoolDefinitionAsset`; `OperationalActivityPoolReleaseStage` é stage operacional; `PoolService.ReleasePoolsForScope` é adapter/service técnico. |
| Isso é comportamento final ou bridge transitória? | Comportamento final para o eixo `Global`/`Activity`. |
| Essa compatibilidade ainda é necessária? | Sim. `Global` preserva o comportamento atual e evita destruir pools de áudio/UI/shared runtime. |
| O erro está no sintoma ou na fronteira arquitetural errada? | A fronteira faltante era lifecycle de pool por escopo, separado de lifecycle do objeto spawnado. |
| Existe owner duplicado para o mesmo lifecycle? | Não. O scope fica no `PoolDefinitionAsset`; o serviço executa cleanup técnico; o pipeline apenas emite o momento de saída da rota Activity. |

## Matriz curta

| Arquivo/classe | Responsabilidade atual | Owner correto | Problema | Ação |
|---|---|---|---|---|
| `PoolDefinitionAsset` | Define prefab/capacidade/prewarm/auto-return | Pool authoring data | Não distinguia pool global de pool de Activity | Adicionar `PoolLifetimeScope` |
| `IPoolService` | Contrato técnico global de pooling | Pooling service contract | Só tinha `Shutdown()` global | Adicionar `ReleasePoolsForScope(PoolLifetimeScope)` |
| `PoolService` | Materializa e limpa pools | Pooling adapter/service | Não liberava subconjunto por escopo | Liberar apenas pools `Activity` |
| `SessionOperationalPipeline` | Orquestra route lifecycle | Pipeline owner | Não comandava limpeza de pools ao sair de Activity | Chamar stage após route-exit/session reset |
| `OperationalActivityPoolReleaseStage` | Novo stage determinístico | Operational stage | Ausente | Executar cleanup de pools `Activity` ao sair para rota sem Activity |
| `PoolDefinition_PrimaryProjectile` | Pool de projectile do player | Pool authoring data | Devia ser Activity-scoped | Marcar `lifetimeScope: Activity` |

## Regra

```text
previous route has Activity + destination route has no SessionActivity handoff
=> release pools with PoolLifetimeScope.Activity
```

`Global` não é liberado por este corte.

## Smoke recomendado

1. Entrar em `activity_01`.
2. Completar activation window.
3. Disparar projectiles.
4. Executar `RouteExitBackToMenu`.
5. Verificar logs:

```text
OperationalActivityPoolReleaseStarted
PoolScopeReleased scope='Activity'
OperationalActivityPoolReleaseCompleted
releasedPoolCount='1'
RouteExitBackToMenu checkpointStatus='Passed'
```

Critérios:

```text
sem error CS
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem pool_return_failed
Global pools não são liberados pelo stage
Activity pools são desmaterializados ao sair para Menu
```
