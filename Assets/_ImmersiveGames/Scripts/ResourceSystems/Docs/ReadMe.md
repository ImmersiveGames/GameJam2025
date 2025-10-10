
# Sistema de Recursos - Documentação Completa

## 📋 Índice
1. [Visão Geral](#visão-geral)
2. [Arquitetura do Sistema](#arquitetura-do-sistema)
3. [Componentes Principais](#componentes-principais)
4. [Configurações (ScriptableObjects)](#configurações-scriptableobjects)
5. [Sistema de Animação](#sistema-de-animação)
6. [Serviços do Sistema](#serviços-do-sistema)
7. [Eventos](#eventos)
8. [Sistema de Debug](#sistema-de-debug)
9. [Fluxo de Trabalho](#fluxo-de-trabalho)
10. [Exemplos de Uso](#exemplos-de-uso)

## 🎯 Visão Geral

O Sistema de Recursos é uma arquitetura modular para gerenciar recursos de entidades (como health, mana, stamina) com suporte a UI dinâmica, animações, auto-regeneração, links entre recursos e sistema de eventos.

### Características Principais:
- ✅ **Modular e Extensível** - Componentes desacoplados
- ✅ **UI Dinâmica** - Slots gerados automaticamente
- ✅ **Múltiplas Animações** - Diferentes estratégias visuais
- ✅ **Auto-Flow** - Regeneração e drenagem automática
- ✅ **Links entre Recursos** - Overflow entre recursos
- ✅ **Sistema de Eventos** - Thresholds e mudanças
- ✅ **Debug Completo** - Ferramentas de teste integradas

## 🏗️ Arquitetura do Sistema

```
Entity (IActor)
├── EntityResourceBridge (Cria ResourceSystem)
├── ResourceSystem (Gerencia recursos)
├── ResourceAutoFlowBridge (Auto-regeneração)
├── ResourceLinkBridge (Links entre recursos)
├── ResourceThresholdBridge (Monitora thresholds)
└── CanvasResourceBinder (UI)

Services Globais:
├── ActorResourceOrchestratorService (Coordena atores↔canvases)
├── ResourceLinkService (Gerencia links)
└── ResourceSlotStrategyFactory (Cria animações)
```

## 🔧 Componentes Principais

### 1. EntityResourceBridge
**Responsabilidade**: Criar e registrar o ResourceSystem para uma entidade.

```csharp
public class EntityResourceBridge : MonoBehaviour
{
    [SerializeField] private ResourceInstanceConfig[] resourceInstances;
    private ResourceSystem _service;
    
    void Awake()
    {
        _service = new ResourceSystem(_actor.ActorId, resourceInstances);
        DependencyManager.Instance.RegisterForObject(_actor.ActorId, _service);
        _orchestrator.RegisterActor(_service);
    }
}
```

### 2. ResourceSystem
**Responsabilidade**: Gerenciar o estado dos recursos de uma entidade.

```csharp
public class ResourceSystem : IDisposable
{
    public string EntityId { get; }
    public void Modify(ResourceType type, float delta);
    public void Set(ResourceType type, float value);
    public IResourceValue Get(ResourceType type);
    public event Action<ResourceUpdateEvent> ResourceUpdated;
}
```

### 3. CanvasResourceBinder
**Responsabilidade**: Gerenciar a UI dos recursos usando object pooling.

```csharp
public class CanvasResourceBinder : MonoBehaviour
{
    [SerializeField] private string canvasId;
    [SerializeField] private ResourceUISlot slotPrefab;
    [SerializeField] private int initialPoolSize = 5;
    
    public void CreateSlotForActor(string actorId, ResourceType type, IResourceValue data);
    public void UpdateResourceForActor(string actorId, ResourceType type, IResourceValue data);
}
```

### 4. Resource Bridges Especializados

| Bridge | Função |
|--------|---------|
| `ResourceAutoFlowBridge` | Gerencia regeneração/drenagem automática |
| `ResourceLinkBridge` | Controla links entre recursos |
| `ResourceThresholdBridge` | Monitora e dispara eventos de threshold |

## ⚙️ Configurações (ScriptableObjects)

### ResourceDefinition
Define um tipo de recurso básico.

```csharp
[CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Definition")]
public class ResourceDefinition : ScriptableObject
{
    public ResourceType type;
    public int initialValue = 100;
    public int maxValue = 100;
    public bool enabled = true;
    public Sprite icon;
}
```

### ResourceInstanceConfig
Configuração específica por instância de recurso.

```csharp
[System.Serializable]
public class ResourceInstanceConfig
{
    public ResourceDefinition resourceDefinition;
    public CanvasTargetMode canvasTargetMode = CanvasTargetMode.Default;
    public string customCanvasId;
    public ResourceUIStyle slotStyle;
    public FillAnimationType fillAnimationType = FillAnimationType.BasicAnimated;
    public ResourceThresholdConfig thresholdConfig;
    public bool hasAutoFlow;
    public ResourceAutoFlowConfig autoFlowConfig;
    public int sortOrder;
}
```

### ResourceAutoFlowConfig
Configura auto-regeneração/drenagem.

```csharp
[CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Auto Flow Config")]
public class ResourceAutoFlowConfig : ScriptableObject
{
    public bool autoFill;
    public bool autoDrain;
    public float tickInterval = 1f;
    public float amountPerTick = 1f;
    public bool usePercentage;
    public float regenDelayAfterDamage;
}
```

### ResourceLinkConfig
Define links entre recursos.

```csharp
[CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Link Config")]
public class ResourceLinkConfig : ScriptableObject
{
    public ResourceType sourceResource;
    public ResourceType targetResource;
    public TransferCondition transferCondition = TransferCondition.WhenSourceEmpty;
    public float transferThreshold;
    public TransferDirection transferDirection = TransferDirection.SourceToTarget;
    public bool affectTargetWithAutoFlow = true;
}
```

### ResourceThresholdConfig
Configura thresholds para eventos.

```csharp
[CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Threshold Config")]
public class ResourceThresholdConfig : ScriptableObject
{
    [Range(0f, 1f)]
    public float[] thresholds = { 0.25f, 0.5f, 0.75f };
}
```

### ResourceUIStyle
Define o visual e animações da UI.

```csharp
[CreateAssetMenu(menuName = "ImmersiveGames/UI/Resource UI Style")]
public class ResourceUIStyle : ScriptableObject
{
    public Gradient fillGradient;
    public Color pendingColor = new Color(1f, 1f, 1f, 0.6f);
    
    // Timing
    public float quickDuration = 0.2f;
    public float slowDuration = 0.8f;
    public float delayBeforeSlow = 0.3f;
    
    // Efeitos de cura/dano
    public float healScaleIntensity = 1.1f;
    public float damageScaleIntensity = 0.95f;
    public float damageShakeStrength = 8f;
    
    // Animação de texto
    public bool enableTextAnimation;
    public float textScaleIntensity = 1.2f;
}
```

## 🎨 Sistema de Animação

### Estratégias de Animação Disponíveis

| Tipo | Descrição |
|------|-----------|
| `Instant` | Sem animação, atualização imediata |
| `BasicAnimated` | Animação básica estilo jogos de luta |
| `AdvancedAnimated` | Efeitos especiais de cura/dano |
| `SmoothAnimated` | Transições suaves sem delays |
| `PulseAnimated` | Animação com pulsos contínuos |

### Interface de Estratégia

```csharp
public interface IResourceSlotStrategy
{
    void ApplyFill(ResourceUISlot slot, float currentPct, float pendingPct, ResourceUIStyle style);
    void ApplyFill(ResourceUISlot slot, float currentPct, ResourceUIStyle style);
    void ApplyText(ResourceUISlot slot, string target, ResourceUIStyle style);
    void ClearVisuals(ResourceUISlot slot);
}
```

### Exemplo: AdvancedAnimatedFillStrategy

```csharp
public class AdvancedAnimatedFillStrategy : IResourceSlotStrategy
{
    public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
    {
        // Barra principal instantânea com efeitos
        slot.FillImage.fillAmount = currentPct;
        
        // Barra pendente animada
        slot.PendingFillImage.DOFillAmount(currentPct, style.slowDuration)
            .SetDelay(style.delayBeforeSlow);
            
        // Efeitos especiais baseados na mudança
        if (currentPct > previousValue)
            ApplyHealEffects(slot.FillImage.transform, style);
        else if (currentPct < previousValue)
            ApplyDamageEffects(slot.FillImage.transform, style);
    }
}
```

## 🔄 Serviços do Sistema

### ActorResourceOrchestratorService
**Função**: Coordenar a comunicação entre ResourceSystems e CanvasBinders.

```csharp
public interface IActorResourceOrchestrator
{
    void RegisterActor(ResourceSystem actor);
    void UnregisterActor(string actorId);
    ResourceSystem GetActorResourceSystem(string actorId);
    void RegisterCanvas(CanvasResourceBinder canvas);
    void UnregisterCanvas(string canvasId);
}
```

### ResourceLinkService
**Função**: Gerenciar links entre recursos e processar overflow.

```csharp
public interface IResourceLinkService
{
    void RegisterLink(string actorId, ResourceLinkConfig linkConfig);
    void UnregisterLink(string actorId, ResourceType sourceResource);
    ResourceLinkConfig GetLink(string actorId, ResourceType sourceResource);
}
```

### ResourceAutoFlowService
**Função**: Processar regeneração e drenagem automática.

```csharp
public class ResourceAutoFlowService : IDisposable
{
    public bool IsPaused { get; }
    public void Process(float deltaTime);
    public void Pause();
    public void Resume();
    public void Toggle();
}
```

### ResourceThresholdService
**Função**: Monitorar thresholds e disparar eventos.

```csharp
public class ResourceThresholdService : IDisposable
{
    public ResourceThresholdService(ResourceSystem resourceSystem);
    public void ForceCheck();
}
```

## 📡 Eventos

### ResourceUpdateEvent
Disparado quando um recurso é modificado.

```csharp
public class ResourceUpdateEvent : IEvent
{
    public string ActorId { get; }
    public ResourceType ResourceType { get; }
    public IResourceValue NewValue { get; }
}
```

### ResourceThresholdEvent
Disparado quando um threshold é cruzado.

```csharp
public class ResourceThresholdEvent : IEvent
{
    public string ActorId { get; }
    public ResourceType ResourceType { get; }
    public float Threshold { get; }
    public bool IsAscending { get; }
    public float CurrentPercentage { get; }
}
```

### Como consumir eventos:

```csharp
public class MyEventHandler : MonoBehaviour
{
    private EventBinding<ResourceThresholdEvent> _thresholdBinding;
    
    void Start()
    {
        _thresholdBinding = new EventBinding<ResourceThresholdEvent>(OnThresholdCrossed);
        EventBus<ResourceThresholdEvent>.Register(_thresholdBinding);
    }
    
    void OnThresholdCrossed(ResourceThresholdEvent evt)
    {
        Debug.Log($"Threshold {evt.Threshold} cruzado por {evt.ActorId}");
    }
    
    void OnDestroy()
    {
        EventBus<ResourceThresholdEvent>.Unregister(_thresholdBinding);
    }
}
```

## 🐛 Sistema de Debug

### ResourceSystemDebugManager
Componente unificado para testes e debug.

**Funcionalidades:**
- ✅ Validação do sistema
- ✅ Testes de boundaries
- ✅ Cenários de simulação
- ✅ Monitoramento de eventos
- ✅ Métricas do sistema

**Menu de Contexto:**
```
Debug/
├── Initialize Debug Manager
├── Canvas/
│   ├── Apply Canvas Offset
│   ├── Reset Canvas Position
│   └── Show Slots State
├── Resources/
│   └── Print All Resources
├── AutoFlow/
│   ├── Show Status
│   ├── Pause
│   ├── Resume
│   └── Toggle
├── Links/
│   └── Show Active Links
├── Threshold/
│   └── Force Check
├── Metrics/
│   ├── Show System Metrics
│   └── Show Recent Events
└── Quick Test All Systems

Tests/
├── Integration/
│   ├── Validate System State
│   ├── Test Resource Boundaries
│   └── Test Dependency Injection
├── Scenarios/
│   ├── Simulate Combat Scenario
│   ├── Test All Resources
│   └── Test AutoFlow Integration
└── Apply Damage/Heal/Modify
```

### Exemplo de uso:

```csharp
// Via código
debugManager.ApplyDamage(20, ResourceType.Health);
debugManager.ApplyHeal(30, ResourceType.Health);

// Via Inspector - Context Menu
// "Tests/Scenarios/Simulate Combat Scenario"
```

## 🔄 Fluxo de Trabalho

### 1. Setup Básico
```csharp
// 1. Criar ResourceDefinition (SO)
// 2. Criar ResourceInstanceConfig (array no EntityResourceBridge)
// 3. Adicionar EntityResourceBridge ao GameObject com IActor
// 4. Adicionar CanvasResourceBinder ao canvas
// 5. Configurar bridges especializadas (opcional)
```

### 2. Modificação de Recursos
```csharp
// Obter ResourceSystem
if (DependencyManager.Instance.TryGetForObject<ResourceSystem>(actorId, out var resourceSystem))
{
    // Aplicar dano
    resourceSystem.Modify(ResourceType.Health, -20f);
    
    // Aplicar cura
    resourceSystem.Modify(ResourceType.Health, 30f);
    
    // Definir valor específico
    resourceSystem.Set(ResourceType.Health, 50f);
}
```

### 3. Configuração de Links
```csharp
// Criar ResourceLinkConfig (SO)
// Configurar source→target
// Adicionar ao ResourceLinkBridge
```

## 🎮 Exemplos de Uso

### Exemplo 1: Sistema de Vida com Auto-Regeneração

```csharp
// ResourceDefinition: Health (100/100)
// ResourceInstanceConfig: 
// - hasAutoFlow = true
// - autoFlowConfig: autoFill=true, tickInterval=2, amountPerTick=5
// - fillAnimationType = AdvancedAnimated
```

### Exemplo 2: Link entre Mana e Energia

```csharp
// ResourceLinkConfig:
// - sourceResource = Mana
// - targetResource = Energy  
// - transferCondition = WhenSourceEmpty
// - transferDirection = SourceToTarget
```

### Exemplo 3: Thresholds para Alertas

```csharp
// ResourceThresholdConfig:
// - thresholds = [0.25f, 0.5f, 0.75f]

// Consumir eventos:
void OnThresholdCrossed(ResourceThresholdEvent evt)
{
    if (evt.ResourceType == ResourceType.Health && evt.Threshold == 0.25f && !evt.IsAscending)
    {
        ShowLowHealthWarning();
    }
}
```

## 🚀 Melhores Práticas

1. **Use ScriptableObjects** para configurações complexas
2. **Registre todos os eventos** no `OnDestroy`
3. **Use o Debug Manager** para testes durante desenvolvimento
4. **Configure pools adequadas** para performance
5. **Use estratégias de animação** apropriadas para cada contexto

## 🔧 Troubleshooting

### Problema: Slots não aparecem
**Solução**: Verifique se:
- ✅ EntityResourceBridge está configurado
- ✅ CanvasResourceBinder está no canvas correto
- ✅ ResourceInstanceConfig tem canvasTargetMode configurado

### Problema: Auto-flow não funciona
**Solução**: Verifique se:
- ✅ ResourceAutoFlowBridge está presente
- ✅ ResourceInstanceConfig tem hasAutoFlow = true
- ✅ AutoFlowConfig está atribuído

### Problema: Links não funcionam
**Solução**: Verifique se:
- ✅ ResourceLinkBridge está presente
- ✅ Ambos os recursos (source e target) existem
- ✅ TransferCondition está sendo atendida

---

Esta documentação cobre todo o sistema de recursos baseado nos arquivos fornecidos. O sistema é robusto, extensível e pronto para uso em produção! 🎯