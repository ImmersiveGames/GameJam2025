using System;
namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public enum ActorsOperationalBindingState
    {
        Unbound = 0,
        Bound = 1,
        Active = 2,
        Disconnected = 3
    }

    public enum ActorsOperationalBindingFlowStep
    {
        Unknown = 0,
        SemanticReady = 1,
        AxisActorResolved = 2,
        RuntimeMaterialized = 3,
        UnityOperationalBound = 4
    }

    public readonly struct ActorsUnityOperationalHandles : IEquatable<ActorsUnityOperationalHandles>
    {
        public ActorsUnityOperationalHandles(int? playerIndex, ulong? inputUserId)
        {
            PlayerIndex = NormalizePlayerIndex(playerIndex);
            InputUserId = NormalizeInputUserId(inputUserId);
        }

        public int? PlayerIndex { get; }
        public ulong? InputUserId { get; }

        public bool HasPlayerIndex => PlayerIndex.HasValue;
        public bool HasInputUserId => InputUserId.HasValue;
        public bool HasAnyHandle => HasPlayerIndex || HasInputUserId;

        public static ActorsUnityOperationalHandles None => new(null, null);

        public bool Equals(ActorsUnityOperationalHandles other)
        {
            return PlayerIndex == other.PlayerIndex && InputUserId == other.InputUserId;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsUnityOperationalHandles other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = PlayerIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ InputUserId.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            string playerIndex = HasPlayerIndex ? PlayerIndex.Value.ToString() : "<none>";
            string inputUserId = HasInputUserId ? InputUserId.Value.ToString() : "<none>";
            return $"playerIndex='{playerIndex}', inputUserId='{inputUserId}'";
        }

        private static int? NormalizePlayerIndex(int? value)
        {
            if (!value.HasValue || value.Value < 0)
            {
                return null;
            }

            return value.Value;
        }

        private static ulong? NormalizeInputUserId(ulong? value)
        {
            if (!value.HasValue || value.Value == 0UL)
            {
                return null;
            }

            return value.Value;
        }
    }

    public readonly struct ActorsOperationalBindingEntry : IEquatable<ActorsOperationalBindingEntry>
    {
        public ActorsOperationalBindingEntry(
            string participantId,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            ActorsUnityOperationalHandles unityHandles,
            ActorsOperationalBindingFlowStep flowStep,
            ActorsOperationalBindingState state,
            string source,
            string reason)
        {
            ParticipantId = Normalize(participantId);
            AxisActorId = axisActorId;
            RuntimeActorId = runtimeActorId;
            UnityHandles = unityHandles;
            FlowStep = flowStep;
            State = state;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string ParticipantId { get; }
        public AxisActorId AxisActorId { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public ActorsUnityOperationalHandles UnityHandles { get; }
        public ActorsOperationalBindingFlowStep FlowStep { get; }
        public ActorsOperationalBindingState State { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ParticipantId) &&
            IsFlowStepValid(FlowStep) &&
            IsOwnershipChainValid(ParticipantId, AxisActorId, RuntimeActorId, UnityHandles, FlowStep, State);

        public bool Equals(ActorsOperationalBindingEntry other)
        {
            return string.Equals(ParticipantId, other.ParticipantId, StringComparison.Ordinal) &&
                   AxisActorId.Equals(other.AxisActorId) &&
                   RuntimeActorId.Equals(other.RuntimeActorId) &&
                   UnityHandles.Equals(other.UnityHandles) &&
                   FlowStep == other.FlowStep &&
                   State == other.State &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsOperationalBindingEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ UnityHandles.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)FlowStep;
                hashCode = (hashCode * 397) ^ (int)State;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"participantId='{AsText(ParticipantId)}', axisActorId='{AxisActorId}', runtimeActorId='{RuntimeActorId}', flowStep='{FlowStep}', state='{State}', handles=({UnityHandles}), source='{AsText(Source)}', reason='{AsText(Reason)}'";
        }

        public static bool TryTransitionState(
            ActorsOperationalBindingEntry current,
            ActorsOperationalBindingState nextState,
            string reason,
            string source,
            out ActorsOperationalBindingEntry next)
        {
            next = default;
            if (!current.IsValid || !CanTransition(current.State, nextState))
            {
                return false;
            }

            next = new ActorsOperationalBindingEntry(
                current.ParticipantId,
                current.AxisActorId,
                current.RuntimeActorId,
                current.UnityHandles,
                current.FlowStep,
                nextState,
                source,
                reason);
            return next.IsValid;
        }

        private static bool IsOwnershipChainValid(
            string participantId,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            ActorsUnityOperationalHandles unityHandles,
            ActorsOperationalBindingFlowStep flowStep,
            ActorsOperationalBindingState state)
        {
            if (string.IsNullOrWhiteSpace(participantId))
            {
                return false;
            }

            if (flowStep >= ActorsOperationalBindingFlowStep.AxisActorResolved && !axisActorId.IsValid)
            {
                return false;
            }

            if (flowStep >= ActorsOperationalBindingFlowStep.RuntimeMaterialized && !runtimeActorId.IsValid)
            {
                return false;
            }

            if (flowStep == ActorsOperationalBindingFlowStep.UnityOperationalBound && !unityHandles.HasAnyHandle)
            {
                return false;
            }

            if (flowStep < ActorsOperationalBindingFlowStep.UnityOperationalBound && state != ActorsOperationalBindingState.Unbound)
            {
                return false;
            }

            if (flowStep == ActorsOperationalBindingFlowStep.UnityOperationalBound &&
                state == ActorsOperationalBindingState.Unbound &&
                unityHandles.HasAnyHandle)
            {
                return false;
            }

            return true;
        }

        private static bool IsFlowStepValid(ActorsOperationalBindingFlowStep flowStep)
        {
            return flowStep == ActorsOperationalBindingFlowStep.SemanticReady ||
                   flowStep == ActorsOperationalBindingFlowStep.AxisActorResolved ||
                   flowStep == ActorsOperationalBindingFlowStep.RuntimeMaterialized ||
                   flowStep == ActorsOperationalBindingFlowStep.UnityOperationalBound;
        }

        private static bool CanTransition(ActorsOperationalBindingState current, ActorsOperationalBindingState next)
        {
            if (current == next)
            {
                return true;
            }

            if (current == ActorsOperationalBindingState.Bound && (next == ActorsOperationalBindingState.Active || next == ActorsOperationalBindingState.Disconnected))
            {
                return true;
            }

            if (current == ActorsOperationalBindingState.Active && next == ActorsOperationalBindingState.Disconnected)
            {
                return true;
            }

            if (current == ActorsOperationalBindingState.Disconnected && (next == ActorsOperationalBindingState.Bound || next == ActorsOperationalBindingState.Active))
            {
                return true;
            }

            return false;
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

    public readonly struct ActorsOperationalBindingSnapshot : IEquatable<ActorsOperationalBindingSnapshot>
    {
        public ActorsOperationalBindingSnapshot(
            string signature,
            ActorsOperationalBindingEntry[] entries,
            int boundCount,
            int activeCount,
            int disconnectedCount,
            string reason)
        {
            Signature = Normalize(signature);
            Entries = entries == null ? Array.Empty<ActorsOperationalBindingEntry>() : (ActorsOperationalBindingEntry[])entries.Clone();
            BoundCount = Clamp(boundCount);
            ActiveCount = Clamp(activeCount);
            DisconnectedCount = Clamp(disconnectedCount);
            Reason = Normalize(reason);
        }

        public string Signature { get; }
        public ActorsOperationalBindingEntry[] Entries { get; }
        public int BoundCount { get; }
        public int ActiveCount { get; }
        public int DisconnectedCount { get; }
        public string Reason { get; }

        public int Count => Entries?.Length ?? 0;
        public bool HasEntries => Count > 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(Signature);

        public static ActorsOperationalBindingSnapshot Empty => new(
            string.Empty,
            Array.Empty<ActorsOperationalBindingEntry>(),
            0,
            0,
            0,
            string.Empty);

        public bool Equals(ActorsOperationalBindingSnapshot other)
        {
            return string.Equals(Signature, other.Signature, StringComparison.Ordinal) &&
                   BoundCount == other.BoundCount &&
                   ActiveCount == other.ActiveCount &&
                   DisconnectedCount == other.DisconnectedCount &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsOperationalBindingSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(Signature ?? string.Empty);
                hashCode = (hashCode * 397) ^ BoundCount;
                hashCode = (hashCode * 397) ^ ActiveCount;
                hashCode = (hashCode * 397) ^ DisconnectedCount;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ Count;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"signature='{AsText(Signature)}', count='{Count}', bound='{BoundCount}', active='{ActiveCount}', disconnected='{DisconnectedCount}', reason='{AsText(Reason)}'";
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value;
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
