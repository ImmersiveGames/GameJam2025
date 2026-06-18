using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Damage/Actor Damageable Endpoint")]
    public sealed class ActorDamageableEndpoint : MonoBehaviour, IActorDamageableEndpoint
    {
        [Header("Damage Target")]
        [Tooltip("Atributo alvo que recebera o dano como subtracao. Ex.: PlayerAttributeDefinitionHealth.")]
        [SerializeField] private ActorAttributeDefinitionAsset targetAttributeDefinition;

        private ActorId _actorId;
        private ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private SessionActivityIdentity _activityIdentity;
        private ActorAttributeId _targetAttributeId;
        private ActorAttributeMutationReceiverEndpoint _mutationReceiver;
        private bool _isConfigured;

        public ActorId ActorId => _actorId;
        public ActorInstanceRuntimeId ActorInstanceRuntimeId => _actorInstanceRuntimeId;
        public SessionActivityIdentity ActivityIdentity => _activityIdentity;
        public ActorAttributeId TargetAttributeId => _targetAttributeId;
        public bool IsConfigured => _isConfigured;

        public void Configure(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            SessionActivityIdentity activityIdentity,
            ActorAttributeMutationReceiverEndpoint mutationReceiver,
            string source,
            string reason)
        {
            if (!actorId.IsValid)
            {
                throw new InvalidOperationException("ActorDamageableEndpoint requires a valid ActorId.");
            }

            if (!actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("ActorDamageableEndpoint requires a valid ActorInstanceRuntimeId.");
            }

            if (!activityIdentity.IsValid)
            {
                throw new InvalidOperationException("ActorDamageableEndpoint requires a valid SessionActivityIdentity.");
            }

            if (mutationReceiver == null || !mutationReceiver.IsConfigured)
            {
                throw new InvalidOperationException("ActorDamageableEndpoint requires a configured ActorAttributeMutationReceiverEndpoint.");
            }

            ActorAttributeId targetAttributeId = ActorAttributeId.FromDefinition(targetAttributeDefinition);
            if (!targetAttributeId.IsValid)
            {
                throw new InvalidOperationException("ActorDamageableEndpoint requires targetAttributeDefinition with a valid ActorAttributeId.");
            }

            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _activityIdentity = activityIdentity;
            _targetAttributeId = targetAttributeId;
            _mutationReceiver = mutationReceiver;
            _isConfigured = true;

            DebugUtility.LogVerbose(
                typeof(ActorDamageableEndpoint),
                $"event='ActorDamageableConfigured' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' activityId='{_activityIdentity.ActivityId}' entrySequence='{_activityIdentity.EntrySequence}' targetAttributeId='{_targetAttributeId}' mutationReceiverPresent='{_mutationReceiver != null}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Info);
        }

        public bool TryApplyDamageIntent(
            ActorDamageIntent intent,
            out ActorDamageResult result)
        {
            if (!_isConfigured || _mutationReceiver == null || !_mutationReceiver.IsConfigured)
            {
                result = ActorDamageResult.Fail(
                    intent.TargetActorId,
                    intent.TargetActorInstanceRuntimeId,
                    _targetAttributeId,
                    intent.RawDamageAmount,
                    0f,
                    "damageable_not_configured");
                return false;
            }

            if (!intent.IsValid)
            {
                result = ActorDamageResult.Reject(intent, _targetAttributeId, 0f, "damage_intent_invalid");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (intent.TargetActorId != _actorId)
            {
                result = ActorDamageResult.Reject(intent, _targetAttributeId, 0f, "foreign_actor_id");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (intent.TargetActorInstanceRuntimeId != _actorInstanceRuntimeId)
            {
                result = ActorDamageResult.Reject(intent, _targetAttributeId, 0f, "foreign_actor_instance_id");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (!MatchesRequiredActivityIdentity(intent.ActivityIdentity, _activityIdentity))
            {
                result = ActorDamageResult.Reject(intent, _targetAttributeId, 0f, "foreign_or_stale_activity_identity");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (!ActorDamagePolicy.TryResolveEffectiveDamage(intent, out float effectiveDamageAmount, out string policyReason))
            {
                result = ActorDamageResult.Reject(intent, _targetAttributeId, 0f, policyReason);
                LogRejected(intent, result.Reason);
                return false;
            }

            ActorAttributeMutationIntent mutationIntent = ActorAttributeMutationIntent.Subtract(
                intent.ActivityIdentity,
                intent.TargetActorId,
                intent.TargetActorInstanceRuntimeId,
                _targetAttributeId,
                effectiveDamageAmount,
                intent.Source,
                intent.Reason);

            if (!_mutationReceiver.TryReceiveMutationIntent(mutationIntent, out ActorAttributeMutationResult mutationResult) ||
                mutationResult.Rejected ||
                mutationResult.Failed)
            {
                result = mutationResult.Rejected
                    ? ActorDamageResult.Reject(intent, _targetAttributeId, effectiveDamageAmount, mutationResult.Reason)
                    : ActorDamageResult.Fail(intent, _targetAttributeId, effectiveDamageAmount, mutationResult.Reason);

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

            result = ActorDamageResult.AppliedResult(
                intent,
                _targetAttributeId,
                effectiveDamageAmount,
                mutationResult);

            DebugUtility.LogVerbose(
                typeof(ActorDamageableEndpoint),
                $"event='ActorDamageIntentApplied' targetActorId='{intent.TargetActorId}' targetActorInstanceRuntimeId='{intent.TargetActorInstanceRuntimeId}' targetAttributeId='{_targetAttributeId}' rawDamageAmount='{intent.RawDamageAmount:0.###}' effectiveDamageAmount='{effectiveDamageAmount:0.###}' changedFact='{mutationResult.HasChangedFact}' thresholdFactCount='{(mutationResult.HasApplyResult ? mutationResult.ApplyResult.ThresholdFactCount : 0)}' source='{intent.Source}' reason='{intent.Reason}'",
                DebugUtility.Colors.Success);
            return true;
        }

        private void LogRejected(ActorDamageIntent intent, string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActorDamageableEndpoint),
                $"event='ActorDamageIntentRejected' targetActorId='{intent.TargetActorId}' targetActorInstanceRuntimeId='{intent.TargetActorInstanceRuntimeId}' expectedActorId='{_actorId}' expectedActorInstanceRuntimeId='{_actorInstanceRuntimeId}' targetAttributeId='{_targetAttributeId}' rawDamageAmount='{intent.RawDamageAmount:0.###}' outcomeReason='{reason.TrimToEmpty()}' source='{intent.Source}' reason='{intent.Reason}'",
                DebugUtility.Colors.Warning);
        }

        private void LogFailed(ActorDamageIntent intent, string reason)
        {
            DebugUtility.LogWarning(
                typeof(ActorDamageableEndpoint),
                $"event='ActorDamageIntentFailed' targetActorId='{intent.TargetActorId}' targetActorInstanceRuntimeId='{intent.TargetActorInstanceRuntimeId}' targetAttributeId='{_targetAttributeId}' rawDamageAmount='{intent.RawDamageAmount:0.###}' outcomeReason='{reason.TrimToEmpty()}' source='{intent.Source}' reason='{intent.Reason}'");
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
