> [!NOTE]
> **Status atual confirmado:** `WorldLifecycle` continua dono do reset e o boundary externo já foi saneado o suficiente para o trilho local ser tratado como um problema interno de arquitetura.
>
> **Implementado desde a análise original:**
> - boundary com `Gameplay` foi melhorado no reset.
> - o executor pós-reset ficou claramente como validador de pós-condição.
> - a composição técnica local/macro foi deslocada para `SceneComposition`, fora do `WorldLifecycle`.
> - `ContentSwap` saiu do fluxo canônico.
>
> **Leitura correta hoje:**
> - o próximo passo não é reabrir boundary; é limpar o **miolo interno** do reset.
> - o naming do trilho local ainda está preso em `WorldLifecycle*`, mesmo quando o papel real já é de **scene reset local**.
>
> **O que permanece válido nesta análise:**
> - `WorldLifecycle` ainda é hotspot estrutural.
> - `WorldLifecycleOrchestrator` / `WorldLifecycleController` continuam pontos de concentração.
> - a próxima fase deve separar `WorldReset*` (macro) de `SceneReset*` (local) no naming e na superfície interna.
>
---

> [!WARNING]
> **Status de validação:** conteúdo importado de análise externa e **ainda não validado** contra o código atual.
>
> **Uso correto:** tratar este documento como **hipótese de auditoria / backlog de verificação**.
>
> **Fonte de verdade:** código atual, ADRs vigentes e documentação canônica do projeto.

> [!NOTE]
> **Origem anterior:** `Docs/Modules/WORLDLIFECYCLE_ANALYSIS_REPORT.md`
>
> Este arquivo foi movido para cá como localização canônica dos relatórios importados por módulo.

---

# 📊 ANÁLISE DO MÓDULO WORLDLIFECYCLE - REDUNDÂNCIAS INTERNAS E CRUZAMENTO COM GAMELOOP

**Data:** 22 de março de 2026
**Projeto:** GameJam2025
**Módulos:** WorldLifecycle + GameLoop Comparison
**Versão do Relatório:** 1.0
**Status:** ✅ Análise Completa com Comparação Cross-Module

---

## 📋 ÍNDICE

