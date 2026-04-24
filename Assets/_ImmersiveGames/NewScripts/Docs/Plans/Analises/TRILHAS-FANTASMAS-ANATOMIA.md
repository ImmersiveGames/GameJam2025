# TRILHAS FANTASMAS - Análise Detalhada
**Data**: 2026-04-24
**Propósito**: Documentar aontomia, causa-raiz e remediação de cada trilha fantasma encontrada

---

## O que é uma "Trilha Fantasma"?

Uma trilha fantasma é um **atalho implícito** que:
1. Viola boundaries arquiteturais prescritos (ADRs)
2. **Esconde** estrutura fraca ou falta de composição explícita
3. Funciona "por acaso" porque dependências foram registradas em outro lugar
4. É difícil de rastrear e auditar
5. Cria risco de falha em composição diferente ou refatoração

**Característica**: Pareça que a arquitetura está certa (há contracts, ports, separação), mas **há um atalho oculto** que a sustenta.

---

## Trilha Fantasma #1: Service Locator em `SessionFlowActorsSemanticPortsAdapter`

### Localização
```
File: Assets/_ImmersiveGames/NewScripts/ActorsSystem/Integration/SessionFlow/SessionFlowActorsSemanticPortsAdapter.cs
Lines: 61-65, 115-133
```

### Código Problemático

```csharp
public bool TryGetCurrent(out ActorsDefinitionsSnapshot snapshot)
{
    // ... setup ...

    // ❌ TRILHA FANTASMA AQUI
    if (!TryResolveDefinitionsDependencies(out ISceneFlowRouteActorSetRefContext routeActorSetContext, out IActorSetSelectionService actorSetSelectionService))
    {
        throw new InvalidOperationException(
            "[FATAL][Config][ActorsSystem] Missing canonical dependencies for definitions projection (ISceneFlowRouteActorSetRefContext/IActorSetSelectionService).");
    }

    // ... continua ...
}

private bool TryResolveDefinitionsDependencies(
    out ISceneFlowRouteActorSetRefContext routeActorSetContext,
    out IActorSetSelectionService actorSetSelectionService)
{
    routeActorSetContext = null;
    actorSetSelectionService = null;

    // ❌ SERVICE LOCATOR: Resolve via DependencyManager global
    if (!_dependencyProvider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out routeActorSetContext) || routeActorSetContext == null)
    {
        return false;
    }

    // ❌ SERVICE LOCATOR: Resolve via DependencyManager global
    if (!_dependencyProvider.TryGetGlobal<IActorSetSelectionService>(out actorSetSelectionService) || actorSetSelectionService == null)
    {
        return false;
    }

    return true;
}
```

### Por que é uma Trilha Fantasma?

1. **Pareça Explícito**: O adapter tem um constructor que recebe `IDependencyProvider`:
   ```csharp
   public SessionFlowActorsSemanticPortsAdapter(
       IGameplayParticipationFlowService participationFlowService,
       IDependencyProvider dependencyProvider)  // ← Pareça injeção, mas é service locator!
   {
       _dependencyProvider = dependencyProvider;
   }
   ```

2. **Resolve Oculto**: Dentro de `TryGetCurrent()` (método crítico de derivação de definições), há `TryGetGlobal` que:
   - Depende de composição prévia invisível
   - Se falhar, lança exceção tardia
   - Não há forma visível de saber onde essas dependências deveriam estar

3. **Composição Distribuída**: As dependências são compostas em:
   - `SceneFlowBootstrap.EnsureRouteActorSetRefContext()` ← para `ISceneFlowRouteActorSetRefContext`
   - ?????? ← para `IActorSetSelectionService` (não encontrado no primeiro `grep`!)

   -> Criar ambiguidade sobre quem é responsável

4. **Hot Path**: Método `TryGetCurrent()` é chamado durante gameplay session flow setup:
   ```
   SessionFlow.Semantic
     ↓
   SessionFlowActorsSemanticPortsAdapter.TryGetCurrent()  ← Hot path!
     ↓
   ActorsSystem.RefreshDefinitions()
     ↓
   Spawn/ActorRegistry update
   ```

