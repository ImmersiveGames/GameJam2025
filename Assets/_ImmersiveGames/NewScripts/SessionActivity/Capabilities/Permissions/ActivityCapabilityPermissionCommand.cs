using System;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityCapabilityPermissionCommand
    {
        public ActivityCapabilityPermissionCommand(
            ActivityCapabilityPermissionId permissionId,
            ActivityCapabilityPermissionScope scope,
            ActivityCapabilityPermissionState state,
            string pipelineId,
            string sessionStateId,
            string activityId,
            int entrySequence,
            string targetId,
            string source,
            string reason)
        {
            PermissionId = permissionId;
            Scope = scope;
            State = state;
            PipelineId = Normalize(pipelineId);
            SessionStateId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            TargetId = Normalize(targetId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActivityCapabilityPermissionId PermissionId { get; }
        public ActivityCapabilityPermissionScope Scope { get; }
        public ActivityCapabilityPermissionState State { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public string TargetId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasIdentity =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            EntrySequence > 0;

        public bool IsValid =>
            PermissionId != ActivityCapabilityPermissionId.Unknown &&
            Scope != ActivityCapabilityPermissionScope.Unknown &&
            State != ActivityCapabilityPermissionState.Unknown &&
            HasIdentity &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
