# Foundation / Core / FSM

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

FSM genérica para fluxos internos simples.

## Para que serve

- Modelar estados locais.
- Separar `enter`, `update`, `fixed update` e `exit`.
- Declarar transições por predicados.
- Usar `AnyTransition` para saídas globais controladas.

## Quando usar

Use em controllers ou fluxos locais que têm estados claros e transições previsíveis.

Exemplos:

- estado local de UI;
- controller de objeto;
- subfluxo interno de gameplay;
- estado técnico pequeno.

## Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Fsm;

public sealed class IdleState : IState
{
    public void OnEnter() { }
    public void Update() { }
    public void FixedUpdate() { }
    public void OnExit() { }
}

public sealed class ExampleFsmDriver
{
    private readonly StateMachine _stateMachine = new();

    public void Configure()
    {
        var idle = new IdleState();
        var active = new ActiveState();

        _stateMachine.RegisterState(idle);
        _stateMachine.RegisterState(active);
        _stateMachine.AddTransition(idle, active, () => CanEnterActive());
        _stateMachine.SetState(idle);
    }

    public void Tick()
    {
        _stateMachine.Update();
    }

    private bool CanEnterActive()
    {
        return true;
    }
}
```

## Quando não usar

- Não usar para substituir fluxo canônico de módulos grandes.
- Não usar para esconder política de domínio.
- Não usar para coordenar vários owners externos.

## Arquivos principais

- `Foundation/Core/Fsm/IState.cs`
- `Foundation/Core/Fsm/ITransition.cs`
- `Foundation/Core/Fsm/IPredicate.cs`
- `Foundation/Core/Fsm/StateMachine.cs`
- `Foundation/Core/Fsm/Transition.cs`
- `Foundation/Core/Fsm/README.md`

## Cuidados

- Estados devem ser registrados antes de uso.
- Evite criar transições novas a cada frame.
- Pause/gates pertencem ao owner do fluxo, não à FSM genérica.
