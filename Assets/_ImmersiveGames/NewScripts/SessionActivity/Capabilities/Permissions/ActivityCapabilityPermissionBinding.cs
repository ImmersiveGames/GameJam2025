using System;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityCapabilityPermissionBinding
    {
        public ActivityCapabilityPermissionBinding(
            ActivityCapabilityPermissionId permissionId,
            ActivityCapabilityPermissionScope scope,
            ActivityCapabilityPermissionState state,
            string pipelineId,
            string sessionStateId,
            string activityId,
            int entrySequence,
            string receiverId,
            string targetId)
        {
            PermissionId = permissionId;
            Scope = scope;
            State = state;
            PipelineId = Normalize(pipelineId);
            SessionStateId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            ReceiverId = Normalize(receiverId);
            TargetId = Normalize(targetId);
        }

        public ActivityCapabilityPermissionId PermissionId { get; }
        public ActivityCapabilityPermissionScope Scope { get; }
        public ActivityCapabilityPermissionState State { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public string ReceiverId { get; }
        public string TargetId { get; }

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
            !string.IsNullOrWhiteSpace(ReceiverId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
