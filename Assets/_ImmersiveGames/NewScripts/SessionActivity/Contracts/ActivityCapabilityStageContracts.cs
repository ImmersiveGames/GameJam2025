using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityCapabilityStageBoundaryKind
    {
        Unknown = 0,
        ActorPresentation = 1,
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
            CapabilityId = capabilityId.TrimToEmpty();
            ActorId = actorId.TrimToEmpty();
            ActorKind = actorKind.TrimToEmpty();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
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
}

    public interface IActivityCapabilityStageBoundary
    {
        ActivityCapabilityStageBoundaryKind BoundaryKind { get; }
    }
}
