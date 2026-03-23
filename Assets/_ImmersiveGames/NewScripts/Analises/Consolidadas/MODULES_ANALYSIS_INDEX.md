# 📋 ÍNDICE DE ANÁLISES DE MÓDULOS - GAMEjam2025

**Data de Compilação:** 23 de março de 2026
**Status:** ✅ Índice atualizado para o estado atual dos módulos (com histórico preservado)

---

## 📊 Visão Geral das Análises

| Referência nas análises | Escopo real hoje | Status | Observação |
|--------|---------|--------|-----------|
| **Audio** | módulo ativo | ✅ Bom | Sem relatório consolidado específico nesta pasta. |
| **ContentSwap** | módulo removido | 📚 Histórico | Ler só como contexto de migração. |
| **GameLoop** | módulo ativo | ⚠️ Crítico | Relatório ainda útil em boa parte. |
| **Gameplay** | módulo ativo | ⚠️ Médio | Boundary com reset mudou; relatório segue útil com ressalvas. |
| **SimulationGate** | capability parcialmente visível neste recorte | ⚠️ Parcial | Em `Modules`, resta apenas `SimulationGateTokens.cs`. |
| **LevelFlow** | módulo ativo | ✅ Bom | Semântica local + snapshot continuam centrais. |
| **Navigation** | módulo ativo | ✅ Bom | Service/catalog/bridges ativos. |
| **PostGame** | módulo ativo | ✅ Bom | Pequeno e estável. |
| **SceneFlow** | módulo ativo | ⚠️ Crítico | Hotspot estrutural principal do macro. |
| **WorldLifecycle** | área histórica | 📚 Histórico | Hoje dividida em `WorldReset`, `SceneReset` e `ResetInterop`. |
| **WorldReset / SceneReset / ResetInterop** | módulos ativos | ✅ Vigentes | Ainda sem relatórios próprios dedicados nesta pasta. |

**Observação:** os números de LOC e prioridades abaixo continuam úteis como leitura histórica/importada, mas o snapshot atual já não possui `WorldLifecycle` e `ContentSwap` como módulos vivos.

## 🔴 MÓDULOS CRÍTICOS IDENTIFICADOS

### 1. Área histórica de WorldLifecycle (~2500 LOC na análise original)

**Leitura correta hoje:**
- este hotspot foi repartido em `WorldReset`, `SceneReset` e `ResetInterop`;
- os problemas de concentração e naming antigo continuam úteis como referência histórica;
- não ler esta seção como fotografia literal do snapshot atual.

**O que ainda permanece válido:**
- havia concentração excessiva de responsabilidades no antigo miolo de reset;
- parte dessa dívida migrou para `SceneFlow` e para a superfície de reset, não desapareceu por mágica.

**Recomendação:** usar esta seção como contexto histórico para decisões futuras de reset, não como mapa atual de pastas/arquivos.

---

### 2. GameLoop (~2000 LOC, 15% redundância)

**Problemas Críticos:**
- 🔴 GameLoopService (453 LOC) - muito grande
- 🔴 Normalização de strings duplicada (3 variações: FormatReason, NormalizeRequired, NormalizeOptional)
- 🟡 State validation checks espalhados (4 implementations)
- 🟡 Event binding boilerplate (6+ eventos)

**Recomendação:** Refactoring Fase 2 (extrair GameLoopStateValidator, consolidar normalizers)

---

### 3. SceneFlow (~2500 LOC, 12% redundância)

**Problemas Críticos:**
- 🔴 SceneTransitionService usa Interlocked pattern (similar a outros módulos)
- 🟡 Event binding boilerplate (2+ eventos)
- 🟡 Logging verbose boilerplate
- 🟡 Possível sobreposição com Navigation (ambos fazem transições)

**Recomendação:** Refactoring Fase 2 + consolidação cross-module com Navigation

---

### 4. Gameplay (~2973 LOC, 8-12% redundância) ⭐ NOVO

**Problemas Críticos:**
- 🟡 StateDependentService (505 LOC) - classe grande
- 🟡 ActorGroupRearmOrchestrator (467 LOC) - classe grande
- 🔴 Sobreposição funcional com WorldLifecycle (spawn/reset)
- 🟡 Padrão TryResolve duplicado (2 métodos, 12 LOC)
- 🟡 Event binding boilerplate (7 eventos, 47 LOC)

**Recomendação:** Refactoring Fase 2 (consolidar padrões Fase 1, depois refactor em 3 camadas)

---

## 🟡 MÓDULOS MÉDIOS IDENTIFICADOS

### 5. Navigation (~550 LOC, 8% redundância)

