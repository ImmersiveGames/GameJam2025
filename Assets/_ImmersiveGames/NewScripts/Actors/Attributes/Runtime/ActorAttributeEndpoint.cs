using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public sealed class ActorAttributeEndpoint : MonoBehaviour
    {
        [SerializeField] private ActorAttributeProfileAsset attributeProfile;

        private readonly Dictionary<ActorAttributeId, ActorAttributeState> statesById = new Dictionary<ActorAttributeId, ActorAttributeState>();
        private ActorAttributeState[] runtimeStates = Array.Empty<ActorAttributeState>();
        private ActorInstanceRuntimeId currentActorInstanceRuntimeId = default;
        private SessionActivityIdentity currentActivityIdentity = default;
        private bool isInitialized;

        public ActorAttributeProfileAsset AttributeProfile => attributeProfile;
        public IReadOnlyList<ActorAttributeState> RuntimeStates => runtimeStates;
        public ActorInstanceRuntimeId CurrentActorInstanceRuntimeId => currentActorInstanceRuntimeId;
        public SessionActivityIdentity CurrentActivityIdentity => currentActivityIdentity;
        public bool IsInitialized => isInitialized;
        public int AttributeCount => runtimeStates.Length;

        public bool TryInitialize(ActorInstanceRuntimeId actorInstanceRuntimeId, out ActorAttributeSetupResult result)
        {
            return TryInitializeFromProfile(
                actorInstanceRuntimeId,
                attributeProfile,
                default,
                out result);
        }

        public bool TryInitialize(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            SessionActivityIdentity activityIdentity,
            out ActorAttributeSetupResult result)
        {
            return TryInitializeFromProfile(
                actorInstanceRuntimeId,
                attributeProfile,
                activityIdentity,
                out result);
        }

        public bool TryInitializeFromProfile(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeProfileAsset profile,
            SessionActivityIdentity activityIdentity,
            out ActorAttributeSetupResult result)
        {
            ClearRuntimeState();

            if (!actorInstanceRuntimeId.IsValid)
            {
                result = ActorAttributeSetupResult.Fail(default, "actor_instance_runtime_id_missing");
                return false;
            }

            if (profile == null)
            {
                result = ActorAttributeSetupResult.Fail(actorInstanceRuntimeId, "attribute_profile_missing");
                return false;
            }

            if (!profile.TryValidate(out var validationReason))
            {
                result = ActorAttributeSetupResult.Fail(actorInstanceRuntimeId, $"attribute_profile_invalid:{validationReason}");
                return false;
            }

            currentActorInstanceRuntimeId = actorInstanceRuntimeId;
            currentActivityIdentity = activityIdentity;
            runtimeStates = profile.CreateStates(currentActorInstanceRuntimeId.Value);
            isInitialized = true;

            if (runtimeStates.Length == 0)
            {
                result = ActorAttributeSetupResult.SkippedNoContent(currentActorInstanceRuntimeId, "attribute_profile_empty");
                return true;
            }

            for (var i = 0; i < runtimeStates.Length; i++)
            {
                var state = runtimeStates[i];
                if (state == null)
                {
                    ClearRuntimeState();
                    result = ActorAttributeSetupResult.Fail(actorInstanceRuntimeId, $"attribute_state_null:index={i}");
                    return false;
                }

                if (!state.IsReady)
                {
                    ClearRuntimeState();
                    result = ActorAttributeSetupResult.Fail(actorInstanceRuntimeId, $"attribute_state_not_ready:index={i}");
                    return false;
                }

                if (statesById.ContainsKey(state.AttributeId))
                {
                    ClearRuntimeState();
                    result = ActorAttributeSetupResult.Fail(actorInstanceRuntimeId, $"duplicate_runtime_attribute_id:attributeId={state.AttributeId}");
                    return false;
                }

                statesById.Add(state.AttributeId, state);
            }

            result = ActorAttributeSetupResult.Ready(currentActorInstanceRuntimeId, runtimeStates.Length);
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
                result = ActorAttributeApplyResult.Fail(command.ActorInstanceRuntimeId, command.AttributeId, "attribute_endpoint_not_initialized");
                return false;
            }

            if (!command.HasValidTarget)
            {
                result = ActorAttributeApplyResult.Reject(command.ActorInstanceRuntimeId, command.AttributeId, "attribute_command_invalid_target");
                return false;
            }

            if (command.ActorInstanceRuntimeId != currentActorInstanceRuntimeId)
            {
                result = ActorAttributeApplyResult.Reject(currentActorInstanceRuntimeId, command.AttributeId, "foreign_actor_instance_id");
                return false;
            }

            if (!MatchesRequiredActivityIdentity(command.ActivityIdentity, currentActivityIdentity))
            {
                result = ActorAttributeApplyResult.Reject(currentActorInstanceRuntimeId, command.AttributeId, "foreign_or_stale_activity_identity");
                return false;
            }

            if (!statesById.TryGetValue(command.AttributeId, out var state))
            {
                result = ActorAttributeApplyResult.Fail(currentActorInstanceRuntimeId, command.AttributeId, "attribute_state_not_found");
                return false;
            }

            if (!TryResolveNewValue(state, command, out var rawValue, out var failureReason))
            {
                result = ActorAttributeApplyResult.Reject(currentActorInstanceRuntimeId, command.AttributeId, failureReason);
                return false;
            }

            var previousValue = state.CurrentValue;
            var clampedValue = state.Clamp(rawValue);
            var clamped = Math.Abs(clampedValue - rawValue) > float.Epsilon;
            state.SetCurrentValue(clampedValue);

            var fact = new ActorAttributeChangedFact(
                command.ActivityIdentity,
                currentActorInstanceRuntimeId,
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
            return TryRelease(default, out result);
        }

        public bool TryRelease(
            SessionActivityIdentity activityIdentity,
            out ActorAttributeReleaseResult result)
        {
            if (!isInitialized)
            {
                result = ActorAttributeReleaseResult.Skipped(currentActorInstanceRuntimeId, "attribute_endpoint_not_initialized");
                return true;
            }

            if (!MatchesRequiredActivityIdentity(activityIdentity, currentActivityIdentity))
            {
                result = ActorAttributeReleaseResult.Reject(currentActorInstanceRuntimeId, "foreign_or_stale_activity_identity");
                return false;
            }

            var releasedCount = runtimeStates.Length;
            var releasedActorInstanceRuntimeId = currentActorInstanceRuntimeId;
            ClearRuntimeState();

            if (releasedCount == 0)
            {
                result = ActorAttributeReleaseResult.Skipped(releasedActorInstanceRuntimeId, "attribute_state_empty");
                return true;
            }

            result = ActorAttributeReleaseResult.ReleasedResult(releasedActorInstanceRuntimeId, releasedCount);
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

        private static bool MatchesRequiredActivityIdentity(SessionActivityIdentity candidateIdentity, SessionActivityIdentity requiredIdentity)
        {
            if (!requiredIdentity.IsValid)
            {
                return true;
            }

            return candidateIdentity.IsValid &&
                   candidateIdentity.CycleKey == requiredIdentity.CycleKey;
        }

        private void ClearRuntimeState()
        {
            statesById.Clear();
            runtimeStates = Array.Empty<ActorAttributeState>();
            currentActorInstanceRuntimeId = default;
            currentActivityIdentity = default;
            isInitialized = false;
        }
    }
}
