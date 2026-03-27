> [!WARNING]
> **Status de validação:** conteúdo importado de análise externa e **editado/validado parcialmente** contra o código atual.
>
> **Uso correto:** tratar este documento como **auditoria atualizada**, mantendo o código atual, os ADRs vigentes, a documentação canônica e os logs recentes como fonte de verdade.
>
> **Fonte de verdade:** código atual, ADRs vigentes, documentação canônica do projeto e evidência recente de runtime.

> [!NOTE]
> **Origem anterior:** `Docs/Modules/GAMELOOP_ANALYSIS_REPORT.md`
>
> Este arquivo foi movido para cá como localização canônica dos relatórios importados por módulo.

---

# 📊 ANÁLISE DO MÓDULO GAMELOOP - REDUNDÂNCIAS E OTIMIZAÇÕES

**Data:** 22 de março de 2026
**Projeto:** GameJam2025
**Módulo:** GameLoop (`Assets/_ImmersiveGames/NewScripts/Modules/GameLoop`)
**Versão do Relatório:** 1.3
**Status:** ✅ Editado sobre a versão 1.2, preservando a estrutura do relatório e atualizado contra o código validado em runtime após a reorganização por capability do módulo, renomeação dos principais arquivos e validação da nova árvore

---

## 📋 ÍNDICE

