# ADR-0016 — Phases/Níveis integradas ao WorldLifecycle (NewScripts)

## Status

**Proposed → Accepted** (ao iniciar implementação)
Data: 2026-01-05

## Contexto e problema

O projeto já possui um pipeline canônico e validado (Baseline 2.0) para reset determinístico durante transições:

`SceneFlow (Started → ScenesReady → Reset/Skip → ResetCompleted → BeforeFadeOut → Completed)`

Precisamos adicionar um sistema de **Fases/Níveis (Phase 1..N)** que:

* selecione spawns/objetos/dados diferentes por fase,
* permita **Restart da fase atual** e **troca de fase**,
* garanta que o `ResetWorld` reconstruirá o **mundo correto** (fase correta),
* evite regressões do Baseline 2.0 (strings, ordem, gates).

## Objetivos

1. Tornar “fase” um **input explícito** do rebuild do mundo.
2. Garantir que **RestartPhase** reconstrói a **mesma fase** (mesmo plano).
3. Garantir que **ChangePhase** reconstrói com a **nova fase** (commit atômico no reset).
4. Preservar o contrato do Baseline 2.0:

    * não alterar strings canônicas (ex.: `ScenesReady/GameplayScene`),
    * não alterar ordem/gates do SceneFlow,
    * manter `WorldLifecycleResetCompletedEvent` como confirmação oficial no caminho canônico.

## Não-objetivos (nesta fase)

* Save/Load persistente completo.
* Hot-swap parcial de conteúdo “sem reset”.
* Streaming de mundos gigantes (sublevels) ou world partition.
* Balanceamento final (difficulty curves) e tooling avançado (editor UI).

## Decisão

Introduzir um **Phase System** composto por:

* um serviço global `IWorldPhaseService` que mantém o estado de fase,
* assets `PhaseDefinition` que descrevem o conteúdo da fase,
* um resolvedor `IPhaseDefinitionResolver` + construção de `PhaseSpawnPlan`,
* integração no WorldLifecycle: capturar `PhaseSnapshot` no início do reset e spawns consumirem `PhaseSpawnPlan`.

**Regra principal:** *mudança de fase só é “aplicada” (commit) no início do ResetWorld*, garantindo atomicidade.

---

# Arquitetura proposta

## Componentes

### A) Global: IWorldPhaseService (fonte de verdade)

Responsabilidades:

* manter `CurrentPhaseId`
* manter `RequestedPhaseId` (opcional)
* manter `PhaseEpoch` (incrementa a cada “enter phase”)
* fornecer `PhaseSnapshot GetSnapshot()`
* expor comandos:

    * `RequestPhase(phaseId, reason)`
    * `RestartPhase(reason)`
    * opcional: `SetSeed(runSeed/phaseSeed)` para determinismo

**Regra:** este serviço não spawna; apenas comanda/declara o estado.

### B) Data: PhaseDefinition (conteúdo por fase)

Asset (ScriptableObject) descrevendo:

* grupos de spawn (Players, Enemies, Props, Objectives…)
* parâmetros e dados específicos (tabelas, configs, wave sets)
* opcional: seed override por fase

### C) Resolver: IPhaseDefinitionResolver / PhaseSpawnPlanBuilder

Dado um `PhaseSnapshot`, resolve:

* `PhaseDefinition`
* gera `PhaseSpawnPlan` (estrutura imutável para o reset atual)

### D) Integração WorldLifecycle: “Phase snapshot is captured once”

No início do reset (antes de spawn), executar:

1. Capturar snapshot atual.
2. Se houver `RequestedPhaseId`, **commit**:

    * `CurrentPhaseId ← RequestedPhaseId`
    * `RequestedPhaseId ← null`
    * `PhaseEpoch++`
3. Resolver `PhaseDefinition` e construir `PhaseSpawnPlan`.
4. Tornar o plan disponível para participants/spawn services do reset (por contexto do reset ou serviço scene).

---

# Integração com SceneFlow e Baseline 2.0

## Caminho canônico (baseline-safe)

Durante transição gameplay:

* `ScenesReady` dispara reset como hoje
* `reason` canônico permanece `ScenesReady/<ActiveScene>` (ex.: `ScenesReady/GameplayScene`) **sem anexar phaseId**
* logs adicionais de fase são **novas assinaturas**, não substituem as existentes.

## Caminho manual (fora de transição)

* `IWorldResetRequestService.RequestResetAsync(source)` pode ser usado para `RestartPhase` e `ChangePhase` quando não há transição
* Como não há `SceneTransitionContext`, **não** existe `WorldLifecycleResetCompletedEvent(signature, reason)` nesse caminho.
* Se gameplay/UI precisar de confirmação, emitir um evento específico:

    * `WorldPhaseResetCompletedEvent(phaseId, epoch, reason)`
      (Não reutilizar o evento do SceneFlow.)

---

# Invariantes (anti-regressão)

