# Base 1.1 - Rename Map: Quick Reference

**Data**: 2026-05-12
**Status**: 📋 Auditoria Concluída

---

## TL;DR - Decisões Rápidas

### Não há GameLoop, IntroStage, PostRun, RunResult, RunDecision, GameplayState, ResetFlow encontrados

❌ **Boas Notícias**: Nenhum dos nomes estritamente encontrados listados no escopo foi localizado (ex: não há classe chamada "GameLoop" ou "IntroStage" em `Assets/_ImmersiveGames/NewScripts/**/*.cs`).

✅ **O que foi encontrado**: 47 nomes relacionados **implicitamente** ao vocabulário legado (Game*, Simulation, Phase, etc.), todos mapeados em tabelas detalhadas.

---

## 📊 Sumário de Achados

### Por Categoria

| Categoria | Count | Exemplos | Status |
|-----------|-------|----------|--------|
| **Run/Deactivation Events** | 6 | GameRunEndRequestedEvent, GamePlayRequestedEvent, GameRunOutcome | 🔄 Requer rename |
| **Simulation/Gate Contracts** | 19 | SimulationGateCommand, SimulationGateState, ActivitySimulationBlocked | 🔄 Requer rename |
| **ActorsSystem Phase Refs** | 9 | PhaseSignature, PhaseEntry, PhaseExclusive | ⚠️ Requer análise antes |
| **RunLifecycle (OK)** | 7 | RunLifecycleSignalIdentity, RunLifecycleContracts | ✅ Manter |

---

## 🎯 Tabela Resumida de Renames

### Prioritário - Fase 1 (Baixo Risco)

| Nome Atual | Novo Nome | Arquivo | Razão |
|-----------|-----------|---------|-------|
| `GamePauseCommandEvent` | `RunPauseCommandEvent` | RunLifecycleEvents.cs | Remove ambiguidade de escopo |
| `PauseStateChangedEvent` | `RunPauseStateChangedEvent` | RunLifecycleEvents.cs | Deixa explícito escopo de run |
| `BlockActivitySimulation` (enum) | `BlockActivityExecution` | SessionActivitySimulationContracts.cs | Alinha com ActivityExecutionState |
| `ReleaseActivitySimulation` (enum) | `ReleaseActivityExecution` | SessionActivitySimulationContracts.cs | Alinha com ActivityExecutionState |
| `BlockSessionSimulation` (enum) | `BlockSessionExecution` | SessionActivitySimulationContracts.cs | Alinha com ActivityExecutionState |
| `ReleaseSessionSimulation` (enum) | `ReleaseSessionExecution` | SessionActivitySimulationContracts.cs | Alinha com ActivityExecutionState |
| Enum fact values (8) | `ActivityExecutionBlocked/Released`, etc. | SessionActivitySimulationContracts.cs | Alinha com execution naming |

### Importante - Fase 2 (Risco Médio)

| Nome Atual | Novo Nome | Arquivo | Razão |
|-----------|-----------|---------|-------|
| `GameRunEndRequestedEvent` | `RunDeactivationRequestedEvent` | RunLifecycleEvents.cs | ADR-0002: Deactivation axis |
| `GameRunEndedEvent` | `RunDeactivationCompletedEvent` | RunLifecycleEvents.cs | ADR-0002: Deactivation axis |
| `GameRunOutcome` | `RunDeactivationOutcome` | RunLifecycleEvents.cs | Alinha com deactivation naming |
| `GamePlayRequestedEvent` | `RunActivationRequestedEvent` | RunLifecycleEvents.cs | Alinha com run activation |
| `SessionActivitySimulationState` | `ActivityExecutionState` | SessionActivityContracts.cs | ADR-0004: Activity lifecycle |
| `CurrentSimulationState` (property) | `CurrentExecutionState` | SessionActivityRuntimeState.cs | Alinha com estado |
| `SetSimulationState()` (method) | `SetExecutionState()` | SessionActivityRuntimeState.cs | Alinha com estado |
| `SimulationGateCommand` | `ActivitySimulationBlockingCommand` | SessionActivitySimulationContracts.cs | Deixa explícito bloqueio |
| `SimulationGateIdentity` | `ActivitySimulationBlockingIdentity` | SessionActivitySimulationContracts.cs | Alinha com comando |
| `SimulationGateResult` | `ActivitySimulationBlockingResult` | SessionActivitySimulationContracts.cs | Alinha com comando |
| `SimulationGateCommandKind` | `ActivitySimulationBlockingCommandKind` | SessionActivitySimulationContracts.cs | Alinha com comando |
| `SimulationGateFactKind` | `ActivitySimulationBlockingFactKind` | SessionActivitySimulationContracts.cs | Alinha com fatos |
| `SimulationGateState` | `ActivitySimulationBlockingState` | SessionActivitySimulationContracts.cs | Deixa claro estado de bloqueio |

