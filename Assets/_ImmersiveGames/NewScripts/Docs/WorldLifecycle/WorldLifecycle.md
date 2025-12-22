Doc update:
- Reset-In-Place semantics clarified
- Pause: GamePauseGateBridge ativa token SimulationGateTokens.Pause e NewScriptsStateDependentService bloqueia Move via gate (sem congelar física/timeScale)
- Infra FSM: assets e GameLoop FSM documentados
- Bootstrap NewScripts isolado do legado: GlobalBootstrap registra gate/bridge/StateDependentService sem depender do DependencyBootstrapper legado
- GlobalBootstrap também registra o driver de runtime do WorldLifecycle para orquestrar resets em produção
# World Lifecycle (NewScripts)

> Este documento descreve **operacionalmente** o comportamento do WorldLifecycle e implementa as decisões descritas no
> **ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas** (`../ADR/ADR-ciclo-de-vida-jogo.md`).

---

## Visão geral

O **WorldLifecycle** é responsável por executar resets de forma **determinística** e **serializada** via `ISimulationGateService`.
Há dois contratos principais:

- **Hard Reset (Full Reset)**: reconstrói o mundo de forma completa (despawn + spawn).
- **Soft Reset Players (Reset-In-Place)**: reset lógico por escopo, **sem despawn/spawn** (instâncias e identidades preservadas).

### Notas rápidas de infraestrutura
- Infra genérica de FSM: `_ImmersiveGames/NewScripts/Infrastructure/Fsm`
- FSM concreta do GameLoop: `_ImmersiveGames/NewScripts/Gameplay/GameLoop`
- Pause usa gate para bloquear ações (ex.: Move) e **não congela física/timeScale**.
- Fronteira de boot: **o legado não inicializa o NewScripts**. O `DependencyBootstrapper` do legado não registra serviços do NewScripts. Em `NEWSCRIPTS_MODE`, o `GlobalBootstrap` do NewScripts registra `ISimulationGateService`, `GamePauseGateBridge`, `NewScriptsStateDependentService` e demais serviços próprios antes das cenas (sem depender do bootstrap legado).
- O `GlobalBootstrap` registra o `WorldLifecycleRuntimeDriver` para acionar resets determinísticos após o Scene Flow.

### Pause (Gate não congela física)
- O pause é propagado via `GamePauseEvent(paused=true)` → `GamePauseGateBridge` → token `SimulationGateTokens.Pause` no `SimulationGateService`.
- O resume é liberado via `GamePauseEvent(paused=false)` ou `GameResumeRequestedEvent` (mesma ponte libera o token).
- **Efeito**: o `NewScriptsStateDependentService` consulta o gate e bloqueia `ActionType.Move` (ações) enquanto o token de pause estiver ativo; **não mexe em `Time.timeScale` nem congela `Rigidbody`**.
- Gravidade e física continuam rodando; apenas os controladores deixam de aplicar inputs/velocidade.
- O serviço de permissões oficial no baseline NewScripts é `NewScriptsStateDependentService`, registrado pelo `GlobalBootstrap` (nenhuma ponte legacy de StateDependent permanece).
- Para detalhes do estado global (Playing/Paused/Boot) e como os eventos entram no loop, veja `../GameLoop/GameLoop.md`.

---

## Reset determinístico — Hard Reset (Full Reset)

O hard reset segue a ordem garantida pelo `WorldLifecycleOrchestrator`:

**Acquire Gate**
→ Hooks de mundo `OnBeforeDespawn`
→ Hooks de ator `OnBeforeActorDespawn`
→ `DespawnAsync`
→ Hooks de mundo `OnAfterDespawn`
→ (opcional) `IResetScopeParticipant` (quando houver `ResetContext`)
→ Hooks de mundo `OnBeforeSpawn`
→ `SpawnAsync`
→ Hooks de ator `OnAfterActorSpawn`
→ Hooks de mundo `OnAfterSpawn`
→ **Release Gate**

