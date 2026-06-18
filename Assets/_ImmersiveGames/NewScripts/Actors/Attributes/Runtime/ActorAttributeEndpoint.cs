using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public sealed class ActorAttributeEndpoint : MonoBehaviour
    {
        [SerializeField] private ActorAttributeProfileAsset attributeProfile;

        private readonly Dictionary<ActorAttributeId, ActorAttributeState> _statesById = new();
        private ActorAttributeState[] _runtimeStates = Array.Empty<ActorAttributeState>();
        private ActorId _currentActorId = default;
        private ActorInstanceRuntimeId _currentActorInstanceRuntimeId = default;
        private SessionActivityIdentity _currentActivityIdentity = default;
        private IActorAttributeEventStream _eventStream;
        private bool _isInitialized;

        public ActorAttributeProfileAsset AttributeProfile => attributeProfile;
        public IReadOnlyList<ActorAttributeState> RuntimeStates => _runtimeStates;
        public ActorId CurrentActorId => _currentActorId;
        public ActorInstanceRuntimeId CurrentActorInstanceRuntimeId => _currentActorInstanceRuntimeId;
        public SessionActivityIdentity CurrentActivityIdentity => _currentActivityIdentity;
        public bool IsInitialized => _isInitialized;
        public int AttributeCount => _runtimeStates.Length;

        public void ConfigureEventStream(
            ActorId actorId,
            IActorAttributeEventStream eventStream,
            string source,
            string reason)
        {
            if (!actorId.IsValid)
            {
                throw new InvalidOperationException("ActorAttributeEndpoint requires a valid ActorId for canonical event publishing.");
            }

            _eventStream = eventStream ?? throw new InvalidOperationException("ActorAttributeEndpoint requires a non-null IActorAttributeEventStream for canonical event publishing.");
            _currentActorId = actorId;

            DebugUtility.LogVerbose(
                typeof(ActorAttributeEndpoint),
                $"event='ActorAttributeEventStreamConfigured' actorId='{_currentActorId}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Info);
        }

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

            if (!profile.TryValidate(out string validationReason))
            {
                result = ActorAttributeSetupResult.Fail(actorInstanceRuntimeId, $"attribute_profile_invalid:{validationReason}");
                return false;
            }

            _currentActorInstanceRuntimeId = actorInstanceRuntimeId;
            _currentActivityIdentity = activityIdentity;
            _runtimeStates = profile.CreateStates(_currentActorInstanceRuntimeId.Value);
            _isInitialized = true;

            if (_runtimeStates.Length == 0)
            {
                result = ActorAttributeSetupResult.SkippedNoContent(_currentActorInstanceRuntimeId, "attribute_profile_empty");
                return true;
            }

            for (int i = 0; i < _runtimeStates.Length; i++)
            {
                var state = _runtimeStates[i];
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

                if (_statesById.ContainsKey(state.AttributeId))
                {
                    ClearRuntimeState();
                    result = ActorAttributeSetupResult.Fail(actorInstanceRuntimeId, $"duplicate_runtime_attribute_id:attributeId={state.AttributeId}");
                    return false;
                }

                _statesById.Add(state.AttributeId, state);
            }

            result = ActorAttributeSetupResult.Ready(_currentActorInstanceRuntimeId, _runtimeStates.Length);
            return true;
        }

        public bool TryGetState(ActorAttributeId attributeId, out ActorAttributeState state)
        {
            if (!_isInitialized || !attributeId.IsValid)
            {
                state = null;
                return false;
            }

            return _statesById.TryGetValue(attributeId, out state);
        }

        public bool TryApplyCommand(ActorAttributeCommand command, out ActorAttributeApplyResult result)
        {
            if (!_isInitialized)
            {
                result = ActorAttributeApplyResult.Fail(command.ActorInstanceRuntimeId, command.AttributeId, "attribute_endpoint_not_initialized");
                return false;
            }

            if (!command.HasValidTarget)
            {
                result = ActorAttributeApplyResult.Reject(command.ActorInstanceRuntimeId, command.AttributeId, "attribute_command_invalid_target");
                return false;
            }

            if (command.ActorInstanceRuntimeId != _currentActorInstanceRuntimeId)
            {
                result = ActorAttributeApplyResult.Reject(_currentActorInstanceRuntimeId, command.AttributeId, "foreign_actor_instance_id");
                return false;
            }

            if (!MatchesRequiredActivityIdentity(command.ActivityIdentity, _currentActivityIdentity))
            {
                result = ActorAttributeApplyResult.Reject(_currentActorInstanceRuntimeId, command.AttributeId, "foreign_or_stale_activity_identity");
                return false;
            }

            if (!_statesById.TryGetValue(command.AttributeId, out var state))
            {
                result = ActorAttributeApplyResult.Fail(_currentActorInstanceRuntimeId, command.AttributeId, "attribute_state_not_found");
                return false;
            }

            if (!TryResolveNewValue(state, command, out float rawValue, out string failureReason))
            {
                result = ActorAttributeApplyResult.Reject(_currentActorInstanceRuntimeId, command.AttributeId, failureReason);
                return false;
            }

            float previousValue = state.CurrentValue;
            float clampedValue = state.Clamp(rawValue);
            bool clamped = Math.Abs(clampedValue - rawValue) > float.Epsilon;
            bool changed = Math.Abs(clampedValue - previousValue) > float.Epsilon;

            if (changed && (!_currentActorId.IsValid || _eventStream == null))
            {
                result = ActorAttributeApplyResult.Fail(_currentActorInstanceRuntimeId, command.AttributeId, "attribute_event_stream_not_configured");
                return false;
            }

            state.SetCurrentValue(clampedValue);

            var fact = new ActorAttributeChangedFact(
                command.ActivityIdentity,
                _currentActorInstanceRuntimeId,
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

            IReadOnlyList<ActorAttributeThresholdCrossedFact> thresholdFacts = ActorAttributeThresholdEvaluator.EvaluateCrossedThresholds(
                command.ActivityIdentity,
                _currentActorInstanceRuntimeId,
                state.AttributeId,
                command.Operation,
                previousValue,
                state.CurrentValue,
                state.MinValue,
                state.MaxValue,
                state.ThresholdDefinitions,
                command.Source,
                command.Reason);

            result = ActorAttributeApplyResult.AppliedWithFact(fact, thresholdFacts);

            if (changed &&
                !ActorAttributeApplyResultEventPublisher.TryPublish(
                    _eventStream,
                    _currentActorId,
                    result,
                    command.Source,
                    command.Reason))
            {
                result = ActorAttributeApplyResult.Fail(_currentActorInstanceRuntimeId, command.AttributeId, "attribute_event_publish_failed");
                return false;
            }

            return true;
        }


        public bool TryResetToInitial(
            SessionActivityIdentity activityIdentity,
            string source,
            string reason,
            out ActorAttributeResetResult result)
        {
            if (!_isInitialized)
            {
                result = ActorAttributeResetResult.Skipped(
                    _currentActorInstanceRuntimeId,
                    ActivityResetIntent.EntryInitialize,
                    ActivityResetStateProfileKind.InitialState,
                    "attribute_endpoint_not_initialized");
                return true;
            }

            if (!MatchesRequiredActivityIdentity(activityIdentity, _currentActivityIdentity))
            {
                result = ActorAttributeResetResult.Reject(
                    _currentActorInstanceRuntimeId,
                    ActivityResetIntent.EntryInitialize,
                    ActivityResetStateProfileKind.InitialState,
                    "foreign_or_stale_activity_identity");
                return false;
            }

            if (_runtimeStates == null || _runtimeStates.Length == 0)
            {
                result = ActorAttributeResetResult.Skipped(
                    _currentActorInstanceRuntimeId,
                    ActivityResetIntent.EntryInitialize,
                    ActivityResetStateProfileKind.InitialState,
                    "attribute_state_empty");
                return true;
            }

            int resetCount = 0;
            for (int i = 0; i < _runtimeStates.Length; i++)
            {
                var state = _runtimeStates[i];
                if (state == null || !state.IsReady)
                {
                    result = ActorAttributeResetResult.Fail(
                        _currentActorInstanceRuntimeId,
                        ActivityResetIntent.EntryInitialize,
                        ActivityResetStateProfileKind.InitialState,
                        $"attribute_state_not_ready:index={i}");
                    return false;
                }

                state.ResetToInitial();
                resetCount += 1;
            }

            result = ActorAttributeResetResult.AppliedResult(
                _currentActorInstanceRuntimeId,
                ActivityResetIntent.EntryInitialize,
                ActivityResetStateProfileKind.InitialState,
                resetCount);
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
            if (!_isInitialized)
            {
                result = ActorAttributeReleaseResult.Skipped(_currentActorInstanceRuntimeId, "attribute_endpoint_not_initialized");
                return true;
            }

            if (!MatchesRequiredActivityIdentity(activityIdentity, _currentActivityIdentity))
            {
                result = ActorAttributeReleaseResult.Reject(_currentActorInstanceRuntimeId, "foreign_or_stale_activity_identity");
                return false;
            }

            int releasedCount = _runtimeStates.Length;
            var releasedActorInstanceRuntimeId = _currentActorInstanceRuntimeId;
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
            _statesById.Clear();
            _runtimeStates = Array.Empty<ActorAttributeState>();
            _currentActorId = default;
            _currentActorInstanceRuntimeId = default;
            _currentActivityIdentity = default;
            _eventStream = null;
            _isInitialized = false;
        }
    }
}
