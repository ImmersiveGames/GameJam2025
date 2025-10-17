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
    /// Representa um predicado que garante a execução de uma ação antes de avaliar o resultado concreto.
    /// </summary>
    public abstract class ActionPredicate : IPredicate
    {
        private readonly Action _action;

        protected ActionPredicate(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public virtual bool Evaluate()
        {
            _action?.Invoke();
            return EvaluateInternal();
        }

        /// <summary>
        /// Avaliação concreta realizada pelas classes derivadas após a execução da ação.
        /// </summary>
        /// <returns>Verdadeiro caso a transição deva ocorrer.</returns>
        protected abstract bool EvaluateInternal();
    }
    public class EventTriggeredPredicate<T> : ActionPredicate where T : IEvent
    {
        private bool _triggered;

        public EventTriggeredPredicate(Action action) : base(action) { }

        protected override bool EvaluateInternal()
        {
            bool result = _triggered;
            _triggered = false; // Reset
            return result;
        }

        public void Trigger() => _triggered = true; // Chamado por listener de evento
    }
}