### Detalhe por etapa
- **Acquire**: tenta adquirir `ISimulationGateService` com token `WorldLifecycle.WorldReset` para serializar resets.
- **Hooks (pré-despawn)**: executa hooks registrados por cena/registry na ordem determinística estabelecida.
- **Actor hooks (pré-despawn)**: percorre atores registrados e executa `OnBeforeActorDespawnAsync()` para cada `IActorLifecycleHook`.
- **Despawn**: chama `DespawnAsync()` de cada `IWorldSpawnService` registrado (com logs de duração).
- **Hooks (pós-despawn)**: executa `OnAfterDespawnAsync()` na mesma ordem determinística.
- **Scoped participants (opt-in)**: se existir `ResetContext`, executa `IResetScopeParticipant.ResetAsync()` filtrado por escopo **antes** do spawn.
- **Hooks (pré-spawn)**: executa `OnBeforeSpawnAsync()`.
- **Spawn**: chama `SpawnAsync()` dos serviços, então finaliza com hooks de ator/mundo.
- **Release**: libera o gate adquirido e finaliza com logs de duração.

### Logs de *skip*
Se não houver hooks em uma fase, o sistema emite log verbose no formato:
`"<PhaseName> phase skipped (hooks=0)"`.

---

## Reset por Escopo — Soft Reset Players (Reset-In-Place)

### Contrato
`Soft Reset Players` é **reset-in-place** por decisão arquitetural.

Durante este fluxo:
- `IWorldSpawnService.DespawnAsync` **não é chamado**.
- `IWorldSpawnService.SpawnAsync` **não é chamado**.
- Nenhuma instância é destruída/recriada.
- `ActorId` é preservado.
- `ActorRegistry` mantém a contagem (não diminui e não recompõe).

### Gate
- Token utilizado: `flow.soft_reset` (`SimulationGateTokens.SoftReset`).

### Ordem operacional (Soft Reset Players)
O soft reset executa um pipeline determinístico **específico para reset-in-place**:

**Acquire Gate (flow.soft_reset)**
→ (opcional) Hooks de mundo (se existirem para este fluxo)
→ Hooks de ator `OnBeforeActorDespawn` (atores existentes, se hooks existirem)
→ `IResetScopeParticipant` filtrado por `ResetContext.Scopes = [Players]` (ex.: `PlayersResetParticipant`)
→ Hooks de ator `OnAfterActorSpawn` (atores existentes, se hooks existirem)
→ (opcional) Hooks de mundo finais
→ **Release Gate (flow.soft_reset)**

> Observação: em soft reset, é **esperado** aparecer nos logs:
> `Despawn service skipped by scope filter` e `Spawn service skipped by scope filter`
> Isso é evidência positiva de conformidade com reset-in-place.

### Regras de execução por escopo
- Soft reset é **opt-in**: somente participantes `IResetScopeParticipant` cujo `Scope` esteja em `ResetContext.Scopes` executam.
- Soft reset **sem escopos** não executa participantes.
- Soft reset não deve desregistrar/recriar binds de UI/canvas por padrão.

---

## Hard Reset vs Soft Reset Players (Resumo)

| Aspecto | Hard Reset | Soft Reset Players (Reset-In-Place) |
|---|---|---|
| Despawn | Sim | Não (skipped by scope filter) |
| Spawn | Sim | Não (skipped by scope filter) |
| ActorId | Recriado | Preservado |
| GameObject | Reinstanciado | Mantido |
| ActorRegistry | Recomposto | Mantido (contagem estável) |
| Gate | `WorldLifecycle.WorldReset` | `flow.soft_reset` |
| Semântica | Reconstrução total | Reset lógico in-place |

---

## Otimização: cache de Actor hooks por ciclo

Durante `ResetWorldAsync`, hooks de ator (`IActorLifecycleHook`) podem ser cacheados por `Transform` **dentro do ciclo** para evitar varreduras duplicadas.

