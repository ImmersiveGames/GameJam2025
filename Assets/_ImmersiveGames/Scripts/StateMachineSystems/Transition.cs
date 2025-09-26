using System;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.StateMachineSystems 
{
    public abstract class Transition {
        public IState To { get; protected set; }
        public abstract bool Evaluate();
    }

    public class Transition<T> : Transition, ITransition
    {
        public T Condition { get; }

        IPredicate ITransition.Condition => Condition as IPredicate;

        public Transition(IState to, T condition)
        {
            To = to;
            Condition = condition;
        }

        public override bool Evaluate()
        {
            if (Condition is IPredicate predicate)
            {
                return predicate.Evaluate();
            }
            if (Condition is Func<bool> func)
            {
                return func.Invoke();
            }
            return false;
        }
    }

    /// <summary>
    /// Represents a predicate that uses a Func delegate to evaluate a condition.
    /// </summary>
    public class FuncPredicate : IPredicate {
        private readonly Func<bool> _func;

        public FuncPredicate(Func<bool> func) {
            _func = func;
        }

        public bool Evaluate() => _func.Invoke();
    }

    /// <summary>
    /// Represents a predicate that encapsulates an action and evaluates to true once the action has been invoked.
    /// </summary>
    public abstract class ActionPredicate : IPredicate
    {
        private readonly Action _action;
        private bool _flag;

        protected ActionPredicate(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public virtual bool Evaluate()
        {
            _action?.Invoke();
            bool result = _flag;
            _flag = false; // Reset após avaliação
            return result;
        }

        // Método para definir a flag (chamado pela lógica da ação)
        public void SetFlag(bool value) => _flag = value;
    }
    public class EventTriggeredPredicate<T> : ActionPredicate where T : IEvent
    {
        private bool _triggered;

        public EventTriggeredPredicate(Action action) : base(action) { }

        public override bool Evaluate()
        {
            base.Evaluate(); // Chama action se necessário
            bool result = _triggered;
            _triggered = false; // Reset
            return result;
        }

        public void Trigger() => _triggered = true; // Chamado por listener de evento
    }
}