### Causa Raiz

**Evolução Histórica Probable**:
1. Inicialmente: `ISceneFlowRouteActorSetRefContext` era passado explicitamente
2. Refatoração: Mudaram para "passar todo o DI" (`IDependencyProvider`)
3. Conveniência: Ao invés de updatear constructor para injetar, apenas "resolvem no método"
4. Resultado: Ficou escondido porque o método recebe `IDependencyProvider`

### Impacto

- ❌ **Falha em Runtime**: Se `SceneFlowBootstrap` não roda (e.g., broken scene), Actor definitions falham
- ❌ **Difícil Debugar**: Stack trace é longo, erro acontece em `TryGetCurrent()` não em bootstrap
- ❌ **Testabilidade**: Testa `SessionFlowActorsSemanticPortsAdapter` requer registrar DI global
- ❌ **Refatoração Frágil**: Se alguém refatora `SceneFlowBootstrap`, tudo quebra silenciosamente
- ❌ **Violação ADR-0057 §15**: Explicitamente proíbe service locator em hot path

### Remediação

#### Passo 1: Injetar Dependências no Constructor

```csharp
// Antes ❌
public SessionFlowActorsSemanticPortsAdapter(
    IGameplayParticipationFlowService participationFlowService,
    IDependencyProvider dependencyProvider)
{
    _participationFlowService = participationFlowService;
    _dependencyProvider = dependencyProvider; // ← Service locator!
}

// Depois ✅
public SessionFlowActorsSemanticPortsAdapter(
    IGameplayParticipationFlowService participationFlowService,
    ISceneFlowRouteActorSetRefContext routeActorSetContext,          // ← Explícito
    IActorSetSelectionService actorSetSelectionService)              // ← Explícito
{
    _participationFlowService = participationFlowService;
    _routeActorSetContext = routeActorSetContext;
    _actorSetSelectionService = actorSetSelectionService;
}
```

#### Passo 2: Remover TryResolveDefinitionsDependencies

```csharp
// Antes ❌
public bool TryGetCurrent(out ActorsDefinitionsSnapshot snapshot)
{
    // ... setup ...

    if (!TryResolveDefinitionsDependencies(out var routeActorSetContext, out var actorSetSelectionService))
    {
        throw new InvalidOperationException(...);
    }

    // ... usa routeActorSetContext, actorSetSelectionService ...
}

// Depois ✅
public bool TryGetCurrent(out ActorsDefinitionsSnapshot snapshot)
{
    // ... setup ...

    // Usa diretamente as dependências injetadas
    if (!_routeActorSetContext.TryGetCurrent(out var actorSetRef, out var routeKind, out var source))
    {
        // ... handle ...
    }

    if (!_actorSetSelectionService.TryResolve(actorSetRef, out var selection))
    {
        // ... handle ...
    }

    // ... continua ...
}
```

#### Passo 3: Atualizar Composição

Onde `SessionFlowActorsSemanticPortsAdapter` é criado, passa as dependências:

```csharp
// Antes ❌
var adapter = new SessionFlowActorsSemanticPortsAdapter(
    participationFlowService,
    DependencyManager.Provider);  // ← Service locator!

// Depois ✅
var routeActorSetContext = DependencyManager.Provider.GetGlobal<ISceneFlowRouteActorSetRefContext>();
var actorSetSelectionService = DependencyManager.Provider.GetGlobal<IActorSetSelectionService>();

var adapter = new SessionFlowActorsSemanticPortsAdapter(
    participationFlowService,
    routeActorSetContext,         // ← Explícito
    actorSetSelectionService);    // ← Explícito
```

#### Passo 4: Listar Dependências Críticas

Adicionar comentário em `SessionFlowActorsSemanticPortsAdapter`:

