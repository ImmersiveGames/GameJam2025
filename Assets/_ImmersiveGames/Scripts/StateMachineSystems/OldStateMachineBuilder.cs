
using _ImmersiveGames.NewScripts.Core.Fsm;
using _ImmersiveGames.NewScripts.Core.Validation;
namespace _ImmersiveGames.Scripts.StateMachineSystems {
    /// <summary>
    /// Builder para criar uma OldStateMachine para o ControllerSystems de forma fluida e modular.
    /// Permite adicionar estados, configurar transi��es espec�ficas (At) e gen�ricas (Any),
    /// definir o estado inicial e construir a m�quina de estados.
    /// </summary>
    public class OldStateMachineBuilder {
        private readonly OldStateMachine _stateMachine = new();

        public OldStateMachineBuilder AddState(OldIState state, out OldIState reference) {
            reference = Preconditions.CheckNotNull(state, "State cannot be null.");
            _stateMachine.RegisterState(reference);
            return this;
        }

        public OldStateMachineBuilder At(OldIState from, OldIState to, IPredicate predicate) {
            Preconditions.CheckNotNull(from, "From state cannot be null.");
            Preconditions.CheckNotNull(to, "To state cannot be null.");
            Preconditions.CheckNotNull(predicate, "Predicate cannot be null.");
            _stateMachine.AddTransition(from, to, predicate);
            return this;
        }

        public OldStateMachineBuilder Any(OldIState to, IPredicate predicate) {
            Preconditions.CheckNotNull(to, "To state cannot be null.");
            Preconditions.CheckNotNull(predicate, "Predicate cannot be null.");
            _stateMachine.AddAnyTransition(to, predicate);
            return this;
        }

        public OldStateMachineBuilder StateInitial(OldIState state) {
            Preconditions.CheckNotNull(state, "Initial state cannot be null.");
            _stateMachine.SetState(state);
            return this;
        }

        public OldStateMachine Build() {
            return _stateMachine;
        }
    }
}

