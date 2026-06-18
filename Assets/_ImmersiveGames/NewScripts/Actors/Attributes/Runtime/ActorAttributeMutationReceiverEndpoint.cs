using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorAttributeMutationReceiverEndpoint : MonoBehaviour, IActorAttributeMutationReceiverEndpoint
    {
        [SerializeField] private ActorAttributeEndpoint attributeEndpoint;

        private ActorId _actorId;
        private ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private SessionActivityIdentity _activityIdentity;
        private bool _isConfigured;

        public ActorId ActorId => _actorId;
        public ActorInstanceRuntimeId ActorInstanceRuntimeId => _actorInstanceRuntimeId;
        public SessionActivityIdentity ActivityIdentity => _activityIdentity;
        public bool IsConfigured => _isConfigured;

        public void Configure(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            SessionActivityIdentity activityIdentity,
            ActorAttributeEndpoint endpoint,
            string source,
            string reason)
        {
            if (!actorId.IsValid)
            {
                throw new InvalidOperationException("ActorAttributeMutationReceiverEndpoint requires a valid ActorId.");
            }

            if (!actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("ActorAttributeMutationReceiverEndpoint requires a valid ActorInstanceRuntimeId.");
            }

            if (!activityIdentity.IsValid)
            {
                throw new InvalidOperationException("ActorAttributeMutationReceiverEndpoint requires a valid SessionActivityIdentity.");
            }

            if (endpoint == null)
            {
                throw new InvalidOperationException("ActorAttributeMutationReceiverEndpoint requires a non-null ActorAttributeEndpoint.");
            }

            attributeEndpoint = endpoint;
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _activityIdentity = activityIdentity;
            _isConfigured = true;

            DebugUtility.LogVerbose(
                typeof(ActorAttributeMutationReceiverEndpoint),
                $"event='ActorAttributeMutationReceiverConfigured' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' activityId='{_activityIdentity.ActivityId}' entrySequence='{_activityIdentity.EntrySequence}' attributeEndpointPresent='{attributeEndpoint != null}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Info);
        }

        public bool TryReceiveMutationIntent(
            ActorAttributeMutationIntent intent,
            out ActorAttributeMutationResult result)
        {
            if (!_isConfigured || attributeEndpoint == null)
            {
                result = ActorAttributeMutationResult.Fail(
                    intent.ActorId,
                    intent.ActorInstanceRuntimeId,
                    intent.AttributeId,
                    intent.Operation,
                    "mutation_receiver_not_configured");
                return false;
            }

            if (!intent.IsValid)
            {
                result = ActorAttributeMutationResult.Reject(intent, "mutation_intent_invalid");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (intent.ActorId != _actorId)
            {
                result = ActorAttributeMutationResult.Reject(intent, "foreign_actor_id");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (intent.ActorInstanceRuntimeId != _actorInstanceRuntimeId)
            {
                result = ActorAttributeMutationResult.Reject(intent, "foreign_actor_instance_id");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (!MatchesRequiredActivityIdentity(intent.ActivityIdentity, _activityIdentity))
            {
                result = ActorAttributeMutationResult.Reject(intent, "foreign_or_stale_activity_identity");
                LogRejected(intent, result.Reason);
                return false;
            }

            if (!attributeEndpoint.TryApplyCommand(intent.ToCommand(), out var applyResult))
            {
                if (applyResult.Rejected)
                {
                    result = ActorAttributeMutationResult.Reject(intent, applyResult.Reason);
                    LogRejected(intent, result.Reason);
                    return false;
                }

                result = ActorAttributeMutationResult.Fail(intent, applyResult.Reason);
                LogFailed(intent, result.Reason);
                return false;
            }

            result = ActorAttributeMutationResult.AppliedResult(intent, applyResult);
            bool changed = applyResult.HasFact && applyResult.Fact.Changed;
            DebugUtility.LogVerbose(
                typeof(ActorAttributeMutationReceiverEndpoint),
                $"event='ActorAttributeMutationIntentApplied' actorId='{intent.ActorId}' actorInstanceRuntimeId='{intent.ActorInstanceRuntimeId}' attributeId='{intent.AttributeId}' operation='{intent.Operation}' changedFact='{changed}' thresholdFactCount='{applyResult.ThresholdFactCount}' attributeEventsPublished='{changed}' source='{intent.Source}' reason='{intent.Reason}'",
                DebugUtility.Colors.Success);
            return true;
        }

        private void LogRejected(ActorAttributeMutationIntent intent, string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActorAttributeMutationReceiverEndpoint),
                $"event='ActorAttributeMutationIntentRejected' actorId='{intent.ActorId}' actorInstanceRuntimeId='{intent.ActorInstanceRuntimeId}' expectedActorId='{_actorId}' expectedActorInstanceRuntimeId='{_actorInstanceRuntimeId}' attributeId='{intent.AttributeId}' operation='{intent.Operation}' outcomeReason='{reason.TrimToEmpty()}' source='{intent.Source}' reason='{intent.Reason}'",
                DebugUtility.Colors.Warning);
        }

        private void LogFailed(ActorAttributeMutationIntent intent, string reason)
        {
            DebugUtility.LogWarning(
                typeof(ActorAttributeMutationReceiverEndpoint),
                $"event='ActorAttributeMutationIntentFailed' actorId='{intent.ActorId}' actorInstanceRuntimeId='{intent.ActorInstanceRuntimeId}' attributeId='{intent.AttributeId}' operation='{intent.Operation}' outcomeReason='{reason.TrimToEmpty()}' source='{intent.Source}' reason='{intent.Reason}'");
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
