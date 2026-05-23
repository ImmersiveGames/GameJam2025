using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public sealed class ActorAttributeEndpoint : MonoBehaviour
    {
        [SerializeField] private ActorAttributeProfileAsset attributeProfile;

        private readonly Dictionary<ActorAttributeId, ActorAttributeState> statesById = new Dictionary<ActorAttributeId, ActorAttributeState>();
        private ActorAttributeState[] runtimeStates = Array.Empty<ActorAttributeState>();
        private string currentActorInstanceId = string.Empty;
        private string currentPipelineIdentity = string.Empty;
        private string currentActivityIdentity = string.Empty;
        private bool isInitialized;

        public ActorAttributeProfileAsset AttributeProfile => attributeProfile;
        public IReadOnlyList<ActorAttributeState> RuntimeStates => runtimeStates;
        public string CurrentActorInstanceId => currentActorInstanceId;
        public string CurrentPipelineIdentity => currentPipelineIdentity;
        public string CurrentActivityIdentity => currentActivityIdentity;
        public bool IsInitialized => isInitialized;
        public int AttributeCount => runtimeStates.Length;

        public bool TryInitialize(string actorInstanceId, out ActorAttributeSetupResult result)
        {
            return TryInitializeFromProfile(
                actorInstanceId,
                attributeProfile,
                string.Empty,
                string.Empty,
                out result);
        }

        public bool TryInitialize(
            string actorInstanceId,
            string pipelineIdentity,
            string activityIdentity,
            out ActorAttributeSetupResult result)
        {
            return TryInitializeFromProfile(
                actorInstanceId,
                attributeProfile,
                pipelineIdentity,
                activityIdentity,
                out result);
        }

        public bool TryInitializeFromProfile(
            string actorInstanceId,
            ActorAttributeProfileAsset profile,
            string pipelineIdentity,
            string activityIdentity,
            out ActorAttributeSetupResult result)
        {
            ClearRuntimeState();

            if (string.IsNullOrWhiteSpace(actorInstanceId))
            {
                result = ActorAttributeSetupResult.Fail(string.Empty, "actor_instance_id_missing");
                return false;
            }

            if (profile == null)
            {
                result = ActorAttributeSetupResult.Fail(actorInstanceId, "attribute_profile_missing");
                return false;
            }

            if (!profile.TryValidate(out var validationReason))
            {
                result = ActorAttributeSetupResult.Fail(actorInstanceId, $"attribute_profile_invalid:{validationReason}");
                return false;
            }

            currentActorInstanceId = actorInstanceId.Trim();
            currentPipelineIdentity = pipelineIdentity ?? string.Empty;
            currentActivityIdentity = activityIdentity ?? string.Empty;
            runtimeStates = profile.CreateStates(currentActorInstanceId);
            isInitialized = true;

            if (runtimeStates.Length == 0)
            {
                result = ActorAttributeSetupResult.SkippedNoContent(currentActorInstanceId, "attribute_profile_empty");
                return true;
            }

            for (var i = 0; i < runtimeStates.Length; i++)
            {
                var state = runtimeStates[i];
                if (state == null)
                {
                    ClearRuntimeState();
                    result = ActorAttributeSetupResult.Fail(actorInstanceId, $"attribute_state_null:index={i}");
                    return false;
                }

                if (!state.IsReady)
                {
                    ClearRuntimeState();
                    result = ActorAttributeSetupResult.Fail(actorInstanceId, $"attribute_state_not_ready:index={i}");
                    return false;
                }

                if (statesById.ContainsKey(state.AttributeId))
                {
                    ClearRuntimeState();
                    result = ActorAttributeSetupResult.Fail(actorInstanceId, $"duplicate_runtime_attribute_id:attributeId={state.AttributeId}");
                    return false;
                }

                statesById.Add(state.AttributeId, state);
            }

            result = ActorAttributeSetupResult.Ready(currentActorInstanceId, runtimeStates.Length);
            return true;
        }

        public bool TryGetState(ActorAttributeId attributeId, out ActorAttributeState state)
        {
            if (!isInitialized || !attributeId.IsValid)
            {
                state = null;
                return false;
            }

            return statesById.TryGetValue(attributeId, out state);
        }

        public bool TryApplyCommand(ActorAttributeCommand command, out ActorAttributeApplyResult result)
        {
            if (!isInitialized)
            {
                result = ActorAttributeApplyResult.Fail(command.ActorInstanceId, command.AttributeId, "attribute_endpoint_not_initialized");
                return false;
            }

            if (!command.HasValidTarget)
            {
                result = ActorAttributeApplyResult.Reject(command.ActorInstanceId, command.AttributeId, "attribute_command_invalid_target");
                return false;
            }

            if (!string.Equals(command.ActorInstanceId, currentActorInstanceId, StringComparison.Ordinal))
            {
                result = ActorAttributeApplyResult.Reject(currentActorInstanceId, command.AttributeId, "foreign_actor_instance_id");
                return false;
            }

            if (!MatchesRequiredIdentity(command.PipelineIdentity, currentPipelineIdentity))
            {
                result = ActorAttributeApplyResult.Reject(currentActorInstanceId, command.AttributeId, "foreign_or_stale_pipeline_identity");
                return false;
            }

            if (!MatchesRequiredIdentity(command.ActivityIdentity, currentActivityIdentity))
            {
                result = ActorAttributeApplyResult.Reject(currentActorInstanceId, command.AttributeId, "foreign_or_stale_activity_identity");
                return false;
            }

            if (!statesById.TryGetValue(command.AttributeId, out var state))
            {
                result = ActorAttributeApplyResult.Fail(currentActorInstanceId, command.AttributeId, "attribute_state_not_found");
                return false;
            }

            if (!TryResolveNewValue(state, command, out var rawValue, out var failureReason))
            {
                result = ActorAttributeApplyResult.Reject(currentActorInstanceId, command.AttributeId, failureReason);
                return false;
            }

            var previousValue = state.CurrentValue;
            var clampedValue = state.Clamp(rawValue);
            var clamped = Math.Abs(clampedValue - rawValue) > float.Epsilon;
            state.SetCurrentValue(clampedValue);

            var fact = new ActorAttributeChangedFact(
                command.PipelineIdentity,
                command.ActivityIdentity,
                currentActorInstanceId,
                state.AttributeId,
                state.AttributeId.ToString(),
                command.Operation,
                previousValue,
                state.CurrentValue,
                state.MinValue,
                state.MaxValue,
                clamped,
                command.Source,
                command.Reason);

            result = ActorAttributeApplyResult.AppliedWithFact(fact);
            return true;
        }

        public bool TryRelease(out ActorAttributeReleaseResult result)
        {
            return TryRelease(string.Empty, string.Empty, out result);
        }

        public bool TryRelease(
            string pipelineIdentity,
            string activityIdentity,
            out ActorAttributeReleaseResult result)
        {
            if (!isInitialized)
            {
                result = ActorAttributeReleaseResult.Skipped(currentActorInstanceId, "attribute_endpoint_not_initialized");
                return true;
            }

            if (!MatchesRequiredIdentity(pipelineIdentity, currentPipelineIdentity))
            {
                result = ActorAttributeReleaseResult.Reject(currentActorInstanceId, "foreign_or_stale_pipeline_identity");
                return false;
            }

            if (!MatchesRequiredIdentity(activityIdentity, currentActivityIdentity))
            {
                result = ActorAttributeReleaseResult.Reject(currentActorInstanceId, "foreign_or_stale_activity_identity");
                return false;
            }

            var releasedCount = runtimeStates.Length;
            var releasedActorInstanceId = currentActorInstanceId;
            ClearRuntimeState();

            if (releasedCount == 0)
            {
                result = ActorAttributeReleaseResult.Skipped(releasedActorInstanceId, "attribute_state_empty");
                return true;
            }

            result = ActorAttributeReleaseResult.ReleasedResult(releasedActorInstanceId, releasedCount);
            return true;
        }

        private static bool TryResolveNewValue(
            ActorAttributeState state,
            ActorAttributeCommand command,
            out float rawValue,
            out string failureReason)
        {
            rawValue = state.CurrentValue;
            failureReason = string.Empty;

            switch (command.Operation)
            {
                case ActorAttributeOperation.Set:
                    rawValue = command.SetValue;
                    return true;
                case ActorAttributeOperation.Add:
                    if (command.Amount < 0f)
                    {
                        failureReason = "negative_amount_not_allowed_for_add";
                        return false;
                    }

                    rawValue = state.CurrentValue + command.Amount;
                    return true;
                case ActorAttributeOperation.Subtract:
                    if (command.Amount < 0f)
                    {
                        failureReason = "negative_amount_not_allowed_for_subtract";
                        return false;
                    }

                    rawValue = state.CurrentValue - command.Amount;
                    return true;
                case ActorAttributeOperation.ResetToInitial:
                    rawValue = state.InitialValue;
                    return true;
                case ActorAttributeOperation.RestoreToMax:
                    rawValue = state.MaxValue;
                    return true;
                default:
                    failureReason = "attribute_operation_unsupported";
                    return false;
            }
        }

        private static bool MatchesRequiredIdentity(string candidateIdentity, string requiredIdentity)
        {
            if (string.IsNullOrWhiteSpace(requiredIdentity))
            {
                return true;
            }

            return string.Equals(candidateIdentity ?? string.Empty, requiredIdentity, StringComparison.Ordinal);
        }

        private void ClearRuntimeState()
        {
            statesById.Clear();
            runtimeStates = Array.Empty<ActorAttributeState>();
            currentActorInstanceId = string.Empty;
            currentPipelineIdentity = string.Empty;
            currentActivityIdentity = string.Empty;
            isInitialized = false;
        }
    }
}