```csharp
/// <summary>
/// Canonical adapter: projects session semantic participation plus route actor set
/// into ActorsSystem inbound contracts.
///
/// DEPENDÊNCIAS CRÍTICAS (must be registered via DI before instantiation):
/// - IGameplayParticipationFlowService
/// - ISceneFlowRouteActorSetRefContext  (composed in SceneFlowBootstrap.EnsureRouteActorSetRefContext)
/// - IActorSetSelectionService          (composed in ??? - verify!)
/// </summary>
public sealed class SessionFlowActorsSemanticPortsAdapter ...
```

### Teste Pós-Remediação

```csharp
[Test]
public void SessionFlowActorsSemanticPortsAdapter_RequiresDependenciesExplicitly()
{
    // Arrange
    var participationFlow = Substitute.For<IGameplayParticipationFlowService>();
    var routeContext = Substitute.For<ISceneFlowRouteActorSetRefContext>();
    var selectionService = Substitute.For<IActorSetSelectionService>();

    // Act - Adapter recebe tudo explicitamente
    var adapter = new SessionFlowActorsSemanticPortsAdapter(
        participationFlow,
        routeContext,
        selectionService);

    // Assert - Nenhum DependencyManager.Provider
    var sourceCode = File.ReadAllText("SessionFlowActorsSemanticPortsAdapter.cs");
    Assert.That(sourceCode, Does.Not.Contain("DependencyManager.Provider.TryGetGlobal"));
}
```

---

## Trilha Fantasma #2: Composição Implícita em `GameplaySessionFlowCompletionGateComposer`

### Localização
```
File: Assets/_ImmersiveGames/NewScripts/SessionFlow/Integration/SceneFlow/GameplaySessionFlowCompletionGateComposer.cs
Lines: 44-56
```

### Código Problemático

```csharp
public static void ComposeOrValidate()
{
    // ... validação ...

    var fallbackGate = new WorldResetCompletionGate(timeoutMs: 20000);
    var composedGate = new GameplaySessionFlowCompletionGate(fallbackGate);

    // ❌ TRILHA FANTASMA AQUI
    IGameplaySessionFlowPrepareOperationalHandoffService handoffService = ResolveRequiredPrepareHandoffService();
    composedGate.ConfigureGameplaySessionFlowGate(new GameplaySessionFlowPrepareCompletionGate(handoffService));

    DependencyManager.Provider.RegisterGlobal<ISceneTransitionCompletionGate>(composedGate, allowOverride: true);
}

private static IGameplaySessionFlowPrepareOperationalHandoffService ResolveRequiredPrepareHandoffService()
{
    if (DependencyManager.Provider == null)
    {
        throw new InvalidOperationException("[FATAL][Config][SessionIntegration] DependencyManager.Provider indisponivel...");
    }

    // ❌ SERVICE LOCATOR: Resolve sem saber donde vem
    if (!DependencyManager.Provider.TryGetGlobal<IGameplaySessionFlowPrepareOperationalHandoffService>(out var handoffService) || handoffService == null)
    {
        throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameplaySessionFlowPrepareOperationalHandoffService ausente...");
    }

    return handoffService;
}
```

### Por que é uma Trilha Fantasma?

1. **Pseudonimizado**: Método estático chamado `ResolveRequiredPrepareHandoffService()` pareça que é "buscar algo que já foi preparado", mas:
   - Não há indício de **onde** foi preparado
   - `grep` procurando "IGameplaySessionFlowPrepareOperationalHandoffService" não mostra quem registra

2. **Pareça Validação, é Composição**: Nome `ComposeOrValidate()` implica:
   - "Se já existe, valida"
   - "Se não existe, cria"
   - Mas na verdade é: "Se não existe, falha com exceção"

3. **Ponto de Falha Oculto**: O serviço deve estar registrado **antes** de chamar `ComposeOrValidate()`:
   - Mas **não há comentário explícito** dizendo isso
   - Se falhar, erro é lançado em `ComposeOrValidate()` que é chamado de um bootstrap qualquer
   - Stack trace é confuso

