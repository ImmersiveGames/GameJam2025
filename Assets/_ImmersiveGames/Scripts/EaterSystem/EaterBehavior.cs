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
using ImprovedTimers;
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
        private EaterConfigSo _config;
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

        private Rect _gameArea;
        private bool _isHungry;
        private bool _isEating;
        private PlanetsMaster _targetPlanet;
        private float _stateTimer;
        private CountdownTimer _wanderingTimer;
        private Vector3 _lastKnownPlayerAnchor;
        private bool _hasCachedPlayerAnchor;
        private bool _pendingHungryEffects;
        private ResourceAutoFlowBridge _autoFlowBridge;
        private EaterDesireService _desireService;
        private bool _hasMovementSample;
        private Vector3 _lastMovementDirection;
        private float _lastMovementSpeed;
        private bool _hasHungryMetrics;
        private float _lastAnchorDistance;
        private float _lastAnchorAlignment;
        private bool _hasProximityContact;
        private PlanetsMaster _proximityPlanet;
        private Vector3 _proximityHoldPosition;
        private bool _hasProximityHoldPosition;

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

        public IState CurrentState => _stateMachine?.CurrentState;
        public string CurrentStateName => GetStateName(_stateMachine?.CurrentState);
        public EaterDesireInfo CurrentDesireInfo => _currentDesireInfo;
        public PlanetsMaster CurrentTarget => _targetPlanet;
        public bool IsEating => _isEating;
        public bool ShouldEnableProximitySensor => ShouldChase || ShouldEat;
        public EaterMaster Master => _master;
        public EaterConfigSo Config => _config;
        public Rect GameArea => _gameArea;
        internal bool IsHungry => _isHungry;
        internal bool ShouldChase => _isHungry && _targetPlanet != null && !_isEating;
        internal bool ShouldEat => _isEating && _targetPlanet != null;
        internal bool LostTargetWhileHungry => _isHungry && _targetPlanet == null && !_isEating;
        internal bool HasTarget => _targetPlanet != null;
        internal bool HasWanderingTimer => _wanderingTimer != null;
        internal bool IsWanderingTimerRunning => _wanderingTimer != null && _wanderingTimer.IsRunning;

        private void Awake()
        {
            EnsureExecutionToggleInitialized();

            _master = GetComponent<EaterMaster>();
            audioEmitter ??= GetComponent<EntityAudioEmitter>();
            _config = overrideConfig != null ? overrideConfig : _master.Config;

            if (_config == null)
            {
                DebugUtility.LogError<EaterBehavior>("Configuração do Eater não definida.", this);
                enabled = false;
                return;
            }

            _gameArea = GameManager.Instance != null ? GameManager.Instance.GameConfig.gameArea : new Rect(-50f, -50f, 100f, 100f);

            if (_master.TryGetComponent(out ResourceAutoFlowBridge autoFlowBridge))
            {
                _autoFlowBridge = autoFlowBridge;
            }

            _desireService = new EaterDesireService(_master, _config);
            _desireService.EventDesireChanged += HandleServiceDesireChanged;
            _currentDesireInfo = EaterDesireInfo.Inactive;

            if (_config.WanderingDuration > 0f)
            {
                _wanderingTimer = new CountdownTimer(_config.WanderingDuration);
            }

            _resolvedDesireSound = desireSelectedSound != null ? desireSelectedSound : _config?.DesireSelectedSound;
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

            // Garantir que sistemas dependentes parem quando o comportamento for desativado.
            EndDesires();
            PauseAutoFlow();
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

            // Prevenir atualizações tardias acessando um master destruído.
            EndDesires();
            PauseAutoFlow();

            if (_desireService != null)
            {
                _desireService.EventDesireChanged -= HandleServiceDesireChanged;
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
            UpdateServices();
            EnsureHungryEffects();
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
            bool changed = SetHungryInternal(isHungry);
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
            bool changed = SetTargetInternal(target);
            if (changed)
            {
                DebugUtility.LogVerbose<EaterBehavior>($"Alvo atualizado: {GetPlanetName(target)}.", null, this);
            }
        }

        /// <summary>
        /// Limpa o alvo atual.
        /// </summary>
        public void ClearTarget()
        {
            SetTarget(null);
        }

        public EaterDesireInfo GetCurrentDesireInfo()
        {
            return _currentDesireInfo;
        }

        /// <summary>
        /// Solicita que o Eater inicie a ação de comer.
        /// </summary>
        public void BeginEating()
        {
            bool changed = SetEatingInternal(true);
            if (changed)
            {
                ClearMovementSample();
                DebugUtility.LogVerbose<EaterBehavior>("Início manual do estado Comendo.");
                PlanetsMaster target = _targetPlanet;
                if (target != null)
                {
                    _master?.OnEventStartEatPlanet(target);
                }
                ResetStateTimer();
                ForceStateEvaluation();
                return;
            }

            ClearMovementSample();
        }

        /// <summary>
        /// Informa o contexto de que o sensor de proximidade detectou o alvo atual.
        /// Responsável por travar o movimento e iniciar o estado de comer quando necessário.
        /// </summary>
        public void RegisterProximityContact(PlanetsMaster planet, Vector3 eaterPosition)
        {
            if (planet == null)
            {
                return;
            }

            bool changed = RegisterProximityContactInternal(planet, eaterPosition);
            if (!_isEating)
            {
                BeginEating();
                return;
            }

            if (changed)
            {
                ForceStateEvaluation();
            }
        }

        /// <summary>
        /// Remove o lock de proximidade ativo, liberando o movimento do Eater.
        /// </summary>
        public void ClearProximityContact(PlanetsMaster planet = null)
        {
            bool cleared = ClearProximityContactInternal(planet);
            if (cleared)
            {
                ClearMovementSample();
            }
        }

        /// <summary>
        /// Finaliza o estado de comer e limpa o alvo se necessário.
        /// </summary>
        public void EndEating(bool satiated)
        {
            bool wasEating = SetEatingInternal(false);
            if (wasEating)
            {
                DebugUtility.LogVerbose<EaterBehavior>("Fim manual do estado Comendo.");
                PlanetsMaster target = _targetPlanet;
                if (target != null)
                {
                    _master?.OnEventEndEatPlanet(target);
                }
            }

            if (satiated)
            {
                SetHungryInternal(false);
            }

            ForceStateEvaluation();
        }

        private void BuildStateMachine()
        {
            var builder = new StateMachineBuilder();

            builder
                .AddState(new EaterWanderingState(this), out _wanderingState)
                .AddState(new EaterHungryState(this), out _hungryState)
                .AddState(new EaterChasingState(this), out _chasingState)
                .AddState(new EaterEatingState(this), out _eatingState)
                .At(_wanderingState, _hungryState, new FuncPredicate(() => _isHungry && !_isEating))
                .At(_wanderingState, _eatingState, new FuncPredicate(() => _isEating))
                .At(_hungryState, _wanderingState, new FuncPredicate(() => !_isHungry))
                .At(_hungryState, _chasingState, new FuncPredicate(() => ShouldChase))
                .At(_hungryState, _eatingState, new FuncPredicate(() => ShouldEat))
                .At(_chasingState, _hungryState, new FuncPredicate(() => LostTargetWhileHungry))
                .At(_chasingState, _wanderingState, new FuncPredicate(() => !_isHungry && !_isEating))
                .At(_chasingState, _eatingState, new FuncPredicate(() => ShouldEat))
                .At(_eatingState, _hungryState, new FuncPredicate(() => _isHungry && !_isEating))
                .At(_eatingState, _wanderingState, new FuncPredicate(() => !_isHungry && !_isEating))
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

            SetEatingInternal(false);
            SetHungryInternal(false);
            ClearTargetInternal();
            RestartWanderingTimer();
            ForceSetState(_wanderingState);
        }

        [ContextMenu("Eater States/Force Hungry")]
        private void ContextForceHungry()
        {
            if (!EnsureStateMachineReady())
            {
                return;
            }

            SetEatingInternal(false);
            SetHungryInternal(true);
            ForceSetState(_hungryState);
        }

        [ContextMenu("Eater States/Force Chasing")]
        private void ContextForceChasing()
        {
            if (!EnsureStateMachineReady())
            {
                return;
            }

            if (!HasTarget)
            {
                DebugUtility.LogWarning<EaterBehavior>("Não há alvo configurado para iniciar a perseguição.", this);
                return;
            }

            SetHungryInternal(true);
            SetEatingInternal(false);
            ForceSetState(_chasingState);
        }

        [ContextMenu("Eater States/Force Eating")]
        private void ContextForceEating()
        {
            if (!EnsureStateMachineReady())
            {
                return;
            }

            if (!HasTarget)
            {
                DebugUtility.LogWarning<EaterBehavior>("Não há alvo configurado para iniciar o consumo.", this);
                return;
            }

            SetHungryInternal(true);
            bool startedEating = SetEatingInternal(true);
            if (startedEating)
            {
                PlanetsMaster target = _targetPlanet;
                if (target != null)
                {
                    _master?.OnEventStartEatPlanet(target);
                }
            }

            ForceSetState(_eatingState);
        }

        private bool EnsureStateMachineReady()
        {
            if (!_stateMachineBuilt || _stateMachine == null)
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

        private void HandleServiceDesireChanged(EaterDesireInfo info)
        {
            EaterDesireInfo previousInfo = _currentDesireInfo;
            _currentDesireInfo = info;
            TryPlayDesireSound(previousInfo, info);
            EventDesireChanged?.Invoke(info);
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
            IActor newMarked = evt.NewMarkedPlanet;
            if (newMarked == null)
            {
                if (ClearTargetInternal())
                {
                    DebugUtility.LogVerbose<EaterBehavior>("Planeta marcado removido. Alvo do Eater limpo.", null, this);
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

            bool changed = SetTargetInternal(newTarget);
            if (!changed)
            {
                return;
            }

            DebugUtility.LogVerbose<EaterBehavior>($"Planeta marcado definido como alvo: {GetPlanetName(newTarget)}.", null, this);
        }

        private void HandlePlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            if (!HasTarget)
            {
                return;
            }

            IActor unmarked = evt.PlanetActor;
            if (unmarked == null)
            {
                return;
            }

            PlanetsMaster currentTarget = _targetPlanet;
            if (currentTarget == null)
            {
                return;
            }

            if (!IsSameActor(currentTarget, unmarked))
            {
                return;
            }

            if (ClearTargetInternal())
            {
                string planetName = GetPlanetName(currentTarget);
                DebugUtility.LogVerbose<EaterBehavior>($"Planeta desmarcado removido do alvo: {planetName}.", null, this);
            }
        }

        private void HandlePlanetDestroyed(PlanetDestroyedEvent evt)
        {
            if (!HasTarget || evt?.Detected?.Owner == null)
            {
                return;
            }

            PlanetsMaster currentTarget = _targetPlanet;
            if (currentTarget == null)
            {
                return;
            }

            if (!IsSameActor(currentTarget, evt.Detected.Owner))
            {
                return;
            }

            if (ClearTargetInternal())
            {
                string planetName = GetPlanetName(currentTarget);
                DebugUtility.LogVerbose<EaterBehavior>($"Planeta alvo destruído removido: {planetName}.", null, this);
            }
        }

        private void HandleResourceUpdated(ResourceUpdateEvent evt)
        {
            if (evt?.NewValue == null || _master == null)
            {
                return;
            }

            if (evt.ActorId != _master.ActorId)
            {
                return;
            }

            if (_config == null || evt.ResourceType != _config.SatiationResourceType)
            {
                return;
            }

            if (!evt.NewValue.IsFull() || !_isHungry)
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

        private static bool IsSamePlanet(PlanetsMaster left, PlanetsMaster right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.ActorId == right.ActorId;
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

            SoundData configSound = overrideConfig != null ? overrideConfig.DesireSelectedSound : null;
            if (configSound == null)
            {
                configSound = _config != null ? _config.DesireSelectedSound : null;
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

        internal void ResetStateTimer()
        {
            _stateTimer = 0f;
        }

        internal void AdvanceStateTimer(float deltaTime)
        {
            _stateTimer += Mathf.Max(deltaTime, 0f);
        }

        internal bool TryGetTargetPosition(out Vector3 position)
        {
            if (_targetPlanet != null)
            {
                position = _targetPlanet.transform.position;
                return true;
            }

            position = default;
            return false;
        }

        internal bool TryGetPlayerAnchor(out Vector3 anchor)
        {
            var playerManager = PlayerManager.Instance;
            if (playerManager != null)
            {
                Vector3 accumulator = Vector3.zero;
                int validPlayers = 0;
                var players = playerManager.Players;
                for (int i = 0; i < players.Count; i++)
                {
                    Transform player = players[i];
                    if (player == null)
                    {
                        continue;
                    }

                    accumulator += player.position;
                    validPlayers++;
                }

                if (validPlayers > 0)
                {
                    anchor = accumulator / validPlayers;
                    _lastKnownPlayerAnchor = anchor;
                    _hasCachedPlayerAnchor = true;
                    return true;
                }
            }

            if (_hasCachedPlayerAnchor)
            {
                anchor = _lastKnownPlayerAnchor;
                return true;
            }

            anchor = transform.position;
            return false;
        }

        internal bool TryGetCachedPlayerAnchor(out Vector3 anchor)
        {
            if (_hasCachedPlayerAnchor)
            {
                anchor = _lastKnownPlayerAnchor;
                return true;
            }

            anchor = transform.position;
            return false;
        }

        internal void RestartWanderingTimer()
        {
            if (_wanderingTimer == null)
            {
                return;
            }

            _wanderingTimer.Stop();
            _wanderingTimer.Reset(Mathf.Max(_config.WanderingDuration, 0.1f));
            _wanderingTimer.Start();
        }

        internal void StopWanderingTimer()
        {
            _wanderingTimer?.Stop();
        }

        internal bool HasWanderingTimerElapsed()
        {
            return _wanderingTimer != null && _wanderingTimer.IsFinished;
        }

        internal float GetWanderingTimerValue()
        {
            return _wanderingTimer != null ? _wanderingTimer.CurrentTime : 0f;
        }

        internal void ReportMovementSample(Vector3 direction, float speed)
        {
            _lastMovementDirection = direction;
            _lastMovementSpeed = speed;
            _hasMovementSample = direction.sqrMagnitude > 0f || speed > 0f;
            _hasHungryMetrics = false;
        }

        internal void ClearMovementSample()
        {
            _lastMovementDirection = Vector3.zero;
            _lastMovementSpeed = 0f;
            _hasMovementSample = false;
            _hasHungryMetrics = false;
        }

        internal void ReportHungryMetrics(float distanceToAnchor, float alignmentWithAnchor)
        {
            _lastAnchorDistance = Mathf.Max(distanceToAnchor, 0f);
            _lastAnchorAlignment = Mathf.Clamp(alignmentWithAnchor, -1f, 1f);
            _hasHungryMetrics = true;
        }

        internal bool RegisterProximityContactInternal(PlanetsMaster planet, Vector3 eaterPosition)
        {
            if (planet == null)
            {
                return false;
            }

            bool changed = !_hasProximityContact || !IsSamePlanet(_proximityPlanet, planet);
            _hasProximityContact = true;
            _proximityPlanet = planet;
            _proximityHoldPosition = eaterPosition;
            _hasProximityHoldPosition = true;
            ClearMovementSample();
            return changed;
        }

        internal bool ClearProximityContactInternal(PlanetsMaster planet = null)
        {
            if (!_hasProximityContact)
            {
                return false;
            }

            if (planet != null && !IsSamePlanet(_proximityPlanet, planet))
            {
                return false;
            }

            ClearProximityContactState();
            return true;
        }

        private void ClearProximityContactState()
        {
            _hasProximityContact = false;
            _proximityPlanet = null;
            _hasProximityHoldPosition = false;
        }

        internal bool TryGetProximityHoldPosition(out Vector3 position)
        {
            if (_hasProximityHoldPosition)
            {
                position = _proximityHoldPosition;
                return true;
            }

            position = default;
            return false;
        }

        internal bool HasProximityContactForTarget => _hasProximityContact && _proximityPlanet != null && _targetPlanet != null && IsSamePlanet(_proximityPlanet, _targetPlanet);
        internal bool HasMovementSample => _hasMovementSample;
        internal Vector3 LastMovementDirection => _lastMovementDirection;
        internal float LastMovementSpeed => _lastMovementSpeed;
        internal bool HasHungryMetrics => _hasHungryMetrics;
        internal float LastAnchorDistance => _lastAnchorDistance;
        internal float LastAnchorAlignment => _lastAnchorAlignment;
        internal float StateTimer => _stateTimer;
        internal bool HasPendingHungryEffects => _pendingHungryEffects;
        internal bool HasAutoFlowService => _autoFlowBridge != null && _autoFlowBridge.HasAutoFlowService;
        internal bool IsAutoFlowActive => _autoFlowBridge != null && _autoFlowBridge.IsAutoFlowActive;
        internal bool AreDesiresActive => _desireService != null && _desireService.IsActive;
        internal bool HasCurrentDesire => _desireService != null && _desireService.HasActiveDesire;
        internal PlanetResources? CurrentDesire => _desireService?.CurrentDesire;
        internal bool CurrentDesireAvailable => _desireService != null && _desireService.CurrentDesireAvailable;
        internal float CurrentDesireRemainingTime => _desireService != null ? _desireService.CurrentDesireRemainingTime : 0f;
        internal float CurrentDesireDuration => _desireService != null ? _desireService.CurrentDesireDuration : 0f;
        internal int CurrentDesireAvailableCount => _desireService != null ? _desireService.CurrentDesireAvailableCount : 0;
        internal float CurrentDesireWeight => _desireService != null ? _desireService.CurrentDesireWeight : 0f;

        private bool SetHungryInternal(bool value)
        {
            if (_isHungry == value)
            {
                return false;
            }

            _isHungry = value;
            if (_master != null)
            {
                _master.InHungry = value;
            }

            if (value)
            {
                bool autoFlowActive = ResumeAutoFlow();
                BeginDesires();
                _pendingHungryEffects = _autoFlowBridge != null && !autoFlowActive;
            }
            else
            {
                PauseAutoFlow();
                EndDesires();
                _pendingHungryEffects = false;
            }

            return true;
        }

        private bool SetEatingInternal(bool value)
        {
            if (_isEating == value)
            {
                return false;
            }

            _isEating = value;
            if (_master != null)
            {
                _master.IsEating = value;
            }

            if (!value)
            {
                ClearProximityContactState();
            }

            return true;
        }

        private bool SetTargetInternal(PlanetsMaster target)
        {
            PlanetsMaster previous = _targetPlanet;
            if (previous == target)
            {
                return false;
            }

            bool targetChanged = !IsSameActor(previous, target);
            _targetPlanet = target;

            if (_hasProximityContact && !HasProximityContactForTarget)
            {
                ClearProximityContactState();
            }

            if (targetChanged && previous != null && _isEating)
            {
                bool stopped = SetEatingInternal(false);
                if (stopped)
                {
                    DebugUtility.LogVerbose<EaterBehavior>(
                        $"Alvo atualizado enquanto o Eater comia. Encerrando consumo do planeta {GetPlanetName(previous)}.",
                        DebugUtility.Colors.Success,
                        this);

                    _master?.OnEventEndEatPlanet(previous);
                }
            }

            if (targetChanged)
            {
                ForceStateEvaluation();
            }

            EventTargetChanged?.Invoke(_targetPlanet);
            return true;
        }

        private bool ClearTargetInternal()
        {
            if (_targetPlanet == null)
            {
                return false;
            }

            PlanetsMaster previous = _targetPlanet;
            _targetPlanet = null;
            if (_isEating)
            {
                bool stopped = SetEatingInternal(false);
                if (stopped)
                {
                    _master?.OnEventEndEatPlanet(previous);
                }
            }

            ClearProximityContactState();
            ForceStateEvaluation();
            EventTargetChanged?.Invoke(null);
            return true;
        }

        private bool ResumeAutoFlow()
        {
            if (_autoFlowBridge == null || !_autoFlowBridge.IsInitialized || !_autoFlowBridge.HasAutoFlowService)
            {
                return false;
            }

            bool resumed = _autoFlowBridge.ResumeAutoFlow();
            if (resumed)
            {
                DebugUtility.LogVerbose<EaterBehavior>($"AutoFlow retomado para {_master?.ActorId ?? name}.");
            }

            return _autoFlowBridge.IsAutoFlowActive;
        }

        private bool PauseAutoFlow()
        {
            if (_autoFlowBridge == null || !_autoFlowBridge.IsInitialized || !_autoFlowBridge.HasAutoFlowService)
            {
                return false;
            }

            bool paused = _autoFlowBridge.PauseAutoFlow();
            if (paused)
            {
                DebugUtility.LogVerbose<EaterBehavior>($"AutoFlow pausado para {_master?.ActorId ?? name}.");
            }

            return paused;
        }

        private bool BeginDesires()
        {
            if (_desireService == null)
            {
                return false;
            }

            bool started = _desireService.Start();
            if (started)
            {
                DebugUtility.LogVerbose<EaterBehavior>($"Desejos iniciados para {_master?.ActorId ?? name}.");
            }

            return started;
        }

        private bool EndDesires()
        {
            if (_desireService == null)
            {
                return false;
            }

            bool stopped = _desireService.Stop();
            if (stopped)
            {
                DebugUtility.LogVerbose<EaterBehavior>($"Desejos pausados para {_master?.ActorId ?? name}.");
            }

            return stopped;
        }

        private void UpdateServices()
        {
            _desireService?.Update();
        }

        private void EnsureHungryEffects()
        {
            if (!_isHungry || !_pendingHungryEffects)
            {
                return;
            }

            bool active = ResumeAutoFlow();
            if (active)
            {
                _pendingHungryEffects = false;
            }
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
            Vector3 anchor = default;
            bool hasAnchor = TryGetCachedPlayerAnchor(out anchor);
            PlanetsMaster target = _targetPlanet;
            string targetName = target != null ? GetPlanetName(target) : string.Empty;

            PlanetResources? currentDesire = CurrentDesire;
            string currentDesireName = currentDesire.HasValue ? currentDesire.Value.ToString() : string.Empty;

            return new EaterBehaviorDebugSnapshot(
                true,
                GetStateName(_stateMachine?.CurrentState),
                _isHungry,
                _isEating,
                HasTarget,
                targetName,
                _stateTimer,
                HasWanderingTimer,
                IsWanderingTimerRunning,
                HasWanderingTimerElapsed(),
                GetWanderingTimerValue(),
                _config != null ? _config.WanderingDuration : 0f,
                transform.position,
                hasAnchor,
                anchor,
                HasAutoFlowService,
                IsAutoFlowActive,
                AreDesiresActive,
                _pendingHungryEffects,
                HasMovementSample,
                _lastMovementDirection,
                _lastMovementSpeed,
                _hasHungryMetrics,
                _hasHungryMetrics ? _lastAnchorDistance : 0f,
                _hasHungryMetrics ? _lastAnchorAlignment : 0f,
                HasCurrentDesire,
                currentDesireName,
                CurrentDesireAvailable,
                CurrentDesireRemainingTime,
                CurrentDesireDuration,
                CurrentDesireAvailableCount,
                CurrentDesireWeight
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
