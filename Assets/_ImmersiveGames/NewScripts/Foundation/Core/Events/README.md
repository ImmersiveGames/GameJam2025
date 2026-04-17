# Core.Events — EventBus tipado

## Contexto

O NewScripts usa um modelo **event-driven** para desacoplar módulos.
Aqui, o EventBus é **tipado**, e o *bus* global pode ser substituído (ex.: por um `FilteredEventBus`) quando necessário.

Arquivos principais:

- `EventBus<T>`: façade estática por tipo de evento.
- `EventBinding<T>`: binding de callback + lifecycle.
- `InjectableEventBus<T>`: implementação default.
- `FilteredEventBus`: wrapper que filtra eventos (útil para testes / escopos).

## Como usar (publish/subscribe)

### 1) Definir um evento

```csharp
namespace _ImmersiveGames.NewScripts.Gameplay
{
    public readonly struct PlayerDiedEvent
    {
        public readonly string Reason;
        public PlayerDiedEvent(string reason) => Reason = reason;
    }
}
```

### 2) Publicar o evento

```csharp
using _ImmersiveGames.NewScripts.Core.Events;

EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent("Combat/HPZero"));
```

### 3) Assinar o evento (MonoBehaviour)

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using UnityEngine;

public sealed class PlayerDeathListener : MonoBehaviour
{
    private EventBinding<PlayerDiedEvent> _binding;

    private void Awake()
    {
        _binding = new EventBinding<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnEnable() => EventBus<PlayerDiedEvent>.Register(_binding);
    private void OnDisable() => EventBus<PlayerDiedEvent>.Unregister(_binding);

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        // Reagir via eventos/serviços, sem acoplar ao emissor.
    }
}
```

> Comentário (política de projeto): preferir assinar/desassinar em `OnEnable/OnDisable` para evitar vazamento.

## Substituindo o bus global (cenários avançados)

`EventBus<T>` expõe `GlobalBus` para permitir substituição controlada:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;

// Exemplo: durante um teste, usar um bus filtrado.
EventBus<PlayerDiedEvent>.GlobalBus = new FilteredEventBus<PlayerDiedEvent>(/* ... */);
```

**Regras práticas**

- Substituição deve ser **centralizada** (bootstrap/test harness), não “no meio” de gameplay.
- Se você substituir o bus, garanta também uma estratégia de `Clear()` quando encerrar o escopo.

## Quando NÃO usar

- Para chamadas síncronas com resposta/retorno (prefira interface/serviço + DI).
- Para dados de alta frequência por frame (prefira pull ou buffers; eventos demais viram ruído/perf overhead).