- Cache é limpo no `finally` do reset (inclusive em falha).
- Não há cache entre resets.

---

## Ciclo de Vida do Jogo (Scene Flow + WorldLifecycle)

O **Scene Flow** orquestra readiness e binds cross-scene; o **WorldLifecycle** executa despawn/spawn/reset.
As fases oficiais foram decididas no ADR:
`../ADR/ADR-ciclo-de-vida-jogo.md`.

### Linha do tempo oficial
````
SceneTransitionStarted
↓
SceneScopeReady (gate adquirido, registries de cena prontos)
↓
SceneTransitionScenesReady
↓
WorldLoaded (WorldLifecycle configurado; registries de actor/spawn ativos)
↓
SpawnPrewarm (Passo 0 — aquecimento de pools)
↓
SceneScopeBound (late bind liberado; HUD/overlays conectados)
↓
SceneTransitionCompleted
↓
GameplayReady (gate liberado; gameplay habilitado)
↓
[Soft Reset → WorldLifecycle reset scoped (Reset-In-Place)]
[Hard Reset → Desbind + WorldLifecycle full reset + reacquire gate]
````

Origem da decisão de fases:
`../ADR/ADR-ciclo-de-vida-jogo.md#definição-de-fases-linha-do-tempo`.

---

## Fases de Readiness (Resumo)

Fases formais que controlam quem pode agir e quando, garantindo que spawn/bind e gameplay sigam uma ordem previsível.

- **SceneScopeReady**: cena configurada e gate adquirido; registries disponíveis; gameplay ainda bloqueado.
- **WorldLoaded**: WorldLifecycle configurado; registries ativos; serviços de mundo podem preparar dados.
- **SceneScopeBound**: late bind liberado; HUD/overlays conectam em providers.
- **GameplayReady**: gate liberado; gameplay pode rodar.

Regra explícita:
- gameplay e lógica de atores só iniciam após `GameplayReady`.
- resets devem respeitar o gate; no soft reset, o gate bloqueia enquanto o reset-in-place roda.

---

## Spawn determinístico e Late Bind

Define como spawn acontece em passes ordenados e como binds tardios evitam inconsistências de UI/canvas cross-scene.
Decisão de passes descrita no ADR:
`../ADR/ADR-ciclo-de-vida-jogo.md#spawn-passes`.

### Por que spawn ocorre em passes
O WorldLifecycle executa passos previsíveis (pré-warm, serviços, atores, late bindables) para manter determinismo e reduzir corrida de dependências.

### Regra de binds tardios
HUD/overlays e outros cross-scene binds só conectam após `SceneScopeBound` (e antes de `GameplayReady`).

### Quando spawn e bind acontecem (passes)
- **SpawnPrewarm (Passo 0)**: aquecimento de pools e recursos.
- **World Services Spawn (Passo 1)**: serviços dependentes de mundo.
- **Actors Spawn (Passo 2)**: atores jogáveis e NPCs em ordem determinística.
- **Late Bindables (Passo 3)**: componentes que precisam existir para UI, mas sem bind imediato.
- **Binds de UI**: conectam após `SceneScopeBound`.

---

## Resets por escopo (semântica funcional)

### Escopos são domínios de gameplay
`ResetScope.Players` representa o baseline funcional de gameplay do jogador (input, câmera, HUD/UI, caches, timers relevantes),
não a hierarquia física do prefab.

### Participantes podem atuar fora do GameObject
Um `IResetScopeParticipant` de `Scope=Players` pode resetar serviços/managers/caches/timers/UI que impactem o baseline do player,
mesmo fora do prefab.

### Anti-pattern explícito
Interpretar `ResetScope.Players` como “reset apenas dos componentes dentro do GameObject Player” é incorreto.

---

## Baseline Audit — ResetScope.Players (2025-12-19)

### Contexto
Foi realizada uma auditoria técnica para verificar o estado real da implementação de `ResetScope.Players`
em relação ao contrato de **Soft Reset Players (Reset-In-Place)**.

