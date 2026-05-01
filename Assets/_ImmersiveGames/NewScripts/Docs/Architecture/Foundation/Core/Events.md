# Foundation / Core / Events

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Sistema base de eventos tipados para comunicação desacoplada entre módulos.

## Para que serve

- Publicar eventos sem acoplar emissor e consumidor.
- Registrar listeners com lifecycle explícito.
- Permitir substituição controlada do bus global em cenários avançados.
- Usar wrappers como `FilteredEventBus` quando for necessário filtrar eventos em escopos específicos.

## Quando usar

Use quando um módulo precisa avisar que algo aconteceu e não precisa de resposta síncrona imediata.

Exemplos:

- estado mudou;
- etapa de fluxo concluiu;
- evento de observabilidade ocorreu;
- request foi emitido para outro owner processar.

## Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Events;

public readonly struct ExampleEvent : IEvent
{
    public readonly string Reason;

    public ExampleEvent(string reason)
    {
        Reason = reason;
    }
}

public sealed class ExampleListener
{
    private readonly EventBinding<ExampleEvent> _binding;

    public ExampleListener()
    {
        _binding = new EventBinding<ExampleEvent>(OnExample);
    }

    public void Bind()
    {
        EventBus<ExampleEvent>.Register(_binding);
    }

    public void Unbind()
    {
        EventBus<ExampleEvent>.Unregister(_binding);
    }

    private void OnExample(ExampleEvent evt)
    {
        // Reage ao evento sem conhecer o emissor.
    }
}
```

## Quando não usar

- Não usar para chamada direta simples dentro do mesmo serviço.
- Não usar quando o caller precisa de retorno imediato.
- Não usar para esconder dependência obrigatória.
- Não usar para tráfego por frame de alta frequência.

## Arquivos principais

- `Foundation/Core/Events/IEvent.cs`
- `Foundation/Core/Events/IEventBinding.cs`
- `Foundation/Core/Events/EventBinding.cs`
- `Foundation/Core/Events/EventBus.cs`
- `Foundation/Core/Events/InjectableEventBus.cs`
- `Foundation/Core/Events/FilteredEventBus.cs`
- `Foundation/Core/Events/EventBusUtil.cs`
- `Foundation/Core/Events/README.md`

## Cuidados

- Bind/unbind deve seguir lifecycle claro.
- O owner do evento precisa ser explícito.
- Eventos não devem virar bypass de arquitetura.
