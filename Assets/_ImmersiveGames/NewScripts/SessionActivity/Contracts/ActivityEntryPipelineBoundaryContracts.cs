namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityEntryStageBoundaryKind
    {
        Unknown = 0,
        ContentLoad = 1,
        SetupInventory = 2,
        ParticipantBinding = 3,
        PlayerReadiness = 4,
        PlayerInputBinding = 5,
        MovementBinding = 6,
        CameraBinding = 7,
        ActivitySetupComplete = 8,
        ActivityRunning = 9,
        Deactivation = 10,
    }

    public readonly struct ActivityEntryPipelineBoundaryContext
    {
        public ActivityEntryPipelineBoundaryContext(
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            string source,
            string reason)
        {
            Identity = identity;
            Definition = definition;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityDefinition Definition { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Definition.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActivityEntryStageBoundary
    {
        ActivityEntryStageBoundaryKind BoundaryKind { get; }
    }

    public interface IActivityEntryPipelineBoundary
    {
        string PipelineId { get; }
        string SessionId { get; }
    }
}
