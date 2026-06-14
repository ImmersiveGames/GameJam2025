using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;
using PlayerActivityParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipationContext;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityActorExitRuntimeState
    {
        public readonly struct ActorPresentationCapabilityState
        {
            public ActorPresentationCapabilityState(
                ActorInstanceRuntimeId actorInstanceRuntimeId,
                string actorId,
                ActorPresentationEndpoint endpoint,
                ActorPresentationRuntimeHandle runtimeHandle,
                string pipelineIdentity,
                string activityIdentity)
            {
                ActorInstanceRuntimeId = actorInstanceRuntimeId;
                ActorId = Normalize(actorId);
                Endpoint = endpoint;
                RuntimeHandle = runtimeHandle;
                PipelineIdentity = Normalize(pipelineIdentity);
                ActivityIdentity = Normalize(activityIdentity);
            }

            public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
            public string ActorId { get; }
            public ActorPresentationEndpoint Endpoint { get; }
            public ActorPresentationRuntimeHandle RuntimeHandle { get; }
            public string PipelineIdentity { get; }
            public string ActivityIdentity { get; }
            public bool IsValid =>
                ActorInstanceRuntimeId.IsValid &&
                !string.IsNullOrWhiteSpace(ActorId) &&
                Endpoint != null &&
                RuntimeHandle.IsValid &&
                !string.IsNullOrWhiteSpace(PipelineIdentity) &&
                !string.IsNullOrWhiteSpace(ActivityIdentity);
        }

        private readonly Dictionary<ActorInstanceRuntimeId, ActorPresentationCapabilityState> _activeActorPresentationByActorInstanceId = new();
        private readonly Dictionary<ActorInstanceRuntimeId, SessionActivityPipeline.ActorAttributeCapabilityState> _activeActorAttributeCapabilitiesByActorInstanceId = new();
        private readonly HashSet<ActorInstanceRuntimeId> _activeActorParticipationsByActorInstanceRuntimeId = new();
        private readonly Dictionary<ActorId, PlayerActivityParticipantBinding> _activePlayerParticipantBindingsByActorId = new();
        private PlayerActivityParticipationContext _currentActivityParticipationContext;
        private ActorInventoryFeedResult _currentActorInventoryFeedResult;

        public int ActivePresentationCount => _activeActorPresentationByActorInstanceId.Count;
        public int ActiveAttributeCount => _activeActorAttributeCapabilitiesByActorInstanceId.Count;
        public int ActiveParticipationCount => _activeActorParticipationsByActorInstanceRuntimeId.Count;
        public int ActivePlayerParticipantBindingCount => _activePlayerParticipantBindingsByActorId.Count;
        public bool HasActorInventoryFeedResult => _currentActorInventoryFeedResult.IsValid;

        public IReadOnlyList<PlayerActivityParticipantBinding> GetActivePlayerParticipantBindings()
        {
            List<PlayerActivityParticipantBinding> bindings = new(_activePlayerParticipantBindingsByActorId.Count);
            foreach (KeyValuePair<ActorId, PlayerActivityParticipantBinding> pair in _activePlayerParticipantBindingsByActorId)
            {
                if (pair.Value.IsValid)
                {
                    bindings.Add(pair.Value);
                }
            }

            return bindings;
        }

        public bool TryGetActivePresentationHandle(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            out ActorPresentationRuntimeHandle handle)
        {
            handle = default;
            if (!actorInstanceRuntimeId.IsValid)
            {
                return false;
            }

            if (!_activeActorPresentationByActorInstanceId.TryGetValue(actorInstanceRuntimeId, out var state) || !state.IsValid)
            {
                return false;
            }

            handle = state.RuntimeHandle;
            return handle.IsValid;
        }

        public bool TryGetActivePresentationState(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            out ActorPresentationCapabilityState state)
        {
            state = default;
            return actorInstanceRuntimeId.IsValid &&
                   _activeActorPresentationByActorInstanceId.TryGetValue(actorInstanceRuntimeId, out state) &&
                   state.IsValid;
        }

        public void StoreActiveActorPresentation(
            ActorPresentationCapabilityState state,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!state.IsValid)
            {
                return;
            }

            _activeActorPresentationByActorInstanceId[state.ActorInstanceRuntimeId] = state;
        }

        public IReadOnlyList<ActorPresentationCapabilityState> ResolveActiveActorPresentationStates(ActorInstanceRuntimeId targetActorInstanceRuntimeId)
        {
            List<ActorPresentationCapabilityState> activeStates = new();
            if (targetActorInstanceRuntimeId.IsValid)
            {
                if (_activeActorPresentationByActorInstanceId.TryGetValue(targetActorInstanceRuntimeId, out var targetedState) &&
                    targetedState.IsValid)
                {
                    activeStates.Add(targetedState);
                }

                return activeStates;
            }

            foreach (var state in _activeActorPresentationByActorInstanceId.Values)
            {
                if (state.IsValid)
                {
                    activeStates.Add(state);
                }
            }

            return activeStates;
        }

        public void RemoveActiveActorPresentation(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            _activeActorPresentationByActorInstanceId.Remove(actorInstanceRuntimeId);
        }

        public bool TryGetActiveActorAttributeCapability(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            out SessionActivityPipeline.ActorAttributeCapabilityState state)
        {
            state = default;
            return actorInstanceRuntimeId.IsValid &&
                   _activeActorAttributeCapabilitiesByActorInstanceId.TryGetValue(actorInstanceRuntimeId, out state) &&
                   state.IsValid;
        }

        public bool TryGetActiveActorAttributeCapability(
            string actorId,
            out SessionActivityPipeline.ActorAttributeCapabilityState state)
        {
            state = default;
            string normalizedActorId = string.IsNullOrWhiteSpace(actorId) ? string.Empty : actorId.Trim();
            if (string.IsNullOrWhiteSpace(normalizedActorId))
            {
                return false;
            }

            foreach (var candidate in _activeActorAttributeCapabilitiesByActorInstanceId.Values)
            {
                if (!candidate.IsValid ||
                    !string.Equals(candidate.ActorId, normalizedActorId, StringComparison.Ordinal))
                {
                    continue;
                }

                state = candidate;
                return true;
            }

            return false;
        }

        public void StoreActiveActorAttributeCapability(
            SessionActivityPipeline.ActorAttributeCapabilityState state,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!state.IsValid)
            {
                return;
            }

            _activeActorAttributeCapabilitiesByActorInstanceId[state.ActorInstanceRuntimeId] = state;
        }

        public IReadOnlyList<SessionActivityPipeline.ActorAttributeCapabilityState> ResolveActiveActorAttributeStates()
        {
            List<SessionActivityPipeline.ActorAttributeCapabilityState> activeStates = new();
            foreach (var state in _activeActorAttributeCapabilitiesByActorInstanceId.Values)
            {
                if (state.IsValid)
                {
                    activeStates.Add(state);
                }
            }

            return activeStates;
        }

        public void RemoveActiveActorAttributeCapability(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            _activeActorAttributeCapabilitiesByActorInstanceId.Remove(actorInstanceRuntimeId);
        }

        public void StoreActiveActorParticipation(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            _activeActorParticipationsByActorInstanceRuntimeId.Add(actorInstanceRuntimeId);
        }

        public ActorParticipationExitResult ExecuteActorParticipationExit(ActorParticipationExitCommand command)
        {
            ActorParticipationExitStageExecutor exitExecutor = new();
            return exitExecutor.Execute(command, _activeActorParticipationsByActorInstanceRuntimeId);
        }

        public void RemoveActiveActorParticipation(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            _activeActorParticipationsByActorInstanceRuntimeId.Remove(actorInstanceRuntimeId);
        }

        public void ClearActiveActorParticipations(string activityId, int entrySequence, string source, string reason)
        {

            _activeActorParticipationsByActorInstanceRuntimeId.Clear();
        }

        public void StoreActivityParticipationExitCorrelation(PlayerActivityParticipationContext context)
        {
            _currentActivityParticipationContext = context;

            bool hasExplicitParticipants = context is { IsValid: true, Participants: { Count: > 0 } };

            if (hasExplicitParticipants)
            {
                _activePlayerParticipantBindingsByActorId.Clear();

                for (int index = 0; index < context.Participants.Count; index++)
                {
                    var binding = context.Participants[index];
                    if (!binding.IsValid || !binding.RequiresPlayerActor || !binding.ActorId.IsValid)
                    {
                        continue;
                    }

                    _activePlayerParticipantBindingsByActorId[binding.ActorId] = binding;
                }
            }

        }

        public bool TryResolveActivePlayerParticipantBindingForExit(
            ActorParticipationExitActorResult actorResult,
            ActorInstanceRecord instance,
            out PlayerActivityParticipantBinding binding,
            out string failureReason)
        {
            binding = default;
            failureReason = "unknown";

            if (!instance.IsValid)
            {
                failureReason = "actor_instance_invalid";
                return false;
            }

            ActorId actorId = new(instance.ActorId);
            if (!actorId.IsValid)
            {
                failureReason = "actor_id_missing_in_actor_participation_record";
                return false;
            }

            if (_activePlayerParticipantBindingsByActorId.TryGetValue(actorId, out var activeBinding) &&
                activeBinding.IsValid)
            {
                binding = activeBinding;
                failureReason = string.Empty;
                return true;
            }

            if (_currentActivityParticipationContext is { IsValid: true, Participants: not null })
            {
                for (int index = 0; index < _currentActivityParticipationContext.Participants.Count; index++)
                {
                    var candidate = _currentActivityParticipationContext.Participants[index];
                    if (!candidate.IsValid || !candidate.RequiresPlayerActor || !candidate.ActorId.IsValid)
                    {
                        continue;
                    }

                    if (candidate.ActorId == actorId)
                    {
                        binding = candidate;
                        failureReason = string.Empty;
                        return true;
                    }
                }
            }

            failureReason = "activity_participant_binding_missing_for_actor_id";
            return false;
        }

        public void StoreActorInventoryFeedResult(
            ActorInventoryFeedResult result,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _currentActorInventoryFeedResult = result;

        }

        public ActorInventoryFeedResult GetActorInventoryFeedForExit(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            if (_currentActorInventoryFeedResult.IsValid &&
                IsSameActivityCycle(_currentActorInventoryFeedResult.Identity, identity))
            {
                return _currentActorInventoryFeedResult;
            }

            throw new InvalidOperationException(
                $"[FATAL][ActivityActorExitRuntimeState][ActorInventoryFeed] Missing current entry ActorInventoryFeedResult activityId='{Normalize(identity.ActivityId)}' entrySequence='{identity.EntrySequence}' source='{Normalize(source)}' reason='{Normalize(reason)}'.");
        }

        public void ClearActorInventoryFeedResult(string activityId, int entrySequence, string source, string reason)
        {

            _currentActorInventoryFeedResult = default;
        }

        public void ClearAll(string activityId, int entrySequence, string source, string reason)
        {

            _activeActorPresentationByActorInstanceId.Clear();
            _activeActorAttributeCapabilitiesByActorInstanceId.Clear();
            _activeActorParticipationsByActorInstanceRuntimeId.Clear();
            _activePlayerParticipantBindingsByActorId.Clear();
            _currentActivityParticipationContext = null;
            _currentActorInventoryFeedResult = default;
        }

        private static bool IsSameActivityCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                   right.IsValid &&
                   string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal) &&
                   string.Equals(left.ActivityId, right.ActivityId, StringComparison.Ordinal) &&
                   left.EntrySequence == right.EntrySequence;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