4. **Composição Distribuída Invisível**:
   - `IGameplaySessionFlowPrepareOperationalHandoffService` é registrado em ??? (não encontrado!)
   - Ou é assumido estar no DI já (implicit composition)
   - Ou é um legacy de refatoração anterior

### Causa Raiz

**Padrão de Copy-Paste**:
1. `SceneFlowBootstrap` usa padrão "Ensure" bem (resolve se não existe)
2. Adapta-se "ComposeOrValidate" que presume existência
3. Sem adicionar dependência explícita como parâmetro

### Impacto

- ❌ **Quebra Silenciosa**: Se bootstrap order mudar, composição falha
- ❌ **Ambiguidade**: Não está claro quem deve registrar `IGameplaySessionFlowPrepareOperationalHandoffService`
- ❌ **Acoplamento**: Compositor depende de composição anterior invisível
- ❌ **Violação ADR-0057 §15**: Composição implícita em seam

### Remediação

#### Passo 1: Receber Dependência Explicitamente

```csharp
// Antes ❌
public static void ComposeOrValidate()
{
    // ...
    IGameplaySessionFlowPrepareOperationalHandoffService handoffService = ResolveRequiredPrepareHandoffService();
    // ...
}

// Depois ✅
public static void ComposeOrValidate(IGameplaySessionFlowPrepareOperationalHandoffService handoffService)
{
    if (handoffService == null)
        throw new ArgumentNullException(nameof(handoffService));

    // ... usa handoffService ...
}
```

#### Passo 2: Remover ResolveRequired

```csharp
// Antes ❌
private static IGameplaySessionFlowPrepareOperationalHandoffService ResolveRequiredPrepareHandoffService()
{
    if (!DependencyManager.Provider.TryGetGlobal<IGameplaySessionFlowPrepareOperationalHandoffService>(out var handoffService) || handoffService == null)
    {
        throw new InvalidOperationException(...);
    }
    return handoffService;
}

// Depois ✅
// [DELETADO - não precisa mais]
```

#### Passo 3: Documentar Dependência

```csharp
/// <summary>
/// Compõe e registra GameplaySessionFlowCompletionGate como canonical completion gate.
///
/// DEPENDÊNCIA CRÍTICA:
/// - handoffService deve ser registrado previamente (vem de GameplaySessionFlowPrepareOperationalHandoffServiceComposer)
/// </summary>
public static void ComposeOrValidate(IGameplaySessionFlowPrepareOperationalHandoffService handoffService)
{
    // ...
}
```

#### Passo 4: Atualizar Chamador

Onde `ComposeOrValidate()` é chamado, passar a dependência:

```csharp
// Localizar em: GameLoop/SessionFlow bootstrap ou initialization

// Antes ❌
GameplaySessionFlowCompletionGateComposer.ComposeOrValidate();

// Depois ✅
var handoffService = DependencyManager.Provider.GetGlobal<IGameplaySessionFlowPrepareOperationalHandoffService>();
GameplaySessionFlowCompletionGateComposer.ComposeOrValidate(handoffService);
```

### Teste Pós-Remediação

```csharp
[Test]
public void GameplaySessionFlowCompletionGateComposer_ReceivesDependencyExplicitly()
{
    // Arrange
    var handoffService = Substitute.For<IGameplaySessionFlowPrepareOperationalHandoffService>();

    // Act - Composer recebe handoff explicitamente
    GameplaySessionFlowCompletionGateComposer.ComposeOrValidate(handoffService);

    // Assert
    Assert.IsNotNull(DependencyManager.Provider.Get<ISceneTransitionCompletionGate>());
}
```

---

## Trilha Fantasma #3: Injeção em Awake em `GameRunEndedEventBridge`

### Localização
```
File: Assets/_ImmersiveGames/NewScripts/SessionFlow/Integration/RunReset/GameRunEndedEventBridge.cs
Lines: 31-38
```

### Código Problemático

