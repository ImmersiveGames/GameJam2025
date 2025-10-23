using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.GameManagerSystems;
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
    public sealed class EaterBehaviorContext
    {
        private readonly EaterMaster _master;
        private readonly Transform _transform;
        private readonly EaterConfigSo _config;
        private readonly ResourceAutoFlowBridge _autoFlowBridge;

        private readonly CountdownTimer _wanderingTimer;
        private IDetectable _target;
        private float _stateTimer;
        private Vector3 _lastKnownPlayerAnchor;
        private bool _hasCachedPlayerAnchor;
        private bool _desiresActive;
        private bool _pendingHungryEffects;

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
        public IDetectable Target => _target;
        public float StateTimer => _stateTimer;
        public bool HasTarget => _target != null;

        public bool ShouldChase => IsHungry && HasTarget && !IsEating;
        public bool ShouldEat => IsEating && HasTarget;
        public bool ShouldWander => !IsHungry && !IsEating;
        public bool LostTargetWhileHungry => IsHungry && !HasTarget && !IsEating;
        public bool HasPlayerAnchor => _hasCachedPlayerAnchor;
        public bool HasWanderingTimer => _wanderingTimer != null;
        public bool IsWanderingTimerRunning => _wanderingTimer != null && _wanderingTimer.IsRunning;
        public bool HasAutoFlowService => _autoFlowBridge != null && _autoFlowBridge.HasAutoFlowService;
        public bool IsAutoFlowActive => _autoFlowBridge != null && _autoFlowBridge.IsAutoFlowActive;
        public bool AreDesiresActive => _desiresActive;
        public bool HasPendingHungryEffects => _pendingHungryEffects;

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

        public bool SetTarget(IDetectable target)
        {
            if (_target == target)
            {
                return false;
            }

            _target = target;
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
            if (_target?.Owner?.Transform != null)
            {
                position = _target.Owner.Transform.position;
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
            if (_desiresActive)
            {
                return false;
            }

            _desiresActive = true;
            DebugUtility.LogVerbose<EaterBehaviorContext>($"Desejos iniciados para {_master.ActorId}.");
            return true;
        }

        public bool EndDesires()
        {
            if (!_desiresActive)
            {
                return false;
            }

            _desiresActive = false;
            DebugUtility.LogVerbose<EaterBehaviorContext>($"Desejos pausados para {_master.ActorId}.");
            return true;
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
    }
}
