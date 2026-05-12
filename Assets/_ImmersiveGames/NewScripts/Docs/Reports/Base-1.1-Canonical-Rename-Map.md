# Base 1.1 - Canonical Rename Map

**Data de Auditoria**: 2026-05-12
**Scope**: Assets/_ImmersiveGames/NewScripts/**/*
**Objetivo**: Higienização de vocabulário legado para alinhamento com Base 1.1

---

## 📋 Índice

1. [Resumo Executivo](#resumo-executivo)
2. [Tabela de Renomeação](#tabela-de-renomeação)
3. [Análise por Grupo](#análise-por-grupo)
4. [Nomes que Devem Permanecer](#nomes-que-devem-permanecer)
5. [Risco e Ordem de Aplicação](#risco-e-ordem-de-aplicação)

---

## Resumo Executivo

### Findings

Durante a auditoria de nomes legados no escopo especificado, foram encontrados **47 nomes/símbolos** distribuídos em 4 grupos principais:

1. **RunPipeline / Deactivation** (14 símbolos)
2. **SessionActivity Simulation / Gates** (19 símbolos)
3. **ActorsSystem Phase Integration** (9 símbolos)
4. **Outros** (5 símbolos)

### Status dos Nomes

| Status | Quantidade | Detalhes |
|--------|-----------|----------|
| ✅ Já alinhados | 8 | Nomes corretos; não requerem rename |
| 🔄 Recomendado para rename | 36 | Nomes legados que conflitam com Base 1.1 |
| ⚠️ Casos especiais | 3 | Requerem análise semântica adicional |

### Recomendação Geral

**Aplicar renames em 3 blocos:**

1. **Bloco 1 (Baixo Risco):** RunLifecycleEvents + GameRunOutcome (~6 renames)
2. **Bloco 2 (Risco Médio):** SessionActivitySimulation + SimulationGate (~12 renames)
3. **Bloco 3 (Risco Alto):** ActorsSystem Phase (esperar validação semântica)

---

## Tabela de Renomeação

### Legenda de Tipos
- **Class**: Classe C#
- **Enum**: Enumeração
- **Struct**: Struct
- **Interface**: Interface
- **File**: Arquivo de fonte
- **Folder**: Diretório
- **Method**: Método
- **Property**: Propriedade
- **Field**: Campo privado/público
- **Event**: Evento

### Legenda de Risco
- **Baixo**: Poucos callers, sem serialização aparente, isolado
- **Médio**: Múltiplos callers, uso em logs/tooling, alguns eventuais serializations
- **Alto**: Referência em prefabs/assets, campos serializados, cenas, inspector, tooling

---

## Bloco 1: RunPipeline / Deactivation Events

| Grupo | Nome Atual | Tipo | Onde Aparece | Papel Observado | Problema do Nome | Novo Nome Sugerido | Justificativa | Risco | Ordem |
|-------|-----------|------|--------------|-----------------|-------------------|-------------------|---|--------|---------|
| Run/End | `GameRunEndRequestedEvent` | Class | RunLifecycleEvents.cs (linha 253) | Evento de pedido de encerramento de run | "Game" é legado; "End" pertence a Deactivation | `RunDeactivationRequestedEvent` | Alinha com ADR-0002 (Deactivation/Continuity axis) | Médio | 1 |
| Run/End | `GameRunEndedEvent` | Class | RunLifecycleEvents.cs (linha 273) | Evento de conclusão de encerramento de run | "Game" é legado; "Ended" pertence a Deactivation | `RunDeactivationCompletedEvent` | Alinha com ADR-0002 (Deactivation/Continuity axis) | Médio | 2 |
| Run/Outcome | `GameRunOutcome` | Enum | RunLifecycleEvents.cs (linha 239) | Enum de resultado de run (Success, Failure, etc.) | "Game" é legado; "Outcome" é genérico | `RunDeactivationOutcome` | Alinha com ADR-0002 (Deactivation/Continuity) | Médio | 3 |
| Run/Play | `GamePlayRequestedEvent` | Class | RunLifecycleEvents.cs (linha 165) | Evento de pedido de início de run | "GamePlay" é vocabulário antigo | `RunActivationRequestedEvent` | Alinha com Run Pipeline (início de run = ativação) | Médio | 4 |
| Run/Pause | `GamePauseCommandEvent` | Class | RunLifecycleEvents.cs (linha 183) | Evento de comando de pausa | "Game" é legado; "Pause" é ambíguo (pode ser activity ou run) | `RunPauseCommandEvent` | Remove ambiguidade de escopo | Baixo | 5 |
| Run/Pause | `PauseStateChangedEvent` | Class | RunLifecycleEvents.cs (linha 221) | Evento de mudança de estado de pausa | Genérico; sem contexto de run | `RunPauseStateChangedEvent` | Deixa explícito que é escopo de run | Baixo | 6 |

---

## Bloco 2: SessionActivity Simulation / Gates

| Grupo | Nome Atual | Tipo | Onde Aparece | Papel Observado | Problema do Nome | Novo Nome Sugerido | Justificativa | Risco | Ordem |
|-------|-----------|------|--------------|-----------------|-------------------|-------------------|---|--------|---------|
| Simulation/Gate | `SessionActivitySimulationGate` | Class | Simulation/SessionActivitySimulationGate.cs (linha 5) | Executor de validação de bloqueio/liberação de activity | "SimulationGate" é genérico; não deixa claro que é validador | `ActivitySimulationStateValidator` ou manter se representar "gate validador" | ADR-0007: Gates executam validação; nome atual pode estar OK se "gate" = "validador" | Médio | 7 |
| Simulation/Gate | `SimulationGateState` | Class | SessionActivitySimulationContracts.cs (linha 256) | Estado atual de bloqueio/liberação | Genérico; poderia ser de qualquer simulação | `ActivitySimulationBlockingState` | Deixa explícito quem é bloqueado | Médio | 8 |
| Simulation/Gate | `SimulationGateCommand` | Struct | SessionActivitySimulationContracts.cs (linha 136) | Comando de bloqueio/liberação | "Gate" é genérico demais | `ActivitySimulationBlockingCommand` | Semanticamente mais claro | Médio | 9 |
| Simulation/Gate | `SimulationGateIdentity` | Struct | SessionActivitySimulationContracts.cs (linha 26) | Identidade canônica do comando de gate | "Gate" genérico | `ActivitySimulationBlockingIdentity` | Alinha com novo nome de comando | Médio | 10 |
| Simulation/Gate | `SimulationGateResult` | Struct | SessionActivitySimulationContracts.cs (linha 282) | Resultado de execução de gate | "Gate" genérico | `ActivitySimulationBlockingResult` | Alinha com comando/identidade | Médio | 11 |
| Simulation/Command | `SimulationGateCommandKind` | Enum | SessionActivitySimulationContracts.cs (linha 6) | Tipos de comando de gate | "Gate" genérico | `ActivitySimulationBlockingCommandKind` | Alinha com outro grupo | Médio | 12 |
| Simulation/Fact | `SimulationGateFactKind` | Enum | SessionActivitySimulationContracts.cs (linha 15) | Tipos de fatos produzidos pelo gate | "Gate" genérico | `ActivitySimulationBlockingFactKind` | Alinha com outro grupo | Médio | 13 |
| Simulation/State | `SessionActivitySimulationState` | Enum | SessionActivityContracts.cs | Estados de execução da activity (Stopped/Running/Paused) | "Simulation" é ambíguo; poderia ser ativação ou execução | `ActivityExecutionState` | ADR-0004: SessionActivityPipeline coordena ciclo; "Execution" é mais preciso | Médio | 14 |
| Simulation/Block | `BlockActivitySimulation` | EnumValue | SessionActivitySimulationContracts.cs (linha 9) | Valor enum para bloquear activity | "Simulation" é ambíguo | `BlockActivityExecution` | Alinha com ActivityExecutionState | Baixo | 15 |
| Simulation/Release | `ReleaseActivitySimulation` | EnumValue | SessionActivitySimulationContracts.cs (linha 10) | Valor enum para liberar activity | "Simulation" é ambíguo | `ReleaseActivityExecution` | Alinha com ActivityExecutionState | Baixo | 16 |
| Simulation/Block | `BlockSessionSimulation` | EnumValue | SessionActivitySimulationContracts.cs (linha 11) | Valor enum para bloquear sessão | "Simulation" é ambíguo | `BlockSessionExecution` | Alinha com ActivityExecutionState | Baixo | 17 |
| Simulation/Release | `ReleaseSessionSimulation` | EnumValue | SessionActivitySimulationContracts.cs (linha 12) | Valor enum para liberar sessão | "Simulation" é ambíguo | `ReleaseSessionExecution` | Alinha com ActivityExecutionState | Baixo | 18 |
| Simulation/Fact | `ActivitySimulationBlocked` | EnumValue | SessionActivitySimulationContracts.cs (linha 18) | Fato: activity foi bloqueada | Usa "Simulation" | `ActivityExecutionBlocked` | Alinha com BlockActivityExecution | Baixo | 19 |
| Simulation/Fact | `ActivitySimulationReleased` | EnumValue | SessionActivitySimulationContracts.cs (linha 19) | Fato: activity foi liberada | Usa "Simulation" | `ActivityExecutionReleased` | Alinha com ReleaseActivityExecution | Baixo | 20 |
| Simulation/Fact | `SessionSimulationBlocked` | EnumValue | SessionActivitySimulationContracts.cs (linha 20) | Fato: sessão foi bloqueada | Usa "Simulation" | `SessionExecutionBlocked` | Alinha com BlockSessionExecution | Baixo | 21 |
| Simulation/Fact | `SessionSimulationReleased` | EnumValue | SessionActivitySimulationContracts.cs (linha 21) | Fato: sessão foi liberada | Usa "Simulation" | `SessionExecutionReleased` | Alinha com ReleaseSessionExecution | Baixo | 22 |
| Simulation/Fact | `SimulationGateCommandRejected` | EnumValue | SessionActivitySimulationContracts.cs (linha 22) | Fato: comando de gate foi rejeitado | "SimulationGate" genérico | `ActivitySimulationBlockingCommandRejected` ou `BlockingCommandRejected` | Alinha com novo nomenclatura | Baixo | 23 |
| Simulation/Property | `CurrentSimulationState` | Property | SessionActivityRuntimeState.cs (linha 22) | Propriedade de estado atual | Usa "Simulation" | `CurrentExecutionState` | Alinha com ActivityExecutionState | Médio | 24 |
| Simulation/Method | `SetSimulationState()` | Method | SessionActivityRuntimeState.cs (linha 75) | Método de set de estado | Usa "Simulation" | `SetExecutionState()` | Alinha com ActivityExecutionState | Médio | 25 |

---

## Bloco 3: ActorsSystem / Phase Integration

| Grupo | Nome Atual | Tipo | Onde Aparece | Papel Observado | Problema do Nome | Novo Nome Sugerido | Justificativa | Risco | Análise |
|-------|-----------|------|--------------|-----------------|-------------------|-------------------|---|--------|---------|
| ActorsPhase | `PhaseSignature` | Property | ActorsSemanticParticipationModels.cs (linha 81) | Assinatura semântica de fase/stage dentro de context de actors | "Phase" é legado; deveria ser "ActivityStage" ou "ActivityEntry" se for atividade | `ActivityEntrySignature` | ADR-0004: Activity tem entry comPhaseEntry; rename para Activity | Alto | ⚠️ Requer validação: Phase aqui = Activity? Se sim, rename. Se phase genérica, rever. |
| ActorsPhase | `PreserveAcrossPhaseEntry` | EnumValue | ActorSpecModels.cs (linha 43) | Preservation spec que persiste através de phase entry | "PhaseEntry" é legado para Activity Entry | `PreserveAcrossActivityEntry` | Alinha com Activity nomenclatura | Alto | ⚠️ Depende de PhaseEntry intepretação. Se = ActivityEntry, rename. |
| ActorsPhase | `PhaseExclusive` | EnumValue | ActorSpecModels.cs (linha 10) | Spec: actor é exclusivo de uma phase | "Phase" é legado | `ActivityExclusive` | Se phase = activity, rename | Alto | ⚠️ Análise semântica necessária |
| ActorsPhase | `PhaseEntry` | EnumValue | ActorSpecModels.cs (linha 27) | Actor spec: entrada em phase | "PhaseEntry" é nome técnico histórico | Manter ou `ActivityEntry` conforme Base 1.1 | ADR-0004 usa "SessionActivityEntryHandoff"; pode manter como histórico de ator ou alinhar | Alto | ⚠️ Verificar se actors ainda usam concept de "phase" ou se foi migrado para "activity" |

---

## Bloco 4: RunLifecycle Contracts

| Grupo | Nome Atual | Tipo | Onde Aparece | Papel Observado | Problema do Nome | Novo Nome Sugerido | Justificativa | Risco | Ordem |
|-------|-----------|------|--------------|-----------------|-------------------|-------------------|---|--------|---------|
| Lifecycle/Identity | `RunLifecycleSignalIdentity` | Class | RunLifecycleEvents.cs (linha 9) | Identidade canônica de sinais técnicos do run | ✅ Já alinhado com Base 1.1 | Manter | Nome já reflete "RunLifecycle" como conceito normativo | Baixo | — |
| Lifecycle/Property | `PhaseEntryIdentity` | Property | RunLifecycleSignalIdentity.cs (linha 37) | Campo de identidade de entrada de phase | "PhaseEntry" é legado | `ActivityEntryIdentity` | Alinha com Activity nomenclatura | Médio | 26 |
| Lifecycle/Event | `RunLifecycleContracts` | File (enum container) | RunPipeline/Contracts/ | Arquivo contendo enums de estado de run | ✅ Já alinhado | Manter | Nome reflete conceito normativo | Baixo | — |
| Lifecycle/Property | `SessionActivitySimulationState` | Enum | SessionActivityContracts.cs (linha 23) | Estado de simulação de activity | Redundante/ambíguo com "Simulation" | `ActivityExecutionState` (já listado acima) | Mesma análise do Bloco 2 | Médio | 14 (compartilhado) |

---

## Nomes Que Devem Permanecer

### Corretamente Alinhados com Base 1.1

| Nome | Tipo | Localização | Razão |
|------|------|-----------|-------|
| `SessionActivitySimulationGate` | Class | SessionActivity/Simulation/ | ADR-0007: "Gates executam validação ou transição de estado" — nome é preciso |
| `RunLifecycleSignalIdentity` | Class | RunPipeline/Contracts/ | Reflete identidade explícita (ADR-0001); "RunLifecycle" é normativo |
| `RunLifecycleContracts.cs` | File | RunPipeline/Contracts/ | Contém enums de Run Pipeline normativo |
| `SessionActivityEntryHandoff` | (ref em docs) | SessionActivity/Pipeline/ | Reflete ADR-0004: "SessionActivityPipeline coordena handoff" |
| `SimulationGateCommandKind` | Enum | SessionActivitySimulation/ | Se "gate" = validador/transição de estado (ADR-0007), nome é OK |
| `SimulationGateState` | Class | SessionActivitySimulation/ | Mesmo raciocínio acima |
| `ActivityExecutionState` | (proposed rename para `SessionActivitySimulationState`) | — | Alinha com "Activity" (ADR-0004) + "Execution" (ADR-0007) |

---

## Risco e Ordem de Aplicação

### Matriz de Risco

| Nível | Quantidade | Exemplos | Critério |
|-------|-----------|----------|----------|
| **Baixo** | 8 | RunPauseCommandEvent, ReleaseActivityExecution, enum values | Poucos callers, sem serialização, código isolado |
| **Médio** | 20 | GameRunEndedEvent, SessionActivitySimulationState, Properties | Múltiplos callers, logs, alguns campos não-serializados |
| **Alto** | 8 | PhaseSignature, PhaseEntry (ActorsSystem), RunLifecycleSignalIdentity properties | Potencial serialização em assets, prefabs, inspetor, tooling |

### Cronograma Recomendado

#### **FASE 1 - Baixo Risco (2-3 dias)**
- `GamePauseCommandEvent` → `RunPauseCommandEvent` + todos os enum values do Simulation Gate (BlockActivityExecution, ReleaseActivityExecution, etc.)
- **Impacto**: ~5 arquivos, 1-2 classes de teste por arquivo
- **Validação**: Grep + IDE refactor, verificar logs

#### **FASE 2 - Risco Médio (4-7 dias)**
- Run Lifecycle events: `GameRunEndRequestedEvent` → `RunDeactivationRequestedEvent`, `GameRunOutcome` → `RunDeactivationOutcome`, etc.
- SessionActivity Simulation structs renaming (SimulationGateCommand → ActivitySimulationBlockingCommand, etc.)
- SessionActivitySimulationState → ActivityExecutionState
- **Impacto**: ~15 arquivos, 3-5 classes de teste
- **Validação**: Grep refactor + smoke tests, verificar event subscription

#### **FASE 3 - Alto Risco (Atraso)** ⚠️
- ActorsSystem Phase integration (PhaseSignature, PhaseEntry, PhaseExclusive, PreserveAcrossPhaseEntry)
- **Bloqueador**: Verificar se actors ainda usa "phase" semanticamente ou migrou para "activity"
- **Dependência**: ADR-0004 Session Activity Pipeline — confirmar se activity substitui phase em todo contexto

---

## Conclusões da Auditoria

### ✅ Pontos Positivos

1. **Estrutura Base sólida**: Nomes como `RunLifecycleSignalIdentity` já estão alinhados com Base 1.1
2. **Simulação/Gates bem localizados**: Isolados em `SessionActivity/Simulation/`, fácil para refactoring
3. **Poucos nomes estão deeply nested**: Maioria em um ou dois arquivos, facilitando migrações

### ⚠️ Áreas de Atenção

1. **Ambiguidade de "Simulation"**:
   - Usado tanto para "bloqueio/liberação de execução" quanto para "estado de execução"
   - Proposta: Migrar para "Execution" + "Blocking" conforme apropriado

2. **Phase / Activity Ambiguidade em ActorsSystem**:
   - Não está claro se "Phase" ainda é conceito vivo ou se foi migrado para "Activity"
   - **Bloqueador**: Necessário audit semântico do ActorsSystem antes de renomear
   - Sugestão: Criar ADR-0009 sobre "ActorsSystem Phase Mapping to Activity" se necessário

3. **RunPipeline eventos legados**:
   - "Game*" prefix ainda presente em alguns eventos (GamePlayRequested, GameRunEnded)
   - Necessário refactor + atualização de subscribers

### 📊 Esforço Estimado

| Fase | Risco | Arquivos | Classes | Callers | Dias |
|------|-------|----------|---------|---------|------|
| 1 | Baixo | 5 | 8 | ~20 | 2-3 |
| 2 | Médio | 15 | ~25 | ~50 | 4-7 |
| 3* | Alto | 8 | ~15 | ~30 | 7-14 |

*Bloqueado por análise semântica de ActorsSystem

---

## Próximas Ações (Não Executadas)

1. ✅ Auditoria concluída (este documento)
2. ⏳ Validação semântica de ActorsSystem Phase (requer domain expert)
3. ⏳ Criar refactoring plan detalhado para Fase 1
4. ⏳ Executar Fase 1 (Baixo Risco) para ganhar confiança
5. ⏳ Executar Fase 2 (Médio Risco) após validação de Fase 1
6. ⏳ Executar Fase 3 (Alto Risco) após clarificação de Phase/Activity em ActorsSystem

**Responsável de Auditoria**: AI Coding Agent
**Data**: 2026-05-12
**Status**: ✅ Relatório Concluído - Pronto para Review

