# Diagnóstico de Saúde Arquitetural - GameJam2025
**Data**: 2026-04-24
**Escopo**: Análise de conformidade com ADRs 0056-0059 (Base 1.0, Baseline 4.0, ActorsSystem, Operational Binding)
**Status**: ⚠️ **Estrutura saudável com trilhas fantasmas críticas**

---

## Resumo Executivo

O sistema apresenta uma **estrutura modular bem pensada** com boundaries claros entre camadas semânticas, seam de integração e execução operacional. **MAS** existem **três trilhas fantasmas críticas** onde service locators e composição implícita criam acoplamento oculto que viola os ADRs.

| Aspecto | Status | Observação |
|---------|--------|-----------|
| Baseline técnico/macro (ADR-0056) | ✅ Saudável | Enxuto, sem absorção de semântica |
| Boundaries entre camadas (ADR-0057) | ⚠️ Comprometido | Service locators em hot paths principais |
| Ownership do ActorsSystem (ADR-0058) | ✅ Saudável | Contracts bem definidos, separação mantida |
| Operational Binding (ADR-0059) | ✅ Saudável | Boundary explícito entre semântica/runtime |

---

## 1. TRILHAS FANTASMAS ENCONTRADAS

### 1.1 🚨 Service Locator em `SessionFlowActorsSemanticPortsAdapter`

**Localização**: `ActorsSystem/Integration/SessionFlow/SessionFlowActorsSemanticPortsAdapter.cs:122`

**Problema**:
```csharp
// ❌ Violação: Service locator no hot path de derivação de definições de atores
if (!_dependencyProvider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out routeActorSetContext) || routeActorSetContext == null)
{
    return false;
}

if (!_dependencyProvider.TryGetGlobal<IActorSetSelectionService>(out actorSetSelectionService) || actorSetSelectionService == null)
{
    return false;
}
```

**Violação**:
- ADR-0057 §15 proíbe explicitamente service locator no hot path
- O adapter deveria receber essas dependências injetadas no constructor
- Criar ambiguidade: parece que há composição explícita em `SceneFlowBootstrap.EnsureRouteActorSetRefContext()`, mas a verdadeira resolução é implícita aqui

**Impacto**:
- Se `ISceneFlowRouteActorSetRefContext` não foi registrado no DI, falha silenciosa ou exceção tardia
- Acoplamento oculto: `SessionFlowActorsSemanticPortsAdapter` depende de quem compôs o bootstrap
- Testabilidade reduzida: impossível injetar mocks sem registrar globalmente

**Remediação Necessária**: Injetar ambas as dependências no constructor

---

### 1.2 🚨 Composição Implícita com `ResolveRequired` em `GameplaySessionFlowCompletionGateComposer`

**Localização**: `SessionFlow/Integration/SceneFlow/GameplaySessionFlowCompletionGateComposer.cs:44-56`

**Problema**:
```csharp
// ❌ Composição implícita: não está claro quem compôs IGameplaySessionFlowPrepareOperationalHandoffService
private static IGameplaySessionFlowPrepareOperationalHandoffService ResolveRequiredPrepareHandoffService()
{
    if (!DependencyManager.Provider.TryGetGlobal<IGameplaySessionFlowPrepareOperationalHandoffService>(out var handoffService) || handoffService == null)
    {
        throw new InvalidOperationException("[FATAL]...");
    }
    return handoffService;
}
```

**Contexto**: Este composer é chamado durante GameLoop bootstrap, e assume que o handoff service já foi registrado. Mas **não há forma visível de saber onde ele foi registrado**.

**Violação**:
- ADR-0056: Baseline não deve absorver costura semântica
- ADR-0057 §15: Composição deve ser explícita, não implícita via service locator
- Estrutura de composição original ilegível: pareça que está tudo no `SceneFlowBootstrap` mas não está

**Impacto**:
- Código defensivo: obrigado a lancar exceção para descobrir que falhou
- Debugging dificultado: stack trace longo até descobrir onde a dependência deveria estar
- Risco de falha em tempo de run (não de compile)