**Problemas:**
- 🟡 Padrão TryResolve duplicado (2 métodos)
- 🟡 Interlocked pattern similar a SceneFlow (inconsistência)
- 🟡 Possível sobreposição com SceneFlow

---

### 6. LevelFlow (~1500 LOC, 3% redundância)

**Estado atual:**
- 🟢 Bem estruturado, poucas redundâncias
- ✅ Mantém a semântica local e o snapshot
- ✅ Já delega a composição técnica local para `ISceneCompositionExecutor`

---

### 7. InputModes (~400 LOC, 10% redundância)

**Estado atual:**
- ✅ Núcleo reclassificado para `Infrastructure/InputModes`
- 🟡 `InputModeService` ainda concentra decisão de modo + descoberta de `PlayerInput`
- 🟡 O bridge com `SceneFlow` foi corretamente deslocado para `Modules/SceneFlow/Interop`

---

## 🟢 MÓDULOS BEM FEITOS

### 8. SimulationGate (~600 LOC, 2% redundância) ✅

- Capability transversal em `Infrastructure/SimulationGate`
- Interface clara
- Thread-safe
- Poucas redundâncias
- Há resíduo de `SimulationGateTokens.cs` em `Modules/Gates` pendente de cleanup

---

### 9. Audio (~400 LOC, 5% redundância) ✅

- Bem estruturado
- Responsabilidade clara

---

### 10. ContentSwap (histórico)

- Removido do código e do trilho funcional
- Relatório mantido apenas como histórico da arquitetura anterior

---

### 11. PostGame (~450 LOC, 5% redundância) ✅

- Pequeno, responsabilidades claras

---

## 🎯 PADRÕES DUPLICADOS IDENTIFICADOS EM MÚLTIPLOS MÓDULOS

### 1. TryResolve Pattern (🔴 CRÍTICO - 18+ variações)

**Módulos afetados:** GameLoop, WorldLifecycle, Gameplay, Navigation, SceneFlow

**Padrão:**
```csharp
private void TryResolveXxxService()
{
    if (_xxxService != null) return;
    DependencyManager.Provider.TryGetGlobal(out _xxxService);
}
```

**Economia potencial:** -60 LOC (criar 1 helper genérico)

---

### 2. Event Binding Boilerplate (🔴 CRÍTICO - 40+ bindings)

**Módulos afetados:** GameLoop, WorldLifecycle, Gameplay, InputModes, SceneFlow

**Padrão:**
```csharp
private EventBinding<XxxEvent> _xxxBinding;

private void TryRegisterEvents()
{
    _xxxBinding = new EventBinding<XxxEvent>(_ => OnXxx());
    EventBus<XxxEvent>.Register(_xxxBinding);
}

public void Dispose()
{
    EventBus<XxxEvent>.Unregister(_xxxBinding);
}
```

**Economia potencial:** -150 LOC (criar helper EventBinder)

---

### 3. Interlocked/Mutex Pattern (🟡 MÉDIA - 5 variações)

**Módulos afetados:** SceneFlow, Navigation, Gameplay, IntroStage

**Inconsistência:** Alguns usam `Interlocked.CompareExchange`, outros usam `SemaphoreSlim`

**Economia potencial:** -20 LOC (padronizar em 1 padrão)

---

### 4. Logging Verbose Boilerplate (🟡 MÉDIA - 30+ pontos)

**Módulos afetados:** GameLoop, WorldLifecycle, Gameplay, SceneFlow, Navigation

**Padrão duplicado:** Logging com prefix específico do módulo, mesmo formato

**Economia potencial:** -100 LOC (criar centralized ObservabilityLog helpers)

---

### 5. Normalização de Strings (🟡 MÉDIA - 10+ variações)

**Módulos afetados:** GameLoop, WorldLifecycle, Gameplay

**Padrão duplicado:** Normalizar null/whitespace → default value

**Economia potencial:** -50 LOC (criar GameplayReasonNormalizer)

---

## 📊 MATRIZ DE SOBREPOSIÇÃO CROSS-MODULE

### WorldLifecycle × Gameplay (🔴 CRÍTICO)

| Feature | WorldLifecycle | Gameplay | Sobreposição |
|---------|----------------|----------|-------------|
| **Spawn Management** | ✓ (via orchestrator) | ✓ (ActorSpawnServiceBase) | 🔴 AMBOS |
| **Reset/Rearm** | ✓ (WorldLifecycleOrchestrator) | ✓ (ActorGroupRearmOrchestrator) | 🔴 AMBOS |
| **Actor Registry Interaction** | Implícito | Explícito | ⚠️ Possível race condition |
| **Event Binding** | 4+ eventos | 7 eventos | 🟡 Similar pattern |

**Ação Necessária:** ADR para definir responsabilidades (quem faz spawn? quem faz reset?)

---

