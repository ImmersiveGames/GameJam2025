using System;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext
{
    public readonly struct PhaseEntryIdentity : IEquatable<PhaseEntryIdentity>
    {
        public PhaseEntryIdentity(
            string phaseEntryId,
            string phaseRuntimeSignature,
            string entrySignature,
            int phaseLocalEntrySequence,
            string sessionSignature,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName,
            string source)
        {
            PhaseEntryId = Normalize(phaseEntryId);
            PhaseRuntimeSignature = Normalize(phaseRuntimeSignature);
            EntrySignature = Normalize(entrySignature);
            PhaseLocalEntrySequence = phaseLocalEntrySequence < 0 ? 0 : phaseLocalEntrySequence;
            SessionSignature = Normalize(sessionSignature);
            RouteId = routeId;
            RouteKind = routeKind;
            SceneName = Normalize(sceneName);
            Source = Normalize(source);
        }

        public string PhaseEntryId { get; }
        public string PhaseRuntimeSignature { get; }
        public string EntrySignature { get; }
        public int PhaseLocalEntrySequence { get; }
        public string SessionSignature { get; }
        public SceneRouteId RouteId { get; }
        public SceneRouteKind RouteKind { get; }
        public string SceneName { get; }
        public string Source { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PhaseEntryId) &&
            !string.IsNullOrWhiteSpace(PhaseRuntimeSignature) &&
            !string.IsNullOrWhiteSpace(EntrySignature) &&
            !string.IsNullOrWhiteSpace(SessionSignature) &&
            RouteId.IsValid &&
            RouteKind != SceneRouteKind.Unspecified;

        public static PhaseEntryIdentity Empty => default;

        public bool Equals(PhaseEntryIdentity other)
        {
            return string.Equals(PhaseEntryId, other.PhaseEntryId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PhaseEntryIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(PhaseEntryId ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid ? PhaseEntryId : "<none>";
        }

        public static bool operator ==(PhaseEntryIdentity left, PhaseEntryIdentity right) => left.Equals(right);
        public static bool operator !=(PhaseEntryIdentity left, PhaseEntryIdentity right) => !left.Equals(right);

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
