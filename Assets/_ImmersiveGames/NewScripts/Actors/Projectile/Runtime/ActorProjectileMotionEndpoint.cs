using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorProjectileMotionEndpoint : MonoBehaviour, IPoolableObject
    {
        private bool _hasLoggedMotionTickAdvanced;

        public bool IsMotionConfigured { get; private set; }
        public ActorProjectileMotionStrategyKind MotionStrategy { get; private set; } = ActorProjectileMotionStrategyKind.Unknown;
        public float LinearSpeed { get; private set; }
        public Vector3 MotionDirection { get; private set; } = Vector3.zero;
        public Vector3 Velocity { get; private set; } = Vector3.zero;

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
                    $"event='ActorProjectileMotionConfigurationRejected' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='{bootstrap.Strategy}' linearSpeed='{bootstrap.Speed:0.###}' direction='{FormatVector(bootstrap.Direction)}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}' failureReason='{failureReason}'.");
                return false;
            }

            if (bootstrap.Strategy != ActorProjectileMotionStrategyKind.Linear)
            {
                failureReason = "projectile_motion_strategy_linear_required";
                DebugUtility.LogWarning(
                    typeof(ActorProjectileMotionEndpoint),
                    $"event='ActorProjectileMotionConfigurationRejected' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='{bootstrap.Strategy}' linearSpeed='{bootstrap.Speed:0.###}' direction='{FormatVector(bootstrap.Direction)}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}' failureReason='{failureReason}'.");
                return false;
            }

            MotionStrategy = bootstrap.Strategy;
            MotionDirection = bootstrap.Direction.normalized;
            LinearSpeed = bootstrap.Speed;
            Velocity = MotionDirection * LinearSpeed;
            IsMotionConfigured = true;
            _hasLoggedMotionTickAdvanced = false;

            DebugUtility.Log(
                typeof(ActorProjectileMotionEndpoint),
                $"event='ActorProjectileMotionConfigured' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='{MotionStrategy}' linearSpeed='{LinearSpeed:0.###}' direction='{FormatVector(MotionDirection)}' velocity='{FormatVector(Velocity)}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        public void OnPoolCreated()
        {
            ClearMotionState("pool_created", false);
        }

        public void OnPoolRent()
        {
            // O bootstrap vem depois do rent, aplicado pelo adapter técnico.
        }

        public void OnPoolReturn()
        {
            ClearMotionState("pool_return", true);
        }

        public void OnPoolDestroyed()
        {
            ClearMotionState("pool_destroyed", true);
        }

        private void Update()
        {
            if (!IsMotionConfigured || MotionStrategy != ActorProjectileMotionStrategyKind.Linear)
            {
                return;
            }

            var delta = Velocity * Time.deltaTime;
            if (delta.sqrMagnitude <= 0f)
            {
                return;
            }

            var before = transform.position;
            transform.position = before + delta;

            if (_hasLoggedMotionTickAdvanced)
            {
                return;
            }

            _hasLoggedMotionTickAdvanced = true;
            DebugUtility.Log(
                typeof(ActorProjectileMotionEndpoint),
                $"event='ActorProjectileMotionTickAdvanced' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='{MotionStrategy}' linearSpeed='{LinearSpeed:0.###}' direction='{FormatVector(MotionDirection)}' delta='{FormatVector(delta)}' positionBefore='{FormatVector(before)}' positionAfter='{FormatVector(transform.position)}' source='{nameof(ActorProjectileMotionEndpoint)}' reason='projectile_motion_tick_advanced'.",
                DebugUtility.Colors.Info);
        }

        private void ClearMotionState(string reason, bool log)
        {
            MotionStrategy = ActorProjectileMotionStrategyKind.Unknown;
            MotionDirection = Vector3.zero;
            Velocity = Vector3.zero;
            LinearSpeed = 0f;
            IsMotionConfigured = false;
            _hasLoggedMotionTickAdvanced = false;

            if (!log)
            {
                return;
            }

            DebugUtility.Log(
                typeof(ActorProjectileMotionEndpoint),
                $"event='ActorProjectileMotionStateCleared' actorId='{ResolveActorId()}' actorInstanceRuntimeId='{ResolveActorInstanceRuntimeId()}' motionStrategy='Unknown' linearSpeed='0' source='{nameof(ActorProjectileMotionEndpoint)}' reason='{reason.TrimToEmpty()}'.",
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
    }
}
