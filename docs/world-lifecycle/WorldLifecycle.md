# World Lifecycle (NewScripts)

> Este documento implementa operacionalmente as decisões descritas no **ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas**.

## Visão geral do reset determinístico
O reset do mundo segue a ordem garantida pelo `WorldLifecycleOrchestrator`:

Acquire Gate → Hooks pré-despawn → Actor hooks pré-despawn → Despawn → Hooks pós-despawn → (se houver `ResetContext`) Scoped Reset Participants → Hooks pré-spawn → Spawn → Actor hooks pós-spawn → Hooks finais → Release.

O fluxo realiza:
- Acquire: tenta adquirir o `ISimulationGateService` usando o token `WorldLifecycle.WorldReset` para serializar resets.
- Hooks (pré-despawn): executa hooks registrados por serviços de spawn, hooks de cena (registrados no provider da cena) e hooks explícitos no `WorldLifecycleHookRegistry`.
- Actor hooks (pré-despawn): percorre atores registrados e executa `OnBeforeActorDespawnAsync()` de cada `IActorLifecycleHook` encontrado.
- Despawn: chama `DespawnAsync()` de cada `IWorldSpawnService` registrado, mantendo logs de início/fim.
- Hooks (pós-despawn): executa `OnAfterDespawnAsync()` na mesma ordem determinística de coleções.
- (Opt-in) Scoped reset participants: quando há `ResetContext`, executa `IResetScopeParticipant.ResetAsync()` apenas para os escopos solicitados antes de seguir para spawn.
- Hooks (pré-spawn): executa `OnBeforeSpawnAsync()` após os participantes de escopo.
- Spawn: chama `SpawnAsync()` dos serviços e, em seguida, hooks de atores e de mundo para `OnAfterSpawnAsync()`.
- Release: devolve o gate adquirido e finaliza com logs de duração.
- Nota: se não houver hooks registrados para uma fase, o sistema emite log verbose `"<PhaseName> phase skipped (hooks=0)"`.

## Ordenação determinística
Todos os hooks de lifecycle, independentemente da origem, são executados em ordem determinística:
- `Order` (quando o hook implementa `IOrderedLifecycleHook`, default = 0)
- `Type.FullName` (ordem ordinal) como desempate estável

Aplica-se a:
- hooks de mundo (`IWorldLifecycleHook`)
- hooks de ator (`IActorLifecycleHook`)

## Otimização: cache de Actor hooks por ciclo
Durante `ResetWorldAsync`, os hooks de ator (`IActorLifecycleHook`) podem ser cacheados por `Transform` dentro do ciclo para evitar varreduras duplicadas.
- O cache é limpo no `finally` do reset (inclusive em falha).
- Não há cache entre resets.

## Escopos de Reset
### Soft Reset (ex.: PlayerDeath)
- Opt-in por escopo: apenas participantes `IResetScopeParticipant` cujo escopo esteja em `ResetContext.Scopes` executam.
- Soft reset sem escopos não executa participantes.
- Não desregistra binds de UI/canvas.

### Hard Reset (ex.: GameOver/Victory)
- Recria o mundo inteiro: despawn completo e reconstrução do grafo de cena conforme Scene Flow.

## Onde o registry é criado e como injetar
- `WorldLifecycleHookRegistry` é criado e registrado apenas pelo `NewSceneBootstrapper`.
- Consumidores obtêm via DI e devem tolerar boot order (preferir `Start()` ou lazy injection + retry).

## Troubleshooting: QA/Testers e Boot Order
- Sintomas típicos: QA/tester não encontra registries, falha em `Awake`, logs iniciais “de erro”.
- Causa provável: bootstrapper ausente ou ordem de execução.
- Ação:
    1. Garantir `NewSceneBootstrapper` presente e ativo.
    2. Usar lazy injection + retry curto + timeout.
    3. Falhar com mensagem acionável se bootstrapper não rodou.

## Migration Strategy (Legacy → NewScripts)
- Consulte: **ADR-0001 — Migração incremental do Legado para o NewScripts**
- Guardrails: NewScripts não referencia concreto do legado fora de adaptadores; pipeline determinístico com gate sempre ativo.

## Baseline Validation Contract
- Checklist detalhado: `Docs/QA/WorldLifecycle-Baseline-Checklist.md`.