1. [Visão Geral](#visão-geral)
2. [Estrutura do Módulo](#estrutura-do-módulo)
3. [Problemas Identificados](#problemas-identificados)
4. [Otimizações Recomendadas](#otimizações-recomendadas)
5. [Impacto Estimado](#impacto-estimado)
6. [Plano de Implementação](#plano-de-implementação)
7. [Conclusão](#conclusão)

---

## 🎯 Visão Geral

O módulo GameLoop é a **coluna vertebral do jogo**, coordenando:
- Estados principais (Boot → Ready → IntroStage → Playing → Paused → PostPlay)
- Resultado de runs (Vitória/Derrota)
- Transições entre cenas e reinicializações

**Pontos Fortes:**
- ✅ Arquitetura clara baseada em State Machine
- ✅ Organização atual por capability (`Core`, `Run`, `Flow`, `Input`, `Bootstrap`)
- ✅ Serviços bem separados (Loop, Snapshot, Outcome, EndRequest)
- ✅ Eventos bem tipados para comunicação
- ✅ Integrações com SceneFlow/PostGame mais legíveis após a reorganização
- ✅ Logging detalhado para debug
- ✅ Idempotência bem tratada
- ✅ Consolidação recente do ciclo de run validada em runtime (`OutcomeService` como owner terminal, `ResultSnapshotService` como projeção fina)
- ✅ Reorganização por capability e renomeação principal validadas em runtime

**Entretanto**, ainda existem **redundâncias e refinamentos relevantes**:
- 🟢 A normalização de `reason` foi centralizada e validada em runtime
- 🟡 Event binding/unregister patterns duplicados (5+ arquivos)
- 🟡 Métodos similares de TryResolve/resolve de contexto ainda espalhados
- 🟡 `GameLoopService` ainda segue como hotspot, embora reduzido após a extração dos side effects e do resolver de snapshot
- 🟢 A duplicação estrutural entre `GameRunResultSnapshotService` e `GameRunOutcomeService` foi reduzida
- 🟢 A validação de gameplay ativo entre snapshot/outcome foi centralizada em `GameRunPlayingStateGuard`

---

## 📁 Estrutura do Módulo

```
GameLoop/
├── Core/
│   ├── GameLoopContracts.cs
│   ├── GameLoopEvents.cs
│   ├── GameLoopReasonFormatter.cs
│   ├── GameLoopService.cs
│   └── GameLoopStateMachine.cs
├── Run/
│   ├── GameRunEndRequestService.cs
│   ├── GameRunEndedEventBridge.cs
│   ├── GameRunOutcomeRequestBridge.cs
│   ├── GameRunOutcomeService.cs
│   ├── GameRunPlayingStateGuard.cs
│   ├── GameRunResultSnapshotService.cs
│   └── IGameRunEndRequestService.cs
├── Flow/
│   ├── GameLoopPostGameSnapshotResolver.cs
│   ├── GameLoopSceneFlowSyncCoordinator.cs
│   └── GameLoopStateTransitionEffects.cs
├── Input/
│   ├── GameLoopCommands.cs
│   ├── GameLoopInputCommandBridge.cs
│   ├── GameLoopInputDriver.cs
│   ├── GameLoopStartRequestEmitter.cs
│   └── IGameLoopCommands.cs
├── Bootstrap/
│   └── GameLoopBootstrap.cs
├── IntroStage/
│   ├── IntroStageContracts.cs
│   ├── IntroStageControlService.cs
│   └── Runtime/
├── Pause/
│   └── GamePauseOverlayController.cs
└── EndConditions/
    ├── GameplayOutcomeConditionsController.cs
    └── GameplayOutcomeQaPanel.cs
```

**Total:** ~2000 linhas de código (reorganizado por capability, sem mudança de comportamento)

---

## 🔴 PROBLEMAS IDENTIFICADOS

### 1️⃣ NORMALIZAÇÃO DE STRINGS DUPLICADA (reduzido, validado e realocado no Core)

**Localização anterior relevante:**
- `GameLoopCommands.cs`
- `GameLoopInputCommandBridge.cs`
- `GameRunEndedEventBridge.cs`
- `GameRunResultSnapshotService.cs`
- `GameRunOutcomeService.cs`
- `GameRunOutcomeRequestBridge.cs`

**Problema anterior:**
Havia variações locais de:
- `FormatReason()`
- `NormalizeRequiredReason()`
- `NormalizeOptionalReason()`
- `NormalizeReason()`

com regras repetidas de:
- `Trim()`
- `IsNullOrWhiteSpace()`
- fallback para `<null>` / `Unspecified`

**Estado atual:**
- a normalização/formatação foi centralizada em `Core/GameLoopReasonFormatter`
- os principais consumers do módulo passaram a usar o formatter único
- o fluxo validado em runtime preservou as `reason` esperadas em:
    - `Victory`
    - `Restart`
    - `Defeat`
    - `ExitToMenu`

**Impacto atual:**
- ✅ uma fonte única de verdade para `reason`
- ✅ menor risco de divergência semântica
- ✅ melhor consistência de logs/eventos
- ✅ validado em runtime no ciclo completo

**Severidade:** 🟢 **BAIXA** - problema estrutural reduzido

---

### 2️⃣ STATE VALIDATION CHECKS DUPLICADOS (reduzido após consolidação dos serviços de run)

**Localização atual relevante:** `GameRunOutcomeService`, `GameRunPlayingStateGuard`, `GameplayOutcomeConditionsController`, `GameRunEndedEventBridge`

**Problema anterior:**
Havia validação duplicada de estado ativo de gameplay entre `GameRunStateService` e `GameRunOutcomeService`, além de outras verificações paralelas em controladores/bridges.

**Estado atual:**
- `GameRunOutcomeService` passou a usar `GameRunPlayingStateGuard`
- `GameRunResultSnapshotService` deixou de revalidar `Playing` e virou projeção fina
- a duplicação estrutural do par state/outcome foi reduzida
- ainda existem checks correlatos fora desse par, especialmente em bridges/controladores de borda

**Impacto atual:**
- ✅ a fonte principal de verdade para “run terminal só em gameplay ativo” ficou centralizada
- ✅ o risco de divergência entre `GameRunStateService` e `GameRunOutcomeService` foi reduzido
- ⚠️ ainda não houve consolidação ampla de todos os checks periféricos do módulo
- ⚠️ o tema continua como refinamento, mas não é mais hotspot principal

**Severidade:** 🟡 **MÉDIA-BAIXA** - Melhorou de forma real, porém ainda há validações paralelas em pontos de borda

---

### 3️⃣ EVENT BINDING/UNREGISTER PATTERNS DUPLICADOS (5+ arquivos)

**Localização:** `GameRunResultSnapshotService`, `GameRunOutcomeService`, `GameplayOutcomeConditionsController`, `GameRunEndedEventBridge`, `GameLoopInputCommandBridge`

**Problema:**

```csharp
// Padrão 1: GameRunStateService
_binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
_startBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
EventBus<GameRunEndedEvent>.Register(_binding);
EventBus<GameRunStartedEvent>.Register(_startBinding);

// ... dispose
try { EventBus<GameRunEndedEvent>.Unregister(_binding); } catch { }
try { EventBus<GameRunStartedEvent>.Unregister(_startBinding); } catch { }

// Padrão 2: GameLoopRunEndEventBridge (com guard _registered)
private void RegisterBinding()
{
    if (_registered) return;
    EventBus<GameRunEndedEvent>.Register(_binding);
    _registered = true;
}

private void UnregisterBinding()
{
    if (!_registered) return;
    EventBus<GameRunEndedEvent>.Unregister(_binding);
    _registered = false;
}

// Padrão 3: GamePlayEndConditionsController (sem guard explícito)
// ... repetido em OnEnable/OnDisable
```

**Impacto:**
- ⚠️ 5+ implementações diferentes do mesmo padrão
- ⚠️ Cada uma com abordagem ligeiramente diferente (com/sem guards)
- ⚠️ Código duplicado: binding creation, register, unregister, try-catch
- ⚠️ Difícil manter consistência em todas instâncias
- ⚠️ ~100 linhas de código duplicado

**Severidade:** 🟡 **MÉDIA** - Afeta manutenibilidade e consistência

---

### 4️⃣ SERVIÇO OUTCOME vs SNAPSHOT - DEDUPLICAÇÃO DE LÓGICA (reduzido e validado)

**Localização:** `GameRunResultSnapshotService` vs `GameRunOutcomeService`

**Problema anterior:**
Os dois serviços sobrepunham partes do mesmo lifecycle:
- ambos escutavam `GameRunStartedEvent`
- ambos lidavam com `GameRunEndedEvent`
- ambos mantinham guards/idempotência próprios
- ambos reimplementavam validação de `Playing`

**Estado atual:**
- `GameRunOutcomeService` permaneceu como **owner terminal** de `GameRunEndedEvent`
- `GameRunResultSnapshotService` virou **projeção/snapshot fino** de `HasResult / Outcome / Reason`
- a validação de gameplay ativo foi centralizada em `GameRunPlayingStateGuard`
- o wiring novo foi validado em runtime no ciclo completo `Victory -> Restart -> Defeat -> ExitToMenu`

**Impacto atual:**
- ✅ a duplicação estrutural principal foi removida
- ✅ o fluxo de run ficou mais claro: owner terminal vs projeção
- ✅ o rearm/start da nova run permaneceu saudável em runtime
- ⚠️ ainda existe oportunidade futura de revisar listeners/boilerplate periféricos do GameLoop
- ⚠️ `GameLoopService` continua grande e segue como hotspot mais óbvio do módulo

**Severidade:** 🟢 **BAIXA** - Problema principal reduzido e validado

---

## 🟢 OTIMIZAÇÕES RECOMENDADAS

### **OTIMIZAÇÃO A: Centralizar Reason Formatting**

**Objetivo original:** Eliminar variações duplicadas de normalização de strings

**Estado atual:** ✅ **Concluída e validada**

**Arquivo:** `Core/GameLoopReasonFormatter.cs`

**Resultado obtido:**
- ✅ Uma única fonte de verdade para `Format`, `NormalizeRequired` e `NormalizeOptional`
- ✅ Consumers principais do `GameLoop` migrados para o formatter central
- ✅ Consistência preservada em runtime para `Victory`, `Restart`, `Defeat` e `ExitToMenu`
- ✅ Redução efetiva de boilerplate de `reason` no módulo

**Leitura atualizada:**
Esta otimização deixou de ser proposta e passou a baseline do módulo.

---

### **OTIMIZAÇÃO B: Revisar validações periféricas de estado**

**Objetivo:** Consolidar apenas os checks periféricos que ainda restaram fora do trilho principal já centralizado em `GameRunPlayingStateGuard`

**Arquivo / alvo atual:** bridges/controladores que ainda carregam validação própria

**Direção recomendada:**
- reutilizar `GameRunPlayingStateGuard` onde fizer sentido
- evitar reespalhar a regra de `Playing`
- manter diferenças reais apenas nos pontos de borda que dependem de scene classifier ou contexto específico

**Benefícios:**
- ✅ Mantém uma fonte principal de verdade para gameplay ativo
- ✅ Reduz divergência em checks periféricos
- ✅ Reaproveita a consolidação já validada em runtime
- ✅ Evita criar outro helper redundante


### **OTIMIZAÇÃO C: Criar EventBindingHelper**

**Objetivo:** Consolidar padrão de binding/unbinding de eventos

**Arquivo:** Novo `EventBindingHelper.cs` (na Core)

**Implementação:**

```csharp
/// <summary>
/// Helper para gerenciar lifecycle de event bindings com guard automático.
/// Elimina boilerplate de register/unregister espalhado em 5+ arquivos.
/// </summary>
public sealed class ManagedEventBinding<TEvent> where TEvent : IEvent
{
    private EventBinding<TEvent> _binding;
    private bool _registered;
    private readonly Action<TEvent> _handler;

    public ManagedEventBinding(Action<TEvent> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _binding = new EventBinding<TEvent>(_handler);
    }

    public void Register()
    {
        if (_registered) return;
        EventBus<TEvent>.Register(_binding);
        _registered = true;
    }

    public void Unregister()
    {
        if (!_registered) return;
        try { EventBus<TEvent>.Unregister(_binding); }
        catch { /* best-effort */ }
        _registered = false;
    }

    public void Dispose() => Unregister();
}
```

**Benefícios:**
- ✅ Padrão único e consistente
- ✅ Elimina duplicação em 5 arquivos
- ✅ Guard automático (_registered)
- ✅ Try-catch automático no unregister
- ✅ ~150 linhas de boilerplate removido

---

### **OTIMIZAÇÃO D: Refatorar GameLoopService (Split / capability-aligned)**

**Objetivo original:** quebrar o acoplamento entre transição de estado, side effects e contexto de postgame

**Estado atual:** 🟡 **Parcialmente concluída e validada**

**Arquivos extraídos / renomeados:**
- `Flow/GameLoopStateTransitionEffects.cs`
- `Flow/GameLoopPostGameSnapshotResolver.cs`

**Resultado obtido:**
- ✅ `GameLoopService` ficou mais focado como coordenador do loop
- ✅ side effects de transição saíram do service principal
- ✅ resolução de snapshot/contexto de postgame saiu do service principal
- ✅ a reorganização por capability e os renames principais não quebraram o runtime
- ✅ fluxo `Victory -> Restart -> Defeat -> ExitToMenu` foi validado em runtime sem regressão

**Leitura atualizada:**
O hotspot do `GameLoopService` foi reduzido, mas ainda não esgotado. O passo restante, se ainda houver ganho real, é revisar o que sobrou de helpers/lookups e decidir se vale continuar a extração.

---

### **OTIMIZAÇÃO E: Revisão concluída do par Outcome + Snapshot Services**

**Objetivo original:** Clarificar responsabilidades de `GameRunStateService` vs `GameRunOutcomeService`

**Estado atual:**
- `GameRunResultSnapshotService`: **consumer/projeção** de `GameRunEndedEvent`
- `GameRunOutcomeService`: **producer/owner terminal** de `GameRunEndedEvent`
- `GameRunPlayingStateGuard`: guard compartilhado para validação de gameplay ativo

**Resultado obtido:**
- ✅ Elimina a confusão principal de quem faz o quê
- ✅ Reduz listeners/guards duplicados no trilho principal
- ✅ Clarifica o fluxo de eventos
- ✅ Validado em runtime

## 📊 IMPACTO ESTIMADO

| Otimização | Redundância Removida | Complexidade | LOC Reduzidas |
|---|---|---|---|
| **A. Reason Formatting** | concluída, validada e realocada em `Core` | ↓ 30% | ~30 |
| **B. Validations periféricas** | checks restantes fora do guard principal | ↓ 15% | ~40 |
| **C. Event Binding Helper** | 5+ padrões duplicados | ↓ 40% | ~150 |
| **D. Split GameLoopService** | parcialmente reduzido + reorganização por capability validada | ↓ 35% | ~70 |
| **E. Outcome + Snapshot Services** | consolidado no trilho principal | ↓ 25% | ~50 |
| **TOTAL** | **7 pontos (com 4 já reduzidos)** | **↓ 41%** | **~260 LOC restantes** |

**Comparação com Navigation:**
- Navigation removeu ~290 LOC
- **GameLoop ainda pode remover ~260 LOC** (a estimativa caiu após `ReasonFormatter`, consolidação do par outcome/snapshot, extração de `GameLoopService` e reorganização por capability)
- GameLoop continua com oportunidades de otimização, mas em escopo mais localizado

---

## 🎯 PLANO DE IMPLEMENTAÇÃO

### **Fase 1: Reason Formatting (Baixo Risco)**
- ✅ Criar `GameLoopReasonFormatter.cs`
- ✅ Refatorar consumers principais do `GameLoop`
- ✅ Validar runtime do ciclo completo

**Tempo:** concluído
**Risco:** Muito Baixo
**Impacto:** ~30 LOC

---

### **Fase 2: Validations periféricas (Baixo-Médio Risco)**
- ⏳ Revisar bridges/controladores que ainda mantêm checks próprios
- ⏳ Reutilizar `GameRunPlayingStateGuard` onde fizer sentido
- ⏳ Evitar reespalhar regra de `Playing`
- ⏳ Testes de validação

**Tempo:** ~1 hora
**Risco:** Baixo-Médio
**Impacto:** ~40 LOC

### **Fase 3: Event Binding Helper (Médio Risco)**
- ⏳ Criar `ManagedEventBinding<T>` (ou melhorar Core)
- ⏳ Refatorar `GameRunStateService.cs`
- ⏳ Refatorar `GameRunOutcomeService.cs`
- ⏳ Refatorar `GamePlayEndConditionsController.cs`
- ⏳ Refatorar `GameLoopRunEndEventBridge.cs`
- ⏳ Testes de binding

**Tempo:** ~2 horas
**Risco:** Médio (toca lifecycle)
**Impacto:** ~150 LOC

---

### **Fase 4: Split GameLoopService (Alto Risco)**
- ✅ Extrair `GameLoopStateTransitionEffects.cs`
- ✅ Extrair `GameLoopPostGameSnapshotResolver.cs`
- ✅ Refatorar `GameLoopService.cs` para coordenar e delegar
- ✅ Reorganizar o módulo por capability (`Core`, `Run`, `Flow`, `Input`, `Bootstrap`)
- ✅ Validar integração completa em runtime
- ⏳ Avaliar se ainda vale nova extração do que restou no service

**Tempo:** parcialmente concluído
**Risco:** Alto (refatoração crítica)
**Impacto:** ~70 LOC já reduzidas no hotspot principal

---

### **Fase 5: Revisão fina pós-consolidação (Opcional)**
- ⏳ Só reabrir o par `Outcome + State` se aparecer nova evidência concreta
- ⏳ Priorizar agora `GameLoopService` e validators periféricos
- ⏳ Manter o wiring validado como baseline

**Tempo:** opcional
**Risco:** desnecessário no momento
**Impacto:** baixo

## 📚 COMPARAÇÃO COM NAVIGATION

| Aspecto | Navigation | GameLoop |
|---------|-----------|---------|
| **Total LOC** | ~1600 | ~2000 |
| **Redundâncias** | 4 maiores | 7 maiores |
| **LOC a Remover** | ~290 | ~530 |
| **Redução Estimada** | 18% | 26.5% |
| **Fases Recomendadas** | 4 fases | 5 fases |
| **Tempo Total** | ~9 horas | ~10.5 horas |
| **Risco Overall** | Médio | Médio-Alto |

---

## 🎓 APRENDIZADOS E DECISÕES

### Por que GameLoop tem mais redundâncias que Navigation?
- Mais serviços (4 vs 2)
- Mais bridges (5 vs 1)
- Mais padrões repetidos (event binding)
- Maior escopo (state machine + post-game + outcome)

### Por que não consolidar State + Outcome services?
- Têm responsabilidades distintas
- Separação é melhor que fusão
- Melhor refatorar listeners duplicados

### Por que priorizar por risco?
- Fase 1-2: Baixo risco, seguro fazer logo
- Fase 3: Médio risco, mas isolado
- Fase 4: Alto risco, deixar por último
- Fase 5: Opcional, validar primeiro

---

## ✅ STATUS GERAL

| Item | Status |
|---------|--------|
| Análise estrutural | ✅ Concluído |
| Identificação de redundâncias | ✅ Concluído (7 problemas) |
| Otimizações propostas | ✅ Concluído (5 soluções, 4 já reduzidas/validadas) |
| Impacto estimado | ✅ Recalibrado (~260 LOC remanescentes) |
| Plano de implementação | ✅ Detalhado e parcialmente executado |

---

## 🎯 CONCLUSÃO

O módulo GameLoop continua **bem arquitetado**, e quatro frentes já foram efetivamente reduzidas e validadas em runtime:

1. **Normalização de strings:** centralizada, validada e posicionada no `Core`
2. **Validações periféricas de estado:** ainda existem fora do guard principal
3. **Event binding:** 5+ padrões diferentes (com/sem guards)
4. **Par Outcome + Snapshot:** problema principal reduzido e validado em runtime
5. **Service/context resolution:** ainda existe em alguns pontos
6. **Tamanho:** `GameLoopService` foi reduzido, reorganizado por capability e continua como hotspot restante
7. **Logging:** boilerplate repetido em várias camadas

### Oportunidades de Otimização

As oportunidades restantes agora se concentram em:
- ✅ manter `ReasonFormatter` como baseline
- ✅ manter o trilho `OutcomeService(owner) + ResultSnapshotService(projeção)` como baseline validado
- ✅ manter a reorganização por capability como baseline estrutural
- ✅ revisar validators periféricos e boilerplate de event binding
- ✅ consolidar mais o que restou de `GameLoopService` apenas se houver ganho real

### Recomendação de Implementação

Repriorizar o plano em **5 Fases**, agora com o seguinte estado:
1. Reason Formatting - ✅ Concluído
2. Validations periféricas - ⏳ Ainda pode gerar ganho
3. Event Binding Helper - ⚠️ Médio
4. Split `GameLoopService` + reorganização por capability - 🟡 Parcialmente concluído e validado
5. Revisão fina pós-consolidação do par outcome/snapshot - ⚪ Opcional

**Tempo total estimado:** menor que o plano original
**Risco overall:** Médio
**Benefício remanescente:** reduzir boilerplate restante, revisar validators periféricos, avaliar helper de event binding e decidir com mais precisão se ainda vale continuar extrações do `GameLoopService`

---

**Relatório gerado:** 24 de março de 2026
**Próxima revisão:** Após eventual consolidação adicional de validators periféricos, event binding ou sincronização de docs canônicos do módulo
