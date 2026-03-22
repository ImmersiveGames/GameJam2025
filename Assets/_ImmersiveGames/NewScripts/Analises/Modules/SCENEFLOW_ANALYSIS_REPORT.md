> [!WARNING]
> **Status de validação:** conteúdo importado de análise externa e **ainda não validado** contra o código atual.
>
> **Uso correto:** tratar este documento como **hipótese de auditoria / backlog de verificação**.
>
> **Fonte de verdade:** código atual, ADRs vigentes e documentação canônica do projeto.

> [!NOTE]
> **Origem anterior:** `Docs/Modules/SCENEFLOW_ANALYSIS_REPORT.md`
>
> Este arquivo foi movido para cá como localização canônica dos relatórios importados por módulo.

---

# 📊 ANÁLISE DO MÓDULO SCENEFLOW - REDUNDÂNCIAS E OTIMIZAÇÕES

**Data:** 22 de março de 2026
**Projeto:** GameJam2025
**Módulo:** SceneFlow (`Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow`)
**Status:** ⚠️ Análise Parcial (Módulo muito grande)

---

## 📋 RESUMO EXECUTIVO

**Tamanho:** ~3000+ LOC (MUITO GRANDE)
**Status:** ⚠️ Complexo - Múltiplas responsabilidades
**Problemas:** 5+ identificados
**Redundância:** ~200 LOC (7% de escopo)
**Recomendação:** ⚠️ Refatoração futura (out of scope para agora)

---

## 🏗️ ESTRUTURA DO MÓDULO

```
SceneFlow/ (GIGANTE!)
├─ Runtime/ (10 arquivos)
│  ├─ SceneTransitionService.cs (726 linhas!) ← CRÍTICO
│  ├─ SceneFlowSameFrameDedupe.cs (23 linhas) ← Helper
│  ├─ SceneFlowSignatureCache.cs (61 linhas)
│  ├─ SceneFlowAdapterFactory.cs
│  └─ outros...
│
├─ Navigation/ (Coordinators, Adapters)
│  ├─ Runtime/
│  ├─ Bindings/
│  └─ Adapters/
│
├─ Transition/ (726 linhas em TransitionService!)
│  ├─ Runtime/
│  ├─ Contracts/
│  └─ Events/
│
├─ Readiness/ (Gates de transição)
│  └─ Runtime/
│     └─ GameReadinessService.cs
│
├─ Fade/ (Animações de fade)
├─ Loading/ (UI de loading)
│  └─ Runtime/
│     └─ LoadingHudService.cs
│
├─ Editor/
└─ Vários contracts e eventos

TOTAL: ~3000+ linhas
```

---

## ✅ ANÁLISE

### O que o módulo faz?

SceneFlow é o **coordinador central de transições de cena**:
- Carrega/descarrega cenas
- Aplica fade in/out
- Gerencia gates de readiness
- Coordena com WorldLifecycle, GameLoop, InputModes
- Publica eventos (Started, ScenesReady, Completed)

### Problema Principal

🔴 **SceneFlow é um HUB gigante** com muitas responsabilidades misturadas

---

## 🔴 PROBLEMAS IDENTIFICADOS

### 1️⃣ SceneTransitionService GIGANTE (726 linhas!) (🔴 CRÍTICA)

**Problema:** Uma única classe com muitas responsabilidades

```
Responsabilidades misturadas:
├─ Orquestração de transição
├─ Loading de cenas
├─ Fade in/out
├─ Readiness gates
├─ Signature caching
├─ Dedupe logic
├─ Event publishing
└─ Logging extensivo
```

**Impacto:** Difícil de testar, difícil de manter, usado por múltiplos módulos

**Solução:** Refatoração futura (quebrar em classes menores)

---

### 2️⃣ Event Binding Duplicado (🟡 ALTA)

**Localização:** `SceneFlowSignatureCache.cs` (linhas ~13-28)

```csharp
public SceneFlowSignatureCache()
{
    _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
    _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);

    EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
    EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);
}

public void Dispose()
{
    try { EventBus<SceneTransitionStartedEvent>.Unregister(_startedBinding); } catch { }
    try { EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding); } catch { }
}
```

**Problema:** Boilerplate de event binding em 8+ módulos