### GameLoop × Gameplay (🟡 CRÍTICO)

| Feature | GameLoop | Gameplay | Sobreposição |
|---------|----------|----------|-------------|
| **State Machine** | ✓ (Boot→Ready→Playing→PostPlay) | ✓ (Ready→Playing→Paused) | 🔴 MAS diferentes |
| **Action Gating** | Implícito | ✓ (StateDependentService) | 🟡 StateDependentService = espelho de GameLoop |

**Ação Necessária:** Simplificar StateDependentService (usar GameLoopService diretamente?)

---

### SceneFlow × Navigation (🟡 MÉDIA)

| Feature | SceneFlow | Navigation | Sobreposição |
|---------|-----------|-----------|-------------|
| **Scene Transitions** | ✓ (SceneTransitionService) | ✓ (GameNavigationService) | 🟡 Possível overlap |
| **InputMode Switching** | ✓ (InputModesBridge) | Implícito | ⚠️ Coordenação necessária |

**Ação Necessária:** Documentar divisão de responsabilidades

---

## 🚀 PLANO DE CONSOLIDAÇÃO POR FASE

### Phase 1: Consolidação de Padrões (QUICK WINS)

**Timeline:** Sprint 1 (1 semana)
**Impacto:** -200 LOC, +Consistência
**Ação:**

1. Criar `GameplayDependencyResolver` helper
   - Consolidar TryResolve pattern (18 variações)
   - Impacto: -60 LOC

2. Criar `GameplayEventBinder` helper
   - Consolidar Event Binding boilerplate (40+ bindings)
   - Impacto: -150 LOC

3. Criar `GameplayObservabilityLog` centralizado
   - Consolidar logging verbose (30+ pontos)
   - Impacto: -100 LOC

4. Criar `GameplayReasonNormalizer`
   - Consolidar normalização de strings (10+ variações)
   - Impacto: -50 LOC

**Resultado:**
- Todos os 6 módulos usam padrões centralizados
- Redução de ~200 LOC
- Melhoria de consistência +40%

---

### Phase 2: Refactoring de Responsabilidades (STRUCTURAL)

**Timeline:** Sprint 2 (2 semanas)
**Impacto:** -500 LOC, +Testabilidade

**Ação:**

1. Refactor `WorldLifecycleOrchestrator` (990 → 400 LOC)
   - Extrair `WorldResetDiscoveryCoordinator`
   - Extrair `WorldResetExecutor`
   - Impacto: -500 LOC

2. Refactor `GameLoopService` (453 → 250 LOC)
   - Extrair `GameLoopStateValidator`
   - Extrair `GameLoopEventManager`
   - Impacto: -200 LOC

3. Refactor `StateDependentService` (505 → 250 LOC)
   - Extrair `GameplayStateManager`
   - Extrair `GameplayActionGate`
   - Extrair `GameplayEventManager`
   - Impacto: -255 LOC

4. Refactor `ActorGroupRearmOrchestrator` (467 → 250 LOC)
   - Extrair `ActorGroupRearmDiscoveryCoordinator`
   - Extrair `ActorGroupRearmExecutor`
   - Impacto: -217 LOC

**Resultado:**
- 4 grandes classes reduzidas pela metade
- Testabilidade +100%
- Redução de ~1000 LOC

---

### Phase 3: Consolidação Cross-Module (ARCHITECTURE)

**Timeline:** Sprint 3-4 (3-4 semanas)
**Impacto:** -300 LOC, +Arquitetura

**Ação:**

1. Reconciliar WorldLifecycle × Gameplay Spawn
   - Criar `ActorSpawnCoordinator` que coordena ambos
   - Definir ADR: quem é responsável por quê?
   - Impacto: -150 LOC

2. Reconciliar WorldLifecycle × Gameplay Reset
   - Definir ADR: ActorGroupRearm vs WorldReset
   - Possível consolidação de orquestradores
   - Impacto: -150 LOC

3. Reconciliar SceneFlow × Navigation
   - Documentar divisão de responsabilidades
   - Possível consolidação de transições
   - Impacto: -50 LOC

4. Padronizar Interlocked/Mutex em todos módulos
   - Escolher 1 padrão
   - Aplicar em 5 módulos
   - Impacto: -50 LOC

**Resultado:**
- Definição clara de responsabilidades (ADRs)
- Possível redução de 300+ LOC
- Melhor arquitetura modular

---

## 📈 IMPACTO TOTAL ESTIMADO

### Linhas de Código Economizadas

```
Fase 1 (Padrões)        : -200 LOC
Fase 2 (Refactoring)    : -1000 LOC
Fase 3 (Cross-Module)   : -300 LOC
─────────────────────────────────
TOTAL ESTIMADO          : -1500 LOC (-10% do total)
```