**Remediação Necessária**: Passar explicitamente `IGameplaySessionFlowPrepareOperationalHandoffService` para o composer

---

### 1.3 🚨 Injeção de Dependência em Awake (MonoBehaviour) em `GameRunEndedEventBridge`

**Localização**: `SessionFlow/Integration/RunReset/GameRunEndedEventBridge.cs:31-38`

**Problema**:
```csharp
// ❌ Composição implícita em Awake(): anti-padrão em Unity com DI explícito
private void Awake()
{
    _runEndMaterializationService = ResolveRequired<IRunEndMaterializationService>(
        "[FATAL][Config][GameplaySessionFlow] IRunEndMaterializationService ausente...");
    _runContinuationSelectionRoutingService = ResolveRequired<IRunContinuationSelectionRoutingService>(...);
    _runContinuationOwnershipService = ResolveRequired<IRunContinuationOwnershipService>(...);
    // ...
}
```

**Violação**:
- MonoBehaviours devem receber dependências via constructor ou campo injetado, não em Awake
- Contradiz padrão de composição explícita usado no resto do projeto
- Cria fragilidade: se o DI global não tem o serviço, falha em Awake (impossível de catchear)

**Impacto**:
- Frágil: uma falha de inicialização de DI torna o GameObject inutilizável
- Difícil de testar: Awake não é chamado explicitamente em testes
- Ambiguidade: onde as dependências foram compostas? Não está claro
- Risco de ordem de inicialização: depende que SceneFlow bootstrap tenha rodado antes

**Remediação Necessária**:
- Passar dependências via injeção ou factory
- Remover resolução implícita do Awake

---

### 1.4 ⚠️ Composição Condicional em `RunEndBridgeRuntimeComposer`

**Localização**: `SessionFlow/Integration/RunReset/Installers/RunEndBridgeRuntimeComposer.cs:44-89`

**Padrão**:
```csharp
// ⚠️ Padrão de composição implícita via ResolveRequired
var service = new RunContinuationSelectionRoutingService(
    ResolveRequired<IRunContinuationOperationalHandoffService>(...),
    ResolveRequired<IGameplaySessionRunResetService>(...),
    ResolveRequired<IRunResetTargetPhaseResolver>(...));
```

**Problema**:
- Não é injeção de dependência, é service locator encapsulado
- Depende fortemente da ordem de registro do DI
- Impossível configurar diferentes implementações sem editar o composer

**Violação**:
- ADR-0057: Composição deve ser explícita e clara, não implícita

**Impacto**: Risco de cascata de falhas se alguma dependência não foi registrada

**Nota**: Menos crítico que os anteriores porque está em um Installer (composição), não em hot path

---

## 2. ÁREAS SAUDÁVEIS

### 2.1 ✅ Baseline Técnico/Macro (`SceneFlow`) - ADR-0056

**Conformidade**: **Excelente**

- ✅ `SceneFlowBootstrap` apenas compõe infraestrutura técnica (SceneTransitionService, Loading/Fade, InputModeBridge, RouteActorSetRef)
- ✅ Sem absorção de semântica de gameplay
- ✅ Sem ownership de participation ou phase selection
- ✅ Transições macro bem separadas de decisões semânticas

**Observação**: O bootstrap está **limpo de bloat semântico**. Cumpre o papel de executor técnico fino.

---

### 2.2 ✅ Ownership do ActorsSystem - ADR-0058

**Conformidade**: **Muito Bom**

**Saudável**:
- ✅ `ActorsEnsembleService` implementa policy de identity/role/relevance corretamente
- ✅ `IActorsDefinitionsPort` define boundary semântico de entrada
- ✅ `IActorsSemanticParticipationInPort` separa semântica de execution
- ✅ Não há scan global de prefabs ou runtime discovery
- ✅ `ActorSpec` congelado como contrato (ADR-0058 §18)

**Não encontrado**:
- ✅ Sem `WorldDefinition` como fonte canônica (correto, per ADR-0058)
- ✅ Sem `PlayerInput` como registry de atores