**Impacto:** ~30 LOC duplicado em SceneFlow

**Solução:** Usar `ManagedEventBinding<T>` (Fase 1)

---

### 3️⃣ Normalização e Sanitização (🟡 MÉDIA)

**Localização:** Múltiplos arquivos (SceneTransitionService, outros)

```csharp
// Método privado em SceneTransitionService:
private static string Sanitize(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return "<empty>";
    return value.Trim();
}
```

**Problema:** Normalização sem padrão centralizado

**Impacto:** ~20 LOC de normalização

**Solução:** Usar `GameplayReasonNormalizer` (Fase 1)

---

### 4️⃣ SceneFlowSameFrameDedupe não usado em SceneTransitionService (🟡 MÉDIA)

**Problema:**

```csharp
// SceneFlowSameFrameDedupe.cs - Helper criado
public static bool ShouldDedupe(ref int lastFrame, ref string lastKey, ...) { ... }

// Mas SceneTransitionService não usa o helper!
private bool ShouldDedupeSameFrame(string signature)
{
    // Implementação duplicada
}
```

**Problema:** Helper existe mas não é usado

**Impacto:** Inconsistência, 15 LOC duplicado

**Solução:** Refatorar SceneTransitionService para usar `DeduplicationHelper` (Fase 1.5)

---

### 5️⃣ LoadingHudService - Event Binding + Dedupe (🟡 MÉDIA)

**Problema:** Mesmo padrão de event binding + dedupe logic

```csharp
// Event binding duplicado
private readonly EventBinding<...> _binding;
EventBus<...>.Register(_binding);

// Dedupe similar
if (SceneFlowSameFrameDedupe.ShouldDedupe(...)) return;
```

**Impacto:** ~25 LOC de padrões repetidos

**Solução:** Usar `ManagedEventBinding<T>` + consolidado `DeduplicationHelper` (Fases 1 e 1.5)

---

## 💡 RECOMENDAÇÕES

### Recomendação 1: Usar ManagedEventBinding<T> (Fase 1)

**Arquivos afetados:**
- SceneFlowSignatureCache.cs: -20 LOC
- LoadingHudService.cs: -20 LOC

**Impacto:** -40 LOC

---

### Recomendação 2: Usar GameplayReasonNormalizer (Fase 1)

**Impacto:** -20 LOC

---

### Recomendação 3: Refatorar SceneTransitionService (FUTURO)

**Quando:** Refatoração maior, não em escopo atual

**Sugestões:**
1. Quebrar em SceneTransitionCoordinator (orquestração)
2. SceneTransitionPhaseExecutor (carregar/descarregar)
3. SceneTransitionGateManager (gates)
4. SceneTransitionEventPublisher (eventos)

**Impacto:** -200+ LOC reorganizados

---

## 📊 IMPACTO TOTAL (para agora)

| Item | LOC | Impacto |
|------|-----|---------|
| **Before** | ~3000 | 5 problemas |
| **After (Fases 1-1.5)** | ~2940 | -60 LOC (-2%) |
| **Impacto Futuro** | ~2700 | -200+ LOC (-7%) |

---

## ✅ CONCLUSÃO

### Status Overall

**SceneFlow é complexo e necessita refatoração**, mas:
- ✅ Funciona bem como está
- ✅ Padrões podem ser consolidados agora (Fases 1-1.5)
- ⚠️ Refatoração maior é out of scope para agora

### Ação Imediata

**Incluir em Fases 1-1.5 (consolidação de patterns):**
1. Usar `ManagedEventBinding<T>` em SceneFlowSignatureCache e LoadingHudService
2. Usar `GameplayReasonNormalizer`
3. Refatorar SceneTransitionService para usar `DeduplicationHelper`

### Ação Futura

**Considerar refatoração maior (semanas 6+):**
- Split SceneTransitionService em múltiplas responsabilidades
- Melhorar testabilidade
- Reduzir acoplamento com múltiplos módulos

---

**Relatório gerado:** 22 de março de 2026
**Próxima ação:** Incluir em Fases 1-1.5 (consolidação patterns)
**Prioridade:** Média-Alta (múltiplos padrões a consolidar)
**Refatoração Futura:** RECOMENDADA (módulo muito grande)