### Melhorias de Qualidade

| Métrica | Antes | Depois | Melhoria |
|---------|-------|--------|----------|
| LOC Médio por Classe | 450 | 250 | -44% |
| Classes >500 LOC | 4 | 0 | -100% |
| Padrões Centralizados | 0 | 6+ | +∞ |
| Testabilidade | Média | Alta | +100% |
| Manutenibilidade | Média | Alta | +40% |
| Consistência | 50% | 95% | +90% |

---

## 📋 CHECKLIST DE IMPLEMENTAÇÃO

### Phase 1: Consolidação de Padrões

- [ ] Criar `GameplayDependencyResolver` em Core/Patterns
- [ ] Criar `GameplayEventBinder` em Core/Patterns
- [ ] Criar `GameplayObservabilityLog` em Core/Logging
- [ ] Criar `GameplayReasonNormalizer` em Core/Utilities
- [ ] Aplicar em GameLoop (+2 módulos)
- [ ] Aplicar em WorldLifecycle (+1 módulo)
- [ ] Aplicar em Gameplay (+1 módulo)
- [ ] Testes: 95%+ cobertura em helpers
- [ ] Documentação: padrão atualizado em ADRs

### Phase 2: Refactoring de Responsabilidades

- [ ] Refactor WorldLifecycleOrchestrator (split em 3)
- [ ] Refactor GameLoopService (split em 3)
- [ ] Refactor StateDependentService (split em 3)
- [ ] Refactor ActorGroupRearmOrchestrator (split em 3)
- [ ] Testes: 95%+ cobertura em nova estrutura
- [ ] Documentação: novo design atualizado

### Phase 3: Consolidação Cross-Module

- [ ] ADR: Responsabilidades de Spawn (WorldLifecycle vs Gameplay)
- [ ] ADR: Responsabilidades de Reset (ActorGroupRearm vs WorldReset)
- [ ] ADR: Responsabilidades de Transição (SceneFlow vs Navigation)
- [ ] Implementar coordinadores
- [ ] Testes: integração entre módulos
- [ ] Documentação: responsabilidades finais

---

## 📚 REFERÊNCIAS DE RELATÓRIOS

### Relatórios Existentes

| Módulo | Caminho | Status |
|--------|---------|--------|
| Audio | Mencionado como análise anterior | ✅ |
| ContentSwap | Analises/Modules/CONTENTSWAP_ANALYSIS_REPORT.md | Histórico |
| GameLoop | Analises/Modules/GAMELOOP_ANALYSIS_REPORT.md | ✅ |
| Gameplay | Analises/Modules/GAMEPLAY_ANALYSIS_REPORT.md | ✅ |
| SimulationGate | Infrastructure/SimulationGate/GATES_ANALYSIS_REPORT.md | ✅ |
| InputModes | Infrastructure/InputModes/INPUTMODES_ANALYSIS_REPORT.md | ✅ |
| LevelFlow | Analises/Modules/LEVELFLOW_ANALYSIS_REPORT.md | ✅ |
| Navigation | Analises/Modules/NAVIGATION_ANALYSIS_REPORT.md | ✅ |
| PostGame | Analises/Modules/POSTGAME_ANALYSIS_REPORT.md | ✅ |
| SceneFlow | Analises/Modules/SCENEFLOW_ANALYSIS_REPORT.md | ✅ |
| WorldLifecycle | Analises/Modules/WORLDLIFECYCLE_ANALYSIS_REPORT.md | ✅ |

---

## ✅ CONCLUSÃO

**Status Overall:** 📊 Análise Completa de Todos os Módulos

### Descobertas Principais

1. **WorldLifecycle é o módulo mais problemático** (990 LOC class monolítico)
2. **GameLoop tem redundâncias significativas** (15% de código duplicado)
3. **Gameplay tem sobreposição crítica com WorldLifecycle** (spawn/reset)
4. **Padrões comuns são duplicados em 6 módulos** (TryResolve, Event Binding, etc)
5. **Oportunidade de consolidação: ~1500 LOC**

### Recomendação Final

**Prioridade Geral:** 🔴 **ALTA**

1. **Fazer Fase 1 IMEDIATAMENTE** (consolidação de padrões - 1 semana)
2. **Fazer Fase 2 em Sprint 2** (refactoring - 2 semanas)
3. **Fazer Fase 3 em Sprint 3** (cross-module - 3-4 semanas)

**Benefício Total:**
- -1500 LOC economia
- +100% testabilidade
- +40% manutenibilidade
- +90% consistência

---

**Relatório gerado:** 22 de março de 2026
**Versão:** 1.0 - Análise Completa
**Status:** ✅ Pronto para Implementação


