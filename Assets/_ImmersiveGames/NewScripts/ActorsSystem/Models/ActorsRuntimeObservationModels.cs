using System;
namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public readonly struct RuntimeActorObservationRecord : IEquatable<RuntimeActorObservationRecord>
    {
        public RuntimeActorObservationRecord(
            RuntimeActorId runtimeActorId,
            string displayName,
            ActorRole observedRole,
            ActorOperationalRecipeKind observedRecipeKind,
            bool isActive)
        {
            RuntimeActorId = runtimeActorId;
            DisplayName = Normalize(displayName);
            ObservedRole = observedRole;
            ObservedRecipeKind = observedRecipeKind;
            IsActive = isActive;
        }

        public RuntimeActorId RuntimeActorId { get; }
        public string DisplayName { get; }
        public ActorRole ObservedRole { get; }
        public ActorOperationalRecipeKind ObservedRecipeKind { get; }
        public bool IsActive { get; }

        public bool IsValid => RuntimeActorId.IsValid;

        public bool Equals(RuntimeActorObservationRecord other)
        {
            return RuntimeActorId.Equals(other.RuntimeActorId) &&
                   string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal) &&
                   ObservedRole == other.ObservedRole &&
                   ObservedRecipeKind == other.ObservedRecipeKind &&
                   IsActive == other.IsActive;
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeActorObservationRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)ObservedRole;
                hashCode = (hashCode * 397) ^ (int)ObservedRecipeKind;
                hashCode = (hashCode * 397) ^ IsActive.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"runtimeActorId='{RuntimeActorId}', displayName='{AsText(DisplayName)}', observedRole='{ObservedRole}', observedRecipeKind='{ObservedRecipeKind}', isActive='{IsActive}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
        }
    }

    public readonly struct ActorsRuntimeObservationSnapshot : IEquatable<ActorsRuntimeObservationSnapshot>
    {
        public ActorsRuntimeObservationSnapshot(string observationSignature, RuntimeActorObservationRecord[] runtimeActors)
        {
            ObservationSignature = Normalize(observationSignature);
            RuntimeActors = runtimeActors == null ? Array.Empty<RuntimeActorObservationRecord>() : (RuntimeActorObservationRecord[])runtimeActors.Clone();
        }

        public string ObservationSignature { get; }
        public RuntimeActorObservationRecord[] RuntimeActors { get; }

        public int Count => RuntimeActors?.Length ?? 0;
        public bool HasRuntimeActors => Count > 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(ObservationSignature);

        public static ActorsRuntimeObservationSnapshot Empty => new(string.Empty, Array.Empty<RuntimeActorObservationRecord>());

        public bool Equals(ActorsRuntimeObservationSnapshot other)
        {
            return string.Equals(ObservationSignature, other.ObservationSignature, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsRuntimeObservationSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(ObservationSignature ?? string.Empty) * 397) ^ Count;
            }
        }

        public override string ToString()
        {
            return $"observationSignature='{AsText(ObservationSignature)}', count='{Count}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
        }
    }
}