```csharp
[DisallowMultipleComponent]
[DebugLevel(DebugLevel.Verbose)]
public sealed class GameRunEndedEventBridge : MonoBehaviour
{
    private IRunEndMaterializationService _runEndMaterializationService;
    private IRunContinuationSelectionRoutingService _runContinuationSelectionRoutingService;
    private IRunContinuationOwnershipService _runContinuationOwnershipService;
    private EventBinding<GameRunEndedEvent> _binding;
    private EventBinding<GameRunStartedEvent> _runStartedBinding;
    private EventBinding<RunContinuationSelectionResolvedEvent> _runContinuationSelectionResolvedBinding;
    private bool _registered;
    private bool _postStagePending;

    // ❌ TRILHA FANTASMA AQUI
    private void Awake()
    {
        _runEndMaterializationService = ResolveRequired<IRunEndMaterializationService>(
            "[FATAL][Config][GameplaySessionFlow] IRunEndMaterializationService ausente no DI global antes de compor GameRunEndedEventBridge.");
        _runContinuationSelectionRoutingService = ResolveRequired<IRunContinuationSelectionRoutingService>(
            "[FATAL][Config][GameplaySessionFlow] IRunContinuationSelectionRoutingService ausente no DI global antes de compor GameRunEndedEventBridge.");
        _runContinuationOwnershipService = ResolveRequired<IRunContinuationOwnershipService>(
            "[FATAL][Config][GameplaySessionFlow] IRunContinuationOwnershipService ausente no DI global antes de compor GameRunEndedEventBridge.");

        _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
        _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
        _runContinuationSelectionResolvedBinding = new EventBinding<RunContinuationSelectionResolvedEvent>(OnRunContinuationSelectionResolved);
        RegisterBinding();
    }

    private void OnEnable() => RegisterBinding();
    private void OnDisable() => UnregisterBinding();
    private void OnDestroy() => UnregisterBinding();

    // ... mais código ...

    private static T ResolveRequired<T>(string errorMessage) where T : class
    {
        if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return service;
    }
}
```

### Por que é uma Trilha Fantasma?

1. **Anti-padrão MonoBehaviour**: No projeto há composição explícita via DI, mas este MonoBehaviour:
   - **Resolve em Awake()** em lugar de receber Dependencies
   - Não há forma de injetar dependencies via editor ou factory
   - Pareça que está "auto-inicializando"

2. **Oculta Composição**: Campos privados resolvidos em Awake parecem dados locais:
   ```csharp
   private IRunEndMaterializationService _runEndMaterializationService;  // ← Pareça local
   // ... mas é resolvido via DI em Awake!
   ```

3. **Frágil à Ordem**: Depende que composição de:
   - `IRunEndMaterializationService`
   - `IRunContinuationSelectionRoutingService`
   - `IRunContinuationOwnershipService`

   ... estejam completos **antes** que GameObject com este componente seja instanciado.

   Se ordem mudar (ex: prefab cria antes de bootstrap rodar), falha em Awake.

4. **Impossível de Testar Explicitamente**:
   ```csharp
   [Test]
   public void GameRunEndedEventBridge_ShouldReactToGameRunEnded()
   {
       // Como setup? Precisa:
       // 1. Registrar DI global com todas as dependências
       // 2. Chamar Awake implicitamente (impossível em unidade test isolada)
       // 3. Nenhuma forma de injetar mocks
   }
   ```

5. **Violação de ADR-0057**: MonoBehaviours em seam de integração devem receber dependências explicitamente, não resolver em Awake

### Causa Raiz

**Padrão Legacy de Auto-Initialization**:
1. Pode ter vindo de código anterior que não usava composição explícita
2. Ao migrar para DI, alguém "reparou" adicionando `ResolveRequired` em Awake
3. Pareça que funciona (porque DI global está sempre setup)
4. Mas cria acoplamento oculto à order de inicialização

### Impacto

- 🔴 **Quebra em Ordem Diferente**: Se GameObject é criado antes de bootstrap rodar, falha em Awake
- 🔴 **Impossível Testar**: Unit test não consegue injetar mocks
- 🔴 **Debugging Difícil**: Erro em Awake de um GameObject é genérico, stack trace confuso
- 🔴 **Risco de Silent Failure**: Se DI global está em estado inconsistente, exceção pode ser "swallowed" por Unity

