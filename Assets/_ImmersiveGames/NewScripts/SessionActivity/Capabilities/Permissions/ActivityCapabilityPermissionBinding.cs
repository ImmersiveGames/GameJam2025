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
            string receiverId,
            string targetId)
        {
            PermissionId = permissionId;
            Scope = scope;
            State = state;
            ReceiverId = Normalize(receiverId);
            TargetId = Normalize(targetId);
        }

        public ActivityCapabilityPermissionId PermissionId { get; }
        public ActivityCapabilityPermissionScope Scope { get; }
        public ActivityCapabilityPermissionState State { get; }
        public string ReceiverId { get; }
        public string TargetId { get; }

        public bool IsValid =>
            PermissionId != ActivityCapabilityPermissionId.Unknown &&
            Scope != ActivityCapabilityPermissionScope.Unknown &&
            State != ActivityCapabilityPermissionState.Unknown &&
            !string.IsNullOrWhiteSpace(ReceiverId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
