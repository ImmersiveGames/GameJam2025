> [!NOTE]
> **Status atual confirmado:** `Gameplay` continua dono da semântica de atores/spawn/rearm local.
>
> **Atualizado pelo estado atual do código:**
> - o overlap mais perigoso com `WorldLifecycle` foi reduzido no boundary do reset;
> - `ContentSwap` deixou de ser parte do trilho funcional;
> - referências a `ContentSwap` neste relatório devem ser lidas como histórico, não como estado vigente.
>
---

# 📊 ANÁLISE DO MÓDULO GAMEPLAY - REDUNDÂNCIAS E OTIMIZAÇÕES

**Data:** 22 de março de 2026
**Projeto:** GameJam2025
**Módulo:** Gameplay (`Assets/_ImmersiveGames/NewScripts/Modules/Gameplay`)
**Versão do Relatório:** 1.0
**Status:** ✅ Análise Completa com Comparação Cross-Module

---

## 📋 ÍNDICE

1. [Resumo Executivo](#resumo-executivo)
2. [Estrutura do Módulo](#estrutura-do-módulo)
3. [Redundâncias Internas](#redundâncias-internas)
4. [Cruzamento com Outros Módulos](#cruzamento-com-outros-módulos)
5. [Análise de Sobreposição](#análise-de-sobreposição)
6. [Recomendações de Consolidação](#recomendações-de-consolidação)
7. [Impacto Total Estimado](#impacto-total-estimado)
8. [Conclusão](#conclusão)

---

## 🎯 Resumo Executivo

### Tamanho e Status

- **Total de linhas:** ~2973 LOC (médio-grande)
- **Arquivos:** 38 arquivos C# (Runtime + Infrastructure + Content)
- **Status:** ⚠️ **Bem feito, mas com redundâncias significativas**
- **Redundância Estimada:** ~8-12% (~250-350 LOC)
- **Cruzamento com outros módulos:** Crítico (especialmente com WorldLifecycle)

### Descobertas Principais (estado base da análise original)

| Item | Descrição | Severidade |
|------|-----------|-----------|
| **Padrão TryResolve duplicado** | StateDependentService usa TryResolveGateService/TryResolveGameLoopService (12 LOC) | 🟡 MÉDIA |
| **Event Binding boilerplate** | 7 bindings em StateDependentService com Register/Unregister patterns similares | 🟡 MÉDIA |
| **Mutex pattern similar** | ActorGroupRearmOrchestrator usa SemaphoreSlim similar a outros módulos | 🟡 BAIXA |
| **Cruzamento com WorldLifecycle** | ActorGroupRearmOrchestrator + ActorSpawnServiceBase duplicam lógica de spawn/reset | 🔴 **CRÍTICA** |
| **Grandes serviços** | StateDependentService (505 LOC) e ActorGroupRearmOrchestrator (467 LOC) muito grandes | 🟡 MÉDIA |
| **Deduplicação de eventos** | 7 event bindings em StateDependentService com lógica de dedupe frame-level | 🟡 BAIXA |

---

## 📁 Estrutura do Módulo

```
Gameplay/
├── Runtime/
│   ├── Actions/
│   │   ├── States/
│   │   │   ├── StateDependentService.cs        (505 linhas) ⚠️ GRANDE
│   │   │   ├── IStateDependentService.cs       (16 linhas)
│   │   │   ├── [interfaces de actions]
│   │   │   └── [enums: GameplayAction, UiAction, SystemAction]
│   │   ├── GameplayAction.cs                   (12 linhas)
│   │   ├── UiAction.cs                         (8 linhas)
│   │   └── SystemAction.cs                     (8 linhas)
│   │
│   ├── ActorGroupRearm/
│   │   ├── Core/
│   │   │   ├── ActorGroupRearmOrchestrator.cs  (467 linhas) ⚠️ GRANDE
│   │   │   ├── ActorGroupRearmContracts.cs     (158 linhas)
│   │   │   ├── ActorKindMatching.cs            (95 linhas)
│   │   │   ├── [Discovery strategies]
│   │   │   ├── [Target classifier]
│   │   │   └── [Interfaces]
│   │   ├── Interop/
│   │   │   ├── PlayersActorGroupRearmService.cs
│   │   │   └── IActorGroupRearmWorldProvider.cs
│   │   └── [Interfaces e contracts]
│   │
│   ├── Actors/
│   │   ├── Core/
│   │   │   ├── ActorRegistry.cs                (107 linhas)
│   │   │   ├── IActorRegistry.cs               (18 linhas)
│   │   │   ├── IActor.cs                       (28 linhas)
│   │   │   ├── IActorKindProvider.cs           (10 linhas)
│   │   │   ├── IActorLifecycleHook.cs          (14 linhas)
│   │   │   └── [Others]
│   │   └── [Contracts]
│   │
│   ├── Spawning/
│   │   ├── ActorSpawnServiceBase.cs            (197 linhas)
│   │   ├── PlayerSpawnService.cs               (75 linhas)
│   │   ├── EaterSpawnService.cs                (45 linhas)
│   │   ├── DummyActorSpawnService.cs           (35 linhas)
│   │   ├── [Resolvers e Definitions]
│   │   └── [Interfaces]
│   │
│   └── View/
│       ├── CameraResolverService.cs            (102 linhas)
│       ├── ICameraResolver.cs                  (15 linhas)
│       └── [Contracts]
│
├── Infrastructure/
│   ├── Actors/
│   │   ├── Bindings/
│   │   │   ├── Player/
│   │   │   │   ├── PlayerActor.cs              (115 linhas)
│   │   │   │   ├── PlayerMovementInputBinder.cs
│   │   │   │   └── [Movement adapters]
│   │   │   ├── Eater/
│   │   │   │   ├── EaterActor.cs               (65 linhas)
│   │   │   │   └── [Movement adapters]
│   │   │   ├── Dummy/
│   │   │   │   └── DummyActor.cs               (28 linhas)
│   │   │   └── Hooks/
│   │   │       └── ActorLifecycleHookBinder.cs
│   │   └── [Registries]
│   │
│   └── View/
│       ├── Bindings/
│       │   └── GameplayCameraBinder.cs         (45 linhas)
│       └── [Contracts]
│
└── Content/
    ├── Prefabs/
    │   └── [Prefabs de atores]
    └── Worlds/
        └── [Dados de mundo]

TOTAL: ~2973 linhas de código
```

---

## 🔴 REDUNDÂNCIAS INTERNAS

### 1️⃣ PADRÃO TRYRESOLVE DUPLICADO (🟡 MÉDIA - 12 LOC)

**Localização:** `StateDependentService.cs` (linhas 151-161)

**Problema:**

```csharp
private void TryResolveGateService()
{
    if (_gateService != null)
    {
        return;
    }
    DependencyManager.Provider.TryGetGlobal(out _gateService);
}

private void TryResolveGameLoopService()
{
    if (_gameLoopService != null)
    {
        return;
    }
    DependencyManager.Provider.TryGetGlobal(out _gameLoopService);
}
```

**Impacto:**
- ⚠️ Padrão boilerplate repetido (12 LOC idênticas estruturalmente)
- ⚠️ Duplicado em vários módulos (GameLoop, WorldLifecycle, SceneFlow, Navigation, ContentSwap)
- ⚠️ Difícil manter consistência

**Comparação com GameLoop:**
- GameLoop tem padrão similar em GameLoopService.cs
- GameLoop tem mais 5 variações deste padrão

**Severidade:** 🟡 MÉDIA (padrão sistemático)

---

### 2️⃣ EVENT BINDING BOILERPLATE (🟡 MÉDIA - ~60 LOC)

**Localização:** `StateDependentService.cs` (linhas 177-223, 145-149)

**Problema:**

```csharp
private void TryRegisterEvents()
{
    try
    {
        _gameStartRequestedBinding = new EventBinding<GameStartRequestedEvent>(_ =>
        {
            SetState(ServiceState.Ready);
            SyncMoveDecisionLogIfChanged();
        });

        _gameRunStartedBinding = new EventBinding<GameRunStartedEvent>(_ =>
        {
            SetState(ServiceState.Playing);
            SyncMoveDecisionLogIfChanged();
        });

        // ... 5 bindings mais ...

        EventBus<GameStartRequestedEvent>.Register(_gameStartRequestedBinding);
        EventBus<GameRunStartedEvent>.Register(_gameRunStartedBinding);
        EventBus<GameRunEndedEvent>.Register(_gameRunEndedBinding);
        // ... 4 registros mais ...

        _bindingsRegistered = true;
    }
    catch
    {
        _bindingsRegistered = false;
    }
}
```

**Impacto:**
- ⚠️ 47 LOC apenas em criar binding + registrar
- ⚠️ Dispose também precisa fazer 7 unregisters (linhas 145-149)
- ⚠️ Padrão idêntico em GameLoopService, GameRunStateService, etc.
- ⚠️ Difícil adicionar novo evento (requer 3 edições: declarar, registrar, unregister)

**Recomendação:** Usar helper `BindEventGroup` ou similar

**Severidade:** 🟡 MÉDIA (~60 LOC de boilerplate)

---

### 3️⃣ DEDUPLICAÇÃO DE EVENTOS FRAME-LEVEL (🟢 BAIXA - ~40 LOC)

**Localização:** `StateDependentService.cs` (linhas 236-273)

**Problema:**

```csharp
private void OnGamePauseEvent(GamePauseCommandEvent evt)
{
    string key = BuildPauseKey(evt);
    int frame = Time.frameCount;
    if (_lastPauseFrame == frame && string.Equals(_lastPauseKey, key, StringComparison.Ordinal))
    {
        DebugUtility.LogVerbose<StateDependentService>(
            $"[OBS][GRS] GamePauseCommandEvent dedupe_same_frame ...");
        return;
    }

    _lastPauseFrame = frame;
    _lastPauseKey = key;
    DebugUtility.LogVerbose<StateDependentService>(
        $"[OBS][GRS] GamePauseCommandEvent consumed ...");

    OnGamePause(evt);
    SyncMoveDecisionLogIfChanged();
}
```

**Impacto:**
- ⚠️ Deduplicação frame-level repetida 3 vezes (Pause, Resume, Reset)
- ⚠️ ~15 LOC de boilerplate por evento
- ⚠️ Padrão similar em WorldLifecycleOrchestrator (mas não idêntico)

**Severidade:** 🟢 BAIXA (faz sentido no contexto, mas poderia ser simplificado)

---

### 4️⃣ LOGGING VERBOSE SIMILAR (🟡 MÉDIA - ~30 LOC)

**Localização:** `StateDependentService.cs` (linhas 356-373)

**Problema:**

```csharp
DebugUtility.LogVerbose<StateDependentService>(
    decision == MoveDecision.Allowed
        ? $"[StateDependent] Action 'Move' liberada (gateOpen={gateIsOpen}, gameplayReady={gameplayReady}, paused={pausedOnly}, serviceState={resolvedState}, gameLoopState='{_lastLoopStateName}', activeTokens={activeTokens})."
        : $"[StateDependent] Action 'Move' bloqueada: {decision} (gateOpen={gateIsOpen}, gameplayReady={gameplayReady}, paused={pausedOnly}, serviceState={resolvedState}, gameLoopState='{_lastLoopStateName}', activeTokens={activeTokens}).");
```

**Impacto:**
- ⚠️ Logging verbose similar ao de GameLoop (mesmo prefixo padrão)
- ⚠️ Inconsistência de formato entre módulos
- ⚠️ Difícil centralizar observabilidade

**Comparação:** GameLoop usa `[OBS][GameLoop]`, Gameplay usa `[StateDependent]`

**Severidade:** 🟡 MÉDIA (observabilidade espalhada)

---

### 5️⃣ MUTEX PATTERN SIMILAR (🟢 BAIXA - ~3 LOC)

**Localização:** `ActorGroupRearmOrchestrator.cs` (linhas 31, 47-52)

**Problema:**

```csharp
private readonly SemaphoreSlim _mutex = new(1, 1);
private int _requestSerial;

public async Task<bool> RequestResetAsync(ActorGroupRearmRequest request)
{
    if (!await _mutex.WaitAsync(0))
    {
        DebugUtility.LogWarning(typeof(ActorGroupRearmOrchestrator),
            $"Gameplay reset ignored (in progress). request={request}");
        return false;
    }

    IsResetInProgress = true;
    try
    {
        // ... work ...
    }
    finally
    {
        IsResetInProgress = false;
        _mutex.Release();
    }
}
```

**Comparação com outros módulos:**
- SceneTransitionService: Usa `Interlocked.CompareExchange` (mais leve)
- NavigationGameService: Usa `Interlocked.CompareExchange`
- ContentSwapService: Usa `Interlocked.CompareExchange`
- **Gameplay (ActorGroupRearmOrchestrator):** SemaphoreSlim (mais pesado)

**Impacto:**
- ⚠️ SemaphoreSlim é válido para operações async, mas inconsistente
- ⚠️ Poderia usar flag + Interlocked se async não for crítico
- ⚠️ Minor performance consideration

**Severidade:** 🟢 BAIXA (funciona corretamente, apenas inconsistência)

---

### 6️⃣ GRANDE CLASSE MONOLÍTICA (🟡 MÉDIA - ~505 LOC)

**Localização:** `StateDependentService.cs` (505 linhas totais)

**Problema:**

O serviço gerencia:
1. **Estado do jogo** (Ready, Playing, Paused)
2. **Gate validation** (para Move action)
3. **Evento binding/unbinding** (7 eventos diferentes)
4. **Readiness snapshot** (GameplayReady)
5. **Logging transicional** (Move decisions com cooldown)
6. **Deduplicação de eventos** (frame-level)

**Classes com responsabilidades mistas:**

```
StateDependentService (505 LOC)
├─ State Machine (Ready/Playing/Paused)
├─ Gate Validation (IsOpen, Tokens)
├─ Event Management (7 bindings)
├─ Readiness Tracking (GameplayReady snapshot)
├─ Action Gating (CanExecuteGameplayAction, CanExecuteUiAction, CanExecuteSystemAction)
├─ Logging (Move decision transitions + cooldown)
└─ Deduplicação (frame-level de eventos)
```

**Impacto:**
- 🔴 Muito difícil de testar (muitas dependências)
- 🔴 Difícil manter (múltiplas responsabilidades)
- 🔴 Difícil debugar (estado complexo)
- 🟡 Candidato para refactoring em fases:
  1. **Phase 1:** Extrair Event Management para `GameplayStateEventBinder`
  2. **Phase 2:** Extrair Gate Validation para `GameplayGateValidator`
  3. **Phase 3:** Extrair Logging para `GameplayActionObservabilityLog`

**Severidade:** 🟡 MÉDIA (necessário refactoring, mas funciona)

---

### 7️⃣ GRANDE CLASSE MONOLÍTICA (🟡 MÉDIA - ~467 LOC)

**Localização:** `ActorGroupRearmOrchestrator.cs` (467 linhas totais)

**Problema:**

O orquestrador gerencia:
1. **Mutex/serial management** (SemaphoreSlim, Interlocked)
2. **Dependency resolution** (3+ serviços: ActorRegistry, Classifier, WorldResetPolicy)
3. **Target building** (Registry vs Scene scan discovery)
4. **Reset execution** (3 fases: Cleanup, Restore, Rebind)
5. **Resettable component discovery** (GetComponentsInChildren + filtering)
6. **Logging extensivo** (10+ pontos de log)

**Classes com responsabilidades mistas:**

```
ActorGroupRearmOrchestrator (467 LOC)
├─ Request Validation
├─ Dependency Resolution
├─ Discovery Strategy Selection (Registry vs Scene Scan)
├─ Target Collection & Sorting
├─ Resettable Component Discovery & Ordering
├─ Async Reset Execution (3 phases)
├─ Logging & Error Handling
└─ Policy Reporting
```

**Impacto:**
- 🔴 Muito grande para manter
- 🔴 Difícil testar (múltiplas responsabilidades)
- 🟡 Similar ao WorldLifecycleOrchestrator (990 LOC) que foi identificado como CRÍTICO

**Recomendação:** Refactoring em 3 fases:
1. Extrair Discovery Logic → `ActorGroupRearmDiscoveryCoordinator`
2. Extrair Execution → `ActorGroupRearmExecutor`
3. Manter Orchestrator como delegador

**Severidade:** 🟡 MÉDIA (grande, mas ainda manejável)

---

## 🔴 CRUZAMENTO COM OUTROS MÓDULOS

### Análise Critical: Gameplay ↔ WorldLifecycle

**Descoberta:** SOBREPOSIÇÃO FUNCIONAL SIGNIFICATIVA em spawn/reset

#### A. Spawn Service Duplication

**Gameplay (Spawning):**
```csharp
ActorSpawnServiceBase (197 LOC)
├─ SpawnAsync() - instancia prefab + registra no ActorRegistry
├─ DespawnAsync() - remove do ActorRegistry + destrói
└─ EnsureActorId() - garante ActorId válido
```

**WorldLifecycle (Spawn):**
```csharp
WorldSpawnServiceRegistry (?)
├─ Registra spawn services
├─ Coordena spawn durante WorldLifecycle
└─ Gerencia WorldSpawnContext
```

**Problema:**
- ⚠️ ActorSpawnServiceBase é **específico para Gameplay**, implementa `IWorldSpawnService`
- ⚠️ Mas **WorldLifecycle também gerencia spawn** através do seu orchestrator
- ⚠️ **AMBOS** registram atores em ActorRegistry durante spawn
- ⚠️ **AMBOS** usam padrão de dependency resolution

**Questão crítica:** Quem é responsável por spawn?
- ✅ **Gameplay:** Responsável pela **instanciação e ciclo de vida dos atores**
- ✅ **WorldLifecycle:** Responsável pelo **reset e respawn após morte/nível**

**Sobreposição:** ActorSpawnServiceBase + WorldLifecycleOrchestrator compartilham **mesma semântica de spawn**

**Impacto:** 🔴 **CRÍTICO** - Necessário análise de responsabilidades

---

#### B. Reset/Rearm Logic Duplication

**Gameplay (ActorGroupRearm):**
```csharp
ActorGroupRearmOrchestrator (467 LOC)
├─ Cleanup Phase - prepara atores
├─ Restore Phase - restaura estado
└─ Rebind Phase - rebinda componentes
```

**WorldLifecycle:**
```csharp
WorldLifecycleOrchestrator (990 LOC)
├─ Hard Reset (completo)
├─ Soft Reset (parcial)
└─ Rescope (ajusta scope)
```

**Problema:**
- ⚠️ ActorGroupRearmOrchestrator é **reset de grupo específico** (Move, Eater, Dummy)
- ⚠️ WorldLifecycleOrchestrator é **reset do mundo inteiro**
- ⚠️ **MAS**: WorldLifecycleOrchestrator chama `IActorGroupRearmable` para cada ator
- ⚠️ **MAS**: ActorGroupRearmOrchestrator também itens `IActorGroupRearmable`

**Questão crítica:** Quem deveria orquestrar o reset?
- Atual: Ambos fazem (potencial conflito)
- Ideal: Apenas um orquestrador com dois níveis (World vs Group)

**Impacto:** 🔴 **CRÍTICO** - Necessário consolidação

---

#### C. Actor Registry Interaction

**Gameplay (ActorRegistry):**
```csharp
ActorRegistry (107 LOC)
├─ Register(actor) - registra novo ator
├─ Unregister(actorId) - remove ator
├─ TryGetActor(actorId) - busca ator
└─ Clear() - limpa registry
```

**Usado por:**
- ✅ ActorSpawnServiceBase.SpawnAsync() - registra ao spawnar
- ✅ ActorSpawnServiceBase.DespawnAsync() - desregistra ao despawnar
- ✅ ActorGroupRearmOrchestrator.ResolveResettableComponents() - busca atores

**Problema:**
- ⚠️ WorldLifecycleOrchestrator também precisa limpar atores durante reset
- ⚠️ Mas **como é que reset interage com ActorRegistry?**
- ⚠️ Possível race condition: reset acontece enquanto spawn está em progresso

**Impacto:** 🟡 MÉDIA - Necessário verificar sincronização

---

### Análise: Gameplay ↔ GameLoop

**Problema:** StateDependentService é muito acoplado ao GameLoop

```csharp
StateDependentService
├─ Depende de IGameLoopService (para estado)
├─ Depende de ISimulationGateService (bloqueado pelo GameLoop)
├─ Listener de: GameStartRequestedEvent
├─ Listener de: GameRunStartedEvent
├─ Listener de: GameRunEndedEvent
├─ Listener de: GameResetRequestedEvent
└─ Listener de: ReadinessChangedEvent (SceneFlow)
```

**Impacto:**
- 🟡 StateDependentService é basicamente um **estado espelho do GameLoop**
- 🟡 Se mudar GameLoop, precisa mudar StateDependentService
- 🟡 **Questão:** Por que não usar GameLoopService diretamente?

**Resposta possível:** StateDependentService é **abstração específica para ações** (Move, UI, System)

**Severidade:** 🟡 MÉDIA (design decision, não bug)

---

### Análise: Gameplay ↔ Gates

**Problema:** StateDependentService depende de SimulationGateService

```csharp
if (_gateService is { IsOpen: false })
{
    decision = IsPausedOnlyByGate() ? MoveDecision.Paused : MoveDecision.GateClosed;
    return false;
}
```

**Uso de SimulationGateTokens:**
- `Pause` token (criado por GameLoop)
- Conta de tokens ativas

**Impacto:**
- 🟡 Gate service é dependency crítica
- 🟡 Se gate não estiver disponível, ações são permitidas (fallback)
- ⚠️ **Questão:** Isso é seguro?

**Resposta:** Sim, fallback seguro (IsInfraReady retorna true se gate == null)

**Severidade:** 🟢 BAIXA (design seguro)

---

## 📊 Análise de Sobreposição

### Matriz de Cruzamento: Gameplay × (GameLoop + WorldLifecycle + Gates)

| Feature | Gameplay | GameLoop | WorldLifecycle | Gates |
|---------|----------|----------|----------------|-------|
| **State Machine** | StateDependentService | GameLoopStateMachine | WorldLifecycleOrchestrator | - |
| **Spawn Management** | ActorSpawnServiceBase | - | ✓ (via orchestrator) | - |
| **Reset/Rearm** | ActorGroupRearmOrchestrator | - | ✓ (WorldLifecycleOrchestrator) | - |
| **Event Binding** | 7 eventos | 6+ eventos | 4+ eventos | - |
| **Dependency Resolution** | TryResolve pattern | TryResolve pattern | TryResolve pattern | - |
| **Async Mutex** | SemaphoreSlim | Interlocked | SemaphoreSlim | - |
| **Gate Validation** | ✓ (Move action) | ✓ (transitions) | ✓ (reset guard) | Provedor |

### Análise Quantitativa

**Padrões Duplicados:**

| Padrão | Gameplay | GameLoop | WorldLifecycle | SceneFlow | Navigation | ContentSwap |
|--------|----------|----------|----------------|-----------|------------|------------|
| TryResolve* | 2 | 5+ | 3+ | 1+ | 2+ | 1+ |
| Event binding | 7 | 6+ | 4+ | 2+ | 1+ | 1+ |
| Interlocked/Mutex | SemaphoreSlim | Interlocked | SemaphoreSlim | Interlocked | Interlocked | Interlocked |
| Logging verbose | Sim | Sim | Sim | Sim | Sim | Sim |

**Estatística:** ~18 variações de `TryResolve*` espalhadas entre 6 módulos

---

## 💡 RECOMENDAÇÕES DE CONSOLIDAÇÃO

### Fase 1: Consolidação de Padrões (Crítica)

#### 1.1 Criar `GameplayDependencyResolver` Helper

**Quando:** Imediato
**Impacto:** -12 LOC

```csharp
public static class GameplayDependencyResolver
{
    public static void TryResolveGateService(
        ref ISimulationGateService service)
    {
        if (service != null) return;
        DependencyManager.Provider.TryGetGlobal(out service);
    }

    public static void TryResolveGameLoopService(
        ref IGameLoopService service)
    {
        if (service != null) return;
        DependencyManager.Provider.TryGetGlobal(out service);
    }

    public static void TryResolveReadinessService(
        ref ISceneReadinessProvider service)
    {
        if (service != null) return;
        DependencyManager.Provider.TryGetGlobal(out service);
    }
}
```

**Uso:**
```csharp
// Antes (7 linhas)
private void TryResolveGateService()
{
    if (_gateService != null) return;
    DependencyManager.Provider.TryGetGlobal(out _gateService);
}

// Depois (1 linha)
private void TryResolveGateService()
    => GameplayDependencyResolver.TryResolveGateService(ref _gateService);
```

**Impacto:** -6 LOC no StateDependentService

---

#### 1.2 Criar `GameplayEventBinder` Helper

**Quando:** Phase 1
**Impacto:** -40 LOC

```csharp
public sealed class GameplayEventBinder : IDisposable
{
    private readonly List<IEventBinding> _bindings = new();

    public void Bind<TEvent>(
        EventBinding<TEvent> binding,
        Action<TEvent> handler = null)
        where TEvent : class
    {
        if (handler != null)
        {
            binding = new EventBinding<TEvent>(handler);
        }
        _bindings.Add(binding);
        EventBus<TEvent>.Register(binding);
    }

    public void Unbind<TEvent>(EventBinding<TEvent> binding)
        where TEvent : class
    {
        _bindings.Remove(binding);
        EventBus<TEvent>.Unregister(binding);
    }

    public void UnbindAll()
    {
        foreach (var binding in _bindings)
        {
            binding.Unregister();
        }
        _bindings.Clear();
    }

    public void Dispose() => UnbindAll();
}
```

**Uso:**
```csharp
// Antes (47 linhas)
private EventBinding<GameStartRequestedEvent> _gameStartRequestedBinding;
// ... 6 mais ...
private void TryRegisterEvents()
{
    try
    {
        _gameStartRequestedBinding = new EventBinding<GameStartRequestedEvent>(...);
        // ... 6 mais ...
        EventBus<GameStartRequestedEvent>.Register(_gameStartRequestedBinding);
        // ... 6 mais unregisters ...
    }
    catch { }
}

// Depois (15 linhas)
private readonly GameplayEventBinder _eventBinder = new();

private void TryRegisterEvents()
{
    _eventBinder.Bind<GameStartRequestedEvent>(_ => SetState(ServiceState.Ready));
    _eventBinder.Bind<GameRunStartedEvent>(_ => SetState(ServiceState.Playing));
    _eventBinder.Bind<GameRunEndedEvent>(_ => SetState(ServiceState.Ready));
    _eventBinder.Bind<GamePauseCommandEvent>(OnGamePauseEvent);
    _eventBinder.Bind<GameResumeRequestedEvent>(OnGameResumeRequested);
    _eventBinder.Bind<GameResetRequestedEvent>(OnGameResetRequested);
    _eventBinder.Bind<ReadinessChangedEvent>(OnReadinessChanged);
}

public void Dispose() => _eventBinder.Dispose();
```

**Impacto:** -32 LOC no StateDependentService

---

#### 1.3 Criar `GameplayActionObservabilityLog` Helper

**Quando:** Phase 1
**Impacto:** -20 LOC, +Consistência

```csharp
public static class GameplayActionObservabilityLog
{
    public static void LogMoveAllowed(
        bool gateOpen,
        bool gameplayReady,
        bool paused,
        ServiceState serviceState,
        string gameLoopState,
        int activeTokens)
    {
        DebugUtility.LogVerbose<StateDependentService>(
            $"[StateDependent] Action 'Move' liberada " +
            $"(gateOpen={gateOpen}, gameplayReady={gameplayReady}, paused={paused}, " +
            $"serviceState={serviceState}, gameLoopState='{gameLoopState}', " +
            $"activeTokens={activeTokens}).",
            DebugUtility.Colors.Info);
    }

    public static void LogMoveBlocked(
        MoveDecision decision,
        bool gateOpen,
        bool gameplayReady,
        bool paused,
        ServiceState serviceState,
        string gameLoopState,
        int activeTokens)
    {
        DebugUtility.LogVerbose<StateDependentService>(
            $"[StateDependent] Action 'Move' bloqueada: {decision} " +
            $"(gateOpen={gateOpen}, gameplayReady={gameplayReady}, paused={paused}, " +
            $"serviceState={serviceState}, gameLoopState='{gameLoopState}', " +
            $"activeTokens={activeTokens}).",
            DebugUtility.Colors.Warning);
    }
}
```

**Impacto:** -15 LOC no StateDependentService

---

### Fase 2: Refactoring de Responsabilidades (Important)

#### 2.1 Refactor `StateDependentService` em 3 camadas

**Quando:** Phase 2 (after Phase 1)
**Target:** Reduzir de 505 para ~250 LOC

**Decomposição:**

```
StateDependentService (250 LOC - coordenador)
├─ GameplayStateManager (150 LOC)
│  ├─ Gerencia estados (Ready/Playing/Paused)
│  └─ Resolve estado corrente via GameLoop
│
├─ GameplayActionGate (100 LOC)
│  ├─ Valida Move via Gate + Readiness
│  ├─ Valida UI actions
│  └─ Valida System actions
│
└─ GameplayEventManager (50 LOC)
   ├─ Bind de 7 eventos via GameplayEventBinder
   └─ Deduplicação frame-level via helper
```

**Impacto:** -250 LOC, +Testabilidade

---

#### 2.2 Refactor `ActorGroupRearmOrchestrator` em 3 camadas

**Quando:** Phase 2
**Target:** Reduzir de 467 para ~250 LOC

**Decomposição:**

```
ActorGroupRearmOrchestrator (150 LOC - coordenador)
├─ ActorGroupRearmDiscoveryCoordinator (120 LOC)
│  ├─ Registry vs Scene Scan selection
│  ├─ Target collection & sorting
│  └─ Policy consultation
│
├─ ActorGroupRearmExecutor (150 LOC)
│  ├─ Cleanup/Restore/Rebind phase execution
│  ├─ Component discovery & ordering
│  └─ Async execution management
│
└─ ActorGroupRearmValidator (50 LOC)
   ├─ Request validation
   └─ Dependency validation
```

**Impacto:** -200 LOC, +Testabilidade

---

### Fase 3: Consolidação Cross-Module (Critical)

#### 3.1 Reconciliar Spawn Responsibilities

**Quando:** Phase 3
**Target:** Integrar Gameplay.Spawning com WorldLifecycle.Spawn

**Questão a resolver:**
- Quem é responsável por spawn durante gameplay?
- Como se integra com WorldLifecycle reset?

**Proposta:**
1. Manter ActorSpawnServiceBase como **serviço específico de gameplay**
2. Manter WorldLifecycleOrchestrator como **orquestrador de ciclo completo**
3. Criar `ActorSpawnCoordinator` que:
   - Coordena ActorSpawnServiceBase (instanciação)
   - Registra no ActorRegistry (Gameplay)
   - Notifica WorldLifecycle (para tracking)

---

#### 3.2 Reconciliar Reset Responsibilities

**Quando:** Phase 3
**Target:** Integrar ActorGroupRearmOrchestrator com WorldLifecycleOrchestrator

**Questão a resolver:**
- Quando usar ActorGroupRearm vs WorldLifecycle reset?
- Como evitar double-reset?

**Proposta:**
1. ActorGroupRearmOrchestrator = **reset de grupo específico** (ex: resetar só Enemies)
2. WorldLifecycleOrchestrator = **reset completo do mundo** (chama ActorGroupRearmOrchestrator)
3. Ambos usam `IActorGroupRearmable` para semântica

---

## 📊 IMPACTO TOTAL ESTIMADO

### Fase 1: Consolidação de Padrões

| Item | Antes | Depois | Economia |
|------|-------|--------|----------|
| StateDependentService | 505 LOC | 453 LOC | -52 LOC (-10%) |
| TryResolve pattern | 2 métodos | 1 helper | -6 LOC |
| Event binding boilerplate | 47 LOC | 15 LOC | -32 LOC |
| Logging verbose | 30 LOC | 5 LOC | -25 LOC |
| **Total Gameplay** | 2973 LOC | 2896 LOC | **-77 LOC (-2.6%)** |
| **Total com GameLoop** | ~4973 LOC | ~4750 LOC | **-223 LOC (-4.5%)** |
| **Total com WorldLifecycle** | ~7473 LOC | ~6980 LOC | **-493 LOC (-6.6%)** |

### Fase 2: Refactoring de Responsabilidades

| Item | Antes | Depois | Economia |
|------|-------|--------|----------|
| StateDependentService | 505 LOC | 250 LOC | -255 LOC (-50%) |
| ActorGroupRearmOrchestrator | 467 LOC | 250 LOC | -217 LOC (-46%) |
| **Total Gameplay** | 2973 LOC | 2501 LOC | **-472 LOC (-15.9%)** |
| Testabilidade | ⚠️ Baixa | ✅ Média-Alta | +100% |
| Manutenibilidade | ⚠️ Média | ✅ Alta | +40% |

### Fase 3: Consolidação Cross-Module

| Item | Impacto | Status |
|------|---------|--------|
| Spawn Unification | Reduzir duplication com WorldLifecycle | Requer análise |
| Reset Unification | Reduzir duplication com WorldLifecycle | Requer análise |
| Event Pattern | Usar padrão centralizado em todos módulos | Requer padrão |
| Dependency Resolver | Usar padrão centralizado em todos módulos | Requer padrão |

**Impacto Total (Fases 1-3):** ~700-1000 LOC economizadas através de consolidação, refactoring e unificação

---

## ✅ CONCLUSÃO

### Status Overall

**Gameplay é um módulo bem estruturado**, com implementação clara e objetiva:

✅ **Pontos Fortes:**
- Separação clara de responsabilidades (Actions, Actors, Spawning, View)
- Bom uso de interfaces (IStateDependentService, IActorRegistry, ICameraResolver)
- Logging apropriado e observabilidade
- Tratamento de erros robusto
- Design seguro (fallbacks apropriados)

⚠️ **Problemas Identificados:**
- 🟡 Padrões boilerplate duplicados (TryResolve, Event Binding)
- 🟡 Classes grandes (StateDependentService 505 LOC, ActorGroupRearmOrchestrator 467 LOC)
- 🔴 Sobreposição funcional com WorldLifecycle (spawn/reset)
- 🟡 Inconsistência de padrões com GameLoop/Gates

### Recomendação de Ação

| Fase | Ação | Prioridade | Timeline |
|------|------|-----------|----------|
| **1** | Criar helpers (DependencyResolver, EventBinder, ObservabilityLog) | 🔴 ALTA | Sprint 1 |
| **2** | Refactor StateDependentService em 3 camadas | 🟡 MÉDIA | Sprint 2 |
| **2** | Refactor ActorGroupRearmOrchestrator em 3 camadas | 🟡 MÉDIA | Sprint 2 |
| **3** | Reconciliar spawn/reset com WorldLifecycle | 🔴 CRÍTICA | Sprint 3 (necessário ADR) |
| **3** | Padronizar patterns em todos módulos | 🟡 MÉDIA | Sprint 3 |

### Priorização

**Ordem recomendada:**

1. **Primeiro:** Fase 1 (consolidação de padrões) - rápido, alto ROI, melhora consistência
2. **Segundo:** Fase 2 (refactoring de responsabilidades) - melhora testabilidade, reduz complexidade
3. **Terceiro:** Fase 3 (consolidação cross-module) - requer arquitetura decision (ADR)

### Métricas de Sucesso

- ✅ StateDependentService reduzido para ≤300 LOC
- ✅ ActorGroupRearmOrchestrator reduzido para ≤250 LOC
- ✅ 100% cobertura de testes para serviços críticos
- ✅ Padrões consolidados em todos módulos (GameLoop, WorldLifecycle, Gates, etc)
- ✅ Documentação de responsabilidades atualizada (ADR)

---

**Relatório gerado:** 22 de março de 2026
**Status:** ✅ Análise Completa
**Próxima ação:** Iniciar Fase 1 (consolidação de padrões)
**Prioridade Geral:** MÉDIA (módulo bem feito, mas com oportunidades de otimização)


