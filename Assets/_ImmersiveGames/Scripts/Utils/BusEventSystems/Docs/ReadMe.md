# 🔔 Sistema de Event Bus — Guia de Uso (v1.0)

## 📚 Índice

1. [Visão Geral](#visão-geral)
2. [Arquitetura Reativa](#arquitetura-reativa)
3. [Componentes Essenciais](#componentes-essenciais)
4. [Ciclo de Vida e Escopos](#ciclo-de-vida-e-escopos)
5. [Integração Passo a Passo](#integração-passo-a-passo)
6. [Boas Práticas e Troubleshooting](#boas-práticas-e-troubleshooting)

---

## 🎯 Visão Geral

O **BusEventSystem** oferece uma infraestrutura de publicação/assinatura totalmente desacoplada para o projeto multiplayer local. Ele foi desenhado para ser **injetável**, **idempotente** e **orientado a escopos**, permitindo que UI, lógica de jogo e serviços compartilhem eventos sem dependências rígidas.

### Destaques

* **Genérico e Type-Safe** — `EventBus<T>` e `FilteredEventBus<T>` trabalham apenas com eventos que implementam `IEvent`.
* **Compatível com DI** — O `InjectableEventBus<T>` pode ser registrado no `DependencyManager`, permitindo substituições em testes.
* **Escopos Inteligentes** — Filtragem por chave (ex.: ActorId) garante isolamento entre jogadores no multiplayer local.
* **Ciclos de Vida Controlados** — `EventBusUtil` inicializa/limpa automaticamente os buses por build e por play mode.

---

## 🧠 Arquitetura Reativa

```
IEvent (contrato)
├── EventBus<T>              → Fachada estática para publicação global
├── FilteredEventBus<T>      → Proxy com dicionário de escopos → lista de bindings
└── InjectableEventBus<T>    → Implementação concreta registrada via DependencyManager

Bindings
├── EventBinding<T>          → Associa delegates seguros ao evento
└── BaseBindHandler          → Helper para MonoBehaviours injetáveis
```

* `EventBusUtil.Initialize()` escaneia assemblies e cria instâncias genéricas de `EventBus<T>` antes da primeira cena carregar.
* `DependencyBootstrapper` garante que cada `IEventBus<T>` tenha uma instância `InjectableEventBus<T>` registrada como serviço global, respeitando idempotência.
* `FilteredEventBus<T>` mantém um dicionário `scope → bindings`, reutilizando o bus global para execução e oferecendo limpeza dedicada por escopo.

---

## 🧩 Componentes Essenciais

### `IEvent`
Contrato base que identifica tipos elegíveis para uso no bus. Qualquer record/struct/class pode implementar.

### `EventBus<T>`
Fachada estática com fallback interno (`InjectableEventBus<T>`). Expõe:
* `Register(EventBinding<T>)`
* `Unregister(EventBinding<T>)`
* `Raise(T evt)`
* `Clear()`

### `FilteredEventBus<T>`
Especialização que associa bindings a um **escopo arbitrário** (`object`). Útil para isolar jogadores, UI específica ou entidades locais. Métodos principais:
* `Register(binding, scope)` — adiciona e reencaminha para `EventBus<T>`
* `RaiseFiltered(evt, targetScope)` — notifica apenas quem compartilha o mesmo escopo
* `Unregister(scope)` — remove e limpa bindings do escopo, evitando leaks

### `InjectableEventBus<T>`
Implementação concreta com `HashSet<EventBinding<T>>`, proteção contra handlers que lançam exceção e snapshot de iteração para evitar mutação concorrente.

### `EventBinding<T>`
Wrapper seguro para delegates com suporte a `Action<T>` e `Action`. Permite composição (`Add/Remove`) e reduz GC ao reutilizar instâncias.

### `EventBusUtil`
Serviço estático que:
* Registra callbacks para limpar buses ao sair do Play Mode (Editor)
* Inicializa todos os buses antes da primeira cena (`RuntimeInitializeOnLoadMethod`)
* Possui cache de tipos (`EventTypes`, `EventBusTypes`) para inspeções e diagnósticos

---

## 🔁 Ciclo de Vida e Escopos

1. **Bootstrap** — `DependencyBootstrapper` registra `InjectableEventBus<T>` como serviço global (via reflexão). `EventBus<T>.GlobalBus` continua apontando para `_internalBus` até ser sobrescrito manualmente.
2. **Registro** — Componentes criam `EventBinding<T>` e chamam `EventBus<T>.Register(binding)` ou `FilteredEventBus<T>.Register(binding, scope)`.
3. **Publicação** — `EventBus<T>.Raise(evt)` notifica todos os bindings registrados; `FilteredEventBus<T>.RaiseFiltered(evt, scope)` restringe por chave.
4. **Limpeza** — `EventBus<T>.Clear()` remove todos os bindings do bus específico. `FilteredEventBus<T>.Unregister(scope)` remove apenas o escopo. `EventBusUtil.ClearAllBuses()` é acionado na troca de modo de jogo.

Escopos recomendados:
* `actor.ActorId` para diferenciar players/instâncias.
* `canvasId` para UI específica.
* `Guid`/`UniqueIdFactory` para objetos temporários.

---

## 🚀 Integração Passo a Passo

1. **Defina o evento**
   ```csharp
   public readonly struct ResourceUpdateEvent : IEvent
   {
       public readonly string ActorId;
       public readonly ResourceType Type;
       public readonly float NewValue;
       public ResourceUpdateEvent(string actorId, ResourceType type, float newValue)
       {
           ActorId = actorId;
           Type = type;
           NewValue = newValue;
       }
   }
   ```

2. **Crie e registre bindings**
   ```csharp
   private EventBinding<ResourceUpdateEvent> _binding;

   private void OnEnable()
   {
       _binding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
       FilteredEventBus<ResourceUpdateEvent>.Register(_binding, _actor.ActorId);
   }

   private void OnDisable()
   {
       FilteredEventBus<ResourceUpdateEvent>.Unregister(_binding, _actor.ActorId);
   }
   ```

3. **Dispare o evento**
   ```csharp
   FilteredEventBus<ResourceUpdateEvent>.RaiseFiltered(
       new ResourceUpdateEvent(actorId, resourceType, value),
       actorId);
   ```

4. **Integrar com DI (opcional)**
   ```csharp
   [Inject] private IEventBus<ResourceUpdateEvent> _eventBus;

   private void Publish(ResourceUpdateEvent evt)
   {
       _eventBus.Raise(evt);
   }
   ```
   > `DependencyBootstrapper` já registra instâncias `InjectableEventBus<T>` automaticamente.

---

## ✅ Boas Práticas e Troubleshooting

| Situação | Diagnóstico | Ação Recomendada |
| --- | --- | --- |
| Eventos não chegam ao listener | Escopo incorreto ou `ActorId` divergente | Logar escopo ao registrar e publicar; utilizar `DebugUtility` com nível Verbose |
| Duplicidade de handlers | `EventBinding` reutilizado sem `Unregister` | Armazene o binding em campo e remova no `OnDisable`/`OnDestroy` |
| Exceção em handler interrompe fluxo | Handler lança exceção e quebra iteração | `InjectableEventBus` captura e loga, mas revise stack trace e proteja handlers |
| Fugas entre Play/Editor | Buses não limpos ao sair do Play Mode | `EventBusUtil` já limpa via `OnPlayModeStateChanged`; certifique-se de que o script está no assembly carregado |
| Testes unitários | Necessidade de simular bus específico | Registre manualmente `EventBus<T>.GlobalBus = new InjectableEventBus<T>();` nos testes |

> **Dica:** Combine com `DependencyManager.InjectDependencies(this)` para receber `IEventBus<T>` automaticamente em componentes que não podem usar membros estáticos.

---

Documentação alinhada com os princípios SOLID e preparada para cenários de multiplayer local, garantindo desacoplamento máximo entre produtores e consumidores de eventos.
