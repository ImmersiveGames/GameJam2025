using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public sealed class ActivityCapabilityPermissionRuntime : IActivityCapabilityPermissionRuntime
    {
        private readonly Dictionary<ActivityCapabilityPermissionReceiverId, IActivityCapabilityPermissionReceiver> _receivers = new();
        private readonly Dictionary<PermissionKey, ActivityCapabilityPermissionBinding> _bindingsByKey = new();
        private string _activePipelineId = string.Empty;
        private string _activeSessionStateId = string.Empty;
        private string _activeActivityId = string.Empty;
        private int _activeEntrySequence;
        private ActivityCapabilityPermissionSnapshot _snapshot =
            new(
                default,
                Array.Empty<ActivityCapabilityPermissionBinding>());

        public ActivityCapabilityPermissionSnapshot Snapshot => _snapshot;

        public void BeginPermissionScope(string pipelineId, string sessionStateId, string activityId, int entrySequence)
        {
            SetActiveIdentity(pipelineId, sessionStateId, activityId, entrySequence);
        }

        public void SetActiveIdentity(string pipelineId, string sessionStateId, string activityId, int entrySequence)
        {
            string nextPipelineId = pipelineId.TrimToEmpty();
            string nextSessionStateId = sessionStateId.TrimToEmpty();
            string nextActivityId = activityId.TrimToEmpty();
            int nextEntrySequence = entrySequence < 0 ? 0 : entrySequence;
            bool identityChanged =
                !string.Equals(_activePipelineId, nextPipelineId, StringComparison.Ordinal) ||
                !string.Equals(_activeSessionStateId, nextSessionStateId, StringComparison.Ordinal) ||
                !string.Equals(_activeActivityId, nextActivityId, StringComparison.Ordinal) ||
                _activeEntrySequence != nextEntrySequence;

            _activePipelineId = nextPipelineId;
            _activeSessionStateId = nextSessionStateId;
            _activeActivityId = nextActivityId;
            _activeEntrySequence = nextEntrySequence;

            if (identityChanged)
            {
                _bindingsByKey.Clear();
                _receivers.Clear();
            }
        }

        public void ReplaceReceivers(IReadOnlyList<ActivityCapabilityPermissionReceiverReference> receivers)
        {
            _receivers.Clear();
            if (receivers == null || receivers.Count == 0)
            {
                return;
            }

            for (int index = 0; index < receivers.Count; index++)
            {
                var reference = receivers[index];
                if (reference == null ||
                    !reference.IsValid ||
                    reference.Receiver == null)
                {
                    continue;
                }

                ActivityCapabilityPermissionReceiverId receiverId = reference.ReceiverId;
                if (!receiverId.IsValid || _receivers.ContainsKey(receiverId))
                {
                    continue;
                }

                if (!MatchesActiveIdentity(reference.Identity))
                {
                    DebugUtility.LogVerbose(
                        typeof(ActivityCapabilityPermissionRuntime),
                        $"event='ActivityCapabilityPermissionReceiverRejectedForeignIdentity' receiverId='{receiverId}' receiverIdentity='{reference.Identity}' activePipelineId='{_activePipelineId}' activeSessionStateId='{_activeSessionStateId}' activeActivityId='{_activeActivityId}' activeEntrySequence='{_activeEntrySequence}'",
                        DebugUtility.Colors.Warning);
                    continue;
                }

                _receivers.Add(receiverId, reference.Receiver);
                DebugUtility.LogVerbose(
                    typeof(ActivityCapabilityPermissionRuntime),
                    $"event='ActivityCapabilityPermissionReceiverRegistered' receiverId='{receiverId}' actorId='{reference.ActorId}' actorInstanceRuntimeId='{reference.ActorInstanceRuntimeId}' playerActorId='{reference.PlayerActorId}' playerSlotId='{reference.PlayerSlotId}'",
                    DebugUtility.Colors.Info);
            }
        }

        public ActivityCapabilityPermissionFact Publish(ActivityCapabilityPermissionCommand command)
        {
            DebugUtility.LogVerbose(
                typeof(ActivityCapabilityPermissionRuntime),
                $"event='ActivityCapabilityPermissionPublished' permissionId='{command.PermissionId}' state='{command.State}' receiverId='{ActivityCapabilityPermissionReceiverId.RuntimeUnbound}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' playerActorId='{command.PlayerActorId}' playerSlotId='{command.PlayerSlotId}' pipelineId='{command.PipelineId}' sessionStateId='{command.SessionStateId}' activityId='{command.ActivityId}' entrySequence='{command.EntrySequence}' source='{command.Source}' reason='{command.Reason}'",
                DebugUtility.Colors.Info);

            if (!command.IsValid)
            {
                var rejectedInvalid = SetSnapshotAndReturnFact(command, PermissionOutcomeKind.RejectedInvalidCommand, "rejected_invalid_command", "Permission command is invalid.");
                LogOutcome("ActivityCapabilityPermissionRejectedInvalidCommand", rejectedInvalid);
                return rejectedInvalid;
            }

            if (!MatchesActiveIdentity(command))
            {
                var rejectedStale = SetSnapshotAndReturnFact(command, PermissionOutcomeKind.RejectedStaleIdentity, "rejected_stale_or_foreign", "Permission command identity does not match active identity.");
                LogOutcome("ActivityCapabilityPermissionRejectedStaleOrForeign", rejectedStale);
                return rejectedStale;
            }

            if (RequiresReceiverForFunctionalSuccess(command) && _receivers.Count <= 0)
            {
                var rejectedMissingReceiver = SetSnapshotAndReturnFact(command, PermissionOutcomeKind.RejectedMissingRequiredReceiver, "rejected_missing_required_receiver", "Permission command requires at least one registered receiver.");
                LogOutcome("ActivityCapabilityPermissionRejectedMissingReceiver", rejectedMissingReceiver);
                return rejectedMissingReceiver;
            }

            var key = PermissionKey.From(command);
            if (_bindingsByKey.TryGetValue(key, out var current) &&
                current.State == command.State)
            {
                var idempotent = SetSnapshotAndReturnFact(command, PermissionOutcomeKind.AcceptedIdempotentNoop, "accepted_idempotent_noop", "Permission command is idempotent; no state change applied.");
                LogOutcome("ActivityCapabilityPermissionSkippedIdempotent", idempotent);
                return idempotent;
            }

            ActivityCapabilityPermissionBinding updatedBinding = new(
                command.PermissionId,
                command.Scope,
                command.State,
                receiverId: ActivityCapabilityPermissionReceiverId.RuntimeUnbound,
                command.ActorId,
                command.ActorInstanceRuntimeId,
                command.PlayerActorId,
                command.PlayerSlotId);

            _bindingsByKey[key] = updatedBinding;

            var fact = SetSnapshotAndReturnFact(command, PermissionOutcomeKind.AcceptedStateChanged, "accepted_state_changed", "Permission state updated.");
            LogOutcome("ActivityCapabilityPermissionApplied", fact);
            NotifyReceivers(fact);
            return fact;
        }

        private void NotifyReceivers(ActivityCapabilityPermissionFact fact)
        {
            foreach (KeyValuePair<ActivityCapabilityPermissionReceiverId, IActivityCapabilityPermissionReceiver> pair in _receivers)
            {
                var receiver = pair.Value;
                if (receiver == null)
                {
                    continue;
                }

                DebugUtility.LogVerbose(
                    typeof(ActivityCapabilityPermissionRuntime),
                    $"event='ActivityCapabilityPermissionReceiverNotified' permissionId='{fact.Command.PermissionId}' state='{fact.Command.State}' outcomeKind='{fact.OutcomeKind}' outcome='{fact.OutcomeCode}' receiverId='{pair.Key}' actorId='{fact.Command.ActorId}' actorInstanceRuntimeId='{fact.Command.ActorInstanceRuntimeId}' playerActorId='{fact.Command.PlayerActorId}' playerSlotId='{fact.Command.PlayerSlotId}' pipelineId='{fact.Command.PipelineId}' sessionStateId='{fact.Command.SessionStateId}' activityId='{fact.Command.ActivityId}' entrySequence='{fact.Command.EntrySequence}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                    DebugUtility.Colors.Info);
                receiver.OnPermissionChanged(fact);
            }
        }

        private static void LogOutcome(string eventName, ActivityCapabilityPermissionFact fact)
        {
            string message = $"event='{eventName}' permissionId='{fact.Command.PermissionId}' state='{fact.Command.State}' outcomeKind='{fact.OutcomeKind}' outcome='{fact.OutcomeCode}' receiverId='{ActivityCapabilityPermissionReceiverId.RuntimeUnbound}' actorId='{fact.Command.ActorId}' actorInstanceRuntimeId='{fact.Command.ActorInstanceRuntimeId}' playerActorId='{fact.Command.PlayerActorId}' playerSlotId='{fact.Command.PlayerSlotId}' pipelineId='{fact.Command.PipelineId}' sessionStateId='{fact.Command.SessionStateId}' activityId='{fact.Command.ActivityId}' entrySequence='{fact.Command.EntrySequence}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'";

            if (string.Equals(eventName, "ActivityCapabilityPermissionApplied", StringComparison.Ordinal))
            {
                DebugUtility.Log(
                    typeof(ActivityCapabilityPermissionRuntime),
                    message,
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogVerbose(
                typeof(ActivityCapabilityPermissionRuntime),
                message,
                DebugUtility.Colors.Info);
        }

        private ActivityCapabilityPermissionFact SetSnapshotAndReturnFact(
            ActivityCapabilityPermissionCommand command,
            PermissionOutcomeKind outcomeKind,
            string outcomeCode,
            string message)
        {
            ActivityCapabilityPermissionFact fact = new(command, outcomeKind, outcomeCode, message);
            _snapshot = new ActivityCapabilityPermissionSnapshot(
                fact,
                BuildBindingsSnapshot());
            return fact;
        }

        private bool MatchesActiveIdentity(ActivityCapabilityPermissionCommand command)
        {
            return string.Equals(command.PipelineId, _activePipelineId, StringComparison.Ordinal) &&
                   string.Equals(command.SessionStateId, _activeSessionStateId, StringComparison.Ordinal) &&
                   string.Equals(command.ActivityId, _activeActivityId, StringComparison.Ordinal) &&
                   command.EntrySequence == _activeEntrySequence;
        }

        private IReadOnlyList<ActivityCapabilityPermissionBinding> BuildBindingsSnapshot()
        {
            if (_bindingsByKey.Count == 0)
            {
                return Array.Empty<ActivityCapabilityPermissionBinding>();
            }

            List<ActivityCapabilityPermissionBinding> list = new(_bindingsByKey.Count);
            foreach (KeyValuePair<PermissionKey, ActivityCapabilityPermissionBinding> pair in _bindingsByKey)
            {
                list.Add(pair.Value);
            }

            return list;
        }
private static bool RequiresReceiverForFunctionalSuccess(ActivityCapabilityPermissionCommand command)
        {
            return command is { State: ActivityCapabilityPermissionState.Allowed, PermissionId: ActivityCapabilityPermissionId.ActivityGameplayControl };
        }

        private bool MatchesActiveIdentity(ActivityCapabilityPermissionReceiverIdentity identity)
        {
            return identity.IsValid &&
                   string.Equals(identity.PipelineId, _activePipelineId, StringComparison.Ordinal) &&
                   string.Equals(identity.SessionStateId, _activeSessionStateId, StringComparison.Ordinal) &&
                   string.Equals(identity.ActivityId, _activeActivityId, StringComparison.Ordinal) &&
                   identity.EntrySequence == _activeEntrySequence;
        }

        private readonly struct PermissionKey : IEquatable<PermissionKey>
        {
            private PermissionKey(
                ActivityCapabilityPermissionId permissionId,
                ActivityCapabilityPermissionScope scope,
                ActorInstanceRuntimeId actorInstanceRuntimeId)
            {
                PermissionId = permissionId;
                Scope = scope;
                ActorInstanceRuntimeId = actorInstanceRuntimeId;
            }

            private ActivityCapabilityPermissionId PermissionId { get; }
            private ActivityCapabilityPermissionScope Scope { get; }
            private ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }

            public static PermissionKey From(ActivityCapabilityPermissionCommand command)
            {
                return new PermissionKey(
                    command.PermissionId,
                    command.Scope,
                    command.ActorInstanceRuntimeId);
            }

            public bool Equals(PermissionKey other)
            {
                return PermissionId == other.PermissionId &&
                       Scope == other.Scope &&
                       ActorInstanceRuntimeId == other.ActorInstanceRuntimeId;
            }

            public override bool Equals(object obj)
            {
                return obj is PermissionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    PermissionId,
                    Scope,
                    ActorInstanceRuntimeId);
            }
        }
    }
}
