using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.EaterSystem.Events;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.GameManagerSystems;
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

        public event Action<EaterDesireInfo> EventDesireChanged;

        private void Awake()
        {
            _master = GetComponent<EaterMaster>();
            _config = _master != null ? _master.Config : null;
            _audioEmitter = GetComponent<EntityAudioEmitter>();
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

            _stateMachine = new StateMachine();

            _wanderingState = RegisterState(new EaterWanderingState());
            _hungryState = RegisterState(new EaterHungryState());
            _chasingState = RegisterState(new EaterChasingState());
            _eatingState = RegisterState(new EaterEatingState());

            ForceSetState(_wanderingState, "Inicialização");
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
            if (_desireService != null)
            {
                _desireService.EventDesireChanged -= HandleDesireChanged;
                _desireService.Stop();
            }
        }
    }
}