### Remediação

#### Passo 1: Adicionar Método de Inicialização Explícito

```csharp
[DisallowMultipleComponent]
[DebugLevel(DebugLevel.Verbose)]
public sealed class GameRunEndedEventBridge : MonoBehaviour
{
    private IRunEndMaterializationService _runEndMaterializationService;
    private IRunContinuationSelectionRoutingService _runContinuationSelectionRoutingService;
    private IRunContinuationOwnershipService _runContinuationOwnershipService;
    private EventBinding<GameRunEndedEvent> _binding;
    private EventBinding<GameRunStartedEvent> _runStartedBinding;
    private EventBinding<RunContinuationSelectionResolvedEvent> _runContinuationSelectionResolvedBinding;
    private bool _registered;
    private bool _postStagePending;

    // ✅ Agora: Inicialização explícita, não em Awake
    public void Initialize(
        IRunEndMaterializationService runEndMaterializationService,
        IRunContinuationSelectionRoutingService runContinuationSelectionRoutingService,
        IRunContinuationOwnershipService runContinuationOwnershipService)
    {
        if (runEndMaterializationService == null)
            throw new ArgumentNullException(nameof(runEndMaterializationService));
        if (runContinuationSelectionRoutingService == null)
            throw new ArgumentNullException(nameof(runContinuationSelectionRoutingService));
        if (runContinuationOwnershipService == null)
            throw new ArgumentNullException(nameof(runContinuationOwnershipService));

        _runEndMaterializationService = runEndMaterializationService;
        _runContinuationSelectionRoutingService = runContinuationSelectionRoutingService;
        _runContinuationOwnershipService = runContinuationOwnershipService;

        _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
        _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
        _runContinuationSelectionResolvedBinding = new EventBinding<RunContinuationSelectionResolvedEvent>(OnRunContinuationSelectionResolved);
        RegisterBinding();
    }

    // ✅ Awake agora apenas garante que Initialize foi chamado
    private void Awake()
    {
        if (_runEndMaterializationService == null)
        {
            throw new InvalidOperationException(
                "[FATAL][Config][GameplaySessionFlow] GameRunEndedEventBridge.Initialize() não foi chamado. Use factory ou composição explicita.");
        }
    }

    private void OnEnable() => RegisterBinding();
    private void OnDisable() => UnregisterBinding();
    private void OnDestroy() => UnregisterBinding();

    // ... resto do código ...
}
```

#### Passo 2: Remover ResolveRequired

```csharp
// Antes ❌
private static T ResolveRequired<T>(string errorMessage) where T : class
{
    if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
    {
        throw new InvalidOperationException(errorMessage);
    }
    return service;
}

// Depois ✅
// [DELETADO - use injeção explícita]
```

#### Passo 3: Criar Factory ou Installer

Adicionar em bootstrap para compor GameRunEndedEventBridge:

```csharp
// Abordagem 1: Factory
public static class GameRunEndedEventBridgeFactory
{
    public static GameRunEndedEventBridge CreateAndAttach(
        GameObject target,
        IRunEndMaterializationService materializationService,
        IRunContinuationSelectionRoutingService selectionRoutingService,
        IRunContinuationOwnershipService continuationOwnershipService)
    {
        var bridge = target.AddComponent<GameRunEndedEventBridge>();
        bridge.Initialize(materializationService, selectionRoutingService, continuationOwnershipService);
        return bridge;
    }
}

// Abordagem 2: Via GameObject Instantiate + Find
// (Garante que Initialize foi chamado antes de Object.Find)
var go = Instantiate(prefabComGameRunEndedEventBridge);
var bridge = go.GetComponent<GameRunEndedEventBridge>();
bridge.Initialize(
    DependencyManager.Provider.GetGlobal<IRunEndMaterializationService>(),
    DependencyManager.Provider.GetGlobal<IRunContinuationSelectionRoutingService>(),
    DependencyManager.Provider.GetGlobal<IRunContinuationOwnershipService>());
```

