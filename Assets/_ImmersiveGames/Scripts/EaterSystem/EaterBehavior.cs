using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.EaterSystem.Animations;
using _ImmersiveGames.Scripts.EaterSystem.Detections;
using _ImmersiveGames.Scripts.EaterSystem.Events;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.PlanetSystems.Managers;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
using Random = UnityEngine.Random;

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

        internal bool ShouldLogStateTransitions => logStateTransitions;

        private StateMachine _stateMachine;
        private EaterBehaviorState _wanderingState;
        private EaterBehaviorState _hungryState;
        private EaterBehaviorState _chasingState;
        private EaterBehaviorState _eatingState;
        private EaterBehaviorState _deathState;

        private EaterMaster _master;
        private EaterConfigSo _config;
        private PlayerManager _playerManager;
        private PlanetMarkingManager _planetMarkingManager;

        private ResourceAutoFlowBridge _autoFlowBridge;
        private bool _missingAutoFlowBridgeLogged;
        private bool _autoFlowUnavailableLogged;

        private EaterDesireService _desireService;
        private EaterDesireInfo _currentDesireInfo = EaterDesireInfo.Inactive;
        private bool _missingDesireServiceLogged;
        private EntityAudioEmitter _audioEmitter;
        private EaterDetectionController _detectionController;
        private EaterAnimationController _animationController;

        private bool _isHungry;
        private bool _isEating;
        private bool _isDead;
        private bool _gameOverRaised;
        private PlanetsMaster _currentTarget;
        private EventBinding<PlanetMarkingChangedEvent> _planetMarkingChangedBinding;
        private bool _planetMarkingRegistered;
        private EventBinding<DeathEvent> _deathBinding;
        private bool _deathListenerRegistered;
        private EventTriggeredPredicate<DeathEvent> _deathPredicate;
        private bool _deathTransitionConfigured;

        public event Action<EaterDesireInfo> EventDesireChanged;
        public event Action<EaterBehaviorState, EaterBehaviorState> EventStateChanged;
        public event Action<PlanetsMaster> EventTargetChanged;

        private void Awake()
        {
            _master = GetComponent<EaterMaster>();
            _config = _master != null ? _master.Config : null;
            _audioEmitter = GetComponent<EntityAudioEmitter>();
            _detectionController = GetComponent<EaterDetectionController>();
            _animationController = GetComponent<EaterAnimationController>();
            _planetMarkingManager = PlanetMarkingManager.Instance;
            _playerManager = PlayerManager.Instance;
            TryEnsureAutoFlowBridge();
            EnsureDesireService();
            EnsureStatesInitialized();
            SyncTargetFromMarkingManager("Awake");
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
            _audioEmitter = GetComponent<EntityAudioEmitter>();
            _detectionController = GetComponent<EaterDetectionController>();
            _animationController = GetComponent<EaterAnimationController>();
        }
