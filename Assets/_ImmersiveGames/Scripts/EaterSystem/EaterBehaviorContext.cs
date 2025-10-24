using System;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using ImprovedTimers;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    /// <summary>
    /// Contexto compartilhado entre os estados do comportamento do Eater.
    /// Mantém referências essenciais e estado atual para facilitar transições previsíveis.
    /// </summary>
    public sealed class EaterBehaviorContext : IDisposable
    {
        private readonly EaterMaster _master;
        private readonly Transform _transform;
        private readonly EaterConfigSo _config;
        private readonly ResourceAutoFlowBridge _autoFlowBridge;

        private readonly CountdownTimer _wanderingTimer;
        private PlanetsMaster _targetPlanet;
        private float _stateTimer;
        private Vector3 _lastKnownPlayerAnchor;
        private bool _hasCachedPlayerAnchor;
        private bool _pendingHungryEffects;
        private readonly EaterDesireService _desireService;
        private bool _hasMovementSample;
        private Vector3 _lastMovementDirection;
        private float _lastMovementSpeed;
        private bool _hasHungryMetrics;
        private float _lastAnchorDistance;
        private float _lastAnchorAlignment;
        private bool _disposed;
        private EaterDesireInfo _currentDesireInfo = EaterDesireInfo.Inactive;

        public event Action<PlanetsMaster, PlanetsMaster> EventTargetChanged;

        public EaterBehaviorContext(EaterMaster master, EaterConfigSo config, Rect gameArea)
        {
            _master = master;
            _transform = master.transform;
            _config = config;
            GameArea = gameArea;
            if (master.TryGetComponent(out ResourceAutoFlowBridge autoFlowBridge))
            {
                _autoFlowBridge = autoFlowBridge;
            }
            _desireService = new EaterDesireService(master, config);
            _desireService.EventDesireChanged += HandleDesireChanged;
            if (config.WanderingDuration > 0f)
            {
                _wanderingTimer = new CountdownTimer(config.WanderingDuration);
            }
        }

        public EaterMaster Master => _master;
        public Transform Transform => _transform;
        public EaterConfigSo Config => _config;
        public Rect GameArea { get; set; }
        public bool IsHungry { get; private set; }
        public bool IsEating { get; private set; }
        /// <summary>
        /// Planeta atualmente selecionado como alvo do Eater.
        /// </summary>
        public PlanetsMaster Target => _targetPlanet;
        public PlanetsMaster TargetPlanet => _targetPlanet;
        public float StateTimer => _stateTimer;
        public bool HasTarget => _targetPlanet != null;

        public bool ShouldChase => IsHungry && HasTarget && !IsEating;
        public bool ShouldEat => IsEating && HasTarget;
        public bool ShouldEnableProximitySensor => ShouldChase || ShouldEat;
        public bool ShouldWander => !IsHungry && !IsEating;
        public bool LostTargetWhileHungry => IsHungry && !HasTarget && !IsEating;
        public bool HasPlayerAnchor => _hasCachedPlayerAnchor;
        public bool HasWanderingTimer => _wanderingTimer != null;
        public bool IsWanderingTimerRunning => _wanderingTimer != null && _wanderingTimer.IsRunning;
        public bool HasAutoFlowService => _autoFlowBridge != null && _autoFlowBridge.HasAutoFlowService;
        public bool IsAutoFlowActive => _autoFlowBridge != null && _autoFlowBridge.IsAutoFlowActive;
        public bool AreDesiresActive => _desireService != null && _currentDesireInfo.ServiceActive;
        public bool HasPendingHungryEffects => _pendingHungryEffects;
        public bool HasMovementSample => _hasMovementSample;
        public Vector3 LastMovementDirection => _lastMovementDirection;
        public float LastMovementSpeed => _lastMovementSpeed;
        public bool HasHungryMetrics => _hasHungryMetrics;
        public float LastAnchorDistance => _lastAnchorDistance;
        public float LastAnchorAlignment => _lastAnchorAlignment;
        public bool HasCurrentDesire => _currentDesireInfo.HasDesire;
        public PlanetResources? CurrentDesire => _currentDesireInfo.Resource;
        public bool CurrentDesireAvailable => _currentDesireInfo.IsAvailable;
        public float CurrentDesireDuration => _currentDesireInfo.Duration;
        public float CurrentDesireRemainingTime => _currentDesireInfo.RemainingTime;
        public int CurrentDesireAvailableCount => _currentDesireInfo.AvailableCount;
        public float CurrentDesireWeight => _currentDesireInfo.Weight;
        public EaterDesireInfo CurrentDesireInfo => _currentDesireInfo;
        public event Action<EaterDesireInfo> EventDesireChanged;

        public void ResetStateTimer()
        {
            _stateTimer = 0f;
        }

        public void AdvanceStateTimer(float deltaTime)
        {
            _stateTimer += Mathf.Max(deltaTime, 0f);
        }

        public bool SetHungry(bool value)
        {
            if (IsHungry == value)
            {
                return false;
            }

            IsHungry = value;
            _master.InHungry = value;

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

        /// <summary>
        /// Define o planeta que o Eater deve perseguir.
        /// </summary>
        public bool SetTarget(PlanetsMaster target)
        {
            PlanetsMaster previousTarget = _targetPlanet;

            if (previousTarget == target)
            {
                return false;
            }

            _targetPlanet = target;
            EventTargetChanged?.Invoke(previousTarget, _targetPlanet);
            return true;
        }

        public bool ClearTarget()
        {
            return SetTarget(null);
        }

        public bool SetEating(bool value)
        {
            if (IsEating == value)
            {
                return false;
            }

            IsEating = value;
            _master.IsEating = value;
            return true;
        }

        public bool TryGetTargetPosition(out Vector3 position)
        {
            if (_targetPlanet != null)
            {
                position = _targetPlanet.transform.position;
                return true;
            }

            position = default;
            return false;
        }

        public bool TryGetPlayerAnchor(out Vector3 anchor)
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

            anchor = Transform.position;
            return false;
        }

        public bool TryGetCachedPlayerAnchor(out Vector3 anchor)
        {
            if (_hasCachedPlayerAnchor)
            {
                anchor = _lastKnownPlayerAnchor;
                return true;
            }

            anchor = Transform.position;
            return false;
        }

        public void RestartWanderingTimer()
        {
            if (_wanderingTimer == null)
            {
                return;
            }

            _wanderingTimer.Stop();
            _wanderingTimer.Reset(_config.WanderingDuration);
            _wanderingTimer.Start();
        }

        public void StopWanderingTimer()
        {
            if (_wanderingTimer == null)
            {
                return;
            }

            _wanderingTimer.Stop();
        }

        public bool HasWanderingTimerElapsed()
        {
            return _wanderingTimer != null && _wanderingTimer.IsFinished;
        }

        public float GetWanderingTimerValue()
        {
            return _wanderingTimer != null ? _wanderingTimer.CurrentTime : 0f;
        }

        public bool ResumeAutoFlow()
        {
            if (_autoFlowBridge == null || !_autoFlowBridge.IsInitialized || !_autoFlowBridge.HasAutoFlowService)
            {
                return false;
            }

            bool resumed = _autoFlowBridge.ResumeAutoFlow();
            if (resumed)
            {
                DebugUtility.LogVerbose<EaterBehaviorContext>($"AutoFlow retomado para {_master.ActorId}.");
            }

            return _autoFlowBridge.IsAutoFlowActive;
        }

        public bool PauseAutoFlow()
        {
            if (_autoFlowBridge == null || !_autoFlowBridge.IsInitialized || !_autoFlowBridge.HasAutoFlowService)
            {
                return false;
            }

            bool paused = _autoFlowBridge.PauseAutoFlow();
            if (paused)
            {
                DebugUtility.LogVerbose<EaterBehaviorContext>($"AutoFlow pausado para {_master.ActorId}.");
            }

            return paused;
        }

        public bool BeginDesires()
        {
            if (_desireService == null)
            {
                return false;
            }

            bool started = _desireService.Start();
            if (started)
            {
                DebugUtility.LogVerbose<EaterBehaviorContext>($"Desejos iniciados para {_master.ActorId}.");
            }

            return started;
        }

        public bool EndDesires()
        {
            if (_desireService == null)
            {
                return false;
            }

            bool stopped = _desireService.Stop();
            if (stopped)
            {
                DebugUtility.LogVerbose<EaterBehaviorContext>($"Desejos pausados para {_master.ActorId}.");
            }

            return stopped;
        }

        public void EnsureHungryEffects()
        {
            if (!IsHungry || !_pendingHungryEffects)
            {
                return;
            }

            bool active = ResumeAutoFlow();
            if (active)
            {
                _pendingHungryEffects = false;
            }
        }

        public void UpdateServices()
        {
            _desireService?.Update();
        }

        public void ReportMovementSample(Vector3 direction, float speed, bool clearHungryMetrics = true)
        {
            _lastMovementDirection = direction;
            _lastMovementSpeed = speed;
            _hasMovementSample = direction.sqrMagnitude > 0f || speed > 0f;
            if (clearHungryMetrics)
            {
                _hasHungryMetrics = false;
            }
        }

        public void ReportHungryMetrics(float distanceToAnchor, float alignmentWithAnchor)
        {
            _lastAnchorDistance = Mathf.Max(distanceToAnchor, 0f);
            _lastAnchorAlignment = Mathf.Clamp(alignmentWithAnchor, -1f, 1f);
            _hasHungryMetrics = true;
        }

        public EaterDesireInfo GetCurrentDesireInfo()
        {
            if (_desireService == null)
            {
                return EaterDesireInfo.Inactive;
            }

            return _currentDesireInfo;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_desireService != null)
            {
                _desireService.EventDesireChanged -= HandleDesireChanged;
            }

            _disposed = true;
        }

        private void HandleDesireChanged(EaterDesireInfo info)
        {
            _currentDesireInfo = info;
            EventDesireChanged?.Invoke(info);
        }
    }
}
