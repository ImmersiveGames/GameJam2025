using System;
using System.Text;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem.Debug;
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
        private IState _lastKnownState;
        private readonly StringBuilder _summaryBuilder = new StringBuilder(256);

        [Header("Execução")]
        [SerializeField, Tooltip("Processa a máquina de estados mesmo quando o GameManager está inativo (útil para testes na cena).")]
        private bool updateWhileGameInactive = true;
        [SerializeField, HideInInspector]
        private bool executionToggleInitialized;

        [Header("Debug")]
        [SerializeField, Tooltip("Exibe logs automáticos quando o estado do comportamento muda.")]
        private bool logStateTransitions = true;
        [SerializeField, Tooltip("Inclui um resumo básico do estado atual no log de transição.")]
        private bool logStateSummaries;

        private bool _hasWarnedAboutInactiveGameState;

        public event Action<IState, IState> EventStateChanged;

        public IState CurrentState => _stateMachine?.CurrentState;
        public string CurrentStateName => GetStateName(_stateMachine?.CurrentState);

        private void Awake()
        {
            EnsureExecutionToggleInitialized();

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

            GameManager gameManager = GameManager.Instance;
            bool isGameActive = gameManager == null || gameManager.IsGameActive();
            bool canUpdate = isGameActive || updateWhileGameInactive;
            if (!canUpdate)
            {
                if (!_hasWarnedAboutInactiveGameState)
                {
                    DebugUtility.LogWarning<EaterBehavior>(
                        "GameManager está inativo e a execução fora da sessão está desabilitada. Ative 'updateWhileGameInactive' para testar o comportamento.",
                        this);
                    _hasWarnedAboutInactiveGameState = true;
                }
                return;
            }

            _hasWarnedAboutInactiveGameState = false;

            _stateMachine.Update();
            TrackStateChange("Update");
            _context.EnsureHungryEffects();
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
            _lastKnownState = _stateMachine.CurrentState;

            if (logStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>($"Estado inicial definido: {GetStateName(_lastKnownState)}.", instance: this);
            }

            LogStateSummary("📊 Resumo inicial do comportamento");

            ForceStateEvaluation();
        }

        private void ForceStateEvaluation()
        {
            if (_stateMachine == null)
            {
                return;
            }

            _stateMachine.Update();
            TrackStateChange("ForceEvaluation");
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
                TrackStateChange("ForceSetState");
                return;
            }

            current?.OnExit();
            _stateMachine.SetState(targetState);
            TrackStateChange("ForceSetState");
        }

        private void TrackStateChange(string reason)
        {
            if (_stateMachine == null)
            {
                return;
            }

            IState current = _stateMachine.CurrentState;
            if (ReferenceEquals(current, _lastKnownState))
            {
                return;
            }

            IState previous = _lastKnownState;
            _lastKnownState = current;

            if (logStateTransitions)
            {
                string message = string.IsNullOrEmpty(reason)
                    ? $"Estado alterado: {GetStateName(previous)} → {GetStateName(current)}."
                    : $"Estado alterado ({reason}): {GetStateName(previous)} → {GetStateName(current)}.";
                DebugUtility.Log<EaterBehavior>(message, instance: this);
            }

            LogStateSummary($"📊 Resumo após transição ({reason})");

            EventStateChanged?.Invoke(previous, current);
        }

        private void LogStateSummary(string title)
        {
            if (!logStateSummaries)
            {
                return;
            }

            EaterBehaviorDebugSnapshot snapshot = CreateDebugSnapshot();
            if (!snapshot.IsValid)
            {
                DebugUtility.LogWarning<EaterBehavior>("Contexto ainda não está disponível para gerar resumo.", this);
                return;
            }

            _summaryBuilder.Clear();
            _summaryBuilder.AppendLine(title);
            _summaryBuilder.AppendLine($"- Estado: {snapshot.CurrentState}");
            _summaryBuilder.AppendLine($"- Fome: {snapshot.IsHungry}, Comendo: {snapshot.IsEating}");
            _summaryBuilder.AppendLine($"- Alvo: {(snapshot.HasTarget ? snapshot.TargetName : "Nenhum")}");
            _summaryBuilder.AppendLine($"- Timer do estado: {snapshot.StateTimer:F2}s");

            if (snapshot.HasWanderingTimer)
            {
                _summaryBuilder.AppendLine($"- Timer de vagar: running={snapshot.WanderingTimerRunning}, finalizado={snapshot.WanderingTimerFinished}, tempo={snapshot.WanderingTimerValue:F2}s de {snapshot.WanderingDuration:F2}s");
            }

            if (snapshot.HasPlayerAnchor)
            {
                _summaryBuilder.AppendLine($"- Âncora de players: {snapshot.PlayerAnchor}");
            }

            if (snapshot.HasAutoFlow)
            {
                _summaryBuilder.AppendLine($"- AutoFlow: ativo={snapshot.AutoFlowActive}, pendente={snapshot.PendingHungryEffects}");
            }

            _summaryBuilder.AppendLine($"- Desejos ativos: {snapshot.DesiresActive}");
            _summaryBuilder.AppendLine($"- Posição: {snapshot.Position}");

            DebugUtility.Log<EaterBehavior>(_summaryBuilder.ToString(), instance: this);
        }

        public EaterBehaviorDebugSnapshot CreateDebugSnapshot()
        {
            if (_context == null)
            {
                return EaterBehaviorDebugSnapshot.Empty;
            }

            Vector3 anchor = default;
            bool hasAnchor = _context.TryGetCachedPlayerAnchor(out anchor);
            var target = _context.Target;
            string targetName = target?.Owner?.ActorName ?? target?.Owner?.Transform?.name ?? string.Empty;

            return new EaterBehaviorDebugSnapshot(
                true,
                GetStateName(_stateMachine?.CurrentState),
                _context.IsHungry,
                _context.IsEating,
                _context.HasTarget,
                targetName,
                _context.StateTimer,
                _context.HasWanderingTimer,
                _context.IsWanderingTimerRunning,
                _context.HasWanderingTimerElapsed(),
                _context.GetWanderingTimerValue(),
                _context.Config.WanderingDuration,
                _context.Transform.position,
                hasAnchor,
                anchor,
                _context.HasAutoFlowService,
                _context.IsAutoFlowActive,
                _context.AreDesiresActive,
                _context.HasPendingHungryEffects
            );
        }

        private static string GetStateName(IState state)
        {
            return state?.GetType().Name ?? "None";
        }

        /// <summary>
        /// Garante que o toggle de execução fora da sessão seja inicializado com o valor padrão seguro.
        /// </summary>
        private void EnsureExecutionToggleInitialized()
        {
            if (executionToggleInitialized)
            {
                return;
            }

            updateWhileGameInactive = true;
            executionToggleInitialized = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureExecutionToggleInitialized();
        }
#endif
    }
}
