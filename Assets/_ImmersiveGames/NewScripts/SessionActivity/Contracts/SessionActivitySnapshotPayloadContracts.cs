namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct SessionActivitySnapshotPayload
    {
        public SessionActivitySnapshotPayload(
            string schemaId,
            string pipelineId,
            string sessionStateId,
            string activityId,
            int activityOrdinal,
            int entrySequence,
            ActivityCapabilitySnapshotEnvelope capabilitySnapshotEnvelope)
        {
            SchemaId = Normalize(schemaId);
            PipelineId = Normalize(pipelineId);
            SessionStateId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            CapabilitySnapshotEnvelope = capabilitySnapshotEnvelope;
        }

        public string SchemaId { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public ActivityCapabilitySnapshotEnvelope CapabilitySnapshotEnvelope { get; }
        public bool HasCapabilitySnapshotEnvelope => CapabilitySnapshotEnvelope.IsValid;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SchemaId) &&
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            HasCapabilitySnapshotEnvelope;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionActivitySnapshotPayloadProvider
    {
        bool TryGetSnapshotPayloadForSaveOnExit(
            string sessionStateId,
            out SessionActivitySnapshotPayload payload,
            out string failureReason);
    }
}
