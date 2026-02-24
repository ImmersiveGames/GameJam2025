# Core.Fsm — StateMachine genérica

## Contexto

Este módulo fornece uma FSM simples para fluxos internos (ex.: estados de um controller, sub-fluxos de UI, etc.).

A implementação suporta:

- Registro explícito de estados (fail-fast se estado não estiver registrado).
- Transições por condição (`Func<bool>` ou `IPredicate`).
- `AnyTransition` (transições globais).

## Tipos

- `IState`: contrato de estado (enter/update/fixed/exit).
- `StateMachine`: orquestra estados e avalia transições.
- `Transition<T>`: avalia condição via `IPredicate` ou `Func<bool>`.
- Predicados prontos: `FuncPredicate`, `EventTriggeredPredicate`.

## How to use

### 1) Criar estados

```csharp
using _ImmersiveGames.NewScripts.Core.Fsm;

public sealed class IdleState : IState
{
    public void OnEnter() { /* ... */ }
    public void Update() { /* ... */ }
    public void FixedUpdate() { /* ... */ }
    public void OnExit() { /* ... */ }
}
```

### 2) Registrar e configurar transições

```csharp
using _ImmersiveGames.NewScripts.Core.Fsm;

var fsm = new StateMachine();

var idle = new IdleState();
var chase = new ChaseState();

fsm.RegisterState(idle);
fsm.RegisterState(chase);

// Ex.: condição via Func<bool>
fsm.AddTransition(idle, chase, () => canChase);

// Ex.: any transition (ex.: stun)
fsm.AddAnyTransition(new StunnedState(), () => isStunned);

fsm.SetState(idle);
```

### 3) Dirigir o update

- Chame `StateMachine.Update()` no `Update()` do seu driver.
- Chame `StateMachine.FixedUpdate()` no `FixedUpdate()` quando necessário.

```csharp
void Update() => fsm.Update();
void FixedUpdate() => fsm.FixedUpdate();
```

## Observações

- `AddAnyTransition(...)` guarda as transições em um `HashSet`. Evite criar novos objetos a cada frame.
- Para transições disparadas por evento, use `EventTriggeredPredicate` e chame `Trigger()` no listener.
- A FSM é **genérica** e não implementa política de *pause/gates*; isso deve ficar em nível de feature.