1. **Commit atômico da fase** ocorre apenas no início do reset (nunca no meio).
2. `PhaseSnapshot` é **imutável por reset** (capturado uma vez).
3. `PhaseSpawnPlan` é derivado apenas do snapshot do reset atual.
4. Strings canônicas do Baseline 2.0 não mudam:

    * `ScenesReady/GameplayScene`
    * `Skipped_StartupOrFrontend:...`
5. Ordem do SceneFlow e gates não mudam (BaselineInvariantAsserter continua válido).
6. Se múltiplas requests de fase ocorrerem durante reset, a última request fica em `RequestedPhaseId` para o **próximo reset**.

---

# Plano de implementação (incremental e registrável)

## Milestone 1 — Foundation (sem alterar spawns ainda)

**Deliverables**

* `IWorldPhaseService`, `PhaseSnapshot`, `PhaseId` (string ou struct)
* `PhaseDefinition` (SO) + um `PhaseDefinitionCatalog` (lista/mapa)
* `IPhaseDefinitionResolver` (catálogo → definição)
* Logs novos (apenas adicionados):

    * `[Phase] Snapshot captured phaseId='X' epoch=Y requested='Z' reason='...'`
    * `[Phase] Committed phase change from='A' to='B' epoch=Y`

**Validação**

* Play Mode: não deve mudar baseline smoke.
* `rg -n "\[Phase\]"` deve mostrar snapshot/commit quando houver request.

## Milestone 2 — SpawnPlan (fase decide o “o quê”)

**Deliverables**

* `PhaseSpawnPlan` (imutável)
* `PhaseSpawnPlanBuilder` (PhaseDefinition + snapshot → plan)
* ponto de integração no reset: plan fica acessível por participants (via serviço scene ou contexto do orchestrator)
* logs:

    * `[Phase] SpawnPlan built phaseId='X' players=... enemies=... props=...`

**Validação**

* Rodar smoke baseline: não pode quebrar.
* Confirmar que logs `[Phase]` não alteram os logs canônicos.

## Milestone 3 — Participants usam o SpawnPlan (fase muda conteúdo)

**Deliverables**

* Atualizar participants/spawn services para consumirem `PhaseSpawnPlan`
* Garantir ordem determinística por escopo se mantém
* “Phase 1 vs Phase 2” com conteúdo diferente (ex.: quantidades/IDs)

**Validação**

* Cenário: Phase1 → Reset (restart) → igual
* Cenário: Phase1 → RequestPhase2 → Reset → conteúdo de Phase2
* ActorRegistry count esperado por fase (se aplicável)

## Milestone 4 — API de jogo (produção) para fase

**Deliverables**

* `IGamePhaseCommands` (ou expandir `IWorldPhaseService` com comandos públicos) com:

    * `RestartCurrentPhase(reason)`
    * `RequestNextPhase(phaseId, reason)`
* Integração com fluxo:

    * Durante gameplay (sem transição): chama `RequestResetAsync("Phase/...")`
    * Se design exigir transição de cena: via `IGameNavigationService` + profile gameplay

**Validação**

* Pressionar comando/hotkey de DEV (se existir) e observar:

    * `Reset IGNORED (scene-transition)` durante transição
    * `Reset REQUESTED ProductionTrigger/...` fora da transição
    * Fase correta aplicada pós reset

---

# Logs e evidências

## Logs canônicos preservados

* `[WorldLifecycle] Disparando hard reset após ScenesReady...`
* `[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent...`
* `[SceneFlow] Completion gate concluído...`

## Logs adicionados (fase)

* `[Phase] ...` (prefixo único para grep)
* Nunca substituir/alterar mensagens baseline.

---

# Critérios de pronto (DoD)

* Baseline 2.0 checklist-driven continua **Pass** (A–E).
* Existem pelo menos 2 fases com conteúdo diferente e testadas:

    * RestartPhase reconstrói a mesma fase.
    * ChangePhase reconstrói a nova fase.
* Requests concorrentes não geram estado “meio fase”.
* Documentação atualizada:

    * este ADR é a referência,
    * `WORLD_LIFECYCLE.md` ganha apenas link para o ADR (sem reescrever baseline).

---

# Riscos e mitigação

* **Risco:** colocar phaseId no `reason` do ResetCompleted e quebrar baseline.
  **Mitigação:** manter `reason` canônico; phaseId apenas em logs `[Phase]`.
* **Risco:** participants cachearem definição antiga.
  **Mitigação:** consumir `PhaseSpawnPlan` do reset atual (imutável) e invalidar por `PhaseEpoch`.
* **Risco:** requests no meio de transição.
  **Mitigação:** respeitar gate `flow.scene_transition` (IGNORED com log explícito) e deixar request pendente para depois.

---

# Pontos de verdade (para evitar “circular”)

1. **Este ADR** define “como fazer fase”.
2. **Baseline 2.0 Spec (frozen)** define “o que não pode quebrar”.
3. **Checklist-driven report** é a evidência operacional de não-regressão.
