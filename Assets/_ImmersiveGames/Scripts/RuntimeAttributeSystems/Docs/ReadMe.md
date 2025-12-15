Perfeito 👌 — vou atualizar **toda a documentação** para refletir a nova arquitetura **event-driven e orientada a injeção** (com `DependencyBootstrapper`, `ResourceInitializationManager`, `EventBus`, e as versões finais das bridges e serviços que você aprovou).

A versão abaixo substitui completamente a anterior — mantendo a estrutura profissional e didática, mas com foco na filosofia **reativa e modular** que o sistema agora segue.

---

# 💠 Sistema de Recursos — Documentação Atualizada (v3.0)

## 📋 Índice

1. [Visão Geral](#visão-geral)
2. [Arquitetura Event-Driven](#arquitetura-event-driven)
3. [Componentes Principais](#componentes-principais)
4. [Configurações (ScriptableObjects)](#configurações-scriptableobjects)
5. [Serviços e Bridges](#serviços-e-bridges)
6. [Sistema de UI e Animação](#sistema-de-ui-e-animação)
7. [Eventos e Fluxo Reativo](#eventos-e-fluxo-reativo)
8. [Debug e Diagnóstico](#debug-e-diagnóstico)
9. [Fluxo de Inicialização e Execução](#fluxo-de-inicialização-e-execução)
10. [Exemplos Práticos](#exemplos-práticos)
11. [Boas Práticas e Troubleshooting](#boas-práticas-e-troubleshooting)

---

## 🎯 Visão Geral

O **Sistema de Recursos (Resource System)** fornece uma infraestrutura modular e orientada a eventos para gerenciar recursos de entidades (vida, mana, energia, etc.) com UI dinâmica, animações configuráveis, links, thresholds e auto-fluxos reativos.

### ✨ Características-Chave

* **Event-Driven Total** — Tudo comunica via `EventBus` e `FilteredEventBus`
* **Dependency Injection** — Serviços globais e objetos injetáveis via `DependencyManager`
* **UI Dinâmica** — Slots criados automaticamente via `CanvasPipelineManager`
* **Serviços Reativos** — Nenhum update manual; tudo é dirigido por eventos
* **AutoFlow & Links** — Recursos com regeneração e overflow automáticos
* **Thresholds Inteligentes** — Eventos disparados com base em percentuais
* **Bridges Leves** — Inicializam automaticamente quando o `ResourceSystem` está pronto

---

## 🧩 Arquitetura Event-Driven

```
IActor (Entidade)
├── EntityResourceBridge           → Cria e registra o ResourceSystem
├── ResourceAutoFlowBridge         → Serviço de auto-flow (regen/drain)
├── ResourceLinkBridge             → Links entre recursos
├── ResourceThresholdBridge        → Thresholds e eventos visuais
└── CanvasBinder (ICanvasBinder)   → UI dos recursos

Serviços Globais:
├── ActorResourceOrchestratorService   (coordena actors ↔ canvases)
├── CanvasPipelineManager              (pipeline de bind event-driven)
├── ResourceLinkService                (links e overflow)
└── ResourceThresholdService           (eventos de thresholds)
```

---

## ⚙️ Componentes Principais

### 🧠 `EntityResourceBridge`

Cria o `ResourceSystem` do ator, registra no `DependencyManager` e no `ActorResourceOrchestrator`.

```csharp
public class EntityResourceBridge : MonoBehaviour
{
    [SerializeField] private ResourceInstanceConfig[] resourceInstances;
    void Awake()
    {
        var system = new ResourceSystem(actor.ActorId, resourceInstances);
        DependencyManager.Instance.RegisterForObject(actor.ActorId, system);
        DependencyManager.Instance.TryGetGlobal(out IActorResourceOrchestrator orchestrator);
        orchestrator.RegisterActor(system);
    }
}
```

---

### 🔁 `ResourceSystem`

Gerencia o estado dos recursos e dispara eventos via `EventBus`.

```csharp
public class ResourceSystem
{
    public string EntityId { get; }
    public event Action<ResourceUpdateEvent> ResourceUpdated;

    public void Modify(ResourceType type, float delta);
    public void Set(ResourceType type, float value);
    public IResourceValue Get(ResourceType type);
}
```

---

## 🎛️ Serviços e Bridges

### 🌐 `ActorResourceOrchestratorService`

Orquestra a comunicação entre atores e canvases, encaminhando bind requests via `EventBus`.

* Registra e gerencia atores (`ResourceSystem`)
* Registra canvases (`ICanvasBinder`)
* Processa binds e pendências via `ResourceEventHub`
* Integra com o `CanvasPipelineManager` para renderização automática

---

### ⚙️ `ResourceBridgeBase`

Base comum de todas as bridges.
Inicializa automaticamente via `DependencyManager` e desativa-se se não puder ser inicializada.

**Fluxo:**

1. Obtém `IActor` local
2. Solicita `IActorResourceOrchestrator` e `ResourceSystem` via `DependencyManager`
3. Chama `OnServiceInitialized()` se tudo estiver pronto
4. Caso contrário, se auto-desativa

---

### 💧 `ResourceAutoFlowBridge`

Gerencia regeneração e drenagem automática com base em `ResourceAutoFlowConfig`.

* Usa `ResourceAutoFlowService`
* Processa recursos automaticamente via `EventBus`
* Sincroniza com a UI quando o canvas está pronto

---

### 🔗 `ResourceLinkBridge`

Gerencia links entre recursos (overflow, drenagem conjunta etc.)

* Usa o serviço global `ResourceLinkService`
* Registra links no evento de inicialização
* Remove todos os links ao ser destruído
* Totalmente reativo via `ResourceUpdateEvent`

---

### ⚡ `ResourceThresholdBridge`

Monitora percentuais de recursos e dispara eventos quando thresholds são cruzados.

* Usa `ResourceThresholdService`
* Registra no `FilteredEventBus`
* Dispara `ResourceVisualFeedbackEvent` para efeitos visuais

---

## 🧩 Configurações (ScriptableObjects)

| Config                      | Função                                    |
| --------------------------- | ----------------------------------------- |
| **ResourceDefinition**      | Define o tipo e visual base de um recurso |
| **ResourceInstanceConfig**  | Configura cada recurso por entidade       |
| **ResourceAutoFlowConfig**  | Controla regeneração/drenagem automática  |
| **ResourceLinkConfig**      | Define como recursos se influenciam       |
| **ResourceThresholdConfig** | Percentuais para disparo de eventos       |
| **ResourceUIStyle**         | Estilo visual e parâmetros de animação    |

*(Os exemplos de código mantêm o mesmo formato da documentação anterior.)*

---

## 🎨 Sistema de UI e Animação

O sistema de UI é controlado por **CanvasPipelineManager** e **CanvasBinders**.

* Slots são criados dinamicamente via `ResourceEventHub.CanvasBindRequest`
* `ResourceUISlot` gerencia visual e animações com estratégia plugável (`IResourceSlotStrategy`)
* Estratégias são criadas pela `ResourceSlotStrategyFactory`

### Estratégias disponíveis

| Tipo               | Descrição                 |
| ------------------ | ------------------------- |
| `Instant`          | Atualização imediata      |
| `BasicAnimated`    | Animação rápida com delay |
| `AdvancedAnimated` | Efeitos visuais dinâmicos |
| `SmoothAnimated`   | Transições contínuas      |
| `PulseAnimated`    | Pulsações periódicas      |

---

## 📡 Eventos e Fluxo Reativo

| Evento                        | Origem                             | Função                        |
| ----------------------------- | ---------------------------------- | ----------------------------- |
| `ResourceUpdateEvent`         | `ResourceSystem`                   | Alteração de qualquer recurso |
| `CanvasBindRequest`           | `ActorResourceOrchestratorService` | Pedido de bind na UI          |
| `ResourceThresholdEvent`      | `ResourceThresholdService`         | Threshold cruzado             |
| `AutoFlowEffectEvent`         | `ResourceAutoFlowService`          | Efeito visual de regen/dreno  |
| `ResourceVisualFeedbackEvent` | `ResourceThresholdBridge`          | Feedback visual para UI       |

### Exemplo de consumo

```csharp
private EventBinding<ResourceThresholdEvent> _bind;

void Awake()
{
    _bind = new EventBinding<ResourceThresholdEvent>(OnThreshold);
    FilteredEventBus<ResourceThresholdEvent>.Register(_bind, "Player01");
}

void OnThreshold(ResourceThresholdEvent evt)
{
    Debug.Log($"[{evt.ActorId}] cruzou {evt.ResourceType} → {evt.Threshold:P}");
}

void OnDestroy() => FilteredEventBus<ResourceThresholdEvent>.Unregister("Player01");
```

---

## 🧪 Debug e Diagnóstico

### 🔍 `DebugUtility`

Todos os logs são padronizados e filtráveis por tipo (`Verbose`, `Warning`, `Error`).

### 🧭 `ResourceSystemDebugManager`

Ferramenta completa de inspeção e simulação:

* Visualiza estados e thresholds
* Aplica dano/cura
* Testa links e auto-flow
* Exibe métricas globais

---

## 🔄 Fluxo de Inicialização e Execução

1. `DependencyBootstrapper` é carregado e inicializa todos os sistemas globais
2. `ResourceInitializationManager` injeta dependências (`ActorResourceOrchestrator`, `CanvasPipelineManager`)
3. `EntityResourceBridge` cria e registra o `ResourceSystem` do ator
4. Bridges (`AutoFlow`, `Link`, `Threshold`) inicializam automaticamente
5. `ActorResourceOrchestrator` publica binds no `EventBus`
6. `CanvasPipelineManager` recebe e instancia os `ResourceUISlot` correspondentes
7. UI e lógica reagem automaticamente via eventos

---

## 🎮 Exemplos Práticos

### 💓 Vida com Auto-Regeneração

```csharp
// Configuração do recurso:
hasAutoFlow = true;
autoFlowConfig.autoFill = true;
autoFlowConfig.tickInterval = 2;
autoFlowConfig.amountPerTick = 5;
```

O `ResourceAutoFlowBridge` cuidará automaticamente da regeneração.

---

### 🔗 Link de Mana → Energia

```csharp
// ResourceLinkConfig:
sourceResource = Mana;
targetResource = Energy;
transferCondition = WhenSourceEmpty;
```

A bridge de link sincronizará os valores automaticamente.

---

### ⚠️ Alertas de Threshold

```csharp
void OnThresholdCrossed(ResourceThresholdEvent evt)
{
    if (evt.ResourceType == ResourceType.Health && evt.Threshold <= 0.25f && !evt.IsAscending)
        ShowCriticalHealthWarning();
}
```

---

## 🧭 Boas Práticas e Troubleshooting

### ✅ Boas Práticas

* Use **ScriptableObjects** para configuração de recursos
* Sempre remova registros de eventos em `OnDestroy()`
* Prefira **EventBus** a chamadas diretas
* Use **bridges leves** — sem update loops manuais
* Utilize o **DebugUtility** com `DebugLevel.Verbose` em dev

### ⚠️ Problemas Comuns

| Sintoma                 | Solução                                                                                            |
| ----------------------- | -------------------------------------------------------------------------------------------------- |
| Slots não aparecem      | Verifique se o `CanvasPipelineManager` e o `ActorResourceOrchestratorService` estão injetados      |
| AutoFlow não ativa      | Certifique-se de que `hasAutoFlow = true` e o bridge está ativo                                    |
| Thresholds não disparam | Confirme se `ResourceThresholdBridge` está presente e thresholds estão definidos                   |
| Links inoperantes       | Verifique se `ResourceLinkBridge` está configurado e os recursos existem no mesmo `ResourceSystem` |

---

## 🚀 Conclusão

O novo **Resource System v3.0** é **100% event-driven**, altamente **modular** e **extensível**.
Os serviços não dependem de ciclos de update nem de polling manual — tudo ocorre por **injeção e eventos**.

Ele agora está pronto para produção em **arquiteturas baseadas em ECS, Dependency Injection ou Scene-based orchestration**.
Com `DebugUtility`, `EventBus` e `DependencyManager`, o sistema garante estabilidade, performance e rastreabilidade completas.