---

### 2.3 ✅ Operational Binding Explícito - ADR-0059

**Conformidade**: **Excelente**

```csharp
// Boundary explícito maintido
public sealed class ActorsOperationalBindingService :
    IActorsOperationalBindingInPort,
    IActorsOperationalBindingQueryPort
{
    private readonly Dictionary<AxisActorId, ActorsOperationalBindingEntry> _byAxisActor;
    private readonly Dictionary<string, AxisActorId> _participantIndex;
    private readonly Dictionary<RuntimeActorId, AxisActorId> _runtimeIndex;
```

**Observações**:
- ✅ Separa explicitamente `AxisActorId`, `ParticipantId` e `RuntimeActorId`
- ✅ Não há colisão entre semântica e handlers operacionais
- ✅ IDs da Unity (`playerIndex`, `InputUser.id`) não vistos em código
- ✅ `participantIndex` e `runtimeIndex` como projeções, não como fonte canônica

---

### 2.4 ✅ Contracts e Ports Bem Definidos

**Ports de Entrada (Inbound)**:
- ✅ `IActorsSemanticParticipationInPort` - semântica de participação
- ✅ `IActorsDefinitionsPort` - definições canônicas
- ✅ `IActorsOperationalBindingInPort` - atualização de binding

**Contracts de Saída (Outbound)**:
- ✅ `IActorsOperationalBindingQueryPort` - leitura de binding
- ✅ Snapshot-based, não stateful command pattern

---

## 3. PROBLEMAS DE ESTRUTURA FRACA (NÃO BLOCKER, MAS ATRITO)

### 3.1 ⚠️ Falta de Documentação Explícita de Composição

**Problema**: Os ADRs definem ownership, mas **não há documento explicit que mapa onde cada serviço é composto**.

**Exemplo**:
- `IGameplaySessionFlowPrepareOperationalHandoffService` é resolvido por `ResolveRequired` em `GameplaySessionFlowCompletionGateComposer`
- Mas **onde** é registrado? Não há comentário, não há ADR mencionando isso.
- O leitor tem que `grep` para encontrar

**Impacto**:
- Triagem manual de composição é cara
- Risco de serviço "órfão" que ninguém sabe que precisa estar registrado

**Recomendação**:
- Criar arquivo `COMPOSIÇÃO-MAP.md` que lista onde cada serviço crítico é registrado
- Ou adicionar comentários em StyleGuide que exigem documentação inline

---

### 3.2 ⚠️ Padrão de Composição Condicional Inconsistente

**Problema**: Dois padrões contraditórios convivem:

1. **Padrão "Ensure" (em SceneFlowBootstrap)**:
```csharp
private static void EnsureSceneTransitionService()
{
    if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var existing) && existing != null)
    {
        return;
    }
    // ... compõe se não existir
}
```

2. **Padrão "Resolve" (em SessionFlow adapters)**:
```csharp
if (!_dependencyProvider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out routeActorSetContext))
{
    return false; // Falha silenciosa ou exceção
}
```

**Problema**: Inconsistência cria confusão:
- "Ensure" presume que o serviço pode não estar sempre presente (resolvido)
- "Resolve" presume que deve estar sempre presente (exceção ou early return)
- Leitor não sabe qual expectativa é correta

**Impacto**: Difícil definer e manter convenções de composição

---

### 3.3 ⚠️ Integração entre `SceneFlow` e `ActorsSystem` Acoplada

**Padrão Atual**:
```
SceneFlow (baseline)
  ├─ ComposeRuntime()
  └─ EnsureRouteActorSetRefContext()
       └─ SceneFlowRouteActorSetRefService registra globalmente

SessionFlow/Integration (seam)
  └─ SessionFlowActorsSemanticPortsAdapter
       └─ TryGetGlobal<ISceneFlowRouteActorSetRefContext>()  // resolve aqui!
```

**Problema**:
- `ISceneFlowRouteActorSetRefContext` é composta no baseline (`SceneFlow`)
- Mas consumida no seam de integração (`SessionFlow/Integration`)
- O seam não recebe explicitamente esta dependência, resolve via DI global

