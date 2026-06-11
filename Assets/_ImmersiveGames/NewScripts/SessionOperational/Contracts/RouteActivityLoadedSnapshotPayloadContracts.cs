using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public readonly struct LoadedRouteActivitySnapshotPayload
    {
        public LoadedRouteActivitySnapshotPayload(
            string schemaId,
            ActivityCapabilitySnapshotEnvelope capabilitySnapshotEnvelope)
        {
            SchemaId = Normalize(schemaId);
            CapabilitySnapshotEnvelope = capabilitySnapshotEnvelope;
        }

        public string SchemaId { get; }
        public ActivityCapabilitySnapshotEnvelope CapabilitySnapshotEnvelope { get; }
        public string PipelineId => CapabilitySnapshotEnvelope.PipelineId;
        public string SessionStateId => CapabilitySnapshotEnvelope.SessionStateId;
        public string ActivityId => CapabilitySnapshotEnvelope.ActivityId;
        public int ActivityOrdinal => CapabilitySnapshotEnvelope.ActivityOrdinal;
        public int SourceEntrySequence => CapabilitySnapshotEnvelope.EntrySequence;
        public int RecordCount => CapabilitySnapshotEnvelope.Records?.Count ?? 0;
        public string PayloadKind => "CapabilitySnapshotEnvelope";

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SchemaId) &&
            CapabilitySnapshotEnvelope.IsValid &&
            string.Equals(SessionStateId, CapabilitySnapshotEnvelope.SessionStateId, System.StringComparison.Ordinal) &&
            string.Equals(ActivityId, CapabilitySnapshotEnvelope.ActivityId, System.StringComparison.Ordinal) &&
            SourceEntrySequence > 0;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IRouteActivityLoadedSnapshotPayloadProvider
    {
        bool TryGetPendingLoadedSnapshotPayload(
            string activityIdentity,
            out LoadedRouteActivitySnapshotPayload payload,
            out string failureReason);
    }
}
