using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;
using PlayerActivityParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipationContext;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityActorExitRuntimeState
    {
        private readonly Dictionary<ActorInstanceId, SessionActivityPipeline.ActorPresentationCapabilityState> _activeActorPresentationByActorInstanceId = new();
        private readonly Dictionary<ActorInstanceId, SessionActivityPipeline.ActorAttributeCapabilityState> _activeActorAttributeCapabilitiesByActorInstanceId = new();
        private readonly HashSet<ActorInstanceId> _activeActorParticipationsByActorInstanceId = new();
        private readonly Dictionary<ActorId, PlayerActivityParticipantBinding> _activePlayerParticipantBindingsByActorId = new();
        private PlayerActivityParticipationContext _currentActivityParticipationContext;
        private ActorInventoryFeedResult _currentActorInventoryFeedResult;

        public int ActivePresentationCount => _activeActorPresentationByActorInstanceId.Count;
        public int ActiveAttributeCount => _activeActorAttributeCapabilitiesByActorInstanceId.Count;
        public int ActiveParticipationCount => _activeActorParticipationsByActorInstanceId.Count;
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
            ActorPresentationEndpointReference presentationReference,
            out ActorPresentationRuntimeHandle handle)
        {
            handle = default;
            if (presentationReference == null || !presentationReference.IsValid)
            {
                return false;
            }

            if (!_activeActorPresentationByActorInstanceId.TryGetValue(presentationReference.ActorInstanceRuntimeId, out SessionActivityPipeline.ActorPresentationCapabilityState state) || !state.IsValid)
            {
                return false;
            }

            handle = state.RuntimeHandle;
            return handle.IsValid;
        }

        public bool TryGetActivePresentationState(
            ActorInstanceId actorInstanceRuntimeId,
            out SessionActivityPipeline.ActorPresentationCapabilityState state)
        {
            state = default;
            return actorInstanceRuntimeId.IsValid &&
                   _activeActorPresentationByActorInstanceId.TryGetValue(actorInstanceRuntimeId, out state) &&
                   state.IsValid;
        }

        public void StoreActiveActorPresentation(
            SessionActivityPipeline.ActorPresentationCapabilityState state,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!state.IsValid)
            {
                return;
            }

            bool hadBefore = _activeActorPresentationByActorInstanceId.ContainsKey(state.ActorInstanceRuntimeId);
            _activeActorPresentationByActorInstanceId[state.ActorInstanceRuntimeId] = state;
            Log(
                "ActivityActorExitRuntimeStatePresentationStateStored",
                activityId,
                entrySequence,
                source,
                reason,
                $"actorId='{Normalize(state.ActorId)}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' hadBefore='{hadBefore.ToString().ToLowerInvariant()}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}'");
        }

        public IReadOnlyList<SessionActivityPipeline.ActorPresentationCapabilityState> ResolveActiveActorPresentationStates(ActorInstanceId targetActorInstanceRuntimeId)
        {
            List<SessionActivityPipeline.ActorPresentationCapabilityState> activeStates = new();
            if (targetActorInstanceRuntimeId.IsValid)
            {
                if (_activeActorPresentationByActorInstanceId.TryGetValue(targetActorInstanceRuntimeId, out SessionActivityPipeline.ActorPresentationCapabilityState targetedState) &&
                    targetedState.IsValid)
                {
                    activeStates.Add(targetedState);
                }

                return activeStates;
            }

            foreach (SessionActivityPipeline.ActorPresentationCapabilityState state in _activeActorPresentationByActorInstanceId.Values)
            {
                if (state.IsValid)
                {
                    activeStates.Add(state);
                }
            }

            return activeStates;
        }

        public void RemoveActiveActorPresentation(
            ActorInstanceId actorInstanceRuntimeId,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            bool removed = _activeActorPresentationByActorInstanceId.Remove(actorInstanceRuntimeId);
            Log(
                "ActivityActorExitRuntimeStatePresentationStateRemoved",
                activityId,
                entrySequence,
                source,
                reason,
                $"actorInstanceRuntimeId='{actorInstanceRuntimeId}' removed='{removed.ToString().ToLowerInvariant()}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}'");
        }

        public bool TryGetActiveActorAttributeCapability(
            ActorInstanceId actorInstanceRuntimeId,
            out SessionActivityPipeline.ActorAttributeCapabilityState state)
        {
            state = default;
            return actorInstanceRuntimeId.IsValid &&
                   _activeActorAttributeCapabilitiesByActorInstanceId.TryGetValue(actorInstanceRuntimeId, out state) &&
                   state.IsValid;
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

            bool hadBefore = _activeActorAttributeCapabilitiesByActorInstanceId.ContainsKey(state.ActorInstanceRuntimeId);
            _activeActorAttributeCapabilitiesByActorInstanceId[state.ActorInstanceRuntimeId] = state;
            Log(
                "ActivityActorExitRuntimeStateAttributeStateStored",
                activityId,
                entrySequence,
                source,
                reason,
                $"actorId='{Normalize(state.ActorId)}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' hadBefore='{hadBefore.ToString().ToLowerInvariant()}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}'");
        }

        public IReadOnlyList<SessionActivityPipeline.ActorAttributeCapabilityState> ResolveActiveActorAttributeStates()
        {
            List<SessionActivityPipeline.ActorAttributeCapabilityState> activeStates = new();
            foreach (SessionActivityPipeline.ActorAttributeCapabilityState state in _activeActorAttributeCapabilitiesByActorInstanceId.Values)
            {
                if (state.IsValid)
                {
                    activeStates.Add(state);
                }
            }

            return activeStates;
        }

        public void RemoveActiveActorAttributeCapability(
            ActorInstanceId actorInstanceRuntimeId,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            bool removed = _activeActorAttributeCapabilitiesByActorInstanceId.Remove(actorInstanceRuntimeId);
            Log(
                "ActivityActorExitRuntimeStateAttributeStateRemoved",
                activityId,
                entrySequence,
                source,
                reason,
                $"actorInstanceRuntimeId='{actorInstanceRuntimeId}' removed='{removed.ToString().ToLowerInvariant()}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}'");
        }

        public void StoreActiveActorParticipation(
            ActorInstanceId actorInstanceRuntimeId,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            bool added = _activeActorParticipationsByActorInstanceId.Add(actorInstanceRuntimeId);
            Log(
                "ActivityActorExitRuntimeStateParticipationRecordStored",
                activityId,
                entrySequence,
                source,
                reason,
                $"actorInstanceRuntimeId='{actorInstanceRuntimeId}' added='{added.ToString().ToLowerInvariant()}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}'");
        }

        public ActorParticipationExitResult ExecuteActorParticipationExit(ActorParticipationExitCommand command)
        {
            ActorParticipationExitStageExecutor exitExecutor = new();
            return exitExecutor.Execute(command, _activeActorParticipationsByActorInstanceId);
        }

        public void RemoveActiveActorParticipation(
            ActorInstanceId actorInstanceRuntimeId,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            bool removed = _activeActorParticipationsByActorInstanceId.Remove(actorInstanceRuntimeId);
            Log(
                "ActivityActorExitRuntimeStateParticipationRecordRemoved",
                activityId,
                entrySequence,
                source,
                reason,
                $"actorInstanceRuntimeId='{actorInstanceRuntimeId}' removed='{removed.ToString().ToLowerInvariant()}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}'");
        }

        public void ClearActiveActorParticipations(string activityId, int entrySequence, string source, string reason)
        {
            int before = _activeActorParticipationsByActorInstanceId.Count;
            _activeActorParticipationsByActorInstanceId.Clear();
            Log(
                "ActivityActorExitRuntimeStateParticipationRecordsCleared",
                activityId,
                entrySequence,
                source,
                reason,
                $"cleared='{before}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}'");
        }


        public void StoreActivityParticipationContext(PlayerActivityParticipationContext context)
        {
            _currentActivityParticipationContext = context;

            string activityId = context?.SessionActivityIdentity.ActivityId ?? string.Empty;
            int entrySequence = context?.SessionActivityIdentity.EntrySequence ?? 0;
            string source = context?.Source ?? string.Empty;
            string reason = context?.Reason ?? string.Empty;

            int participantCount = 0;
            int storedPlayerExitBindingCount = 0;
            bool hasExplicitParticipants = context != null &&
                                           context.IsValid &&
                                           context.Participants != null &&
                                           context.Participants.Count > 0;

            if (hasExplicitParticipants)
            {
                _activePlayerParticipantBindingsByActorId.Clear();
                participantCount = context.Participants.Count;

                for (int index = 0; index < context.Participants.Count; index++)
                {
                    PlayerActivityParticipantBinding binding = context.Participants[index];
                    if (!binding.IsValid || !binding.RequiresPlayerActor || !binding.ActorId.IsValid)
                    {
                        continue;
                    }

                    _activePlayerParticipantBindingsByActorId[binding.ActorId] = binding;
                    storedPlayerExitBindingCount++;
                }
            }
            else if (context != null && context.IsValid && context.Participants != null)
            {
                participantCount = context.Participants.Count;
            }

            string correlationMode = hasExplicitParticipants
                ? "replace_from_current_activity_participation_context"
                : "keep_existing_bindings_for_exit_lookup";

            Log(
                "ActivityActorExitRuntimeStateActivityParticipationContextStored",
                activityId,
                entrySequence,
                source,
                reason,
                $"participantCount='{participantCount}' storedPlayerExitBindingCount='{storedPlayerExitBindingCount}' playerExitBindingCount='{_activePlayerParticipantBindingsByActorId.Count}' correlationMode='{correlationMode}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}'");
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

            if (_activePlayerParticipantBindingsByActorId.TryGetValue(actorId, out PlayerActivityParticipantBinding activeBinding) &&
                activeBinding.IsValid)
            {
                binding = activeBinding;
                failureReason = string.Empty;
                Log(
                    "ActivityActorExitRuntimeStatePlayerExitContextResolved",
                    instance.Identity.ActivityId,
                    instance.Identity.EntrySequence,
                    "ActivityActorExitRuntimeState",
                    "active_binding_by_actor_id",
                    $"actorId='{Normalize(instance.ActorId)}' actorInstanceRuntimeId='{instance.ActorInstanceId}' playerExitBindingCount='{_activePlayerParticipantBindingsByActorId.Count}' resolution='active_binding'");
                return true;
            }

            if (_currentActivityParticipationContext != null &&
                _currentActivityParticipationContext.IsValid &&
                _currentActivityParticipationContext.Participants != null)
            {
                for (int index = 0; index < _currentActivityParticipationContext.Participants.Count; index++)
                {
                    PlayerActivityParticipantBinding candidate = _currentActivityParticipationContext.Participants[index];
                    if (!candidate.IsValid || !candidate.RequiresPlayerActor || !candidate.ActorId.IsValid)
                    {
                        continue;
                    }

                    if (candidate.ActorId == actorId)
                    {
                        binding = candidate;
                        failureReason = string.Empty;
                        Log(
                            "ActivityActorExitRuntimeStatePlayerExitContextResolved",
                            instance.Identity.ActivityId,
                            instance.Identity.EntrySequence,
                            "ActivityActorExitRuntimeState",
                            "participation_context_scan",
                            $"actorId='{Normalize(instance.ActorId)}' actorInstanceRuntimeId='{instance.ActorInstanceId}' playerExitBindingCount='{_activePlayerParticipantBindingsByActorId.Count}' resolution='context_scan'");
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
            int actorInstanceCount = result.ActorInstances?.Count ?? 0;
            int actorEntryCount = result.ActorEntries?.Count ?? 0;
            int actorParticipationCount = result.ActorParticipations?.Count ?? 0;
            Log(
                "ActivityActorExitRuntimeStateInventoryFeedStored",
                activityId,
                entrySequence,
                source,
                reason,
                $"actorInstanceCount='{actorInstanceCount}' actorEntryCount='{actorEntryCount}' actorParticipationCount='{actorParticipationCount}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}'");
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
            bool hadBefore = _currentActorInventoryFeedResult.IsValid;
            _currentActorInventoryFeedResult = default;
            Log(
                "ActivityActorExitRuntimeStateInventoryFeedCleared",
                activityId,
                entrySequence,
                source,
                reason,
                $"hadBefore='{hadBefore.ToString().ToLowerInvariant()}' presentationStateCount='{_activeActorPresentationByActorInstanceId.Count}' attributeStateCount='{_activeActorAttributeCapabilitiesByActorInstanceId.Count}' participationRecordCount='{_activeActorParticipationsByActorInstanceId.Count}' playerExitBindingCount='{_activePlayerParticipantBindingsByActorId.Count}'");
        }


        public void ClearAll(string activityId, int entrySequence, string source, string reason)
        {
            int presentationBefore = _activeActorPresentationByActorInstanceId.Count;
            int attributeBefore = _activeActorAttributeCapabilitiesByActorInstanceId.Count;
            int participationBefore = _activeActorParticipationsByActorInstanceId.Count;
            int playerBindingBefore = _activePlayerParticipantBindingsByActorId.Count;
            bool hadInventoryFeed = _currentActorInventoryFeedResult.IsValid;
            _activeActorPresentationByActorInstanceId.Clear();
            _activeActorAttributeCapabilitiesByActorInstanceId.Clear();
            _activeActorParticipationsByActorInstanceId.Clear();
            _activePlayerParticipantBindingsByActorId.Clear();
            _currentActivityParticipationContext = null;
            _currentActorInventoryFeedResult = default;
            Log(
                "ActivityActorExitRuntimeStateCleared",
                activityId,
                entrySequence,
                source,
                reason,
                $"presentationCleared='{presentationBefore}' attributeCleared='{attributeBefore}' participationCleared='{participationBefore}' playerBindingCleared='{playerBindingBefore}' inventoryFeedCleared='{hadInventoryFeed.ToString().ToLowerInvariant()}' presentationStateCount='0' attributeStateCount='0' participationRecordCount='0' playerExitBindingCount='0'");
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

        private static void Log(string eventName, string activityId, int entrySequence, string source, string reason, string details)
        {
            DebugUtility.Log(
                typeof(ActivityActorExitRuntimeState),
                $"[OBS][ActivityActorExitRuntimeState] event='{Normalize(eventName)}' owner='ActivityActorExitRuntimeState' activityId='{Normalize(activityId)}' entrySequence='{Math.Max(0, entrySequence)}' source='{Normalize(source)}' reason='{Normalize(reason)}' {details}.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
