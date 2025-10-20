using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    /// <summary>
    /// Controla o comportamento do Eater utilizando a infraestrutura de StateMachine do projeto.
    /// Define os estados Vagando, Com Fome, Perseguindo e Comendo.
    /// </summary>
    [RequireComponent(typeof(EaterMaster))]
    [DefaultExecutionOrder(10)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterBehavior : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private EaterConfigSo overrideConfig;

        private EaterMaster _master;
        private EaterBehaviorContext _context;
        private StateMachine _stateMachine;

        private IState _wanderingState;
        private IState _hungryState;
        private IState _chasingState;
        private IState _eatingState;
        private bool _stateMachineBuilt;

        private void Awake()
        {
            _master = GetComponent<EaterMaster>();
            var config = overrideConfig != null ? overrideConfig : _master.Config;

            if (config == null)
            {
                DebugUtility.LogError<EaterBehavior>("Configuração do Eater não definida.", this);
                enabled = false;
                return;
            }

            Rect gameArea = GameManager.Instance != null ? GameManager.Instance.GameConfig.gameArea : new Rect(-50f, -50f, 100f, 100f);
            _context = new EaterBehaviorContext(_master, config, gameArea);
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            BuildStateMachine();
        }

        private void Update()
        {
            if (_stateMachine == null)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
            {
                return;
            }

            _stateMachine.Update();
        }

        private void FixedUpdate()
        {
            if (_stateMachine == null)
            {
                return;
            }

            _stateMachine.FixedUpdate();
        }

        /// <summary>
        /// Define se o Eater está com fome.
        /// </summary>
        public void SetHungry(bool isHungry)
        {
            if (_context == null)
            {
                return;
            }

            bool changed = _context.SetHungry(isHungry);
            if (changed)
            {
                DebugUtility.LogVerbose<EaterBehavior>($"Estado de fome atualizado: {isHungry}");
                ForceStateEvaluation();
            }
        }

        /// <summary>
        /// Atualiza o alvo perseguido pelo Eater.
        /// </summary>
        public void SetTarget(IDetectable target)
        {
            if (_context == null)
            {
                return;
            }

            bool changed = _context.SetTarget(target);
            if (changed)
            {
                DebugUtility.LogVerbose<EaterBehavior>($"Alvo atualizado: {(target != null ? target.Owner?.ActorName : "Nenhum")}");
                ForceStateEvaluation();
            }
        }

        /// <summary>
        /// Limpa o alvo atual.
        /// </summary>
        public void ClearTarget()
        {
            SetTarget(null);
        }

        /// <summary>
        /// Solicita que o Eater inicie a ação de comer.
        /// </summary>
        public void BeginEating()
        {
            if (_context == null)
            {
                return;
            }

            bool changed = _context.SetEating(true);
            if (changed)
            {
                DebugUtility.LogVerbose<EaterBehavior>("Início manual do estado Comendo.");
                if (_context.Target != null)
                {
                    _context.Master.OnEventStartEatPlanet(_context.Target);
                }
                ForceStateEvaluation();
            }
        }

        /// <summary>
        /// Finaliza o estado de comer e limpa o alvo se necessário.
        /// </summary>
        public void EndEating(bool satiated)
        {
            if (_context == null)
            {
                return;
            }

            bool wasEating = _context.SetEating(false);
            if (wasEating)
            {
                DebugUtility.LogVerbose<EaterBehavior>("Fim manual do estado Comendo.");
                if (_context.Target != null)
                {
                    _context.Master.OnEventEndEatPlanet(_context.Target);
                }
            }

            if (satiated)
            {
                _context.SetHungry(false);
            }

            ForceStateEvaluation();
        }

        private void BuildStateMachine()
        {
            var builder = new StateMachineBuilder();

            builder
                .AddState(new EaterWanderingState(_context), out _wanderingState)
                .AddState(new EaterHungryState(_context), out _hungryState)
                .AddState(new EaterChasingState(_context), out _chasingState)
                .AddState(new EaterEatingState(_context), out _eatingState)
                .At(_wanderingState, _hungryState, new FuncPredicate(() => _context.IsHungry && !_context.IsEating))
                .At(_wanderingState, _eatingState, new FuncPredicate(() => _context.IsEating))
                .At(_hungryState, _wanderingState, new FuncPredicate(() => !_context.IsHungry))
                .At(_hungryState, _chasingState, new FuncPredicate(() => _context.ShouldChase))
                .At(_hungryState, _eatingState, new FuncPredicate(() => _context.ShouldEat))
                .At(_chasingState, _hungryState, new FuncPredicate(() => _context.LostTargetWhileHungry))
                .At(_chasingState, _wanderingState, new FuncPredicate(() => !_context.IsHungry && !_context.IsEating))
                .At(_chasingState, _eatingState, new FuncPredicate(() => _context.ShouldEat))
                .At(_eatingState, _hungryState, new FuncPredicate(() => _context.IsHungry && !_context.IsEating))
                .At(_eatingState, _wanderingState, new FuncPredicate(() => !_context.IsHungry && !_context.IsEating))
                .StateInitial(_wanderingState);

            _stateMachine = builder.Build();
            _stateMachineBuilt = true;
            ForceStateEvaluation();
        }

        private void ForceStateEvaluation()
        {
            if (_stateMachine == null)
            {
                return;
            }

            _stateMachine.Update();
        }

        [ContextMenu("Eater States/Force Wandering")]
        private void ContextForceWandering()
        {
            if (!EnsureStateMachineReady())
            {
                return;
            }

            _context.SetEating(false);
            _context.SetHungry(false);
            _context.ClearTarget();
            _context.RestartWanderingTimer();
            ForceSetState(_wanderingState);
        }

        [ContextMenu("Eater States/Force Hungry")]
        private void ContextForceHungry()
        {
            if (!EnsureStateMachineReady())
            {
                return;
            }

            _context.SetEating(false);
            _context.SetHungry(true);
            ForceSetState(_hungryState);
        }

        [ContextMenu("Eater States/Force Chasing")]
        private void ContextForceChasing()
        {
            if (!EnsureStateMachineReady())
            {
                return;
            }

            if (!_context.HasTarget)
            {
                DebugUtility.LogWarning<EaterBehavior>("Não há alvo configurado para iniciar a perseguição.", this);
                return;
            }

            _context.SetHungry(true);
            _context.SetEating(false);
            ForceSetState(_chasingState);
        }

        [ContextMenu("Eater States/Force Eating")]
        private void ContextForceEating()
        {
            if (!EnsureStateMachineReady())
            {
                return;
            }

            if (!_context.HasTarget)
            {
                DebugUtility.LogWarning<EaterBehavior>("Não há alvo configurado para iniciar o consumo.", this);
                return;
            }

            _context.SetHungry(true);
            bool startedEating = _context.SetEating(true);
            if (startedEating)
            {
                _context.Master.OnEventStartEatPlanet(_context.Target);
            }

            ForceSetState(_eatingState);
        }

        private bool EnsureStateMachineReady()
        {
            if (!_stateMachineBuilt || _stateMachine == null || _context == null)
            {
                DebugUtility.LogWarning<EaterBehavior>("StateMachine do Eater ainda não foi inicializada.", this);
                return false;
            }

            return true;
        }

        private void ForceSetState(IState targetState)
        {
            if (_stateMachine == null || targetState == null)
            {
                return;
            }

            var current = _stateMachine.CurrentState;
            if (current == targetState)
            {
                current?.OnExit();
                _stateMachine.SetState(targetState);
                return;
            }

            current?.OnExit();
            _stateMachine.SetState(targetState);
        }
    }
}
