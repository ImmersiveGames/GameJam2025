using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.EaterSystem.Animations;
using _ImmersiveGames.Scripts.EaterSystem.Detections;
using _ImmersiveGames.Scripts.EaterSystem.Events;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Managers;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
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
        private DeathEventPredicate _deathPredicate;
        private ReviveEventPredicate _revivePredicate;
        private EaterWanderingState _wanderingState;
        private EaterHungryState _hungryState;
        private EaterChasingState _chasingState;
        private EaterEatingState _eatingState;
        private EaterDeathState _deathState;

        private EaterMaster _master;
        private EaterConfigSo _config;
        private PlayerManager _playerManager;
        private PlanetMarkingManager _planetMarkingManager;

        private ResourceAutoFlowBridge _autoFlowBridge;
        private bool _missingAutoFlowBridgeLogged;
        private bool _autoFlowUnavailableLogged;
        private bool _missingResourceSystemLogged;
        private IDamageReceiver _selfDamageReceiver;
        private bool _missingSelfDamageReceiverLogged;

        private EaterDesireService _desireService;
        private EaterDesireInfo _currentDesireInfo = EaterDesireInfo.Inactive;
        private bool _missingDesireServiceLogged;
        private bool _missingMasterForPredicatesLogged;
        private WanderingTimeoutPredicate _wanderingTimeoutPredicate;
        private HungryChasingPredicate _hungryChasingPredicate;
        private ChasingEatingPredicate _chasingEatingPredicate;
        private PlanetUnmarkedPredicate _planetUnmarkedPredicate;
        private EatingWanderingPredicate _eatingWanderingPredicate;
        private EntityAudioEmitter _audioEmitter;
        private EaterDetectionController _detectionController;
        private EaterAnimationController _animationController;
        private Transform _lastOrbitTarget;
        private float _lastOrbitRadius = -1f;
        private float _lastSurfaceStopDistance = -1f;

        public event Action<EaterDesireInfo> EventDesireChanged;

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

            var builder = new StateMachineBuilder();

            _wanderingState = RegisterState(builder, new EaterWanderingState());
            _hungryState = RegisterState(builder, new EaterHungryState());
            _chasingState = RegisterState(builder, new EaterChasingState());
            _eatingState = RegisterState(builder, new EaterEatingState());
            _deathState = RegisterState(builder, new EaterDeathState());

            ConfigureTransitions(builder);

            builder.StateInitial(_wanderingState);
            _stateMachine = builder.Build();

            return _stateMachine != null;
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
            if (logStateTransitions)
            {
                string message = $"Estado definido: {GetStateName(previous)} -> {GetStateName(targetState)} ({reason}).";
                DebugUtility.Log(message, DebugUtility.Colors.CrucialInfo, this, this);
            }

        }

        private void ConfigureTransitions(StateMachineBuilder builder)
        {
            IPredicate deathPredicate = EnsureDeathPredicate();
            IPredicate revivePredicate = EnsureRevivePredicate();
            IPredicate wanderingTimeoutPredicate = EnsureWanderingTimeoutPredicate();
            IPredicate hungryChasingPredicate = EnsureHungryChasingPredicate();
            IPredicate chasingEatingPredicate = EnsureChasingEatingPredicate();
            IPredicate planetUnmarkedPredicate = EnsurePlanetUnmarkedPredicate();
            builder.Any(_deathState, deathPredicate);
            builder.At(_deathState, _wanderingState, revivePredicate);
            builder.At(_wanderingState, _hungryState, wanderingTimeoutPredicate);
            builder.At(_hungryState, _chasingState, hungryChasingPredicate);
            builder.At(_chasingState, _eatingState, chasingEatingPredicate);
            builder.At(_chasingState, _hungryState, planetUnmarkedPredicate);
            builder.At(_eatingState, _hungryState, planetUnmarkedPredicate);
            builder.At(_eatingState, _wanderingState, EnsureEatingWanderingPredicate());
        }

        private T RegisterState<T>(StateMachineBuilder builder, T state) where T : EaterBehaviorState
        {
            state.Attach(this);
            builder.AddState(state, out _);
            return state;
        }

        private IPredicate EnsureHungryChasingPredicate()
        {
            if (_hungryChasingPredicate != null)
            {
                return _hungryChasingPredicate;
            }

            if (_hungryState == null || _chasingState == null)
            {
                return FalsePredicate.Instance;
            }

            _hungryChasingPredicate = new HungryChasingPredicate(_hungryState);
            return _hungryChasingPredicate;
        }

        private IPredicate EnsureChasingEatingPredicate()
        {
            if (_chasingEatingPredicate != null)
            {
                return _chasingEatingPredicate;
            }

            if (_chasingState == null || _eatingState == null)
            {
                return FalsePredicate.Instance;
            }

            _chasingEatingPredicate = new ChasingEatingPredicate(_chasingState);
            return _chasingEatingPredicate;
        }

        private IPredicate EnsurePlanetUnmarkedPredicate()
        {
            if (_planetUnmarkedPredicate != null)
            {
                return _planetUnmarkedPredicate;
            }

            _planetUnmarkedPredicate = new PlanetUnmarkedPredicate();
            return _planetUnmarkedPredicate;
        }

        private IPredicate EnsureEatingWanderingPredicate()
        {
            if (_eatingWanderingPredicate != null)
            {
                return _eatingWanderingPredicate;
            }

            if (_eatingState == null || _wanderingState == null)
            {
                return FalsePredicate.Instance;
            }

            _eatingWanderingPredicate = new EatingWanderingPredicate(_eatingState);
            return _eatingWanderingPredicate;
        }

        private IPredicate EnsureWanderingTimeoutPredicate()
        {
            if (_wanderingTimeoutPredicate != null)
            {
                return _wanderingTimeoutPredicate;
            }

            if (_wanderingState == null || _hungryState == null)
            {
                return FalsePredicate.Instance;
            }

            _wanderingTimeoutPredicate = new WanderingTimeoutPredicate(_wanderingState);
            return _wanderingTimeoutPredicate;
        }

        private IPredicate EnsureDeathPredicate()
        {
            if (_deathPredicate != null)
            {
                return _deathPredicate;
            }

            if (_master == null)
            {
                LogMissingMasterForPredicates();
                return FalsePredicate.Instance;
            }

            _deathPredicate = new DeathEventPredicate(_master.ActorId);
            return _deathPredicate;
        }

        private IPredicate EnsureRevivePredicate()
        {
            if (_revivePredicate != null)
            {
                return _revivePredicate;
            }

            if (_master == null)
            {
                LogMissingMasterForPredicates();
                return FalsePredicate.Instance;
            }

            _revivePredicate = new ReviveEventPredicate(_master.ActorId);
            return _revivePredicate;
        }

        private void LogMissingMasterForPredicates()
        {
            if (_missingMasterForPredicatesLogged)
            {
                return;
            }

            DebugUtility.LogWarning(
                "EaterMaster não encontrado. Transições de morte/revive permanecerão desabilitadas.",
                this,
                this);

            _missingMasterForPredicatesLogged = true;
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

        internal Transform CurrentTargetPlanet => _planetMarkingManager?.CurrentlyMarkedPlanet != null
            ? _planetMarkingManager.CurrentlyMarkedPlanet.transform
            : null;

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
                string actorId = _master != null ? _master.ActorId : null;
                if (!string.IsNullOrEmpty(actorId)
                    && DependencyManager.Instance.TryGetForObject(actorId, out EaterAnimationController resolvedController))
                {
                    _animationController = resolvedController;
                }
                else if (!TryGetComponent(out _animationController))
                {
                    _animationController = null;
                }
            }

            animationController = _animationController;
            return animationController != null;
        }

        internal bool TryGetAudioEmitter(out EntityAudioEmitter audioEmitter)
        {
            if (_audioEmitter == null)
            {
                if (_master != null)
                {
                    string actorId = _master.ActorId;
                    if (!string.IsNullOrEmpty(actorId)
                        && DependencyManager.Instance.TryGetForObject(actorId, out EntityAudioEmitter resolvedEmitter))
                    {
                        _audioEmitter = resolvedEmitter;
                    }
                }

                if (_audioEmitter == null)
                {
                    TryGetComponent(out _audioEmitter);
                }
            }

            audioEmitter = _audioEmitter;
            return audioEmitter != null;
        }

        internal bool TryApplySelfHealing(ResourceType resourceType, float amount,
            DamageType healDamageType = DamageType.Pure)
        {
            if (amount <= Mathf.Epsilon)
            {
                return false;
            }

            if (!TryGetSelfDamageReceiver(out IDamageReceiver damageReceiver))
            {
                if (logStateTransitions && !_missingSelfDamageReceiverLogged)
                {
                    DebugUtility.LogWarning(
                        "DamageReceiver do eater não encontrado. Não foi possível recuperar recursos via DamageSystem.",
                        this,
                        this);
                    _missingSelfDamageReceiverLogged = true;
                }

                return false;
            }

            _missingSelfDamageReceiverLogged = false;

            float clampedAmount = Mathf.Max(0f, amount);
            if (clampedAmount <= Mathf.Epsilon)
            {
                return false;
            }

            string attackerId = _master != null ? _master.ActorId : string.Empty;
            string targetId = damageReceiver.GetReceiverId();
            Vector3 hitPosition = transform.position;

            var context = new DamageContext(attackerId, targetId, -clampedAmount, resourceType, healDamageType, hitPosition);
            damageReceiver.ReceiveDamage(context);
            return true;
        }

        private bool TryGetSelfDamageReceiver(out IDamageReceiver damageReceiver)
        {
            if (_selfDamageReceiver == null)
            {
                if (_master != null)
                {
                    string actorId = _master.ActorId;
                    if (!string.IsNullOrEmpty(actorId)
                        && DependencyManager.Instance.TryGetForObject(actorId, out IDamageReceiver resolvedReceiver))
                    {
                        _selfDamageReceiver = resolvedReceiver;
                    }
                }

                if (_selfDamageReceiver == null)
                {
                    TryGetComponent(out _selfDamageReceiver);
                }
            }

            damageReceiver = _selfDamageReceiver;
            return damageReceiver != null;
        }

        /// <summary>
        /// Registra informações sobre o último ponto em que a perseguição foi interrompida.
        /// Mantém a distância radial calculada a partir do centro do planeta e a separação da superfície.
        /// </summary>
        internal void RegisterOrbitAnchor(Transform target, Vector3 targetCenter, float surfaceStopDistance)
        {
            if (target == null)
            {
                ClearOrbitAnchor();
                return;
            }

            _lastOrbitTarget = target;
            _lastSurfaceStopDistance = Mathf.Max(0f, surfaceStopDistance);
            float computedRadius = Vector3.Distance(transform.position, targetCenter);
            _lastOrbitRadius = Mathf.Max(computedRadius, 0f);
        }

        /// <summary>
        /// Obtém o último ponto de parada registrado para o planeta informado.
        /// </summary>
        internal bool TryGetOrbitAnchor(Transform target, out float orbitRadius, out float surfaceStopDistance)
        {
            if (ReferenceEquals(target, _lastOrbitTarget) && _lastOrbitRadius > 0f)
            {
                orbitRadius = _lastOrbitRadius;
                surfaceStopDistance = _lastSurfaceStopDistance;
                return true;
            }

            orbitRadius = 0f;
            surfaceStopDistance = 0f;
            return false;
        }

        /// <summary>
        /// Limpa o ponto de parada registrado, evitando reaproveitar dados obsoletos.
        /// </summary>
        internal void ClearOrbitAnchor(Transform target = null)
        {
            if (target != null && !ReferenceEquals(target, _lastOrbitTarget))
            {
                return;
            }

            _lastOrbitTarget = null;
            _lastOrbitRadius = -1f;
            _lastSurfaceStopDistance = -1f;
        }

        internal bool ResumeAutoFlow(string reason)
        {
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
            if (direction.sqrMagnitude <= Mathf.Epsilon || speed <= 0f)
            {
                return;
            }

            Vector3 displacement = direction.normalized * speed * deltaTime;
            Translate(displacement, respectPlayerBounds);
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

        internal void RotateTowards(Vector3 direction, float deltaTime)
        {
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
                _missingAutoFlowBridgeLogged = false;
                _autoFlowUnavailableLogged = false;
                _missingResourceSystemLogged = false;
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

            DebugUtility.LogWarning(message, this, this);
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
                DebugUtility.Log(message, DebugUtility.Colors.Success, this, this);
            }
            else
            {
                DebugUtility.LogWarning(message, this, this);
            }
        }

        internal bool TryRestoreResource(ResourceType resourceType, float amount)
        {
            if (amount <= Mathf.Epsilon)
            {
                return false;
            }

            if (!TryEnsureAutoFlowBridge())
            {
                LogAutoFlowIssue(
                    "ResourceAutoFlowBridge não encontrado para recuperar recursos manualmente.",
                    ref _missingAutoFlowBridgeLogged);
                return false;
            }

            if (!_autoFlowBridge.HasAutoFlowService)
            {
                LogAutoFlowIssue(
                    "ResourceAutoFlowBridge ainda não possui serviço inicializado para recuperar recursos manualmente.",
                    ref _autoFlowUnavailableLogged);
                return false;
            }

            ResourceSystem resourceSystem = _autoFlowBridge.GetResourceSystem();
            if (resourceSystem == null)
            {
                LogAutoFlowIssue(
                    "ResourceSystem indisponível ao tentar recuperar recursos manualmente.",
                    ref _missingResourceSystemLogged);
                return false;
            }

            _missingResourceSystemLogged = false;
            resourceSystem.Modify(resourceType, Mathf.Max(0f, amount));
            return true;
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

        internal bool BeginDesires(string reason, bool forceRestart = false)
        {
            if (!EnsureDesireService())
            {
                return false;
            }

            if (forceRestart)
            {
                _desireService.Stop();
            }

            bool resumed = !forceRestart && _desireService.TryResume();
            if (resumed)
            {
                if (logStateTransitions)
                {
                    DebugUtility.Log($"Desejos retomados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
                }

                return true;
            }

            bool started = _desireService.Start();
            if (logStateTransitions && started)
            {
                DebugUtility.Log($"Desejos ativados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
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
                    DebugUtility.Log($"Desejos pausados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
                }
            }

            if (!stopped)
            {
                EnsureNoActiveDesire(reason);
            }

            return stopped;
        }

        internal bool SuspendDesires(string reason)
        {
            if (_desireService == null)
            {
                return false;
            }

            bool suspended = _desireService.Suspend();
            if (logStateTransitions && suspended)
            {
                DebugUtility.Log($"Desejos suspensos mantendo seleção atual ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
            }

            return suspended;
        }

        internal void EnsureNoActiveDesire(string reason)
        {
            if (!_currentDesireInfo.ServiceActive && !_currentDesireInfo.HasDesire)
            {
                return;
            }

            if (logStateTransitions)
            {
                DebugUtility.Log($"Desejos finalizados ({reason}).", DebugUtility.Colors.CrucialInfo, this, this);
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
                if (logStateTransitions && !_missingDesireServiceLogged)
                {
                    DebugUtility.LogWarning("Não foi possível inicializar o serviço de desejos (Master ou Config ausentes).", this, this);
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
            if (_desireService != null)
            {
                _desireService.EventDesireChanged -= HandleDesireChanged;
                _desireService.Stop();
            }

            _deathPredicate?.Dispose();
            _deathPredicate = null;

            _revivePredicate?.Dispose();
            _revivePredicate = null;

            _planetUnmarkedPredicate?.Dispose();
            _planetUnmarkedPredicate = null;
            _eatingWanderingPredicate = null;
        }

        private sealed class FalsePredicate : IPredicate
        {
            public static readonly FalsePredicate Instance = new();

            private FalsePredicate()
            {
            }

            public bool Evaluate()
            {
                return false;
            }
        }
    }
}
