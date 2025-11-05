using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    /// <summary>
    /// Controle básico do comportamento do Eater.
    /// Cria os estados conhecidos e permite alterná-los manualmente via menu de contexto.
    /// </summary>
    [RequireComponent(typeof(EaterMaster))]
    [DefaultExecutionOrder(10)]
    public sealed class EaterBehavior : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField, Tooltip("Registra mudanças de estado para depuração básica.")]
        private bool logStateTransitions = true;

        internal bool ShouldLogStateTransitions => logStateTransitions;

        private StateMachine _stateMachine;
        private EaterBehaviorState _wanderingState;
        private EaterBehaviorState _hungryState;
        private EaterBehaviorState _chasingState;
        private EaterBehaviorState _eatingState;

        private void Awake()
        {
            BuildStates();
        }

        private void BuildStates()
        {
            _stateMachine = new StateMachine();

            _wanderingState = RegisterState(new EaterWanderingState());
            _hungryState = RegisterState(new EaterHungryState());
            _chasingState = RegisterState(new EaterChasingState());
            _eatingState = RegisterState(new EaterEatingState());

            ForceSetState(_wanderingState, "Inicialização");
        }

        private void EnsureStateMachine()
        {
            if (_stateMachine == null)
            {
                BuildStates();
            }
        }

        [ContextMenu("Eater States/Set Wandering")]
        private void ContextSetWandering()
        {
            EnsureStateMachine();
            ForceSetState(_wanderingState, "ContextMenu/Wandering");
        }

        [ContextMenu("Eater States/Set Hungry")]
        private void ContextSetHungry()
        {
            EnsureStateMachine();
            ForceSetState(_hungryState, "ContextMenu/Hungry");
        }

        [ContextMenu("Eater States/Set Chasing")]
        private void ContextSetChasing()
        {
            EnsureStateMachine();
            ForceSetState(_chasingState, "ContextMenu/Chasing");
        }

        [ContextMenu("Eater States/Set Eating")]
        private void ContextSetEating()
        {
            EnsureStateMachine();
            ForceSetState(_eatingState, "ContextMenu/Eating");
        }

        private void ForceSetState(EaterBehaviorState targetState, string reason)
        {
            if (_stateMachine == null || targetState == null)
            {
                return;
            }

            IState previous = _stateMachine.CurrentState;
            previous?.OnExit();

            _stateMachine.SetState(targetState);
            if (logStateTransitions)
            {
                string message = $"Estado definido: {GetStateName(previous)} -> {GetStateName(targetState)} ({reason}).";
                DebugUtility.Log<EaterBehavior>(message, DebugUtility.Colors.CrucialInfo, this, this);
            }

        }

        private T RegisterState<T>(T state) where T : EaterBehaviorState
        {
            state.Attach(this);
            _stateMachine.RegisterState(state);
            return state;
        }

        private static string GetStateName(IState state)
        {
            if (state is EaterBehaviorState eaterState)
            {
                return eaterState.StateName;
            }

            return state?.GetType().Name ?? "estado desconhecido";
        }
    }
}
