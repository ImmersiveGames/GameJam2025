using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Damage/Actor Damage Source Endpoint")]
    public sealed class ActorDamageSourceEndpoint : MonoBehaviour, IActorDamageSourceEndpoint
    {
        [Header("Damage Source")]
        [Tooltip("Tipo semantico simples do dano emitido por esta fonte. Ex.: direct, projectile, hazard.")]
        [SerializeField] private string damageKind = "direct";

        private SessionActivityIdentity _activityIdentity;

        public ActorId SourceActorId { get; private set; }
        public ActorInstanceRuntimeId SourceActorInstanceRuntimeId { get; private set; }
        public SessionActivityIdentity ActivityIdentity => _activityIdentity;
        public bool IsConfigured { get; private set; }

        public void Configure(
            ActorId sourceActorId,
            ActorInstanceRuntimeId sourceActorInstanceRuntimeId,
            SessionActivityIdentity activityIdentity,
            string source,
            string reason)
        {
            if (!sourceActorId.IsValid)
            {
                throw new InvalidOperationException("ActorDamageSourceEndpoint requires a valid source ActorId.");
            }

            if (!sourceActorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("ActorDamageSourceEndpoint requires a valid source ActorInstanceRuntimeId.");
            }

            if (!activityIdentity.IsValid)
            {
                throw new InvalidOperationException("ActorDamageSourceEndpoint requires a valid SessionActivityIdentity.");
            }

            SourceActorId = sourceActorId;
            SourceActorInstanceRuntimeId = sourceActorInstanceRuntimeId;
            _activityIdentity = activityIdentity;
            IsConfigured = true;

            DebugUtility.LogVerbose(
                typeof(ActorDamageSourceEndpoint),
                $"event='ActorDamageSourceConfigured' sourceActorId='{SourceActorId}' sourceActorInstanceRuntimeId='{SourceActorInstanceRuntimeId}' activityId='{_activityIdentity.ActivityId}' entrySequence='{_activityIdentity.EntrySequence}' damageKind='{damageKind.TrimToEmpty()}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Info);
        }

        public bool TryEmitDamageIntent(
            ActorDamageSourceIntent intent,
            IActorDamageableEndpoint target,
            out ActorDamageSourceResult result)
        {
            if (!IsConfigured)
            {
                result = ActorDamageSourceResult.Fail(
                    intent.SourceActorId,
                    intent.SourceActorInstanceRuntimeId,
                    intent.TargetActorId,
                    intent.TargetActorInstanceRuntimeId,
                    intent.RawDamageAmount,
                    "damage_source_not_configured");
                return false;
            }

            if (!intent.IsValid)
            {
                result = ActorDamageSourceResult.Reject(intent, "damage_source_intent_invalid");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (target == null || !target.IsConfigured)
            {
                result = ActorDamageSourceResult.Reject(intent, "damage_target_not_configured");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (intent.SourceActorId != SourceActorId)
            {
                result = ActorDamageSourceResult.Reject(intent, "foreign_source_actor_id");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (intent.SourceActorInstanceRuntimeId != SourceActorInstanceRuntimeId)
            {
                result = ActorDamageSourceResult.Reject(intent, "foreign_source_actor_instance_id");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (intent.TargetActorId != target.ActorId)
            {
                result = ActorDamageSourceResult.Reject(intent, "foreign_target_actor_id");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (intent.TargetActorInstanceRuntimeId != target.ActorInstanceRuntimeId)
            {
                result = ActorDamageSourceResult.Reject(intent, "foreign_target_actor_instance_id");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (!MatchesRequiredActivityIdentity(intent.ActivityIdentity, _activityIdentity) ||
                !MatchesRequiredActivityIdentity(intent.ActivityIdentity, target.ActivityIdentity))
            {
                result = ActorDamageSourceResult.Reject(intent, "foreign_or_stale_activity_identity");
                LogRejected(intent, result.Reason);
                return false;
            }

            ActorDamageIntent damageIntent = new(
                intent.ActivityIdentity,
                intent.TargetActorId,
                intent.TargetActorInstanceRuntimeId,
                intent.SourceActorId,
                intent.RawDamageAmount,
                string.IsNullOrWhiteSpace(intent.DamageKind) ? damageKind : intent.DamageKind,
                intent.Source,
                intent.Reason);

            if (!target.TryApplyDamageIntent(damageIntent, out var damageResult) ||
                damageResult.Rejected ||
                damageResult.Failed)
            {
                result = damageResult.Rejected
                    ? ActorDamageSourceResult.Reject(intent, damageResult.Reason)
                    : ActorDamageSourceResult.Fail(intent, damageResult.Reason);

                if (result.Rejected)
                {
                    LogRejected(intent, result.Reason);
                }
                else
                {
                    LogFailed(intent, result.Reason);
                }

                return false;
            }

            result = ActorDamageSourceResult.EmittedResult(intent, damageResult);

            DebugUtility.LogVerbose(
                typeof(ActorDamageSourceEndpoint),
                $"event='ActorDamageSourceIntentEmitted' sourceActorId='{intent.SourceActorId}' sourceActorInstanceRuntimeId='{intent.SourceActorInstanceRuntimeId}' targetActorId='{intent.TargetActorId}' targetActorInstanceRuntimeId='{intent.TargetActorInstanceRuntimeId}' damageKind='{damageIntent.DamageKind.TrimToEmpty()}' rawDamageAmount='{intent.RawDamageAmount:0.###}' effectiveDamageAmount='{damageResult.EffectiveDamageAmount:0.###}' changedFact='{damageResult.HasChangedFact}' thresholdFacts='{damageResult.HasThresholdFacts}' source='{intent.Source}' reason='{intent.Reason}'",
                DebugUtility.Colors.Success);
            return true;
        }

        private void LogRejected(ActorDamageSourceIntent intent, string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActorDamageSourceEndpoint),
                $"event='ActorDamageSourceIntentRejected' sourceActorId='{intent.SourceActorId}' sourceActorInstanceRuntimeId='{intent.SourceActorInstanceRuntimeId}' targetActorId='{intent.TargetActorId}' targetActorInstanceRuntimeId='{intent.TargetActorInstanceRuntimeId}' rawDamageAmount='{intent.RawDamageAmount:0.###}' outcomeReason='{reason.TrimToEmpty()}' source='{intent.Source}' reason='{intent.Reason}'",
                DebugUtility.Colors.Warning);
        }

        private void LogFailed(ActorDamageSourceIntent intent, string reason)
        {
            DebugUtility.LogWarning(
                typeof(ActorDamageSourceEndpoint),
                $"event='ActorDamageSourceIntentFailed' sourceActorId='{intent.SourceActorId}' sourceActorInstanceRuntimeId='{intent.SourceActorInstanceRuntimeId}' targetActorId='{intent.TargetActorId}' targetActorInstanceRuntimeId='{intent.TargetActorInstanceRuntimeId}' rawDamageAmount='{intent.RawDamageAmount:0.###}' outcomeReason='{reason.TrimToEmpty()}' source='{intent.Source}' reason='{intent.Reason}'");
        }

        private static bool MatchesRequiredActivityIdentity(
            SessionActivityIdentity candidateIdentity,
            SessionActivityIdentity requiredIdentity)
        {
            if (!requiredIdentity.IsValid)
            {
                return true;
            }

            return candidateIdentity.IsValid &&
                candidateIdentity.CycleKey == requiredIdentity.CycleKey;
        }
    }
}