**Violação**: ADR-0057 prescreve que o seam deve receber dependências explicitamente, não via service locator

**Risco**: Se `SceneFlowBootstrap.EnsureRouteActorSetRefContext()` não for chamado antes de `SessionFlowActorsSemanticPortsAdapter.TryGetCurrent()`, há falha silenciosa

---

## 4. DIAGNÓSTICO DE OWNERSHIP (Base 1.0 - ADR-0057)

### Mapa Atual de Ownership

| Papel | Proprietário | Status | Observações |
|-------|-------------|--------|--------------|
| **Semantica: Gameplay Session** | `GameplaySessionFlow` (SessionFlow/Semantic) | ✅ Saudável | Ownership claro, não confundido com baseline |
| **Semantica: Participation** | `GameplayParticipationFlowService` | ✅ Saudável | Port bem definido |
| **Semantica: Actors Ensemble** | `ActorsEnsembleService` | ✅ Saudável | Policy de identity/role clara |
| **Seam: Translação Session→Op** | `SessionFlowActorsSemanticPortsAdapter` + contexto | ⚠️ Acoplado | Service locator oculto |
| **Seam: Tradução Run→PostRun** | `GameRunEndedEventBridge` | ⚠️ Frágil | Composição em Awake |
| **Seam: Route→ActorSet** | `SceneFlowRouteActorSetRefService` | ✅ Saudável | Boundary explícito |
| **Baseline: Scene Transition** | `SceneFlow` (SceneFlowBootstrap) | ✅ Saudável | Apenas técnico |
| **Baseline: Loading/Fade** | `SceneFlow` (SceneFlowBootstrap) | ✅ Saudável | Apenas técnico |
| **Baseline: Input Mode Bridge** | `SceneFlow` (SceneFlowBootstrap) | ✅ Saudável | Apenas técnico |
| **Executor: Actor Binding** | `ActorsOperationalBindingService` | ✅ Saudável | Não reclama propriedade semântica |

### Desvios de Ownership

1. **`SessionFlowActorsSemanticPortsAdapter` → Service Locator**
   - Deveria receber context explicitamente
   - Está "roubando" resoluções do DI global

2. **`GameRunEndedEventBridge` → Awake Composition**
   - Deveria receber dependências via composição
   - Está fazendo resoluções em runtime

3. **`RunEndBridgeRuntimeComposer` → Implicit chaining**
   - Cria serviços resolvendo dependências implicitamente
   - Deveria receber todas as dependências já compostas

---

## 5. RECOMENDAÇÕES DE REMEDIAÇÃO

### 🔴 CRÍTICO (Quebra conformidade com ADRs)

#### 5.1 Remover Service Locator de `SessionFlowActorsSemanticPortsAdapter`

**Arquivo**: `ActorsSystem/Integration/SessionFlow/SessionFlowActorsSemanticPortsAdapter.cs`

**Ação**:
```csharp
// Antes ❌
public SessionFlowActorsSemanticPortsAdapter(
    IGameplayParticipationFlowService participationFlowService,
    IDependencyProvider dependencyProvider)
{
    _participationFlowService = participationFlowService;
    _dependencyProvider = dependencyProvider; // Service locator!
}

// Depois ✅
public SessionFlowActorsSemanticPortsAdapter(
    IGameplayParticipationFlowService participationFlowService,
    ISceneFlowRouteActorSetRefContext routeActorSetContext,
    IActorSetSelectionService actorSetSelectionService)
{
    _participationFlowService = participationFlowService;
    _routeActorSetContext = routeActorSetContext;
    _actorSetSelectionService = actorSetSelectionService;
}
```

**Impacto**:
- ✅ Torna dependency injection explícita
- ✅ Falha em compile-time se dependência falta, não em runtime
- ⚠️ Exige mudar quem compõe este adapter

---

#### 5.2 Remover Composição Implícita de `GameplaySessionFlowCompletionGateComposer`

