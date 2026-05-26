using System;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityInventoryId : IEquatable<ActivityCapabilityInventoryId>
    {
        public ActivityCapabilityInventoryId(
            string pipelineId,
            string sessionStateId,
            string activityId,
            int entrySequence)
        {
            PipelineId = Normalize(pipelineId);
            SessionStateId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            Signature = BuildSignature(PipelineId, SessionStateId, ActivityId, EntrySequence);
        }

        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public string Signature { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(Signature);

        public static ActivityCapabilityInventoryId FromIdentity(SessionActivityIdentity identity)
        {
            return new ActivityCapabilityInventoryId(identity.PipelineId, identity.SessionId, identity.ActivityId, identity.EntrySequence);
        }

        public bool Equals(ActivityCapabilityInventoryId other)
        {
            return string.Equals(Signature, other.Signature, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityCapabilityInventoryId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Signature ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid
                ? $"pipelineId='{PipelineId}', sessionStateId='{SessionStateId}', activityId='{ActivityId}', entrySequence='{EntrySequence}'"
                : "<none>";
        }

        public static bool operator ==(ActivityCapabilityInventoryId left, ActivityCapabilityInventoryId right) => left.Equals(right);
        public static bool operator !=(ActivityCapabilityInventoryId left, ActivityCapabilityInventoryId right) => !left.Equals(right);

        public static string DeriveOwnerId(ActivityCapabilityInventoryId inventoryId, ActivityCapabilityOwnerKind ownerKind, string ownerPath, string ownerSource)
        {
            return $"{Normalize(inventoryId.Signature)}|ownerKind={ownerKind}|ownerPath={Normalize(ownerPath)}|ownerSource={Normalize(ownerSource)}";
        }

        public static string DeriveCapabilityId(
            ActivityCapabilityInventoryId inventoryId,
            string ownerId,
            ActivityCapabilityKind capabilityKind,
            string moduleId,
            string componentPath)
        {
            return $"{Normalize(inventoryId.Signature)}|ownerId={Normalize(ownerId)}|capabilityKind={capabilityKind}|moduleId={Normalize(moduleId)}|componentPath={Normalize(componentPath)}";
        }

        private static string BuildSignature(string pipelineId, string sessionStateId, string activityId, int entrySequence)
        {
            return $"{pipelineId}|{sessionStateId}|{activityId}|{entrySequence}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
