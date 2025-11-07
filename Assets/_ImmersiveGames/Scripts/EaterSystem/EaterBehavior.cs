using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.EaterSystem.Animations;
using _ImmersiveGames.Scripts.EaterSystem.Debug;
using _ImmersiveGames.Scripts.EaterSystem.Events;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Managers;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    /// <summary>
    /// Comportamento principal do Eater. Mantém os estados registrados e oferece utilitários
    /// compartilhados pelos estados para movimentação, desejos e depuração.
    /// </summary>
    [RequireComponent(typeof(EaterMaster))]
    [DefaultExecutionOrder(10)]
    public sealed class EaterBehavior : MonoBehaviour
    {
        private const string ContextMenuRoot = "Eater States";

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Quando habilitado registra entradas importantes para depuração.")]
        private bool logStateTransitions = true;

        private StateMachine _stateMachine;
        private EaterWanderingState _wanderingState;
        private EaterHungryState _hungryState;
        private EaterChasingState _chasingState;
        private EaterEatingState _eatingState;
        private EaterDeathState _deathState;

        private EaterMaster _master;
        private EaterConfigSo _config;
        private PlayerManager _playerManager;
        private PlanetMarkingManager _planetMarkingManager;
        private PlanetsManager _planetsManager;
        private PlayerAnimationController _animationController;
        private ResourceAutoFlowBridge _autoFlowBridge;
        private EntityAudioEmitter _audioEmitter;

        private bool _loggedMissingAutoFlowBridge;
        private bool _loggedAutoFlowUnavailable;
        private bool _audioEmitterResolved;
        private bool _loggedMissingAudioEmitter;
        private bool _loggedMissingDesireService;

        private EaterDesireService _desireService;
        private EaterDesireInfo _currentDesireInfo = EaterDesireInfo.Inactive;

        private PlanetsMaster _currentTarget;
        private bool _hasProximityContact;
        private PlanetsMaster _proximityPlanet;

        private float _stateTimer;
        private EaterBehaviorState _lastStateSampled;

        private bool _hasMovementSample;
        private Vector3 _lastMovementDirection;
        private float _lastMovementSpeed;

        private bool _hasPlayerAnchorSample;
        private Vector3 _lastPlayerAnchorPosition;
        private float _lastPlayerAnchorDistance;
        private float _lastPlayerAnchorAlignment;

        public event Action<EaterDesireInfo> EventDesireChanged;
        public event Action<IState, IState> EventStateChanged;
        public event Action<PlanetsMaster> EventTargetChanged;
        public event Action<PlanetsMaster, bool> EventProximityContactChanged;

        /// <summary>
        /// Indica se logs de transição devem ser gerados.
        /// </summary>
        internal bool ShouldLogStateTransitions => logStateTransitions;

        public IState CurrentState => _stateMachine?.CurrentState;

        public string CurrentStateName => GetStateName(CurrentState);

        public PlanetsMaster CurrentTarget => _currentTarget;

        internal Transform CurrentTargetPlanet => _currentTarget != null ? _currentTarget.transform : null;

        public bool IsEating => _stateMachine != null && ReferenceEquals(_stateMachine.CurrentState, _eatingState);

        internal bool IsHungry => IsHungryState(_stateMachine?.CurrentState as EaterBehaviorState);

        internal bool HasProximityContact => _hasProximityContact;

        internal bool HasProximityContactForTarget => _hasProximityContact && IsCurrentTarget(_proximityPlanet);

        internal EaterMaster Master => _master;

        internal EaterConfigSo Config => _config;

        internal PlayerAnimationController AnimationController => _animationController;

        private void Awake()
        {
            _master = GetComponent<EaterMaster>();
            _config = _master != null ? _master.Config : null;
            _playerManager = PlayerManager.Instance;
            _planetMarkingManager = PlanetMarkingManager.Instance;
            _planetsManager = PlanetsManager.Instance;
            _animationController = GetComponent<PlayerAnimationController>() ??
                                   GetComponentInChildren<PlayerAnimationController>(true);

            TryEnsureAutoFlowBridge();
            EnsureDesireService();
            EnsureStatesInitialized();
            RefreshCurrentTarget();
            UpdateMasterState();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            _master ??= GetComponent<EaterMaster>();
            _config = _master != null ? _master.Config : null;
        }
#endif

        private void Update()
        {
            RefreshCurrentTarget();
            _desireService?.Update();
            _stateMachine?.Update();
            SamplePlayerAnchor();
            AdvanceStateTimer(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _stateMachine?.FixedUpdate();
        }

        private void EnsureStatesInitialized()
        {
            if (_stateMachine != null)
            {
                return;
            }

            _stateMachine = new StateMachine();

            _wanderingState = RegisterState(new EaterWanderingState());
            _hungryState = RegisterState(new EaterHungryState());
            _chasingState = RegisterState(new EaterChasingState());
            _eatingState = RegisterState(new EaterEatingState());
            _deathState = RegisterState(new EaterDeathState());

            ForceSetState(_wanderingState, "Inicialização");
        }

        [ContextMenu(ContextMenuRoot + "/Set Wandering")]
        private void ContextSetWandering()
        {
            EnsureStatesInitialized();
            ForceSetState(_wanderingState, "ContextMenu/Wandering");
        }

        [ContextMenu(ContextMenuRoot + "/Set Hungry")]
        private void ContextSetHungry()
        {
            EnsureStatesInitialized();
            ForceSetState(_hungryState, "ContextMenu/Hungry");
        }

        [ContextMenu(ContextMenuRoot + "/Set Chasing")]
        private void ContextSetChasing()
        {
            EnsureStatesInitialized();
            ForceSetState(_chasingState, "ContextMenu/Chasing");
        }

        [ContextMenu(ContextMenuRoot + "/Set Eating")]
        private void ContextSetEating()
        {
            EnsureStatesInitialized();
            ForceSetState(_eatingState, "ContextMenu/Eating");
        }

        [ContextMenu(ContextMenuRoot + "/Set Death")]
        private void ContextSetDeath()
        {
            EnsureStatesInitialized();
            ForceSetState(_deathState, "ContextMenu/Death");
        }

        private void ForceSetState(EaterBehaviorState targetState, string source)
        {
            if (_stateMachine == null || targetState == null)
            {
                return;
            }

            IState previous = _stateMachine.CurrentState;
            if (ReferenceEquals(previous, targetState))
            {
                return;
            }

            previous?.OnExit();
            _stateMachine.SetState(targetState);

            _stateTimer = 0f;
            _lastStateSampled = targetState;

            UpdateMasterState();

            if (logStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>(
                    $"Estado definido: {GetStateName(previous)} -> {GetStateName(targetState)} ({source}).",
                    DebugUtility.Colors.CrucialInfo,
                    context: this,
                    instance: this);
            }

            EventStateChanged?.Invoke(previous, targetState);

            if (!ReferenceEquals(targetState, _chasingState) && !ReferenceEquals(targetState, _eatingState))
            {
                ClearProximityContact();
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

        private bool RefreshCurrentTarget()
        {
            PlanetsMaster resolved = ResolveMarkedPlanet();
            if (ReferenceEquals(_currentTarget, resolved))
            {
                return false;
            }

            PlanetsMaster previous = _currentTarget;
            _currentTarget = resolved;

            if (previous != null && !ReferenceEquals(previous, _currentTarget))
            {
                ClearProximityContact(previous);
            }

            EventTargetChanged?.Invoke(_currentTarget);

            if (logStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>(
                    $"Alvo atualizado: {FormatPlanetName(previous)} -> {FormatPlanetName(_currentTarget)}.",
                    DebugUtility.Colors.CrucialInfo,
                    context: this,
                    instance: this);
            }

            return true;
        }

        private PlanetsMaster ResolveMarkedPlanet()
        {
            MarkPlanet mark = _planetMarkingManager?.CurrentlyMarkedPlanet;
            if (mark == null)
            {
                return null;
            }

            PlanetsMaster planet = null;
            IActor actor = mark.PlanetActor;

            if (actor != null)
            {
                if (_planetsManager != null && _planetsManager.TryGetPlanet(actor, out PlanetsMaster resolved))
                {
                    planet = resolved;
                }
                else if (actor is PlanetsMaster directMaster)
                {
                    planet = directMaster;
                }
            }

            if (planet == null)
            {
                planet = mark.GetComponentInParent<PlanetsMaster>();
            }

            return planet;
        }

        internal bool IsCurrentTarget(PlanetsMaster planet)
        {
            if (planet == null || _currentTarget == null)
            {
                return false;
            }

            return ReferenceEquals(_currentTarget, planet);
        }

        internal void RegisterProximityContact(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            bool alreadyTracking = _hasProximityContact && ReferenceEquals(_proximityPlanet, planet);

            _hasProximityContact = true;
            _proximityPlanet = planet;
            if (!alreadyTracking)
            {
                if (logStateTransitions)
                {
                    DebugUtility.Log<EaterBehavior>(
                        $"Sensor de proximidade detectou {FormatPlanetName(planet)}.",
                        DebugUtility.Colors.Success,
                        context: this,
                        instance: this);
                }

                EventProximityContactChanged?.Invoke(planet, true);
            }
        }

        internal void ClearProximityContact()
        {
            ClearProximityContact(null);
        }

        internal void ClearProximityContact(PlanetsMaster planet)
        {
            if (!_hasProximityContact)
            {
                return;
            }

            if (planet != null && !ReferenceEquals(_proximityPlanet, planet))
            {
                return;
            }

            PlanetsMaster previous = _proximityPlanet;

            _hasProximityContact = false;
            _proximityPlanet = null;
            if (logStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>(
                    $"Sensor de proximidade liberou {FormatPlanetName(previous)}.",
                    DebugUtility.Colors.CrucialInfo,
                    context: this,
                    instance: this);
            }

            EventProximityContactChanged?.Invoke(previous, false);
        }

        internal void EndEating(bool desireSatisfied)
        {
            if (_stateMachine == null || _eatingState == null)
            {
                return;
            }

            if (!ReferenceEquals(_stateMachine.CurrentState, _eatingState))
            {
                return;
            }

            ForceSetState(_chasingState, $"EndEating({desireSatisfied})");
        }

        internal bool TryPlayDesireSelectedSound(string source)
        {
            if (_config?.DesireSelectedSound == null)
            {
                return false;
            }

            if (!TryEnsureAudioEmitter())
            {
                if (logStateTransitions && !_loggedMissingAudioEmitter)
                {
                    DebugUtility.LogWarning<EaterBehavior>(
                        "EntityAudioEmitter não encontrado para reproduzir som de desejo.",
                        context: this,
                        instance: this);

                    _loggedMissingAudioEmitter = true;
                }

                return false;
            }

            _loggedMissingAudioEmitter = false;

            AudioContext context = AudioContext.Default(transform.position, _audioEmitter.UsesSpatialBlend);
            _audioEmitter.Play(_config.DesireSelectedSound, context);

            if (logStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>(
                    $"Som de desejo reproduzido ({source}).",
                    DebugUtility.Colors.CrucialInfo,
                    context: this,
                    instance: this);
            }

            return true;
        }

        private bool TryEnsureAudioEmitter()
        {
            if (_audioEmitterResolved)
            {
                return _audioEmitter != null;
            }

            _audioEmitter = GetComponent<EntityAudioEmitter>() ?? GetComponentInChildren<EntityAudioEmitter>(true);
            _audioEmitterResolved = true;
            return _audioEmitter != null;
        }

        private static string FormatPlanetName(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return "nenhum planeta";
            }

            return !string.IsNullOrEmpty(planet.ActorName) ? planet.ActorName : planet.name;
        }

        private bool TryGetClosestPlayer(out Transform player, out float sqrDistance)
        {
            _playerManager ??= PlayerManager.Instance;

            IReadOnlyList<Transform> players = _playerManager?.Players;
            player = null;
            sqrDistance = 0f;

            if (players == null || players.Count == 0)
            {
                return false;
            }

            Vector3 origin = transform.position;
            float bestDistance = float.MaxValue;
            Transform bestPlayer = null;

            foreach (Transform candidate in players)
            {
                if (candidate == null)
                {
                    continue;
                }

                float candidateDistance = (candidate.position - origin).sqrMagnitude;
                if (candidateDistance < bestDistance)
                {
                    bestDistance = candidateDistance;
                    bestPlayer = candidate;
                }
            }

            if (bestPlayer == null)
            {
                return false;
            }

            player = bestPlayer;
            sqrDistance = bestDistance;
            return true;
        }

        private Vector3 ApplyPlayerBounds(Vector3 desiredPosition)
        {
            if (!TryGetClosestPlayerAnchor(out Vector3 anchor, out _))
            {
                return desiredPosition;
            }

            float maxDistance = Mathf.Max(0f, _config?.WanderingMaxDistanceFromPlayer ?? 0f);
            float minDistance = Mathf.Max(0f, _config?.WanderingMinDistanceFromPlayer ?? 0f);

            if (maxDistance <= 0f && minDistance <= 0f)
            {
                return desiredPosition;
            }

            if (maxDistance > 0f && maxDistance < minDistance)
            {
                maxDistance = minDistance;
            }

            Vector3 offset = desiredPosition - anchor;
            float sqrMagnitude = offset.sqrMagnitude;

            if (maxDistance > 0f)
            {
                float maxDistanceSqr = maxDistance * maxDistance;
                if (sqrMagnitude > maxDistanceSqr)
                {
                    desiredPosition = anchor + offset.normalized * maxDistance;
                    offset = desiredPosition - anchor;
                    sqrMagnitude = offset.sqrMagnitude;
                }
            }

            if (minDistance > 0f)
            {
                float minDistanceSqr = minDistance * minDistance;
                if (sqrMagnitude < minDistanceSqr)
                {
                    Vector3 direction = offset.sqrMagnitude > Mathf.Epsilon ? offset.normalized : transform.forward;
                    if (direction.sqrMagnitude <= Mathf.Epsilon)
                    {
                        direction = Vector3.forward;
                    }

                    desiredPosition = anchor + direction * minDistance;
                }
            }

            return desiredPosition;
        }

        internal float GetRandomRoamingSpeed()
        {
            if (_config == null)
            {
                return 0f;
            }

            return Random.Range(_config.MinSpeed, _config.MaxSpeed);
        }

        internal float GetChaseSpeed()
        {
            if (_config == null)
            {
                return 0f;
            }

            float baseSpeed = _config.MaxSpeed;
            return baseSpeed * _config.MultiplierChase;
        }

        internal void Move(Vector3 direction, float speed, float deltaTime, bool respectPlayerBounds)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon || speed <= 0f)
            {
                return;
            }

            Vector3 normalized = direction.normalized;
            Vector3 displacement = normalized * speed * deltaTime;
            Translate(displacement, respectPlayerBounds);
            RecordMovement(normalized, speed);
        }

        internal void Translate(Vector3 displacement, bool respectPlayerBounds)
        {
            Vector3 desiredPosition = transform.position + displacement;
            if (respectPlayerBounds)
            {
                desiredPosition = ApplyPlayerBounds(desiredPosition);
            }

            transform.position = desiredPosition;
        }

        internal void RecordMovement(Vector3 direction, float speed)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon || speed <= Mathf.Epsilon)
            {
                return;
            }

            _hasMovementSample = true;
            _lastMovementDirection = direction.normalized;
            _lastMovementSpeed = speed;
        }

        internal void RotateTowards(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float rotationSpeed = _config != null ? Mathf.Max(_config.RotationSpeed, 0f) : 5f;
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, deltaTime * rotationSpeed);
        }

        internal void LookAt(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = targetRotation;
        }

        private bool TryEnsureAutoFlowBridge()
        {
            if (_autoFlowBridge != null)
            {
                return true;
            }

            if (TryGetComponent(out ResourceAutoFlowBridge bridge))
            {
                _autoFlowBridge = bridge;
                _loggedMissingAutoFlowBridge = false;
                _loggedAutoFlowUnavailable = false;
                return true;
            }

            return false;
        }

        private void LogAutoFlowIssue(string message, ref bool cacheFlag)
        {
            if (!logStateTransitions || cacheFlag)
            {
                return;
            }

            DebugUtility.LogWarning<EaterBehavior>(message, context: this, instance: this);
            cacheFlag = true;
        }

        private void LogAutoFlowResult(bool success, string message)
        {
            if (!logStateTransitions)
            {
                return;
            }

            if (success)
            {
                DebugUtility.Log<EaterBehavior>(message, DebugUtility.Colors.Success, context: this, instance: this);
            }
            else
            {
                DebugUtility.LogWarning<EaterBehavior>(message, context: this, instance: this);
            }
        }

        internal bool ResumeAutoFlow(string source)
        {
            if (!TryEnsureAutoFlowBridge())
            {
                LogAutoFlowIssue("ResourceAutoFlowBridge não encontrado para controlar AutoFlow.", ref _loggedMissingAutoFlowBridge);
                return false;
            }

            if (!_autoFlowBridge.HasAutoFlowService)
            {
                LogAutoFlowIssue("ResourceAutoFlowBridge ainda não possui serviço inicializado.", ref _loggedAutoFlowUnavailable);
                return false;
            }

            bool resumed = _autoFlowBridge.ResumeAutoFlow();
            LogAutoFlowResult(resumed,
                resumed
                    ? $"AutoFlow retomado ({source})."
                    : $"AutoFlow permaneceu pausado ({source}).");
            return resumed;
        }

        internal bool PauseAutoFlow(string source)
        {
            if (!TryEnsureAutoFlowBridge())
            {
                LogAutoFlowIssue("ResourceAutoFlowBridge não encontrado para pausar AutoFlow.", ref _loggedMissingAutoFlowBridge);
                return false;
            }

            if (!_autoFlowBridge.HasAutoFlowService)
            {
                LogAutoFlowIssue("ResourceAutoFlowBridge ainda não possui serviço inicializado.", ref _loggedAutoFlowUnavailable);
                return false;
            }

            bool paused = _autoFlowBridge.PauseAutoFlow();
            LogAutoFlowResult(paused,
                paused
                    ? $"AutoFlow pausado ({source})."
                    : $"Falha ao pausar AutoFlow ({source}).");
            return paused;
        }

        internal bool TryGetClosestPlayerAnchor(out Vector3 anchor, out float distance)
        {
            if (TryGetClosestPlayer(out Transform player, out float sqrDistance))
            {
                anchor = player.position;
                distance = Mathf.Sqrt(sqrDistance);
                return true;
            }

            anchor = default;
            distance = 0f;
            return false;
        }

        private void SamplePlayerAnchor()
        {
            if (TryGetClosestPlayerAnchor(out Vector3 anchor, out float distance))
            {
                _hasPlayerAnchorSample = true;
                _lastPlayerAnchorPosition = anchor;
                _lastPlayerAnchorDistance = distance;

                Vector3 toAnchor = anchor - transform.position;
                Vector3 forward = transform.forward;

                if (toAnchor.sqrMagnitude > Mathf.Epsilon && forward.sqrMagnitude > Mathf.Epsilon)
                {
                    _lastPlayerAnchorAlignment = Vector3.Dot(forward.normalized, toAnchor.normalized);
                }
                else
                {
                    _lastPlayerAnchorAlignment = 0f;
                }
            }
            else
            {
                _hasPlayerAnchorSample = false;
                _lastPlayerAnchorPosition = Vector3.zero;
                _lastPlayerAnchorDistance = 0f;
                _lastPlayerAnchorAlignment = 0f;
            }
        }

        private void AdvanceStateTimer(float deltaTime)
        {
            if (_stateMachine == null)
            {
                _lastStateSampled = null;
                _stateTimer = 0f;
                return;
            }

            if (_stateMachine.CurrentState is not EaterBehaviorState currentState)
            {
                _lastStateSampled = null;
                _stateTimer = 0f;
                return;
            }

            if (!ReferenceEquals(currentState, _lastStateSampled))
            {
                _stateTimer = 0f;
                _lastStateSampled = currentState;
                return;
            }

            _stateTimer += Mathf.Max(deltaTime, 0f);
        }

        private void UpdateMasterState()
        {
            if (_master == null)
            {
                return;
            }

            bool isEating = _stateMachine != null && ReferenceEquals(_stateMachine.CurrentState, _eatingState);
            bool isHungry = IsHungryState(_stateMachine?.CurrentState as EaterBehaviorState);

            _master.IsEating = isEating;
            _master.InHungry = isHungry;
        }

        private bool IsHungryState(EaterBehaviorState state)
        {
            if (state == null)
            {
                return false;
            }

            return ReferenceEquals(state, _hungryState)
                   || ReferenceEquals(state, _chasingState)
                   || ReferenceEquals(state, _eatingState);
        }

        internal bool BeginDesires(string source)
        {
            if (!EnsureDesireService())
            {
                return false;
            }

            bool started = _desireService.Start();

            if (started && logStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>($"Desejos ativados ({source}).", DebugUtility.Colors.CrucialInfo, context: this, instance: this);
            }

            return started;
        }

        internal bool EndDesires(string source)
        {
            bool stopped = false;

            if (_desireService != null)
            {
                stopped = _desireService.Stop();

                if (stopped && logStateTransitions)
                {
                    DebugUtility.Log<EaterBehavior>($"Desejos pausados ({source}).", DebugUtility.Colors.CrucialInfo, context: this, instance: this);
                }
            }

            if (!stopped)
            {
                EnsureNoActiveDesire(source);
            }

            return stopped;
        }

        internal void EnsureNoActiveDesire(string source)
        {
            if (!_currentDesireInfo.ServiceActive && !_currentDesireInfo.HasDesire)
            {
                return;
            }

            if (logStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>($"Desejos finalizados ({source}).", DebugUtility.Colors.CrucialInfo, context: this, instance: this);
            }

            UpdateDesireInfo(EaterDesireInfo.Inactive);
        }

        public EaterDesireInfo GetCurrentDesireInfo()
        {
            return _currentDesireInfo;
        }

        private bool EnsureDesireService()
        {
            if (_desireService != null)
            {
                return true;
            }

            if (_master == null || _config == null)
            {
                if (logStateTransitions && !_loggedMissingDesireService)
                {
                    DebugUtility.LogWarning<EaterBehavior>(
                        "Não foi possível inicializar o serviço de desejos (Master ou Config ausentes).",
                        context: this,
                        instance: this);

                    _loggedMissingDesireService = true;
                }

                return false;
            }

            _desireService = new EaterDesireService(_master, _config);
            _desireService.EventDesireChanged += HandleDesireChanged;
            _loggedMissingDesireService = false;
            return true;
        }

        private void HandleDesireChanged(EaterDesireInfo info)
        {
            UpdateDesireInfo(info);
        }

        private void UpdateDesireInfo(EaterDesireInfo info)
        {
            _currentDesireInfo = info;
            EventDesireChanged?.Invoke(info);
            EventBus<EaterDesireInfoChangedEvent>.Raise(new EaterDesireInfoChangedEvent(this, info));
        }

        internal EaterBehaviorDebugSnapshot CreateDebugSnapshot()
        {
            bool isValid = _stateMachine != null;
            string currentState = GetStateName(_stateMachine?.CurrentState);
            bool isEating = IsEating;
            bool isHungry = IsHungry;
            bool hasTarget = _currentTarget != null;
            string targetName = hasTarget ? FormatPlanetName(_currentTarget) : string.Empty;

            bool hasPlayerAnchor = _hasPlayerAnchorSample;
            Vector3 playerAnchor = _lastPlayerAnchorPosition;
            float playerAnchorDistance = _hasPlayerAnchorSample ? _lastPlayerAnchorDistance : 0f;
            float playerAnchorAlignment = _hasPlayerAnchorSample ? _lastPlayerAnchorAlignment : 0f;

            bool hasDesire = _currentDesireInfo.HasDesire;
            string desireName = string.Empty;
            if (hasDesire && _currentDesireInfo.TryGetResource(out PlanetResources resource))
            {
                desireName = resource.ToString();
            }

            bool hasAutoFlow = TryEnsureAutoFlowBridge() && _autoFlowBridge.HasAutoFlowService;
            bool autoFlowActive = hasAutoFlow && _autoFlowBridge.IsAutoFlowActive;
            bool desiresActive = _currentDesireInfo.ServiceActive || (_desireService?.IsActive ?? false);

            return new EaterBehaviorDebugSnapshot(
                isValid,
                currentState,
                isHungry,
                isEating,
                hasTarget,
                targetName,
                _stateTimer,
                hasWanderingTimer: false,
                wanderingTimerRunning: false,
                wanderingTimerFinished: false,
                wanderingTimerValue: 0f,
                wanderingDuration: 0f,
                transform.position,
                hasPlayerAnchor,
                playerAnchor,
                hasAutoFlow,
                autoFlowActive,
                desiresActive,
                pendingHungryEffects: false,
                _hasMovementSample,
                _lastMovementDirection,
                _lastMovementSpeed,
                _hasPlayerAnchorSample,
                playerAnchorDistance,
                playerAnchorAlignment,
                hasDesire,
                desireName,
                _currentDesireInfo.IsAvailable,
                _currentDesireInfo.RemainingTime,
                _currentDesireInfo.Duration,
                _currentDesireInfo.AvailableCount,
                _currentDesireInfo.Weight);
        }

        private void OnDestroy()
        {
            if (_desireService != null)
            {
                _desireService.EventDesireChanged -= HandleDesireChanged;
                _desireService.Stop();
            }
        }
    }
}