**Arquivo**: `SessionFlow/Integration/SceneFlow/GameplaySessionFlowCompletionGateComposer.cs`

**Ação**:
```csharp
// Antes ❌
public static void ComposeOrValidate()
{
    var composedGate = new GameplaySessionFlowCompletionGate(fallbackGate);
    IGameplaySessionFlowPrepareOperationalHandoffService handoffService = ResolveRequiredPrepareHandoffService();
    composedGate.ConfigureGameplaySessionFlowGate(new GameplaySessionFlowPrepareCompletionGate(handoffService));
}

// Depois ✅
public static void ComposeOrValidate(IGameplaySessionFlowPrepareOperationalHandoffService handoffService)
{
    var composedGate = new GameplaySessionFlowCompletionGate(fallbackGate);
    composedGate.ConfigureGameplaySessionFlowGate(new GameplaySessionFlowPrepareCompletionGate(handoffService));
}
```

**Chamador**: Quem chama `ComposeOrValidate()` deve passar a dependência

**Impacto**:
- ✅ Torna pipeline de composição explícita
- ⚠️ Exige rastrear onde `handoffService` é composto

---

#### 5.3 Remover Injeção em Awake de `GameRunEndedEventBridge`

**Arquivo**: `SessionFlow/Integration/RunReset/GameRunEndedEventBridge.cs`

**Ação**:
```csharp
// Antes ❌
private void Awake()
{
    _runEndMaterializationService = ResolveRequired<IRunEndMaterializationService>(...);
    _runContinuationSelectionRoutingService = ResolveRequired<IRunContinuationSelectionRoutingService>(...);
    _runContinuationOwnershipService = ResolveRequired<IRunContinuationOwnershipService>(...);
}

// Depois ✅
public void Initialize(
    IRunEndMaterializationService runEndMaterializationService,
    IRunContinuationSelectionRoutingService runContinuationSelectionRoutingService,
    IRunContinuationOwnershipService runContinuationOwnershipService)
{
    _runEndMaterializationService = runEndMaterializationService;
    _runContinuationSelectionRoutingService = runContinuationSelectionRoutingService;
    _runContinuationOwnershipService = runContinuationOwnershipService;
    RegisterBinding();
}
```

**Composição**: Adicionar em bootstrap ou via factory

**Impacto**:
- ✅ Falha em compose-time se dependência falta
- ✅ Testável explicitamente
- ⚠️ Exige adicionar chamada de Initialize após FindObjectOfType

---

### 🟡 IMPORTANTE (Melhora clareza, previne drift)

#### 5.4 Criar Documento de Mapa de Composição

**Arquivo**: `Assets/_ImmersiveGames/NewScripts/Docs/COMPOSIÇÃO-MAPA.md`

**Conteúdo**: Tabela com:
- Serviço
- Onde é composto (arquivo, função)
- Dependências que precisa já estar registrado
- Quando é composto (bootstrap phase)

**Exemplo**:
| Serviço | Composto em | Dependências Prévias | Fase |
|---------|------------|-------------------|------|
| `ISceneTransitionService` | `SceneFlowBootstrap.EnsureSceneTransitionService()` | `INavigationPolicy`, `IRouteGuard`, `IRouteResetPolicy` | SceneFlow Runtime |
| `IGameplaySessionFlowPrepareOperationalHandoffService` | `SessionIntegration.Initialize()` | Nenhuma (input) | Session Integration Setup |

---

#### 5.5 Padronizar Padrão de Composição Condicional

**Recomendação**: Usar **Ensure** pattern globalmente para serviços opcionais, **Resolve** para obrigatórios

**Guideline**:
```csharp
// Para serviços OBRIGATÓRIOS (devem estar lá sempre)
private static T ResolveRequired<T>(string errorMessage) where T : class
{
    if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
        throw new InvalidOperationException(errorMessage);
    return service;
}

// Para serviços OPCIONAIS (podem não estar)
private static void EnsureOptional<T>(Action<T> ifMissing) where T : class
{
    if (DependencyManager.Provider.TryGetGlobal<T>(out var service) && service != null)
        return;
    ifMissing?.Invoke(null);
}
```

