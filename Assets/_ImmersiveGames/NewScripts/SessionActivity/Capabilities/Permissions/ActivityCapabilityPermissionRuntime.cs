using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public sealed class ActivityCapabilityPermissionRuntime : IActivityCapabilityPermissionRuntime
    {
        private readonly Dictionary<string, IActivityCapabilityPermissionReceiver> _receivers = new(StringComparer.Ordinal);
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

        public void SetActiveIdentity(string pipelineId, string sessionStateId, string activityId, int entrySequence)
        {
            _activePipelineId = Normalize(pipelineId);
            _activeSessionStateId = Normalize(sessionStateId);
            _activeActivityId = Normalize(activityId);
            _activeEntrySequence = entrySequence < 0 ? 0 : entrySequence;
        }

        public void ReplaceReceivers(IReadOnlyList<IActivityCapabilityPermissionReceiver> receivers)
        {
            _receivers.Clear();
            if (receivers == null || receivers.Count == 0)
            {
                return;
            }

            for (int index = 0; index < receivers.Count; index++)
            {
                IActivityCapabilityPermissionReceiver receiver = receivers[index];
                if (receiver == null)
                {
                    continue;
                }

                string receiverId = Normalize(receiver.ReceiverId);
                if (string.IsNullOrWhiteSpace(receiverId) || _receivers.ContainsKey(receiverId))
                {
                    continue;
                }

                _receivers.Add(receiverId, receiver);
                DebugUtility.Log(
                    typeof(ActivityCapabilityPermissionRuntime),
                    $"[OBS][ActivityCapabilityPermission] event='ActivityCapabilityPermissionReceiverRegistered' receiverId='{receiverId}'",
                    DebugUtility.Colors.Info);
            }
        }

        public ActivityCapabilityPermissionFact Publish(ActivityCapabilityPermissionCommand command)
        {
            DebugUtility.Log(
                typeof(ActivityCapabilityPermissionRuntime),
                $"[OBS][ActivityCapabilityPermission] event='ActivityCapabilityPermissionPublished' permissionId='{command.PermissionId}' state='{command.State}' receiverId='runtime.unbound' playerActorId='{command.TargetId}' playerSlotId='' pipelineId='{command.PipelineId}' sessionStateId='{command.SessionStateId}' activityId='{command.ActivityId}' entrySequence='{command.EntrySequence}' source='{command.Source}' reason='{command.Reason}'",
                DebugUtility.Colors.Info);

            if (!command.IsValid)
            {
                ActivityCapabilityPermissionFact rejectedInvalid = SetSnapshotAndReturnFact(command, "rejected_invalid_command", "Permission command is invalid.");
                LogOutcome("ActivityCapabilityPermissionRejectedStaleOrForeign", rejectedInvalid);
                return rejectedInvalid;
            }

            if (!MatchesActiveIdentity(command))
            {
                ActivityCapabilityPermissionFact rejectedStale = SetSnapshotAndReturnFact(command, "rejected_stale_or_foreign", "Permission command identity does not match active identity.");
                LogOutcome("ActivityCapabilityPermissionRejectedStaleOrForeign", rejectedStale);
                return rejectedStale;
            }

            if (RequiresReceiverForFunctionalSuccess(command) && _receivers.Count <= 0)
            {
                ActivityCapabilityPermissionFact rejectedMissingReceiver = SetSnapshotAndReturnFact(command, "rejected_missing_required_receiver", "Permission command requires at least one registered receiver.");
                LogOutcome("ActivityCapabilityPermissionRejectedMissingReceiver", rejectedMissingReceiver);
                return rejectedMissingReceiver;
            }

            PermissionKey key = PermissionKey.From(command);
            if (_bindingsByKey.TryGetValue(key, out ActivityCapabilityPermissionBinding current) &&
                current.State == command.State)
            {
                ActivityCapabilityPermissionFact idempotent = SetSnapshotAndReturnFact(command, "accepted_idempotent_noop", "Permission command is idempotent; no state change applied.");
                LogOutcome("ActivityCapabilityPermissionSkippedIdempotent", idempotent);
                return idempotent;
            }

            ActivityCapabilityPermissionBinding updatedBinding = new(
                command.PermissionId,
                command.Scope,
                command.State,
                command.PipelineId,
                command.SessionStateId,
                command.ActivityId,
                command.EntrySequence,
                receiverId: "runtime.unbound",
                command.TargetId);

            _bindingsByKey[key] = updatedBinding;

            ActivityCapabilityPermissionFact fact = SetSnapshotAndReturnFact(command, "accepted_state_changed", "Permission state updated.");
            LogOutcome("ActivityCapabilityPermissionApplied", fact);
            NotifyReceivers(fact);
            return fact;
        }

        private void NotifyReceivers(ActivityCapabilityPermissionFact fact)
        {
            foreach (KeyValuePair<string, IActivityCapabilityPermissionReceiver> pair in _receivers)
            {
                IActivityCapabilityPermissionReceiver receiver = pair.Value;
                if (receiver == null)
                {
                    continue;
                }

                DebugUtility.Log(
                    typeof(ActivityCapabilityPermissionRuntime),
                    $"[OBS][ActivityCapabilityPermission] event='ActivityCapabilityPermissionReceiverNotified' permissionId='{fact.Command.PermissionId}' state='{fact.Command.State}' outcome='{fact.Outcome}' receiverId='{pair.Key}' playerActorId='{fact.Command.TargetId}' playerSlotId='' pipelineId='{fact.Command.PipelineId}' sessionStateId='{fact.Command.SessionStateId}' activityId='{fact.Command.ActivityId}' entrySequence='{fact.Command.EntrySequence}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                    DebugUtility.Colors.Info);
                receiver.OnPermissionChanged(fact);
            }
        }

        private static void LogOutcome(string eventName, ActivityCapabilityPermissionFact fact)
        {
            DebugUtility.Log(
                typeof(ActivityCapabilityPermissionRuntime),
                $"[OBS][ActivityCapabilityPermission] event='{eventName}' permissionId='{fact.Command.PermissionId}' state='{fact.Command.State}' outcome='{fact.Outcome}' receiverId='runtime.unbound' playerActorId='{fact.Command.TargetId}' playerSlotId='' pipelineId='{fact.Command.PipelineId}' sessionStateId='{fact.Command.SessionStateId}' activityId='{fact.Command.ActivityId}' entrySequence='{fact.Command.EntrySequence}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                DebugUtility.Colors.Info);
        }

        private ActivityCapabilityPermissionFact SetSnapshotAndReturnFact(
            ActivityCapabilityPermissionCommand command,
            string outcome,
            string message)
        {
            ActivityCapabilityPermissionFact fact = new(command, outcome, message);
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool RequiresReceiverForFunctionalSuccess(ActivityCapabilityPermissionCommand command)
        {
            return command.State == ActivityCapabilityPermissionState.Allowed &&
                   command.PermissionId == ActivityCapabilityPermissionId.ActivityGameplayControl;
        }

        private readonly struct PermissionKey : IEquatable<PermissionKey>
        {
            private PermissionKey(
                ActivityCapabilityPermissionId permissionId,
                ActivityCapabilityPermissionScope scope,
                string pipelineId,
                string sessionStateId,
                string activityId,
                int entrySequence,
                string targetId)
            {
                PermissionId = permissionId;
                Scope = scope;
                PipelineId = pipelineId;
                SessionStateId = sessionStateId;
                ActivityId = activityId;
                EntrySequence = entrySequence;
                TargetId = targetId;
            }

            private ActivityCapabilityPermissionId PermissionId { get; }
            private ActivityCapabilityPermissionScope Scope { get; }
            private string PipelineId { get; }
            private string SessionStateId { get; }
            private string ActivityId { get; }
            private int EntrySequence { get; }
            private string TargetId { get; }

            public static PermissionKey From(ActivityCapabilityPermissionCommand command)
            {
                return new PermissionKey(
                    command.PermissionId,
                    command.Scope,
                    command.PipelineId,
                    command.SessionStateId,
                    command.ActivityId,
                    command.EntrySequence,
                    command.TargetId);
            }

            public bool Equals(PermissionKey other)
            {
                return PermissionId == other.PermissionId &&
                       Scope == other.Scope &&
                       string.Equals(PipelineId, other.PipelineId, StringComparison.Ordinal) &&
                       string.Equals(SessionStateId, other.SessionStateId, StringComparison.Ordinal) &&
                       string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal) &&
                       EntrySequence == other.EntrySequence &&
                       string.Equals(TargetId, other.TargetId, StringComparison.Ordinal);
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
                    PipelineId ?? string.Empty,
                    SessionStateId ?? string.Empty,
                    ActivityId ?? string.Empty,
                    EntrySequence,
                    TargetId ?? string.Empty);
            }
        }
    }
}
