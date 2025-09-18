using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.StateMachineSystems {
    /// <summary>
    /// Builder para criar uma StateMachine para o ControllerSystems de forma fluida e modular.
    /// Permite adicionar estados, configurar transições específicas (At) e genéricas (Any),
    /// definir o estado inicial e construir a máquina de estados.
    /// </summary>
    public class StateMachineBuilder {
        private readonly StateMachine _stateMachine = new();

        public StateMachineBuilder AddState(IState state, out IState reference) {
            reference = Preconditions.CheckNotNull(state, "State cannot be null.");
            return this;
        }

        public StateMachineBuilder At(IState from, IState to, IPredicate predicate) {
            Preconditions.CheckNotNull(from, "From state cannot be null.");
            Preconditions.CheckNotNull(to, "To state cannot be null.");
            Preconditions.CheckNotNull(predicate, "Predicate cannot be null.");
            _stateMachine.AddTransition(from, to, predicate);
            return this;
        }

        public StateMachineBuilder Any(IState to, IPredicate predicate) {
            Preconditions.CheckNotNull(to, "To state cannot be null.");
            Preconditions.CheckNotNull(predicate, "Predicate cannot be null.");
            _stateMachine.AddAnyTransition(to, predicate);
            return this;
        }

        public StateMachineBuilder StateInitial(IState state) {
            Preconditions.CheckNotNull(state, "Initial state cannot be null.");
            _stateMachine.SetState(state);
            return this;
        }

        public StateMachine Build() {
            return _stateMachine;
        }
    }
}