---

#### 5.6 Documentar Pipeline de Composição Explicitamente

**Localização**: `SessionFlow/Integration/README.md` ou novo arquivo `COMPOSIÇÃO-PHASES.md`

**Conteúdo**: Explicitar ordem de composição:
1. **SceneFlowBootstrap**: boot, scene macro, loading/fade
2. **SessionFlowBootstrap** (quando criado): session semantic
3. **SessionFlow/Integration**: seams e bridges
4. **ActorsSystem bootstrap**: actor ensemble, definitions
5. **GameLoop bootstrap**: run lifecycle

---

### 🟢 BOM TER (Robustez, documentation)

#### 5.7 Adicionar Testes de Composição

**Tipo**: Integration tests que verificam:
- ✅ Todas as dependências declaradas em `ResolveRequired` estão registradas
- ✅ Nenhum serviço é composto em ordem errada
- ✅ Nenhum serviço é composto mais de uma vez

**Exemplo**:
```csharp
[Test]
public void SessionFlowCompletionGateComposer_RequiredDependencies_AreRegistered()
{
    // Setup: simular SceneFlowBootstrap
    var provider = new DependencyProvider();
    DependencyManager.Initialize(provider);
    // ...

    // Act
    GameplaySessionFlowCompletionGateComposer.ComposeOrValidate();

    // Assert
    Assert.IsNotNull(provider.TryGetGlobal<ISceneTransitionCompletionGate>());
}
```

---

## 6. RESUMO EXECUTIVO DE AÇÕES

### Imediato (Sprint atual)
- [ ] **Remover service locator de `SessionFlowActorsSemanticPortsAdapter`** - CRÍTICO
- [ ] **Remover composition implícita de `GameplaySessionFlowCompletionGateComposer`** - CRÍTICO
- [ ] **Remover injeção em Awake de `GameRunEndedEventBridge`** - CRÍTICO

### Curto prazo (Próximos 2 sprints)
- [ ] Criar documento de mapa de composição
- [ ] Padronizar padrão de composição condicional
- [ ] Adicionar testes de integracao de composição

### Médio prazo (Quando houver tempo)
- [ ] Refatorar `RunEndBridgeRuntimeComposer` para ser mais explícito
- [ ] Adicionar validação de composição no editor (EditorMenuItems)

---

## 7. ANÁLISE POR ADR

### ADR-0056: Baseline 4.0 como Executor Técnico Fino
**Status**: ✅ **CONFORME**
- ✅ SceneFlowBootstrap é fino, não absorve semântica
- ✅ Sem ownership de participation, phase, ou session
- ✅ Sem bootstrap bloat

### ADR-0057: Base 1.0 como Leitura Sistemica
**Status**: ⚠️ **PARCIALMENTE CONFORME**
- ✅ Estrutura semântica acima, baseline abaixo, executores embaixo
- ⚠️ Seam não recebe dependências sempre explicitamente
- ❌ Service locators em hot paths (viola §15)

### ADR-0058: ActorsSystem como Owner Semântico
**Status**: ✅ **CONFORME**
- ✅ Ownership do conjunto claro
- ✅ Sem Spawn como pseudo-owner
- ✅ ActorSpec congelado
- ✅ Sem WorldDefinition como fonte canônica

### ADR-0059: Operational Binding Explícito
**Status**: ✅ **CONFORME**
- ✅ IDs semânticos vs operacionais bem separado
- ✅ Binding não legitima actor
- ✅ Sem descoberta implícita via runtime

---

## Conclusão

**O sistema é arquiteturalmente saudável em termos de conceitos e boundaries.** As trilhas fantasmas são **técnicas, não conceituais**: service locators e composição implícita que violam os ADRs mas não impedem o funcionamento.

**Recomendação**: Corrigir as 3 trilhas fantasmas críticas para tornar o sistema 100% conforme aos ADRs. Estas correções são **refatorações de baixo risco** que não impactam lógica de negócio, apenas clareza/estrutura.


