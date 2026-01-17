# Plano — Evolução do WorldLifecycle para WorldCycle Config-Driven (Baseline 2.1 → 2.2)

## Objetivo

Centralizar a “evolução do jogo” (fases/níveis) em **um conjunto de ScriptableObjects**, de modo que:

* o runtime **consuma configuração** (fases, spawns, intro/pre-game, regras de transição),
* os módulos “abaixo” (SceneFlow, WorldLifecycle, Phases, Spawn) virem **executores determinísticos**,
* a observabilidade do Baseline continue válida (sem regressões) com **assinaturas/reasons canônicos**.

---

## Escopo do que “sobe” primeiro (baixo risco)

Nesta etapa, o WorldCycle passa a controlar:

1. **PhaseId + ContentSignature** (fonte única de verdade por fase)
2. **WorldDefinition por fase** (spawn preset)
3. **IntroStagePolicy por fase** (Disabled/Manual/AutoSkip)
4. **Aplicação determinística no timing correto** (ScenesReady → Apply → Reset → Commit)

**Fora de escopo (por enquanto):**

* Navegação 100% data-driven (substituir `GameNavigationCatalog`)
* “Scene recipes” completos por fase (load/unload/active)
  *Motivo: impacto alto e acoplamento com cenas/produção.*

---

## Princípios e invariantes (contrato anti-regressão)

### Princípios

* **Modular / SOLID**: WorldCycle não “invade” módulos; ele injeta decisões via *resolvers* e *appliers*.
* **Event-driven**: eventos oficiais permanecem o mecanismo de integração (SceneFlow, PhaseCommitted, ResetCompleted).
* **Determinismo**: “o mesmo input (signature/reason/phaseId) produz o mesmo estado”.

### Invariantes (devem permanecer verdadeiros)

* `SceneTransitionScenesReadyEvent` acontece antes de `SceneTransitionCompletedEvent`.
* Em `profile=gameplay`: **ResetWorld** ocorre no `ScenesReady` (ou skip explicitamente justificado).
* `WorldLifecycleResetCompletedEvent` só publica após reset efetivo ou skip canônico.
* **Commit de fase** ocorre **sempre** após o reset aplicável (in-place ou com transição).

---

## Artefatos de configuração (ScriptableObjects)

### 1) `WorldCycleDefinition` (asset raiz)

Contém:

* `cycleId`, `version`
* `defaultGameplayPhaseId`
* `phases[]` (lista de `PhaseDefinition`)
* `entryRules[]` (mapeia *SceneFlow signature* → `phaseId`), com fallback para `defaultGameplayPhaseId`

### 2) `PhaseDefinition`

Campos (mínimo viável):

* `phaseId` (canônico)
* `contentSignature` (canônico)
* `worldDefinition` (spawn preset)
* `introStagePolicy` (Disabled / Manual / AutoSkip)

Campos (opcionais, para etapa futura):

* `phaseTags` / `rules`
* `transitionOverrides` (para in-place sofisticado)
* “scene recipe” por fase

---

## Serviços runtime (contratos)

### 1) `IWorldCycleCatalog`

* Fornece o `WorldCycleDefinition` ativo (produção/QA).
* Responsável por “onde buscar o asset” (Resources/Addressables/DI).

### 2) `IPhaseDefinitionResolver`

* Resolve `phaseId` → `PhaseDefinition`
* Resolve `SceneTransitionContext(signature/profile/target)` → `phaseId` (via `entryRules`)

### 3) `IPhaseWorldConfigurationApplier`

Responsável por aplicar “estado de mundo” **antes do reset**:

* Seleciona `WorldDefinition` da fase.
* Rebuild do `IWorldSpawnServiceRegistry` da cena (clear + register via factory).
* (Opcional) aplica outros knobs (gate tokens, etc.) se existirem.

### 4) `IIntroStagePolicyResolver` (config-driven)

* Política de intro por fase, substituindo heurísticas por cena/profile.

---

## Pipeline canônico de produção (ScenesReady)

### Driver oficial (novo ou evolução do existente)

**Evento de entrada:** `SceneTransitionScenesReadyEvent`
**Condição:** `profile == gameplay` (ou regras equivalentes)

**Algoritmo (determinístico):**

1. Determinar `phaseId`:

    * se existir intent no `IPhaseTransitionIntentRegistry` para a signature → usa;
    * senão resolve por `entryRules` → fallback `defaultGameplayPhaseId`.
2. `phaseDef = IPhaseDefinitionResolver.Resolve(phaseId)`
3. `IPhaseContextService.SetPending(plan)`

    * `PhasePlan` deve carregar `phaseId + contentSignature`
4. `IPhaseWorldConfigurationApplier.Apply(phaseDef, sceneContext)`
5. `ResetWorldAsync(reason=SceneFlow/ScenesReady + signature)`
6. `IPhaseContextService.TryCommitPending(signature, reason)`
7. Publicar `WorldLifecycleResetCompletedEvent` (libera completion gate)

