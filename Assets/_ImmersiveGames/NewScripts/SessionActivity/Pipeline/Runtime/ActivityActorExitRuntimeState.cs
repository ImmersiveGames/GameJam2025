using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;
using PlayerActivityParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipationContext;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal enum ActivityActorParticipationExitBindingResolutionKind
    {
        Unknown = 0,
        Resolved = 1,
        Missed = 2,
        RejectedInvalidIdentity = 3,
        RejectedInvalidActor = 4,
        RejectedForeign = 5,
        RejectedStale = 6
    }

    internal enum ActivityActorParticipationExitBindingResolutionSourceKind
    {
        Unknown = 0,
        None = 1,
        ActiveIndex = 2,
        CurrentContext = 3
    }

    internal readonly struct ActivityActorParticipationExitBindingResolutionResult
    {
        private ActivityActorParticipationExitBindingResolutionResult(
            ActivityActorParticipationExitBindingResolutionKind kind,
            ActivityActorParticipationExitBindingResolutionSourceKind sourceKind,
            PlayerActivityParticipantBinding binding,
            string reason)
        {
            Kind = kind;
            SourceKind = sourceKind;
            Binding = binding;
            Reason = reason.TrimToEmpty();
        }

        public ActivityActorParticipationExitBindingResolutionKind Kind { get; }
        public ActivityActorParticipationExitBindingResolutionSourceKind SourceKind { get; }
        public PlayerActivityParticipantBinding Binding { get; }
        public string Reason { get; }

        public bool IsResolved => Kind == ActivityActorParticipationExitBindingResolutionKind.Resolved && Binding.IsValid;
        public bool IsRejected =>
            Kind == ActivityActorParticipationExitBindingResolutionKind.RejectedInvalidIdentity ||
            Kind == ActivityActorParticipationExitBindingResolutionKind.RejectedInvalidActor ||
            Kind == ActivityActorParticipationExitBindingResolutionKind.RejectedForeign ||
            Kind == ActivityActorParticipationExitBindingResolutionKind.RejectedStale;

        public static ActivityActorParticipationExitBindingResolutionResult Resolved(
            PlayerActivityParticipantBinding binding,
            ActivityActorParticipationExitBindingResolutionSourceKind sourceKind)
        {
            return new ActivityActorParticipationExitBindingResolutionResult(ActivityActorParticipationExitBindingResolutionKind.Resolved, sourceKind, binding, string.Empty);
        }

        public static ActivityActorParticipationExitBindingResolutionResult Missed(string reason)
        {
            return new ActivityActorParticipationExitBindingResolutionResult(ActivityActorParticipationExitBindingResolutionKind.Missed, ActivityActorParticipationExitBindingResolutionSourceKind.None, default, reason);
        }

        public static ActivityActorParticipationExitBindingResolutionResult RejectedInvalidIdentity(string reason)
        {
            return new ActivityActorParticipationExitBindingResolutionResult(ActivityActorParticipationExitBindingResolutionKind.RejectedInvalidIdentity, ActivityActorParticipationExitBindingResolutionSourceKind.None, default, reason);
        }

        public static ActivityActorParticipationExitBindingResolutionResult RejectedInvalidActor(string reason)
        {
            return new ActivityActorParticipationExitBindingResolutionResult(ActivityActorParticipationExitBindingResolutionKind.RejectedInvalidActor, ActivityActorParticipationExitBindingResolutionSourceKind.None, default, reason);
        }

        public static ActivityActorParticipationExitBindingResolutionResult RejectedForeign(string reason)
        {
            return new ActivityActorParticipationExitBindingResolutionResult(ActivityActorParticipationExitBindingResolutionKind.RejectedForeign, ActivityActorParticipationExitBindingResolutionSourceKind.None, default, reason);
        }

        public static ActivityActorParticipationExitBindingResolutionResult RejectedStale(string reason)
        {
            return new ActivityActorParticipationExitBindingResolutionResult(ActivityActorParticipationExitBindingResolutionKind.RejectedStale, ActivityActorParticipationExitBindingResolutionSourceKind.None, default, reason);
        }
    }

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
                ActorId = actorId.TrimToEmpty();
                Endpoint = endpoint;
                RuntimeHandle = runtimeHandle;
                PipelineIdentity = pipelineIdentity.TrimToEmpty();
                ActivityIdentity = activityIdentity.TrimToEmpty();
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
        private ActorInventoryFeedResult _currentActorInventoryFeedResult;

        public int ActivePresentationCount => _activeActorPresentationByActorInstanceId.Count;
        public int ActiveAttributeCount => _activeActorAttributeCapabilitiesByActorInstanceId.Count;
        public int ActiveParticipationCount => _activeActorParticipationsByActorInstanceRuntimeId.Count;
        public int ActivePlayerParticipantBindingCount => _activePlayerParticipantBindingsByActorId.Count;
        public bool HasActorInventoryFeedResult => _currentActorInventoryFeedResult.IsValid;
        public PlayerActivityParticipationContext CurrentActivityParticipationContext { get; private set; }

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
            string normalizedActorId = actorId.TrimToEmpty();
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
            CurrentActivityParticipationContext = context;
            _activePlayerParticipantBindingsByActorId.Clear();

            if (context is not { IsValid: true, Participants: { Count: > 0 } })
            {
                return;
            }

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

        public ActivityActorParticipationExitBindingResolutionResult ResolveActivePlayerParticipantBindingForExit(
            SessionActivityIdentity expectedIdentity,
            ActorInstanceRecord instance)
        {
            if (!expectedIdentity.IsValid)
            {
                return ActivityActorParticipationExitBindingResolutionResult.RejectedInvalidIdentity("expected_identity_invalid");
            }

            if (!instance.IsValid)
            {
                return ActivityActorParticipationExitBindingResolutionResult.RejectedInvalidActor("actor_instance_invalid");
            }

            ActorId actorId = new(instance.ActorId);
            if (!actorId.IsValid)
            {
                return ActivityActorParticipationExitBindingResolutionResult.RejectedInvalidActor("actor_id_missing_in_actor_participation_record");
            }

            if (CurrentActivityParticipationContext is not { IsValid: true })
            {
                string reason = _activePlayerParticipantBindingsByActorId.Count > 0
                    ? "active_binding_index_without_valid_context"
                    : "activity_participant_binding_context_missing";
                return ActivityActorParticipationExitBindingResolutionResult.RejectedStale(reason);
            }

            var contextIdentity = CurrentActivityParticipationContext.SessionActivityIdentity;
            if (!IsSameActivityCycle(contextIdentity, expectedIdentity))
            {
                if (IsSamePipelineSessionActivity(contextIdentity, expectedIdentity))
                {
                    return ActivityActorParticipationExitBindingResolutionResult.RejectedStale("activity_participant_binding_context_stale");
                }

                return ActivityActorParticipationExitBindingResolutionResult.RejectedForeign("activity_participant_binding_context_foreign");
            }

            if (_activePlayerParticipantBindingsByActorId.TryGetValue(actorId, out var activeBinding) &&
                activeBinding.IsValid)
            {
                return ActivityActorParticipationExitBindingResolutionResult.Resolved(
                    activeBinding,
                    ActivityActorParticipationExitBindingResolutionSourceKind.ActiveIndex);
            }

            if (CurrentActivityParticipationContext.Participants != null)
            {
                for (int index = 0; index < CurrentActivityParticipationContext.Participants.Count; index++)
                {
                    var candidate = CurrentActivityParticipationContext.Participants[index];
                    if (!candidate.IsValid || !candidate.RequiresPlayerActor || !candidate.ActorId.IsValid)
                    {
                        continue;
                    }

                    if (candidate.ActorId == actorId)
                    {
                        return ActivityActorParticipationExitBindingResolutionResult.Resolved(
                            candidate,
                            ActivityActorParticipationExitBindingResolutionSourceKind.CurrentContext);
                    }
                }
            }

            return ActivityActorParticipationExitBindingResolutionResult.Missed("activity_participant_binding_missing_for_actor_id");
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
                $"[FATAL][ActivityActorExitRuntimeState][ActorInventoryFeed] Missing current entry ActorInventoryFeedResult activityId='{identity.ActivityId.TrimToEmpty()}' entrySequence='{identity.EntrySequence}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
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
            CurrentActivityParticipationContext = null;
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

        private static bool IsSamePipelineSessionActivity(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) &&
                string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal) &&
                string.Equals(left.ActivityId, right.ActivityId, StringComparison.Ordinal);
        }
    }
}
