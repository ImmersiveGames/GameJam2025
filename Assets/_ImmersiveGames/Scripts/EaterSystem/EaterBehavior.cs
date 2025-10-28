using System;
using System.Text;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.EaterSystem.Debug;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
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
        [SerializeField, Tooltip("Emissor de áudio responsável pelos efeitos do Eater.")]
        private EntityAudioEmitter audioEmitter;
        [SerializeField, Tooltip("Som reproduzido sempre que um novo desejo é sorteado.")]
        private SoundData desireSelectedSound;

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
        private EaterDesireInfo _currentDesireInfo = EaterDesireInfo.Inactive;
        private SoundData _resolvedDesireSound;
        private bool _warnedMissingAudioEmitter;
        private bool _warnedMissingDesireSound;
        private EventBinding<PlanetMarkingChangedEvent> _planetMarkingChangedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;
        private EventBinding<PlanetDestroyedEvent> _planetDestroyedBinding;
        private EventBinding<ResourceUpdateEvent> _resourceUpdateBinding;

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
        public event Action<EaterDesireInfo> EventDesireChanged;
        public event Action<PlanetsMaster> EventTargetChanged;
        public event Action<PlanetsMaster, bool> EventTargetProximityChanged;

        public IState CurrentState => _stateMachine?.CurrentState;
        public string CurrentStateName => GetStateName(_stateMachine?.CurrentState);
        public EaterDesireInfo CurrentDesireInfo => _currentDesireInfo;
        public PlanetsMaster CurrentTarget => _context?.TargetPlanet;
        public bool IsEating => _context?.IsEating ?? false;
        public bool ShouldEnableProximitySensor => _context?.ShouldEnableProximitySensor ?? false;
        public bool IsTargetInProximity => _context?.IsTargetInProximity ?? false;

        private void Awake()
        {
            EnsureExecutionToggleInitialized();

            _master = GetComponent<EaterMaster>();
            audioEmitter ??= GetComponent<EntityAudioEmitter>();
            var config = overrideConfig != null ? overrideConfig : _master.Config;

            if (config == null)
            {
                DebugUtility.LogError<EaterBehavior>("Configuração do Eater não definida.", this);
                enabled = false;
                return;
            }

            Rect gameArea = GameManager.Instance != null ? GameManager.Instance.GameConfig.gameArea : new Rect(-50f, -50f, 100f, 100f);
            _context = new EaterBehaviorContext(_master, config, gameArea);
            _currentDesireInfo = _context.CurrentDesireInfo;
            _resolvedDesireSound = desireSelectedSound != null ? desireSelectedSound : config?.DesireSelectedSound;
            _context.EventDesireChanged += HandleContextDesireChanged;
            _context.EventTargetChanged += HandleContextTargetChanged;
            _context.EventTargetProximityChanged += HandleContextTargetProximityChanged;
        }

        private void OnEnable()
        {
            if (!enabled)
            {
                return;
            }

            RegisterEventListeners();
        }

        private void OnDisable()
        {
            UnregisterEventListeners();

            if (_context != null)
            {
                // Garantir que sistemas dependentes parem quando o comportamento for desativado.
                _context.EndDesires();
                _context.PauseAutoFlow();
            }
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            BuildStateMachine();
        }

        private void OnDestroy()
        {
            UnregisterEventListeners();

            if (_context != null)
            {
                // Prevenir atualizações tardias acessando um master destruído.
                _context.EndDesires();
                _context.PauseAutoFlow();
            }

            if (_context != null)
            {
                _context.EventDesireChanged -= HandleContextDesireChanged;
                _context.EventTargetChanged -= HandleContextTargetChanged;
                _context.EventTargetProximityChanged -= HandleContextTargetProximityChanged;
                _context.Dispose();
            }
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
            _context.UpdateServices();
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
        /// Atualiza o planeta alvo perseguido pelo Eater.
        /// </summary>
        public void SetTarget(PlanetsMaster target)
        {
            if (_context == null)
            {
                return;
            }

            bool changed = _context.SetTarget(target);
            if (changed)
            {
                DebugUtility.LogVerbose<EaterBehavior>($"Alvo atualizado: {GetPlanetName(target)}.", null, this);
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

        public void NotifyTargetProximityEntered(PlanetsMaster planet)
        {
            if (_context == null)
            {
                return;
            }

            bool changed = _context.MarkTargetProximity(planet);
            if (changed)
            {
                DebugUtility.LogVerbose<EaterBehavior>(
                    $"Planeta {GetPlanetName(planet)} entrou na proximidade do Eater.",
                    DebugUtility.Colors.Success,
                    this);
            }
        }

        public void NotifyTargetProximityExited(PlanetsMaster planet)
        {
            if (_context == null)
            {
                return;
            }

            bool changed = _context.UnmarkTargetProximity(planet);
            if (changed)
            {
                DebugUtility.LogVerbose<EaterBehavior>(
                    $"Planeta {GetPlanetName(planet)} saiu da proximidade do Eater.",
                    null,
                    this);
            }
        }

        public EaterDesireInfo GetCurrentDesireInfo()
        {
            return _context != null ? _currentDesireInfo : EaterDesireInfo.Inactive;
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
                PlanetsMaster target = _context.Target;
                if (target != null)
                {
                    if (_context.Master != null)
                    {
                        _context.Master.OnEventStartEatPlanet(target);
                    }
                }
                _context.ResetStateTimer();
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
                PlanetsMaster target = _context.Target;
                if (target != null)
                {
                    if (_context.Master != null)
                    {
                        _context.Master.OnEventEndEatPlanet(target);
                    }
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
                PlanetsMaster target = _context.Target;
                if (target != null)
                {
                    _context.Master.OnEventStartEatPlanet(target);
                }
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

        private void HandleContextDesireChanged(EaterDesireInfo info)
        {
            EaterDesireInfo previousInfo = _currentDesireInfo;
            _currentDesireInfo = info;
            TryPlayDesireSound(previousInfo, info);
            EventDesireChanged?.Invoke(info);
        }

        private void HandleContextTargetChanged(PlanetsMaster previous, PlanetsMaster current)
        {
            if (_context == null)
            {
                return;
            }

            bool targetChanged = !IsSameActor(previous, current);
            if (previous != null && targetChanged && _context.IsEating)
            {
                bool stopped = _context.SetEating(false);
                if (stopped)
                {
                    DebugUtility.LogVerbose<EaterBehavior>(
                        $"Alvo atualizado enquanto o Eater comia. Encerrando consumo do planeta {GetPlanetName(previous)}.",
                        DebugUtility.Colors.Success,
                        this);

                    _context.Master.OnEventEndEatPlanet(previous);
                    ForceStateEvaluation();
                }
            }

            EventTargetChanged?.Invoke(current);
        }

        private void HandleContextTargetProximityChanged(PlanetsMaster planet, bool isInProximity)
        {
            EventTargetProximityChanged?.Invoke(planet, isInProximity);
        }

        private void RegisterEventListeners()
        {
            if (_planetMarkingChangedBinding == null)
            {
                _planetMarkingChangedBinding = new EventBinding<PlanetMarkingChangedEvent>(HandlePlanetMarkingChanged);
            }

            if (_planetUnmarkedBinding == null)
            {
                _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(HandlePlanetUnmarked);
            }

            if (_planetDestroyedBinding == null)
            {
                _planetDestroyedBinding = new EventBinding<PlanetDestroyedEvent>(HandlePlanetDestroyed);
            }

            if (_resourceUpdateBinding == null)
            {
                _resourceUpdateBinding = new EventBinding<ResourceUpdateEvent>(HandleResourceUpdated);
            }

            EventBus<PlanetMarkingChangedEvent>.Register(_planetMarkingChangedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
            EventBus<PlanetDestroyedEvent>.Register(_planetDestroyedBinding);
            EventBus<ResourceUpdateEvent>.Register(_resourceUpdateBinding);
        }

        private void UnregisterEventListeners()
        {
            if (_planetMarkingChangedBinding != null)
            {
                EventBus<PlanetMarkingChangedEvent>.Unregister(_planetMarkingChangedBinding);
            }

            if (_planetUnmarkedBinding != null)
            {
                EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
            }

            if (_planetDestroyedBinding != null)
            {
                EventBus<PlanetDestroyedEvent>.Unregister(_planetDestroyedBinding);
            }

            if (_resourceUpdateBinding != null)
            {
                EventBus<ResourceUpdateEvent>.Unregister(_resourceUpdateBinding);
            }
        }

        private void HandlePlanetMarkingChanged(PlanetMarkingChangedEvent evt)
        {
            if (_context == null)
            {
                return;
            }

            IActor newMarked = evt.NewMarkedPlanet;
            if (newMarked == null)
            {
                if (_context.ClearTarget())
                {
                    DebugUtility.LogVerbose<EaterBehavior>("Planeta marcado removido. Alvo do Eater limpo.", null, this);
                    ForceStateEvaluation();
                }
                return;
            }

            PlanetsManager manager = PlanetsManager.Instance;
            if (manager == null || !manager.TryGetPlanet(newMarked, out PlanetsMaster newTarget))
            {
                DebugUtility.LogWarning<EaterBehavior>(
                    $"Não foi possível localizar o planeta marcado em PlanetsManager: {newMarked.ActorName}.",
                    this);
                return;
            }

            bool changed = _context.SetTarget(newTarget);
            if (!changed)
            {
                return;
            }

            DebugUtility.LogVerbose<EaterBehavior>($"Planeta marcado definido como alvo: {GetPlanetName(newTarget)}.", null, this);
            ForceStateEvaluation();
        }

        private void HandlePlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            if (_context == null || !_context.HasTarget)
            {
                return;
            }

            IActor unmarked = evt.PlanetActor;
            if (unmarked == null)
            {
                return;
            }

            PlanetsMaster currentTarget = _context.TargetPlanet;
            if (currentTarget == null)
            {
                return;
            }

            if (!IsSameActor(currentTarget, unmarked))
            {
                return;
            }

            if (_context.ClearTarget())
            {
                string planetName = GetPlanetName(currentTarget);
                DebugUtility.LogVerbose<EaterBehavior>($"Planeta desmarcado removido do alvo: {planetName}.", null, this);
                ForceStateEvaluation();
            }
        }

        private void HandlePlanetDestroyed(PlanetDestroyedEvent evt)
        {
            if (_context == null || !_context.HasTarget || evt?.Detected?.Owner == null)
            {
                return;
            }

            PlanetsMaster currentTarget = _context.TargetPlanet;
            if (currentTarget == null)
            {
                return;
            }

            if (!IsSameActor(currentTarget, evt.Detected.Owner))
            {
                return;
            }

            if (_context.ClearTarget())
            {
                string planetName = GetPlanetName(currentTarget);
                DebugUtility.LogVerbose<EaterBehavior>($"Planeta alvo destruído removido: {planetName}.", null, this);
                ForceStateEvaluation();
            }
        }

        private void HandleResourceUpdated(ResourceUpdateEvent evt)
        {
            if (_context == null || evt?.NewValue == null || _master == null)
            {
                return;
            }

            if (evt.ActorId != _master.ActorId)
            {
                return;
            }

            if (evt.ResourceType != _context.Config.SatiationResourceType)
            {
                return;
            }

            if (!evt.NewValue.IsFull() || !_context.IsHungry)
            {
                return;
            }

            DebugUtility.LogVerbose<EaterBehavior>(
                $"Recurso de saciedade cheio ({evt.ResourceType}). Retornando ao estado Vagando.",
                null,
                this);
            SetHungry(false);
        }

        private static bool IsSameActor(IActor left, IActor right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.ActorId == right.ActorId;
        }

        private static bool IsSameActor(PlanetsMaster planet, IActor actor)
        {
            if (planet == null || actor == null)
            {
                return false;
            }

            return planet.ActorId == actor.ActorId;
        }

        /// <summary>
        /// Resolve o SoundData utilizado para o áudio de desejos considerando overrides e configuração global.
        /// </summary>
        private void EnsureDesireSoundResolved()
        {
            if (_resolvedDesireSound != null)
            {
                return;
            }

            SoundData configSound = null;
            if (overrideConfig != null)
            {
                configSound = overrideConfig.DesireSelectedSound;
            }

            if (configSound == null && _context != null)
            {
                configSound = _context.Config?.DesireSelectedSound;
            }

            _resolvedDesireSound = desireSelectedSound != null ? desireSelectedSound : configSound;
        }

        /// <summary>
        /// Avalia se um novo desejo foi sorteado e dispara o som correspondente via AudioSystem.
        /// </summary>
        private void TryPlayDesireSound(EaterDesireInfo previousInfo, EaterDesireInfo newInfo)
        {
            if (!newInfo.ServiceActive || !newInfo.HasDesire || !newInfo.HasResource)
            {
                return;
            }

            EnsureDesireSoundResolved();

            if (!EnsureAudioEmitter())
            {
                return;
            }

            if (_resolvedDesireSound == null || _resolvedDesireSound.clip == null)
            {
                if (!_warnedMissingDesireSound)
                {
                    DebugUtility.LogWarning<EaterBehavior>(
                        "SoundData para o desejo do Eater não foi configurado.",
                        this);
                    _warnedMissingDesireSound = true;
                }
                return;
            }

            bool hadPrevious = previousInfo.ServiceActive && previousInfo.HasDesire && previousInfo.HasResource;
            bool resourceChanged = !hadPrevious || previousInfo.Resource != newInfo.Resource;
            bool timerReset = newInfo.Duration > 0f &&
                              newInfo.RemainingTime >= Mathf.Max(newInfo.Duration - 0.01f, 0f);

            if (!resourceChanged && !timerReset)
            {
                return;
            }

            _warnedMissingDesireSound = false;

            Transform emitterTransform = audioEmitter != null ? audioEmitter.transform : transform;
            Vector3 position = emitterTransform.position;
            bool spatial = audioEmitter != null && audioEmitter.UsesSpatialBlend;
            AudioContext audioContext = AudioContext.Default(position, spatial);

            audioEmitter.Play(_resolvedDesireSound, audioContext);

            DebugUtility.LogVerbose<EaterBehavior>(
                $"🔊 Som de desejo reproduzido para {newInfo.Resource!.Value} (disp={newInfo.IsAvailable}, planetas={newInfo.AvailableCount}).",
                null,
                this);
        }

        /// <summary>
        /// Garante a presença do emissor de áudio necessário para reproduzir o som de desejos.
        /// </summary>
        private bool EnsureAudioEmitter()
        {
            if (audioEmitter != null)
            {
                _warnedMissingAudioEmitter = false;
                return true;
            }

            if (!TryGetComponent(out audioEmitter))
            {
                if (!_warnedMissingAudioEmitter)
                {
                    DebugUtility.LogWarning<EaterBehavior>(
                        "EntityAudioEmitter não encontrado. O som de desejo não será reproduzido.",
                        this);
                    _warnedMissingAudioEmitter = true;
                }
                return false;
            }

            _warnedMissingAudioEmitter = false;
            return true;
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

            if (snapshot.HasMovementSample)
            {
                _summaryBuilder.AppendLine($"- Movimento: direção={snapshot.MovementDirection}, velocidade={snapshot.MovementSpeed:F2}");
            }

            if (snapshot.HasHungryMetrics)
            {
                _summaryBuilder.AppendLine($"- Métricas de fome: distânciaJogadores={snapshot.PlayerAnchorDistance:F2}, alinhamento={snapshot.PlayerAnchorAlignment:F2}");
            }

            if (snapshot.DesiresActive)
            {
                string desireInfo = snapshot.HasCurrentDesire
                    ? $"{snapshot.CurrentDesireName} (disp={snapshot.CurrentDesireAvailable}, planetas={snapshot.CurrentDesireAvailableCount}, peso={snapshot.CurrentDesireWeight:F2}, restante={snapshot.CurrentDesireRemaining:F2}s de {snapshot.CurrentDesireDuration:F2}s)"
                    : "Aguardando sorteio";
                _summaryBuilder.AppendLine($"- Desejos: ativo=True, atual={desireInfo}");
            }
            else
            {
                _summaryBuilder.AppendLine("- Desejos: ativo=False");
            }

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
            PlanetsMaster target = _context.Target;
            string targetName = target != null ? GetPlanetName(target) : string.Empty;

            PlanetResources? currentDesire = _context.CurrentDesire;
            string currentDesireName = currentDesire.HasValue ? currentDesire.Value.ToString() : string.Empty;

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
                _context.HasPendingHungryEffects,
                _context.HasMovementSample,
                _context.LastMovementDirection,
                _context.LastMovementSpeed,
                _context.HasHungryMetrics,
                _context.HasHungryMetrics ? _context.LastAnchorDistance : 0f,
                _context.HasHungryMetrics ? _context.LastAnchorAlignment : 0f,
                _context.HasCurrentDesire,
                currentDesireName,
                _context.CurrentDesireAvailable,
                _context.CurrentDesireRemainingTime,
                _context.CurrentDesireDuration,
                _context.CurrentDesireAvailableCount,
                _context.CurrentDesireWeight
            );
        }

        private static string GetStateName(IState state)
        {
            return state?.GetType().Name ?? "None";
        }

        private static string GetPlanetName(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return "Nenhum";
            }

            string actorName = planet.ActorName;
            return string.IsNullOrEmpty(actorName) ? planet.name : actorName;
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
            audioEmitter ??= GetComponent<EntityAudioEmitter>();
        }
#endif
    }
}