**Saída garantida:**

* `PhaseCommittedEvent` ocorre após reset
* logs/assinaturas estáveis para evidência

---

## Fluxo In-Place (PhaseChange)

**Objetivo:** in-place também vira “config-driven” e com commit garantido.

**Algoritmo:**

1. Resolve `phaseDef` por `phaseId`
2. `SetPending(plan)`
3. `Apply(phaseDef)` (rebuild spawn registry se necessário)
4. `RequestResetAsync(reason=Phase/InPlace)`
5. `TryCommitPending(...)`

---

## QA e Evidências (sem regressão)

### Regras de QA

* QA via **Context Menu** com nomes objetivos e descrição do caso.
* Cada marco entrega:

    * logs “assinatura-chave”
    * asserts/invariantes onde aplicável
    * atualização do checklist/matriz Baseline

### Evidências mínimas por marco

* Entrar gameplay (Menu→Gameplay):

    * aparece `ScenesReady`
    * aparece `ResetWorld` (ou skip canônico)
    * aparece `PhasePendingSet`
    * aparece `PhaseCommitted` com `phaseId` e `contentSignature`
    * aparece `ResetCompletedEvent` (gate libera)
* In-place:

    * mesma sequência, com `reason=Phase/InPlace`

---

## Plano incremental (marcos)

### Marco 0 — Documento e contratos (zero comportamento)

**Entrega**

* Documento deste plano no repositório (Docs/Reports ou Docs/ADR se preferir)
* Definição do schema dos SOs (`WorldCycleDefinition`, `PhaseDefinition`) *somente design*
* Contratos (interfaces) definidos *somente design*

**Critério de aceite**

* Nenhuma mudança de fluxo/resultado do Baseline 2.1

---

### Marco 1 — Catálogo + Resolver (ainda sem aplicar spawns)

**Entrega**

* `WorldCycleDefinition` ativo (asset default)
* `IWorldCycleCatalog` e `IPhaseDefinitionResolver`
* `PhasePlan` passa a carregar `contentSignature` real

**Risco controlado**

* Ainda não mexe no spawn registry; apenas “enriquece” fase/assinatura

**Aceite**

* Baseline atual continua PASS
* Logs de fase passam a exibir `contentSignature` canônica

---

### Marco 2 — Commit canônico (fecha o ciclo de fase)

**Entrega**

* In-place: commit garantido após reset
* WithTransition: driver em `ScenesReady` passa a **consumir intent → pending → reset → commit**
* `PhaseStartPhaseCommitBridge` passa a funcionar em produção (sem depender de QA)

**Aceite**

* “Commit sempre acontece” validado nos cenários A–E do baseline

---

### Marco 3 — Aplicação de WorldDefinition por fase (spawn preset por fase)

**Entrega**

* `IPhaseWorldConfigurationApplier` com rebuild do `IWorldSpawnServiceRegistry`
* Apply acontece **antes do reset** (ScenesReady e in-place)
* Fallback seguro: se `PhaseDefinition.worldDefinition == null`, mantém comportamento atual (por cena)

**Aceite**

* Gameplay entra com spawns correspondentes à fase configurada
* Sem duplicação de spawn / sem vazamento entre fases

---

### Marco 4 — IntroStage config-driven

**Entrega**

* `IIntroStagePolicyResolver` consulta `PhaseDefinition.introStagePolicy`
* Remover heurísticas por cena/profile (ou mantê-las como fallback temporário)

**Aceite**

* Fases com `Disabled` não bloqueiam sim
* Fases com `Manual` mantêm bloqueio e liberam via UI/QA conforme esperado
* AutoSkip funciona sem QA quando configurado

---

### Marco 5 (opcional) — Routes/Scenes data-driven

**Entrega**

* Catálogo de rotas por SO (ou “scene recipes” no WorldCycle)
* `GameNavigationCatalog` vira fallback e/ou usa o asset

**Aceite**

* Transições passam a ser configuráveis sem mexer em código
* Baseline mantém evidência de ordem (Started/ScenesReady/ResetCompleted/Completed)

---

## Estratégia de rollback e segurança

* Feature flag por etapa (ex.: `WORLD_CYCLE_CONFIG_ENABLED`)
* Fallbacks explícitos:

    * sem `WorldCycleDefinition` → comportamento atual
    * sem `PhaseDefinition` para phaseId → log de erro + fallback atual (para não travar produção)
    * `worldDefinition null` → mantém worldDefinition de cena

---

## Resultado esperado ao final

* Um único lugar (assets) define:

    * fases, spawns, intro policy
* O runtime executa:

    * resolve → apply → reset → commit
* Baseline vira “contrato” real:

    * logs + invariantes estáveis, sem depender de configuração espalhada em módulos

---

Se você colar este `.md` no repositório, o próximo passo natural é eu adaptar este plano para o **layout exato de Docs/ADRs** que vocês já usam (incluindo referência de evidência e checklist Baseline), mantendo a metodologia que vocês definiram para evitar regressões.
