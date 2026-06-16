using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorProjectileMotionEndpoint : MonoBehaviour, IPoolableObject
    {
        private ActorProjectileMotionStrategyKind _motionStrategy = ActorProjectileMotionStrategyKind.Unknown;
        private Vector3 _motionDirection = Vector3.zero;
        private Vector3 _velocity = Vector3.zero;
        private float _linearSpeed;
        private bool _isMotionConfigured;
        private bool _hasLoggedMotionTickAdvanced;

        public bool IsMotionConfigured => _isMotionConfigured;
        public ActorProjectileMotionStrategyKind MotionStrategy => _motionStrategy;
        public float LinearSpeed => _linearSpeed;
        public Vector3 MotionDirection => _motionDirection;
        public Vector3 Velocity => _velocity;

        public bool TryConfigureMotion(
            ActorProjectileMotionBootstrap bootstrap,
            string source,
            string reason,
            out string failureReason)
        {
            failureReason = string.Empty;

            if (!bootstrap.IsValid)
            {
                failureReason = "projectile_motion_bootstrap_invalid";
                DebugUtility.LogWarning(
                    typeof(ActorProjectileMotionEndpoint),
                    $"event='ActorProjectileMotionConfigurationRejected' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='{bootstrap.Strategy}' linearSpeed='{bootstrap.Speed:0.###}' direction='{FormatVector(bootstrap.Direction)}' source='{Normalize(source)}' reason='{Normalize(reason)}' failureReason='{failureReason}'.");
                return false;
            }

            if (bootstrap.Strategy != ActorProjectileMotionStrategyKind.Linear)
            {
                failureReason = "projectile_motion_strategy_linear_required";
                DebugUtility.LogWarning(
                    typeof(ActorProjectileMotionEndpoint),
                    $"event='ActorProjectileMotionConfigurationRejected' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='{bootstrap.Strategy}' linearSpeed='{bootstrap.Speed:0.###}' direction='{FormatVector(bootstrap.Direction)}' source='{Normalize(source)}' reason='{Normalize(reason)}' failureReason='{failureReason}'.");
                return false;
            }

            _motionStrategy = bootstrap.Strategy;
            _motionDirection = bootstrap.Direction.normalized;
            _linearSpeed = bootstrap.Speed;
            _velocity = _motionDirection * _linearSpeed;
            _isMotionConfigured = true;
            _hasLoggedMotionTickAdvanced = false;

            DebugUtility.Log(
                typeof(ActorProjectileMotionEndpoint),
                $"event='ActorProjectileMotionConfigured' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='{_motionStrategy}' linearSpeed='{_linearSpeed:0.###}' direction='{FormatVector(_motionDirection)}' velocity='{FormatVector(_velocity)}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        public void OnPoolCreated()
        {
            ClearMotionState("pool_created", log: false);
        }

        public void OnPoolRent()
        {
            // O bootstrap vem depois do rent, aplicado pelo adapter técnico.
        }

        public void OnPoolReturn()
        {
            ClearMotionState("pool_return", log: true);
        }

        public void OnPoolDestroyed()
        {
            ClearMotionState("pool_destroyed", log: true);
        }

        private void Update()
        {
            if (!_isMotionConfigured || _motionStrategy != ActorProjectileMotionStrategyKind.Linear)
            {
                return;
            }

            Vector3 delta = _velocity * Time.deltaTime;
            if (delta.sqrMagnitude <= 0f)
            {
                return;
            }

            Vector3 before = transform.position;
            transform.position = before + delta;

            if (_hasLoggedMotionTickAdvanced)
            {
                return;
            }

            _hasLoggedMotionTickAdvanced = true;
            DebugUtility.Log(
                typeof(ActorProjectileMotionEndpoint),
                $"event='ActorProjectileMotionTickAdvanced' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='{_motionStrategy}' linearSpeed='{_linearSpeed:0.###}' direction='{FormatVector(_motionDirection)}' delta='{FormatVector(delta)}' positionBefore='{FormatVector(before)}' positionAfter='{FormatVector(transform.position)}' source='{nameof(ActorProjectileMotionEndpoint)}' reason='projectile_motion_tick_advanced'.",
                DebugUtility.Colors.Info);
        }

        private void ClearMotionState(string reason, bool log)
        {
            _motionStrategy = ActorProjectileMotionStrategyKind.Unknown;
            _motionDirection = Vector3.zero;
            _velocity = Vector3.zero;
            _linearSpeed = 0f;
            _isMotionConfigured = false;
            _hasLoggedMotionTickAdvanced = false;

            if (!log)
            {
                return;
            }

            DebugUtility.Log(
                typeof(ActorProjectileMotionEndpoint),
                $"event='ActorProjectileMotionStateCleared' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='Unknown' linearSpeed='0' source='{nameof(ActorProjectileMotionEndpoint)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private ActorId ResolveActorId()
        {
            var actor = GetComponent<RuntimeSpawnedActor>();
            return actor == null ? default : actor.ActorIdValue;
        }

        private ActorInstanceRuntimeId ResolveActorInstanceRuntimeId()
        {
            var actor = GetComponent<RuntimeSpawnedActor>();
            return actor == null ? default : actor.RuntimeActorInstanceId;
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