#### Passo 4: Documentar Contrato

```csharp
/// <summary>
/// Bridge que reage a eventos de fim de run e coordena transição pós-run.
///
/// INICIALIZAÇÃO:
/// - Não inicializa automaticamente em Awake
/// - Deve ser inicializado explicitamente via Initialize()
/// - Use factory ou manuel initialization em bootstrap
///
/// DEPENDÊNCIAS:
/// - IRunEndMaterializationService
/// - IRunContinuationSelectionRoutingService
/// - IRunContinuationOwnershipService
/// </summary>
[DisallowMultipleComponent]
[DebugLevel(DebugLevel.Verbose)]
public sealed class GameRunEndedEventBridge : MonoBehaviour
{
    // ...
}
```

### Teste Pós-Remediação

```csharp
[Test]
public void GameRunEndedEventBridge_InitializeExplicitly_NoServiceLocator()
{
    // Arrange
    var go = new GameObject();
    var bridge = go.AddComponent<GameRunEndedEventBridge>();
    var materializationService = Substitute.For<IRunEndMaterializationService>();
    var selectionRoutingService = Substitute.For<IRunContinuationSelectionRoutingService>();
    var continuationOwnershipService = Substitute.For<IRunContinuationOwnershipService>();

    // Act - Initialize é chamado explicitamente
    bridge.Initialize(materializationService, selectionRoutingService, continuationOwnershipService);

    // Assert - Sem DependencyManager.Provider
    Assert.IsNotNull(bridge);
    Assert.DoesNotThrow(() => {
        // Se não foi inicializada explicitamente, Awake lança
        go.SetActive(true);
    });
}

[Test]
public void GameRunEndedEventBridge_NotInitialized_ThrowsInAwake()
{
    // Arrange
    var go = new GameObject();
    go.AddComponent<GameRunEndedEventBridge>();

    // Act/Assert - Não inicializar causa exceção em Awake
    Assert.Throws<InvalidOperationException>(() => {
        go.SetActive(true);
    });
}
```

---

## Resumo de Trilhas Fantasmas

| Trilha | Localização | Tipo | Severidade | Remediação |
|--------|------------|------|-----------|------------|
| #1: Service Locator em Adapter | SessionFlowActorsSemanticPortsAdapter.cs:122 | Resolver via TryGetGlobal | 🔴 Crítico | Injetar no constructor |
| #2: Composição Implícita | GameplaySessionFlowCompletionGateComposer.cs:44 | ResolveRequired | 🔴 Crítico | Receber como parâmetro |
| #3: Awake Resolution | GameRunEndedEventBridge.cs:31 | ResolveRequired em Awake | 🔴 Crítico | Method Initialize() explícito |

---

## Como Evitar no Futuro

### Guideline: "Não há Resolução em Hot Path"

Regra de Ouro:
```csharp
// ❌ NUNCA
public class Something
{
    private IDependency _dep;

    public void HotPath()
    {
        if (_dep == null)
            _dep = DependencyManager.Provider.GetGlobal<IDependency>();  // ← Resolução!
        // ... usa _dep
    }
}

// ✅ SEMPRE
public class Something
{
    private readonly IDependency _dep;

    public Something(IDependency dep)
    {
        _dep = dep ?? throw new ArgumentNullException(nameof(dep));
    }

    public void HotPath()
    {
        // ... usa _dep (sempre disponível, falha em compose-time se não injetado)
    }
}
```

### Código Review Checklist

Quando revisar PR:
- [ ] Nenhum `TryGetGlobal` ou `ResolveRequired` em método público de hot path
- [ ] Nenhuma composição (new Service(...) com TryGetGlobal) fora de Bootstrap/Installer
- [ ] Nenhum MonoBehaviour.Awake() que faça resolve/compose
- [ ] Todas as dependências recebidas via constructor ou Initialize() explícito
- [ ] Stack trace de erro aponta para lugar certo (bootstrap, não runtime)