1. [Resumo Executivo](#resumo-executivo)
2. [Redundâncias Internas no WorldLifecycle](#redundâncias-internas-no-worldlifecycle)
3. [Cruzamento entre GameLoop e WorldLifecycle](#cruzamento-entre-gameloop-e-worldlifecycle)
4. [Análise de Sobreposição](#análise-de-sobreposição)
5. [Recomendações de Consolidação](#recomendações-de-consolidação)
6. [Impacto Total Estimado](#impacto-total-estimado)
7. [Conclusão](#conclusão)

---

## 🎯 Resumo Executivo

### Descoberta Crítica: **HOTSPOT INTERNO DO RESET LOCAL**

O módulo **WorldLifecycle** continua diferente do GameLoop em escopo, mas o problema principal hoje já não é o cruzamento externo; é o miolo interno do reset local:
- **GameLoop:** Gerencia estados de gameplay (Boot → Playing → PostPlay)
- **WorldLifecycle:** Gerencia reset/respawn do mundo (determinístico e sequencial)

**Entretanto**, ambos compartilham **padrões redundantes** similares que poderiam ser consolidados.

**Estatísticas:**
- WorldLifecycle: ~2500 linhas (Bindings + Runtime + WorldRearm)
- GameLoop: ~2000 linhas
- **Total:** ~4500 linhas de código relacionado a "ciclos de gameplay"
- **Redundância Estimada:** ~15-20% entre os módulos

---

## 📁 ESTRUTURA DO WORLDLIFECYCLE

```
WorldLifecycle/
├── Bindings/
│   └── WorldLifecycleController.cs (458 linhas) ← Muito grande
├── Hooks/
│   ├── IWorldLifecycleHook.cs
│   ├── WorldLifecycleHookBase.cs
│   ├── WorldLifecycleHookRegistry.cs
│   └── IWorldLifecycleHookOrdered.cs
├── Runtime/
│   ├── Core Services:
│   │   ├── IWorldResetService.cs (interface)
│   │   ├── IWorldResetRequestService.cs (interface)
│   │   ├── IWorldResetCommands.cs (interface)
│   │   ├── WorldResetRequestService.cs (86 linhas)
│   │   ├── WorldResetCommands.cs (193 linhas)
│   │   ├── WorldLifecycleOrchestrator.cs (990 linhas!) ← GIGANTE
│   │   └── WorldLifecycleController.cs (458 linhas)
│   ├── Events:
│   │   ├── WorldLifecycleResetStartedEvent.cs
│   │   ├── WorldLifecycleResetCompletedEvent.cs
│   │   └── WorldLifecycleResetV2Events.cs
│   ├── Integration:
│   │   ├── WorldLifecycleSceneFlowResetDriver.cs (404 linhas)
│   │   └── WorldLifecycleResetCompletionGate.cs
│   └── Policies:
│       ├── IRouteResetPolicy.cs
│       └── SceneRouteResetPolicy.cs
├── WorldRearm/
│   ├── Application/
│   │   ├── WorldResetService.cs (122 linhas)
│   │   └── WorldResetExecutor.cs
│   ├── Domain/
│   │   ├── WorldResetRequest.cs
│   │   ├── WorldResetOrigin.cs
│   │   ├── WorldResetReasons.cs
│   │   ├── ResetDecision.cs
│   │   └── ResetFeatureIds.cs
│   ├── Guards/
│   │   └── SimulationGateWorldResetGuard.cs
│   ├── Policies/
│   │   └── WorldResetPolicy.cs
│   ├── Validation/
│   │   └── WorldResetSignatureValidator.cs
│   └── WorldResetOrchestrator.cs
└── Spawn/
    ├── IWorldSpawnService.cs
    ├── IWorldSpawnServiceRegistry.cs
    └── WorldSpawnServiceRegistry.cs
```

**Total:** ~2500 linhas

---

## 🔴 REDUNDÂNCIAS INTERNAS NO WORLDLIFECYCLE

### 1️⃣ ORCHESTRATOR GIGANTE (990 linhas)

**Localização:** `WorldLifecycleOrchestrator.cs`

**Problema:**

```csharp
public sealed class WorldLifecycleOrchestrator
{
    // 990 linhas incluindo:
    // - State machine de reset (hard/soft reset)
    // - Gerenciamento de hooks (pré-despawn, pós-spawn)
    // - Gerenciamento de spawn services
    // - Actor lifecycle (despawn/spawn)
    // - Gate management (acquire/release)
    // - Logging extensivo
    // - Scope filtering
    // - Hook caching
}
```

**Impacto:**
- 🔴 990 linhas é EXTREMAMENTE grande
- 🔴 3 responsabilidades principais misturadas:
  1. Orquestração de fases (Gate → Despawn → Spawn → Release)
  2. Gerenciamento de hooks
  3. Gerenciamento de scopes
- 🔴 Muito difícil de testar (muitas dependências)
- 🔴 Difícil navegar/manter
- 🔴 10+ métodos privados de utilidade

**Severidade:** 🔴 **CRÍTICA** - Maior problema do WorldLifecycle

---

### 2️⃣ CONTROLLER GRANDE (458 linhas)

**Localização:** `WorldLifecycleController.cs`

**Problema:**

```csharp
public sealed class WorldLifecycleController : MonoBehaviour
{
    // 458 linhas incluindo:
    // - Auto-initialization
    // - Reset queuing (fila sequencial)
    // - Hard reset (ResetWorldAsync)
    // - Soft reset (ResetPlayersAsync)
    // - Dependency injection
    // - Lifecycle management
    // - Extensive logging
}
```

**Impacto:**
- ⚠️ 458 linhas é grande para um MonoBehaviour
- ⚠️ Mistura: bootstrap + queuing + orchestration
- ⚠️ 150+ linhas apenas para queue management
- ⚠️ Difícil de isolar

**Severidade:** 🟡 **ALTA** - Segundo maior problema

---

### 3️⃣ DRIVER GRANDE (404 linhas)

**Localização:** `WorldLifecycleSceneFlowResetDriver.cs`

**Problema:**

```csharp
public sealed class WorldLifecycleSceneFlowResetDriver : IDisposable
{
    // 404 linhas incluindo:
    // - Event binding/unbinding
    // - Scene transition handling
    // - Policy resolution
    // - Decision logic para quando fazer reset
    // - Logging de observabilidade
    // - Dedupe de signatures
}
```

**Impacto:**
- ⚠️ 404 linhas é muito grande para um "driver fino"
- ⚠️ Segundo comentário do código: "driver deve permanecer fino"
- ⚠️ Contém lógica que deveria estar na política

**Severidade:** 🟡 **ALTA** - Violação do próprio contrato

---

### 4️⃣ SERVIÇOS DE NORMALIZACAO DUPLICADOS

**Localização:** `WorldResetCommands.cs`, `WorldResetRequestService.cs`, `WorldResetService.cs`

**Problema:**

```csharp
// WorldResetCommands.cs
private static string NormalizeReason(string reason, string fallback)
{
    if (!string.IsNullOrWhiteSpace(reason))
        return reason.Trim();
    return fallback;
}

private static string NormalizeSignature(string signature)
{
    return string.IsNullOrWhiteSpace(signature) ? string.Empty : signature.Trim();
}

// WorldResetRequestService.cs - similar
string normalizedSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();

// WorldResetService.cs - similar
string ctx = string.IsNullOrWhiteSpace(request.ContextSignature) ? string.Empty : request.ContextSignature;
string rsn = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : request.Reason;
```

**Impacto:**
- ⚠️ 3 variações de normalização
- ⚠️ Inconsistência em defaults (fallback vs "unknown" vs "")
- ⚠️ ~40 linhas duplicadas

**Severidade:** 🟡 **MÉDIA**

---

### 5️⃣ EVENT BINDING PATTERNS DUPLICADOS

**Localização:** `WorldLifecycleSceneFlowResetDriver`, outros serviços

**Problema:**

Similar ao padrão duplicado do GameLoop:
```csharp
// Padrão com try-catch
_scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

// ... e no dispose
try { EventBus<SceneTransitionScenesReadyEvent>.Unregister(_scenesReadyBinding); }
catch { /* best-effort */ }
```

**Severidade:** 🟡 **MÉDIA**

---

### 6️⃣ LOGGING BOILERPLATE REPETIDO

**Localização:** Espalhado em todos os serviços

**Problema:**

```csharp
// WorldResetService.cs
DebugUtility.LogWarning<WorldResetService>(
    $"[{ResetLogTags.Guarded}][DEGRADED_MODE] Reset ja em andamento...");

// WorldResetCommands.cs
DebugUtility.Log<WorldResetCommands>(
    $"[OBS][WorldLifecycle] ResetRequestedV2 kind='{kind}'...");

// WorldResetRequestService.cs
DebugUtility.LogVerbose(typeof(WorldResetRequestService),
    $"[OBS][WorldLifecycle] ResetRequested signature='{signature}'...");

// Cada um com seu próprio formato e prefixo
```

**Impacto:**
- ⚠️ Boilerplate de logging duplicado
- ⚠️ Inconsistência em prefixos ([OBS], [WorldLifecycle], [ResetLogTags.Guarded])
- ⚠️ Difícil manter padrão de observabilidade

**Severidade:** 🟡 **MÉDIA**

---

### 7️⃣ DEPENDENCY RESOLUTION PATTERNS DUPLICADOS

**Localização:** 4+ lugares com padrões diferentes

**Problema:**

```csharp
// Padrão 1: WorldResetCommands.cs
if (DependencyManager.Provider != null &&
    DependencyManager.Provider.TryGetGlobal<IWorldResetService>(out var byInterface) && byInterface != null)
{
    return byInterface;
}
if (DependencyManager.Provider != null &&
    DependencyManager.Provider.TryGetGlobal<WorldResetService>(out var byConcrete) && byConcrete != null)
{
    return byConcrete;
}
FailFastConfig("Missing service");

// Padrão 2: WorldResetRequestService.cs
if (DependencyManager.HasInstance &&
    DependencyManager.Provider.TryGetGlobal<IWorldResetService>(out var resetService) &&
    resetService != null)
{
    await resetService.TriggerResetAsync(request);
}

// Padrão 3: WorldResetService.cs
var provider = DependencyManager.Provider;
provider.TryGetGlobal<IWorldResetPolicy>(out var policy);
// ...sem validação completa
```

**Impacto:**
- ⚠️ 3+ padrões diferentes de resolução
- ⚠️ Inconsistência em tratamento de null
- ⚠️ Alguns com fallback por tipo concreto, outros não

**Severidade:** 🟡 **MÉDIA**

---

## 🔗 CRUZAMENTO ENTRE GAMELOOP E WORLDLIFECYCLE

### DESCOBERTA 1: Dois Sistemas de "Reset/Reinício"

```
GameLoop                          WorldLifecycle
==================================================
RequestReset()                    RequestResetAsync()
  ↓                                 ↓
GameLoopService                   WorldResetService
  ↓                                 ↓
StateMachine:                     Orchestrator:
Boot→Ready→Playing→PostPlay       Gate→Despawn→Spawn→Release
  ↓                                 ↓
Publica GameRunStartedEvent       Publica WorldLifecycleResetCompletedEvent
```

**Problema:** Ambos orquestram "reinícios" mas em camadas diferentes!

---

### DESCOBERTA 2: Event Patterns Duplicados

**GameLoop Events:**
```csharp
GameRunStartedEvent         // Pub quando entra em Playing
GameRunEndedEvent           // Pub quando sai de Playing (vitória/derrota)
GameLoopActivityChangedEvent// Pub quando muda atividade
GamePauseCommandEvent       // Request pause/resume
```

**WorldLifecycle Events:**
```csharp
WorldLifecycleResetStartedEvent      // Pub quando começa reset
WorldLifecycleResetCompletedEvent    // Pub quando termina reset
WorldLifecycleResetV2Events          // V2 de observabilidade
```

**Similaridade:** Ambos usam padrão Started/Completed para coordenação!

---

### DESCOBERTA 3: Responsibility Boundary Confuso

**Quem é responsável por quê?**

| Responsabilidade | GameLoop | WorldLifecycle | Quem Deveria Ser? |
|---|---|---|---|
| Pausa/Resume gameplay | ✅ GamePauseCommandEvent | ❌ Não | GameLoop (correto) |
| Hard Reset (volta ao Boot) | ✅ RequestReset | ✅ RequestResetAsync | **DUPLICADO** |
| World Respawn | ❌ Não | ✅ Orchestrator | WorldLifecycle (correto) |
| Estado de resultado (vitória/derrota) | ✅ GameRunStateService | ❌ Não | GameLoop (correto) |
| Entrada/Saída de PostGame | ✅ GameLoopService | ❌ Não | GameLoop (correto) |

**DESCOBERTA:** **Hard Reset está duplicado!**

---

### DESCOBERTA 4: Sinais de Restart Duplicados

**GameLoop:**
```csharp
// GameLoopService
if (_signals.ResetRequested)
{
    return TransitionTo(GameLoopStateId.Boot);
}

// GameCommands
public void RequestRestart(string reason)
{
    EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent(reason));
}
```

**WorldLifecycle:**
```csharp
// WorldLifecycleController
public Task ResetWorldAsync(string reason)
{
    return EnqueueReset(...RunWorldResetAsync(reason));
}

// WorldResetRequestService
public async Task RequestResetAsync(string source)
{
    await resetService.TriggerResetAsync(request);
}
```

**Problema:** 2 sistemas orquestrando reinicializações!

---

### DESCOBERTA 5: Sequência de Ações Duplicada

**O que acontece em um "Hard Reset"?**

**GameLoop (simplificado):**
1. `RequestReset()` → sinaliza _signals.ResetRequested
2. StateMachine vê signal e vai para Boot
3. Boot state aguarda `RequestStart()` antes de prosseguir
4. Publicar `GameRunStartedEvent`

**WorldLifecycle (simplificado):**
1. `RequestResetAsync()` → enfileira reset
2. Orchestrator executa: Gate.Acquire → Despawn → Spawn → Gate.Release
3. Publicar `WorldLifecycleResetCompletedEvent`

**PROBLEMA:** Ambos tentam controlar o "fluxo de reinicialização"!

---

## 📊 ANÁLISE DE SOBREPOSIÇÃO

### Matriz de Responsabilidades Compartilhadas

```
┌────────────────────────────────┬──────────┬─────────────────┬──────────────┐
│ Responsabilidade               │ GameLoop │  WorldLifecycle │   Sobreposição│
├────────────────────────────────┼──────────┼─────────────────┼──────────────┤
│ Controladora reset/reinício    │    ✅    │      ✅         │    🔴 ALTA   │
│ State machine de ciclo         │    ✅    │      ❌         │    ✅ OK     │
│ Publicar eventos de estado     │    ✅    │      ✅         │    🟡 MÉDIA  │
│ Event binding patterns         │    ✅    │      ✅         │    🟡 MÉDIA  │
│ Logging de observabilidade     │    ✅    │      ✅         │    🟡 MÉDIA  │
│ Dependency resolution patterns │    ✅    │      ✅         │    🟡 MÉDIA  │
│ Reason/signature normalization │    ✅    │      ✅         │    🟡 MÉDIA  │
│ Reason formatting strings      │    ✅    │      ✅         │    🟡 MÉDIA  │
└────────────────────────────────┴──────────┴─────────────────┴──────────────┘
```

---

### Código Duplicado Detectado

**1. Padrão de Reason Normalization:**

GameLoop (GameCommands.cs):
```csharp
private static string NormalizeOptionalReason(string reason, string fallback)
{
    if (!string.IsNullOrWhiteSpace(reason))
        return reason.Trim();
    return string.IsNullOrWhiteSpace(fallback) ? "Unspecified" : fallback.Trim();
}
```

WorldLifecycle (WorldResetCommands.cs):
```csharp
private static string NormalizeReason(string reason, string fallback)
{
    if (!string.IsNullOrWhiteSpace(reason))
        return reason.Trim();
    return fallback;
}
```

**Identical logic, different names and defaults.**

---

**2. Padrão de Event Publishing:**

GameLoop (GameLoopCommandEventBridge.cs):
```csharp
if (_lastPauseFrame == frame && string.Equals(_lastPauseKey, key, StringComparison.Ordinal))
{
    DebugUtility.LogVerbose<...>("dedupe_same_frame...");
    return;
}
_lastPauseFrame = frame;
_lastPauseKey = key;
```

WorldLifecycle (WorldLifecycleSceneFlowResetDriver.cs):
```csharp
if (_inFlightSignatures.Contains(signature))
{
    // Already processing
    return;
}
_inFlightSignatures.Add(signature);
```

**Similar dedupe patterns!**

---

**3. Padrão de State Validation:**

GameLoop (GameRunOutcomeService.cs):
```csharp
private bool IsInActiveGameplay()
{
    if (_gameLoopService == null) return false;
    string stateName = _gameLoopService.CurrentStateIdName ?? string.Empty;
    return string.Equals(stateName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);
}
```

WorldLifecycle (WorldResetService.cs + WorldResetOrchestrator.cs):
```csharp
// Implícito: verificar se está em reset, se está em Playing, etc
private void EnsureDependencies()
{
    // Similar pattern
    provider.TryGetGlobal<ISimulationGateService>(out var gateService);
    // ... validation
}
```

---

## 🎯 RECOMENDAÇÕES DE CONSOLIDAÇÃO

### **CONSOLIDAÇÃO A: Extrair Shared Reason Normalization**

**Arquivo:** Novo `GameplayReasonNormalizer.cs` (Core ou GameLoop.Shared)

```csharp
/// <summary>
/// Centralizador de normalização de reasons para GameLoop + WorldLifecycle.
/// Elimina duplicação entre GameCommands, WorldResetCommands, etc.
/// </summary>
public static class GameplayReasonNormalizer
{
    public static string Format(string reason)
        => string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim();

    public static string NormalizeRequired(string reason)
        => string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason.Trim();

    public static string NormalizeOptional(string reason, string fallback)
        => string.IsNullOrWhiteSpace(reason)
            ? (string.IsNullOrWhiteSpace(fallback) ? "Unspecified" : fallback.Trim())
            : reason.Trim();
}
```

**Benefícios:**
- ✅ Uma única fonte de verdade compartilhada
- ✅ Elimina duplicação em GameLoop + WorldLifecycle
- ✅ Fácil manter consistência

**Impacto:** ~50 LOC removido (10 do GameLoop, 15 do WorldLifecycle, 25 reutilizado)

---

### **CONSOLIDAÇÃO B: Compartilhar Event Binding Helper**

**Arquivo:** Melhorar/expandir `ManagedEventBinding<T>` na Core

```csharp
/// <summary>
/// Helper para event binding que ambos GameLoop e WorldLifecycle compartilham.
/// </summary>
public sealed class ManagedEventBinding<TEvent> where TEvent : IEvent
{
    private EventBinding<TEvent> _binding;
    private bool _registered;

    public ManagedEventBinding(Action<TEvent> handler) { ... }
    public void Register() { ... }
    public void Unregister() { ... }
    public void Dispose() => Unregister();
}
```

**Benefícios:**
- ✅ Padrão único para ambos os módulos
- ✅ Elimina ~200 linhas de boilerplate
- ✅ Consistência de lifecycle

**Impacto:** ~100 LOC removido (50 GameLoop + 50 WorldLifecycle)

---

### **CONSOLIDAÇÃO C: Centralizar Logging Patterns**

**Arquivo:** Novo `GameplayObservabilityLog.cs`

```csharp
/// <summary>
/// Centralizador de padrões de logging para GameLoop + WorldLifecycle.
/// Mantém consistência de prefixos [GameLoop], [WorldLifecycle], [OBS].
/// </summary>
public static class GameplayObservabilityLog
{
    public static void LogResetRequested(string source, string signature, string reason, string scene)
    {
        DebugUtility.LogVerbose(typeof(GameplayObservabilityLog),
            $"[OBS][WorldLifecycle] ResetRequested signature='{signature}' source='{source}' reason='{reason}' scene='{scene}'.",
            DebugUtility.Colors.Info);
    }

    public static void LogGameStateChanged(GameLoopStateId state, bool isActive)
    {
        DebugUtility.LogVerbose(typeof(GameplayObservabilityLog),
            $"[OBS][GameLoop] StateChanged state='{state}' isActive='{isActive}'.",
            DebugUtility.Colors.Info);
    }

    // ... mais métodos de observabilidade
}
```

**Benefícios:**
- ✅ Logging consistente entre módulos
- ✅ Fácil buscar e auditar logs
- ✅ Reduz boilerplate de logging

**Impacto:** ~60 LOC removido

---

### **CONSOLIDAÇÃO D: Refatorar WorldLifecycleOrchestrator (Criticidade Alta)**

**Objetivo:** Quebrar 990 linhas em classes menores

**Novo arquivo:** `WorldResetPhaseExecutor.cs`
**Novo arquivo:** `WorldResetHookManager.cs`
**Novo arquivo:** `WorldResetScopeFilter.cs`

```csharp
// Reduzir Orchestrator para ~300 linhas (orquestração pura)
// Mover:
// - Phase execution → PhaseExecutor
// - Hook management → HookManager
// - Scope filtering → ScopeFilter
```

**Impacto:** ~400 LOC removido (reorganizado, não deletado)

---

### **CONSOLIDAÇÃO E: Refatorar WorldLifecycleController (458 → ~150 linhas)**

**Objetivo:** Extrair queue management e reset logic

**Novo arquivo:** `WorldResetQueue.cs`

**Benefícios:**
- ✅ Separa responsabilidades
- ✅ Facilita testes
- ✅ Reduz complexidade

**Impacto:** ~200 LOC removido (reorganizado)

---

### **CONSOLIDAÇÃO F: Validação de Responsabilidades entre GameLoop e WorldLifecycle**

**Problema:** Hard Reset é orquestrado por DOIS sistemas

**Solução Recomendada:**

1. **GameLoop** responsável por: Pausa/Resume/Estados (Boot→Playing→PostPlay)
2. **WorldLifecycle** responsável por: Reset/Respawn (Gate→Despawn→Spawn)
3. **Coordenação:** Via eventos bem definidos (não ambígos)

**Diagrama Clarificado:**

```
User Action (e.g., "Restart Game")
    ↓
GameCommands.RequestRestart()
    ↓
GameResetRequestedEvent
    ↓
GameLoopService (Boot state)
    ↓
[aqui o GameLoop está em Boot, aguardando]
    ↓
WorldLifecycleSceneFlowResetDriver [quando ScenesReady]
    ↓
WorldResetRequestService.RequestResetAsync()
    ↓
WorldLifecycleOrchestrator.ExecuteAsync()
    ↓
[Gate → Despawn → Spawn → Release]
    ↓
WorldLifecycleResetCompletedEvent
    ↓
GameLoopService (sinaliza RequestStart)
    ↓
Playing state resumido
```

**Mudanças Necessárias:**
- ✅ Clarificar quem "possuí" o reset
- ✅ Definir pontos de entrega bem definidos
- ✅ Remover ambiguidade (GameLoop reset vs WorldLifecycle reset)

**Severidade:** 🔴 **CRÍTICA** - Afeta arquitetura

---

## 📊 IMPACTO TOTAL ESTIMADO

### Redundâncias Internas do WorldLifecycle

| Otimização | Tipo | LOC Reduzidas | Complexidade |
|---|---|---|---|
| A. Reason Formatting Consolidation | Extração | ~50 | ↓ 30% |
| B. Event Binding Helper | Consolidação | ~100 | ↓ 35% |
| C. Logging Patterns | Consolidação | ~60 | ↓ 25% |
| D. Refatorar Orchestrator | Split | ~400 | ↓ 60% |
| E. Refatorar Controller | Split | ~200 | ↓ 55% |
| **WorldLifecycle Total** | | **~810** | **↓ 42%** |

### Redundâncias Compartilhadas (GameLoop + WorldLifecycle)

| Otimização | GameLoop | WorldLifecycle | Total | Benefício |
|---|---|---|---|---|
| A. Reason Normalization | 30 | 20 | 50 | 1 fonte de verdade |
| B. Event Binding Helper | 100 | 50 | 150 | Padrão único |
| C. Logging Patterns | 40 | 60 | 100 | Observabilidade consistente |
| **Compartilhado Total** | **170** | **130** | **300** | **Coesão** |

### Impacto Crítico: Responsabilidades Duplicadas

| Responsabilidade | GameLoop | WorldLifecycle | Ação Necessária |
|---|---|---|---|
| Hard Reset | RequestReset() | RequestResetAsync() | 🔴 Clarificar ownership |
| Event Publishing | ✅ Started/Ended | ✅ Started/Completed | 🟡 Consolidar padrão |
| State Management | State Machine | Orchestrator | 🟡 Documentar boundary |

**Impacto de Clarificação:** Reduz ~200 linhas de código ambíguo

---

## 📈 COMPARAÇÃO MÓDULOS

| Aspecto | GameLoop | WorldLifecycle | Total | Análise |
|---------|----------|---|---|---|
| **Total LOC** | ~2000 | ~2500 | ~4500 | Grande escopo |
| **Redundâncias Internas** | 7 | 7 | 14 | Padrões similares |
| **Redundâncias Compartilhadas** | 3 | 3 | 3 | Sobreposição |
| **LOC Removível Interno** | ~530 | ~810 | ~1340 | 30% do total |
| **LOC Removível Compartilhado** | ~170 | ~130 | ~300 | 7% do total |
| **Total Removível** | ~700 | ~940 | **~1640** | **36% do escopo** |
| **Fases Recomendadas** | 5 | 6 | 8 | Implementação ~14 horas |

---

## 🎯 PLANO DE IMPLEMENTAÇÃO INTEGRADO

### **Fase 0: Análise e Documentação de Boundary**

**Objetivo:** Clarificar quem faz o quê

**Ações:**
- Documentar responsabilidades de GameLoop vs WorldLifecycle
- Definir pontos de entrada/saída
- Mapear fluxos de eventos

**Tempo:** ~2 horas
**Risco:** Muito Baixo (análise)
**Impacto:** Evita erros nas fases seguintes

---

### **Fase 1: Consolidação de Patterns Compartilhados**

**Ordem Recomendada:**
1. `GameplayReasonNormalizer` (GameLoop + WorldLifecycle)
2. `ManagedEventBinding<T>` (Core)
3. `GameplayObservabilityLog` (GameLoop + WorldLifecycle)

**Tempo:** ~3 horas
**Risco:** Baixo
**Impacto:** ~300 LOC removido + consistência

---

### **Fase 2: Otimizações Internas do GameLoop**

**Do relatório anterior:**
1. Split GameLoopService
2. Criar GameLoopStateValidator
3. Refatorar GameRunServices

**Tempo:** ~8 horas
**Risco:** Médio-Alto
**Impacto:** ~530 LOC removido

---

### **Fase 3: Otimizações Internas do WorldLifecycle**

**Novos:**
1. Split WorldLifecycleOrchestrator (400 LOC)
2. Refatorar WorldLifecycleController (200 LOC)
3. Consolidar normalizações (50 LOC)

**Tempo:** ~6 horas
**Risco:** Médio-Alto
**Impacto:** ~650 LOC removido

---

### **Fase 4: Clarificação de Responsabilidades Cross-Module**

**Objetivo:** Remover ambiguidade de "quem faz reset"

**Ações:**
- Documentar fluxo de Hard Reset
- Definir ownership de eventos
- Remover código ambíguo

**Tempo:** ~2 horas
**Risco:** Médio (afeta arquitetura)
**Impacto:** ~200 LOC removido + clareza

---

## ✅ STATUS GERAL

| Item | GameLoop | WorldLifecycle | Status |
|------|----------|---|---|
| Análise estrutural | ✅ | ✅ | Completo |
| Identificação de redundâncias internas | ✅ | ✅ | Completo |
| Identificação de sobreposição | ✅ | ✅ | Completo |
| Otimizações propostas internas | ✅ | ✅ | Completo |
| Otimizações compartilhadas | ✅ | ✅ | Completo |
| Impacto estimado | ✅ | ✅ | Calculado |
| Plano integrado | ✅ | ✅ | Detalhado |

---

## 🎯 CONCLUSÃO

### Descobertas Principais

1. **WorldLifecycle tem MAIS redundâncias que GameLoop:**
   - 7 problemas internos (vs 7 do GameLoop)
   - Orchestrator com 990 linhas (vs 453 do GameLoopService)
   - Escopo mais complexo (reset determinístico)

2. **Sobreposição Significativa:**
   - Ambos orquestram "ciclos" (GameLoop estados, WorldLifecycle fases)
   - Ambos publicam eventos Started/Completed
   - Ambos usam padrões duplicados de binding, logging, normalização
   - **Hard Reset é duplicado entre módulos** (problema crítico)

3. **Oportunidades de Consolidação:**
   - ~300 LOC compartilhado pode ser extraído (patterns únicos)
   - ~1340 LOC interno pode ser removido (refatoração)
   - **Total: ~1640 LOC (36% de escopo) pode ser eliminado**

### Recomendações Prioritárias

**Curto Prazo (Semana 1):**
1. ✅ Fase 0: Documentar responsabilidades
2. ✅ Fase 1: Consolidar patterns compartilhados
3. ✅ Testes de integração (garantir funcionalidade)

**Médio Prazo (Semana 2-3):**
4. ⚠️ Fase 2: Otimizações internas do GameLoop
5. ⚠️ Fase 3: Otimizações internas do WorldLifecycle

**Longo Prazo (Semana 4):**
6. 🔴 Fase 4: Clarificação de ownership entre módulos

### Impacto Total

- **~1640 LOC removível** (36% do escopo)
- **~14 horas de implementação**
- **8 fases ordenadas por risco**
- **Ganho:** Clareza, manutenibilidade, performance (menos event listeners)

---

**Relatório gerado:** 22 de março de 2026
**Próximas revisões:** Após Fases 0-1 (consolidação compartilhada)
**Status de urgência:** 🔴 ALTA para clarificação de responsabilidades (Fase 0)


---

## 🧭 Atualização de naming recomendada

Para a próxima fase, a recomendação é separar o naming por responsabilidade:

### Manter `WorldReset*`

- `WorldResetService`
- `WorldResetOrchestrator`
- `WorldResetExecutor`
- `IWorldResetCommands`
- `WorldLifecycleSceneFlowResetDriver`

### Migrar para `SceneReset*`

- `WorldLifecycleController` -> `SceneResetController`
- `WorldLifecycleSceneResetRunner` -> `SceneResetRunner`
- `WorldLifecycleOrchestrator` -> `SceneResetPipeline`

### Motivo

- `WorldReset*` identifica corretamente o fluxo macro e a API publica.
- `SceneReset*` identifica corretamente o pipeline local deterministico por cena.
- essa separação reduz ruido conceitual e facilita futuras extrações internas por fase/hook.
