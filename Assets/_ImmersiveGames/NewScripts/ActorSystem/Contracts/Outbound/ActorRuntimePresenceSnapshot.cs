using System;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;

namespace _ImmersiveGames.NewScripts.ActorSystem.Contracts.Outbound
{
    /// <summary>
    /// Read-only runtime presence projection for actor discovery.
    /// </summary>
    public readonly struct ActorRuntimePresenceSnapshot : IEquatable<ActorRuntimePresenceSnapshot>
    {
        public ActorRuntimePresenceSnapshot(string actorId, string displayName, ActorKind kind, bool isActive)
        {
            ActorId = Normalize(actorId);
            DisplayName = Normalize(displayName);
            Kind = kind;
            IsActive = isActive;
        }

        public string ActorId { get; }
        public string DisplayName { get; }
        public ActorKind Kind { get; }
        public bool IsActive { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ActorId);

        public bool Equals(ActorRuntimePresenceSnapshot other)
        {
            return string.Equals(ActorId, other.ActorId, StringComparison.Ordinal) &&
                   string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal) &&
                   Kind == other.Kind &&
                   IsActive == other.IsActive;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorRuntimePresenceSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ActorId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)Kind;
                hashCode = (hashCode * 397) ^ IsActive.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            string displayName = string.IsNullOrWhiteSpace(DisplayName) ? "<none>" : DisplayName;
            return $"actorId='{ActorId}', displayName='{displayName}', kind='{Kind}', isActive='{IsActive}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}