using System;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.PlanetSystems;
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
    [AddComponentMenu("ImmersiveGames/Eater/Eater Behavior")]
    [DefaultExecutionOrder(10)]
    public sealed class EaterBehavior : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField, Tooltip("Registra mudanças de estado para depuração básica.")]
        private bool logStateTransitions = true;

        private StateMachine _stateMachine;
        private IState _wanderingState;
        private IState _hungryState;
        private IState _chasingState;
        private IState _eatingState;
        private PlanetsMaster _currentTarget;
        private bool _isEating;
        private readonly EaterDesireInfo _currentDesireInfo = EaterDesireInfo.Inactive;

        public event Action<IState, IState> EventStateChanged;
        public event Action<EaterDesireInfo> EventDesireChanged;
        public event Action<PlanetsMaster> EventTargetChanged;

        public IState CurrentState => _stateMachine?.CurrentState;
        public string CurrentStateName => GetStateName(_stateMachine?.CurrentState);
        public EaterDesireInfo CurrentDesireInfo => _currentDesireInfo;
        public PlanetsMaster CurrentTarget => _currentTarget;
        public bool IsEating => _isEating;
        public bool ShouldEnableProximitySensor => CurrentState is EaterChasingState || CurrentState is EaterEatingState;

        private void Awake()
        {
            BuildStates();
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        private void FixedUpdate()
        {
            _stateMachine?.FixedUpdate();
        }

        /// <summary>
        /// Mantido por compatibilidade. A lógica será reintroduzida futuramente.
        /// </summary>
        public void SetHungry(bool isHungry)
        {
        }

        /// <summary>
        /// Define o planeta alvo atual do Eater.
        /// </summary>
        public void SetTarget(PlanetsMaster target)
        {
            if (_currentTarget == target)
            {
                return;
            }

            _currentTarget = target;
            EventTargetChanged?.Invoke(_currentTarget);
        }

        /// <summary>
        /// Limpa o planeta alvo atual.
        /// </summary>
        public void ClearTarget()
        {
            SetTarget(null);
        }

        /// <summary>
        /// Retorna informações sobre desejos do Eater.
        /// </summary>
        public EaterDesireInfo GetCurrentDesireInfo()
        {
            return _currentDesireInfo;
        }

        /// <summary>
        /// Inicia manualmente o estado de comer.
        /// </summary>
        public void BeginEating()
        {
            _isEating = true;
            EnsureStateMachine();
            ForceSetState(_eatingState, "BeginEating");
        }

        /// <summary>
        /// Registra contato de proximidade com um planeta. Implementação propositalmente vazia.
        /// </summary>
        public void BeginEating()
        {
        }

        /// <summary>
        /// Remove o contato de proximidade ativo. Implementação propositalmente vazia.
        /// </summary>
        public void EndEating(bool satisfied)
        {
        }

        /// <summary>
        /// Finaliza o estado de comer.
        /// </summary>
        public void RegisterProximityContact(PlanetsMaster planet, Vector3 eaterPosition)
        {
            _isEating = false;
            if (satiated)
            {
                SetHungry(false);
            }
        }

        private void BuildStates()
        {
            _stateMachine = new StateMachine();

            _wanderingState = new EaterWanderingState();
            _hungryState = new EaterHungryState();
            _chasingState = new EaterChasingState();
            _eatingState = new EaterEatingState();

            _stateMachine.RegisterState(_wanderingState);
            _stateMachine.RegisterState(_hungryState);
            _stateMachine.RegisterState(_chasingState);
            _stateMachine.RegisterState(_eatingState);

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

        private void ForceSetState(IState targetState, string reason)
        {
            if (_stateMachine == null || targetState == null)
            {
                return;
            }

            IState previous = _stateMachine.CurrentState;
            previous?.OnExit();

            _stateMachine.SetState(targetState);
            UpdateInternalFlags();

            if (logStateTransitions)
            {
                string message = $"Estado definido: {GetStateName(previous)} -> {GetStateName(targetState)} ({reason}).";
                DebugUtility.Log<EaterBehavior>(message, DebugUtility.Colors.CrucialInfo, this, this);
            }

            EventStateChanged?.Invoke(previous, targetState);
        }

        private void UpdateInternalFlags()
        {
            _isEating = _stateMachine?.CurrentState is EaterEatingState;
        }

        private static string GetStateName(IState state)
        {
            return state?.GetType().Name ?? "estado desconhecido";
        }
    }
}