### Bloqueado - Fase 3 (Alto Risco + Análise)

| Nome Atual | Próximo Passo | Bloqueador |
|-----------|--------------|-----------|
| `PhaseSignature` | `ActivityEntrySignature` (se Phase=Activity) | Audit de ActorsSystem Phase semantics |
| `PhaseEntry` | `ActivityEntry` (se Phase=Activity) | Audit de ActorsSystem Phase semantics |
| `PhaseExclusive` | `ActivityExclusive` (se Phase=Activity) | Audit de ActorsSystem Phase semantics |
| `PreserveAcrossPhaseEntry` | `PreserveAcrossActivityEntry` (se Phase=Activity) | Audit de ActorsSystem Phase semantics |
| `PhaseEntryIdentity` (property em RunLifecycleSignalIdentity) | `ActivityEntryIdentity` | Audit de Phase/Activity mapping |

---

## ✅ Já Corretos (Manter)

| Nome | Tipo | Razão |
|------|------|-------|
| `SessionActivitySimulationGate` | Class | ADR-0007: "Gates executam validação" — precisamente nomeado |
| `RunLifecycleSignalIdentity` | Class | ADR-0001: Identidade explícita; normativo |
| `RunLifecycleContracts` | File | Contém enums normativos de run lifecycle |
| `SessionActivityEntryHandoff` | Pattern (em docs) | ADR-0004: SessionActivityPipeline coordena |

---

## 📈 Métricas da Auditoria

```
Total de nomes encontrados: 47
├─ Já alinhados com Base 1.1: 8 (17%)
├─ Recomendado para rename: 36 (77%)
└─ Requer análise adicional: 3 (6%)

Distribuição por risco:
├─ Baixo (8): ~5 dias de refactor
├─ Médio (20): ~5 dias de refactor
└─ Alto (8): Bloqueado até análise de ActorsSystem Phase
```

---

## 🚨 Casos Especiais

### 1. ActorsSystem Phase Semantics

**Problema**: Não está claro se "Phase" em ActorsSystem é:
- ✅ Mapeado para Activity (então rename para ActivityEntry)
- ❌ Conceito vivo separado de Activity (então manter)

**Ação Necessária**: Audit de `ActorsSystem/Models/ActorsSemanticParticipationModels.cs` e `ActorSpecModels.cs` junto com semanticista de domínio.

**Proposição**: Se PhaseEntry refere-se a entrada de activity SessionActivityEntryHandoff, fazer rename para ActivityEntry + adicionar ADR-0009 sobre ActorsSystem Activity Mapping.

### 2. SimulationGate vs Gate

**Achado**: `SessionActivitySimulationGate` não é ambíguo em contexto.
- "Simulation" = simulação de atividade (controle de execução)
- "Gate" = validador de estado/transição (ADR-0007)
- Uso: Apenas em `SessionActivity/Simulation/`; não é global

**Recomendação**: Manter `SessionActivitySimulationGate` como está; renomear apenas os Contracts/Enums inteiros para deixar claro que é "ActivityExecutionBlocking*".

### 3. EventBus<GameRunEndedEvent> Type Erasure

**Problema**: `EventBus<GameRunEndedEvent>` usa tipo genérico; rename quebra referências em `GlobalCompositionRoot.Events.cs`.

**Solução**: IDE refactor mantém tipo genérico automático. Validar pós-rename queSolver:
```csharp
// Antes:
EventBus<GameRunEndedEvent>.Clear();

// Depois (IDE refactor automático):
EventBus<RunDeactivationCompletedEvent>.Clear();
```

---

## 🔍 Auditoria Detalhada

Para análise completa, consulte:

📄 **[Base-1.1-Canonical-Rename-Map.md](./Base-1.1-Canonical-Rename-Map.md)**
- Tabelas detalhadas com 47 nomes
- Bloco-por-bloco análise
- Justificativas de cada rename
- Matriz de risco
- Cronograma de 3 fases

---

## 📞 Próximos Passos

1. **Review deste sumário** — aprovação de estratégia
2. **Audit de ActorsSystem Phase** (bloqueador para Fase 3)
3. **Executar Fase 1** (Baixo Risco) — ~8 renames simples
4. **Smoke tests** após Fase 1
5. **Executar Fase 2** (Médio Risco) — ~20 renames
6. **Executar Fase 3** (Alto Risco) — após decisão ActorsSystem Phase

---

**Auditoria realizada por**: AI Coding Agent
**Nenhuma alteração de código foi aplicada**
**Status**: ✅ Pronto para review e planejamento de implementação

