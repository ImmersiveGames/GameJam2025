namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityCapabilityStageBoundaryKind
    {
        Unknown = 0,
        ActorPresentation = 1,
        NonPlayerActor = 2,
        ActorAttributes = 3,
        ActivityObject = 4,
        PlayerActor = 5,
    }

    public readonly struct ActivityCapabilityStageBoundaryContext
    {
        public ActivityCapabilityStageBoundaryContext(
            SessionActivityIdentity identity,
            string capabilityId,
            string actorId,
            string actorKind,
            string source,
            string reason)
        {
            Identity = identity;
            CapabilityId = Normalize(capabilityId);
            ActorId = Normalize(actorId);
            ActorKind = Normalize(actorKind);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string CapabilityId { get; }
        public string ActorId { get; }
        public string ActorKind { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(CapabilityId) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActivityCapabilityStageBoundary
    {
        ActivityCapabilityStageBoundaryKind BoundaryKind { get; }
    }
}