O objetivo foi identificar:
- quais mecanismos já existem,
- quais subsistemas efetivamente participam do reset,
- e quais lacunas impedem o uso do soft reset como mecânica de gameplay (retry / restart).

### Estado encontrado (As-Is)

**Infraestrutura**
- `ResetScope`, `ResetContext`, `IResetScopeParticipant` e filtros por escopo existem em `ResetScopeTypes.cs`.
- `WorldLifecycleController` expõe `ResetPlayersAsync` para soft reset Players.
- `WorldLifecycleOrchestrator` executa reset por escopo via `ResetScopesAsync` com gate `SimulationGateTokens.SoftReset`.
- O filtro por escopo impede despawn/spawn e ignora hooks de mundo e de ator (comportamento consistente com reset-in-place).

**Participantes**
- Existe apenas um participante registrado para `ResetScope.Players`: `PlayersResetParticipant`.
- O participante atual executa apenas logs e **não reseta subsistemas**.

**Efeito prático**
- O Soft Reset Players executa gate + pipeline, mas **não restaura baseline de gameplay**.
- Nenhum estado de player, input, câmera, UI, cooldowns ou caches é resetado.

### Baseline funcional identificado (fora do NewScripts)

Foram identificados subsistemas com APIs explícitas de reset-in-place já existentes:

**Dentro do prefab do Player**
- `PlayerMovementController` — possui `Reset_CleanupAsync`, `Reset_RestoreAsync`, `Reset_RebindAsync`.
- `PlayerShootController` — possui `Reset_CleanupAsync`, `Reset_RestoreAsync`, `Reset_RebindAsync`.
- `PlayerInteractController` — possui `Reset_CleanupAsync`, `Reset_RestoreAsync`, `Reset_RebindAsync`.
- `PlayerDetectionController` — possui `Reset_CleanupAsync`, `Reset_RestoreAsync`, `Reset_RebindAsync`.

**Fora do prefab (cross-object)**
- `CanvasCameraBinder` — rebind de câmera em UI world-space.
- `RuntimeAttributeControllers / bridges` — UI/atributos ligados a atores.

Esses sistemas **não estão atualmente conectados** ao `ResetScope.Players`.

### Lacunas principais
1. `ResetScope.Players` não executa reset funcional de gameplay.
2. Hooks de mundo e de ator não participam do soft reset (por filtro de escopo).
3. Nenhum participante externo (UI, câmera, domínios/managers/timers) está registrado.
4. Serviços de domínio/registries e pools podem reter estado entre retries.

### Conclusão
O contrato de Soft Reset Players está corretamente definido e protegido (reset-in-place, sem despawn/spawn),
porém **carece de payload funcional**.

Próximos passos devem focar em acionar o baseline existente via `IResetScopeParticipant`, sem alterar o pipeline
nem o contrato do WorldLifecycle.

---

## Onde o registry é criado e como injetar

- `WorldLifecycleHookRegistry` é criado e registrado apenas pelo `NewSceneBootstrapper`.
- Consumidores obtêm via DI e devem tolerar boot order:
    - preferir `Start()` ou
    - lazy injection + retry curto + timeout com mensagem acionável.

---

## Troubleshooting: QA/Testers e Boot Order

Sintomas típicos:
- tester não encontra registries
- falha em `Awake`
- logs iniciais “de erro” antes do bootstrap

Ações:
1. Garantir `NewSceneBootstrapper` presente e ativo.
2. Usar lazy injection + retry curto + timeout.
3. Falhar com mensagem acionável se bootstrapper não rodou.

---

## Migration Strategy (Legacy → NewScripts)

- Consulte: **ADR-0001 — Migração incremental do Legado para o NewScripts**
- Guardrails: NewScripts não referencia concretos do legado fora de adaptadores; pipeline determinístico com gate sempre ativo.

---

## Baseline Validation Contract

Checklist detalhado:
`../QA/WorldLifecycle-Baseline-Checklist.md`