#endif

        private void OnEnable()
        {
            RegisterPlanetMarkingListener();
            RegisterDeathListener();
            SyncTargetFromMarkingManager("OnEnable");
        }

        private void OnDisable()
        {
            UnregisterPlanetMarkingListener();
            UnregisterDeathListener();
        }

        private void Update()
        {
            _desireService?.Update();
            _stateMachine?.Update();
        }

        private void EnsureStateMachine()
        {
            _stateMachine?.FixedUpdate();
        }

        private bool EnsureStatesInitialized()
        {
            if (_stateMachine != null)
            {
                return true;
            }

            _stateMachine = new StateMachine();

            _wanderingState = RegisterState(new EaterWanderingState());
            _hungryState = RegisterState(new EaterHungryState());
            _chasingState = RegisterState(new EaterChasingState());
            _eatingState = RegisterState(new EaterEatingState());
            _deathState = RegisterState(new EaterDeathState());

            ConfigureStateTransitions();
            ForceSetState(_wanderingState, "Inicialização");
            return _stateMachine != null;
        }

        private void ConfigureStateTransitions()
        {
            if (_stateMachine == null)
            {
                return;
            }

            _deathPredicate ??= new EventTriggeredPredicate<DeathEvent>(() =>
            {
                if (ShouldLogStateTransitions)
                {
                    DebugUtility.Log<EaterBehavior>(
                        "Predicado de morte acionado.",
                        DebugUtility.Colors.CrucialInfo,
                        this,
                        this);
                }
            });

            if (!_deathTransitionConfigured)
            {
                _stateMachine.AddAnyTransition(_deathState, _deathPredicate);
                _deathTransitionConfigured = true;
            }

            IPredicate hungryPredicate = new PredicateIsHungry(() => !_isDead && _isHungry);
            IPredicate hasTargetPredicate = new PredicateHasTarget(() => !_isDead ? _currentTarget : null);
            IPredicate eatingPredicate = new PredicateIsEating(() => !_isDead && _isEating);

            IPredicate notHungry = new Not(hungryPredicate);
            IPredicate noTarget = new Not(hasTargetPredicate);
            IPredicate notEating = new Not(eatingPredicate);

            _stateMachine.AddTransition(_wanderingState, _hungryState, new And(notEating, noTarget, hungryPredicate));
            _stateMachine.AddTransition(_wanderingState, _chasingState, new And(notEating, hasTargetPredicate));
            _stateMachine.AddTransition(_wanderingState, _eatingState, eatingPredicate);

            _stateMachine.AddTransition(_hungryState, _wanderingState, new And(notEating, noTarget, notHungry));
            _stateMachine.AddTransition(_hungryState, _chasingState, new And(notEating, hasTargetPredicate));
            _stateMachine.AddTransition(_hungryState, _eatingState, eatingPredicate);

            _stateMachine.AddTransition(_chasingState, _eatingState, eatingPredicate);
            _stateMachine.AddTransition(_chasingState, _hungryState, new And(notEating, noTarget, hungryPredicate));
            _stateMachine.AddTransition(_chasingState, _wanderingState, new And(notEating, noTarget, notHungry));

            _stateMachine.AddTransition(_eatingState, _chasingState, new And(notEating, hasTargetPredicate));
            _stateMachine.AddTransition(_eatingState, _hungryState, new And(notEating, noTarget, hungryPredicate));
            _stateMachine.AddTransition(_eatingState, _wanderingState, new And(notEating, noTarget, notHungry));
        }

        [ContextMenu("Eater States/Set Wandering")]
        private void ContextSetWandering()
        {
            EnsureStatesInitialized();
            ForceSetState(_wanderingState, "ContextMenu/Wandering");
        }

        [ContextMenu("Eater States/Set Hungry")]
        private void ContextSetHungry()
        {
            EnsureStatesInitialized();
            ForceSetState(_hungryState, "ContextMenu/Hungry");
        }

        [ContextMenu("Eater States/Set Chasing")]
        private void ContextSetChasing()
        {
            EnsureStatesInitialized();
            ForceSetState(_chasingState, "ContextMenu/Chasing");
        }

        [ContextMenu("Eater States/Set Eating")]
        private void ContextSetEating()
        {
            EnsureStatesInitialized();
            ForceSetState(_eatingState, "ContextMenu/Eating");
        }

        [ContextMenu("Eater States/Set Death")]
        private void ContextSetDeath()
        {
            EnsureStatesInitialized();
            ForceSetState(_deathState, "ContextMenu/Death");
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
            EventStateChanged?.Invoke(previous as EaterBehaviorState, targetState);
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

            float bestDistance = float.MaxValue;
            Transform bestPlayer = null;
            Vector3 origin = transform.position;

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

        internal EaterMaster Master => _master;

        internal EaterConfigSo Config => _config;

        internal bool IsHungry => _isHungry;

        internal bool IsEating => _isEating;

        internal bool IsDead => _isDead;

        internal PlanetsMaster CurrentTarget => _currentTarget;

        internal Transform CurrentTargetPlanet
        {
            get
            {
                if (_currentTarget != null)
                {
                    return _currentTarget.transform;
                }

                MarkPlanet markPlanet = _planetMarkingManager?.CurrentlyMarkedPlanet;
                return markPlanet != null ? markPlanet.transform : null;
            }
        }

        internal bool TryGetDetectionController(out EaterDetectionController detectionController)
        {
            if (_detectionController == null)
            {
                TryGetComponent(out _detectionController);
            }

            detectionController = _detectionController;
            return detectionController != null;
        }

        internal bool TryGetAnimationController(out EaterAnimationController animationController)
        {
            if (_animationController == null)
            {
                TryGetComponent(out _animationController);
            }

            animationController = _animationController;
            return animationController != null;
        }

        internal bool ResumeAutoFlow(string reason)
        {
            if (_isDead)
            {
                return false;
            }

            if (!TryEnsureAutoFlowBridge())
            {
                LogAutoFlowIssue("ResourceAutoFlowBridge não encontrado para controlar AutoFlow.", ref _missingAutoFlowBridgeLogged);
                return false;
            }

            if (!_autoFlowBridge.HasAutoFlowService)
            {
                LogAutoFlowIssue("ResourceAutoFlowBridge ainda não possui serviço inicializado.", ref _autoFlowUnavailableLogged);
                return false;
            }

            bool resumed = _autoFlowBridge.ResumeAutoFlow();
            LogAutoFlowResult(resumed,
                resumed
                    ? $"AutoFlow retomado ({reason})."
                    : $"AutoFlow permaneceu pausado ({reason}).");
            return resumed;
        }

        internal bool PauseAutoFlow(string reason)
        {
            if (_isDead)
            {
                return false;
            }

            if (!TryEnsureAutoFlowBridge())
            {
                LogAutoFlowIssue("ResourceAutoFlowBridge não encontrado para pausar AutoFlow.", ref _missingAutoFlowBridgeLogged);
                return false;
            }

            if (!_autoFlowBridge.HasAutoFlowService)
            {
                LogAutoFlowIssue("ResourceAutoFlowBridge ainda não possui serviço inicializado.", ref _autoFlowUnavailableLogged);
                return false;
            }

            bool paused = _autoFlowBridge.PauseAutoFlow();
            LogAutoFlowResult(paused,
                paused
                    ? $"AutoFlow pausado ({reason})."
                    : $"Falha ao pausar AutoFlow ({reason}).");
            return paused;
        }

        internal float GetRandomRoamingSpeed()
        {
            if (_config == null)
            {
                return 0f;
            }

            float min = _config.MinSpeed;
            float max = _config.MaxSpeed;
            return Random.Range(min, max);
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
            if (_isDead)
            {
                return;
            }

            if (direction.sqrMagnitude <= Mathf.Epsilon || speed <= 0f)
            {
                return;
            }

            Vector3 displacement = direction.normalized * speed * deltaTime;
            Translate(displacement, respectPlayerBounds);
        }

        internal void Translate(Vector3 displacement, bool respectPlayerBounds)
        {
            if (_isDead)
            {
                return;
            }

            Vector3 desiredPosition = transform.position + displacement;
            if (respectPlayerBounds)
            {
                desiredPosition = ApplyPlayerBounds(desiredPosition);
            }

            transform.position = desiredPosition;
        }

        internal void RotateTowards(Vector3 direction, float deltaTime)
        {
            if (_isDead)
            {
                return;
            }

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float rotationSpeed = _config != null ? _config.RotationSpeed : 5f;
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, deltaTime * rotationSpeed);
        }

        internal void LookAt(Vector3 targetPosition)
        {
            if (_isDead)
            {
                return;
            }

            Vector3 direction = targetPosition - transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = targetRotation;
        }

        public void SetHungry(bool hungry, string reason = null)
        {
            if (_isDead && hungry)
            {
                return;
            }

            if (_isHungry == hungry)
            {
                return;
            }

            _isHungry = hungry;
            if (_master != null)
            {
                _master.InHungry = hungry;
            }

            if (ShouldLogStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>(
                    $"Fome atualizada para {hungry} ({FormatContext(reason)}).",
                    DebugUtility.Colors.CrucialInfo,
                    this,
                    this);
            }
        }

        public void SetTarget(PlanetsMaster planet, string reason = null)
        {
            if (_isDead && planet != null)
            {
                return;
            }

            if (ReferenceEquals(_currentTarget, planet))
            {
                return;
            }

            if (_isEating && !ReferenceEquals(_currentTarget, planet))
            {
                EndEating(false, "Troca de alvo");
            }

            PlanetsMaster previous = _currentTarget;
            _currentTarget = planet;

            if (ShouldLogStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>(
                    $"Alvo atualizado: {GetPlanetDebugName(previous)} -> {GetPlanetDebugName(_currentTarget)} ({FormatContext(reason)}).",
                    DebugUtility.Colors.CrucialInfo,
                    this,
                    this);
            }

            EventTargetChanged?.Invoke(_currentTarget);
        }

        public void RegisterProximityContact(PlanetsMaster planet, Vector3 contactPoint)
        {
            if (_isDead || planet == null)
            {
                return;
            }

            if (!ReferenceEquals(_currentTarget, planet))
            {
                SetTarget(planet, "Contato de proximidade");
            }

            BeginEating(planet, contactPoint, "Contato de proximidade");
        }

        public void ClearProximityContact(PlanetsMaster planet)
        {
            if (!_isEating)
            {
                return;
            }

            if (planet != null && !ReferenceEquals(_currentTarget, planet))
            {
                return;
            }

            EndEating(false, "Perda de proximidade");
        }

        public void BeginEating(PlanetsMaster planet, Vector3 contactPoint, string reason = null)
        {
            if (_isDead || planet == null)
            {
                return;
            }

            if (!ReferenceEquals(_currentTarget, planet))
            {
                SetTarget(planet, reason);
            }

            if (_isEating)
            {
                return;
            }

            _isEating = true;
            if (_master != null)
            {
                _master.IsEating = true;
                _master.OnEventStartEatPlanet(planet);
            }

            if (ShouldLogStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>(
                    $"Iniciando consumo do planeta {GetPlanetDebugName(planet)} ({FormatContext(reason)}).",
                    DebugUtility.Colors.CrucialInfo,
                    this,
                    this);
            }
        }

        public void EndEating(bool consumptionCompleted, string reason = null)
        {
            if (!_isEating)
            {
                return;
            }

            _isEating = false;
            if (_master != null)
            {
                _master.IsEating = false;
            }

            PlanetsMaster planet = _currentTarget;
            if (_master != null && planet != null)
            {
                _master.OnEventEndEatPlanet(planet);
            }

            if (ShouldLogStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>(
                    $"Consumo encerrado para {GetPlanetDebugName(planet)} ({FormatContext(reason)}).",
                    DebugUtility.Colors.CrucialInfo,
                    this,
                    this);
            }
        }

        private static string FormatContext(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "sem contexto" : reason;
        }

        private static string GetPlanetDebugName(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return "nenhum";
            }

            if (!string.IsNullOrWhiteSpace(planet.ActorName))
            {
                return planet.ActorName;
            }

            return planet.name;
        }

        private void RegisterPlanetMarkingListener()
        {
            if (_planetMarkingRegistered)
            {
                return;
            }

            _planetMarkingChangedBinding ??= new EventBinding<PlanetMarkingChangedEvent>(HandlePlanetMarkingChanged);
            EventBus<PlanetMarkingChangedEvent>.Register(_planetMarkingChangedBinding);
            _planetMarkingRegistered = true;
        }

        private void UnregisterPlanetMarkingListener()
        {
            if (!_planetMarkingRegistered)
            {
                return;
            }

            if (_planetMarkingChangedBinding != null)
            {
                EventBus<PlanetMarkingChangedEvent>.Unregister(_planetMarkingChangedBinding);
            }

            _planetMarkingRegistered = false;
        }

        private void HandlePlanetMarkingChanged(PlanetMarkingChangedEvent evt)
        {
            if (_isDead)
            {
                return;
            }

            PlanetsMaster newTarget = ResolvePlanetFromActor(evt.NewMarkedPlanet);
            SetTarget(newTarget, "PlanetMarkingChangedEvent");
        }

        private void SyncTargetFromMarkingManager(string reason)
        {
            PlanetsMaster target = ResolvePlanetFromMark(_planetMarkingManager?.CurrentlyMarkedPlanet);
            SetTarget(target, reason);
        }

        private static PlanetsMaster ResolvePlanetFromActor(IActor actor)
        {
            switch (actor)
            {
                case null:
                    return null;
                case PlanetsMaster planetsMaster:
                    return planetsMaster;
                case Component component:
                    component.TryGetComponent(out PlanetsMaster masterFromComponent);
                    return masterFromComponent ?? component.GetComponentInParent<PlanetsMaster>();
                default:
                    return null;
            }
        }

        private static PlanetsMaster ResolvePlanetFromMark(MarkPlanet mark)
        {
            if (mark == null)
            {
                return null;
            }

            return ResolvePlanetFromActor(mark.PlanetActor);
        }

        private void RegisterDeathListener()
        {
            if (_deathListenerRegistered || _master == null || string.IsNullOrEmpty(_master.ActorId))
            {
                return;
            }

            _deathBinding ??= new EventBinding<DeathEvent>(HandleDeathEvent);
            FilteredEventBus<DeathEvent>.Register(_deathBinding, _master.ActorId);
            _deathListenerRegistered = true;
        }

        private void UnregisterDeathListener()
        {
            if (!_deathListenerRegistered || _master == null || string.IsNullOrEmpty(_master.ActorId))
            {
                return;
            }

            if (_deathBinding != null)
            {
                FilteredEventBus<DeathEvent>.Unregister(_deathBinding, _master.ActorId);
            }

            _deathListenerRegistered = false;
        }

        private void HandleDeathEvent(DeathEvent evt)
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;

            if (_isEating)
            {
                EndEating(false, "DeathEvent");
            }

            SetHungry(false, "DeathEvent");
            SetTarget(null, "DeathEvent");

            if (ShouldLogStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>(
                    "DeathEvent recebido. Solicitando transição para o estado de morte.",
                    DebugUtility.Colors.CrucialInfo,
                    this,
                    this);
            }

            _deathPredicate?.Trigger();

            if (!_gameOverRaised)
            {
                EventBus<GameOverEvent>.Raise(new GameOverEvent());
                _gameOverRaised = true;
            }

            EndDesires("DeathEvent");
            EnsureNoActiveDesire("DeathEvent");
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
                _missingAutoFlowBridgeLogged = false;
                _autoFlowUnavailableLogged = false;
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

            DebugUtility.LogWarning<EaterBehavior>(message, this, this);
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
                DebugUtility.Log<EaterBehavior>(message, DebugUtility.Colors.Success, this, this);
            }
            else
            {
                DebugUtility.LogWarning<EaterBehavior>(message, this, this);
            }
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

        internal bool BeginDesires(string reason)
        {
            if (_isDead)
            {
                return false;
            }

            if (!EnsureDesireService())
            {
                return false;
            }

            bool started = _desireService.Start();
            if (logStateTransitions && started)
            {
                DebugUtility.Log<EaterBehavior>($"Desejos ativados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
            }

            return started;
        }

        internal bool EndDesires(string reason)
        {
            bool stopped = false;

            if (_desireService != null)
            {
                stopped = _desireService.Stop();
                if (logStateTransitions && stopped)
                {
                    DebugUtility.Log<EaterBehavior>($"Desejos pausados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
                }
            }

            if (!stopped)
            {
                EnsureNoActiveDesire(reason);
            }

            return stopped;
        }

        internal void EnsureNoActiveDesire(string reason)
        {
            if (!_currentDesireInfo.ServiceActive && !_currentDesireInfo.HasDesire)
            {
                return;
            }

            if (logStateTransitions)
            {
                DebugUtility.Log<EaterBehavior>($"Desejos finalizados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
            }

            UpdateDesireInfo(EaterDesireInfo.Inactive);
        }

        public EaterDesireInfo GetCurrentDesireInfo()
        {
            return _currentDesireInfo;
        }

        private bool EnsureDesireService()
        {
            if (_isDead)
            {
                return false;
            }

            if (_desireService != null)
            {
                return true;
            }

            if (_master == null || _config == null)
            {
                if (logStateTransitions && !_missingDesireServiceLogged)
                {
                    DebugUtility.LogWarning<EaterBehavior>("Não foi possível inicializar o serviço de desejos (Master ou Config ausentes).", this, this);
                    _missingDesireServiceLogged = true;
                }

                return false;
            }

            _desireService = new EaterDesireService(_master, _config, _audioEmitter);
            _desireService.EventDesireChanged += HandleDesireChanged;
            _missingDesireServiceLogged = false;
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

        private void OnDestroy()
        {
            UnregisterPlanetMarkingListener();
            UnregisterDeathListener();

            if (_desireService != null)
            {
                _desireService.EventDesireChanged -= HandleDesireChanged;
                _desireService.Stop();
            }
        }
    }
}
