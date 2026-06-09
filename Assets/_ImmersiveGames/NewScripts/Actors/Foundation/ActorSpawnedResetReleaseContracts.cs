using System;

namespace _ImmersiveGames.NewScripts.Actors.Foundation
{
    public enum SpawnedActorReturnToPoolResultKind
    {
        Unknown = 0,
        Success = 1,
        Skipped = 2,
        Failed = 3,
        InvalidCommand = 4,
        MissingPoolOrigin = 5,
        AlreadyReturned = 6,
    }

    /// <summary>
    /// Command futuro para o adapter retornar um Actor spawnable ao pool de origem.
    /// Contrato passivo: não chama pool, não executa side-effects e não decide lifecycle.
    /// </summary>
    public readonly struct SpawnedActorReturnToPoolCommand
    {
        public SpawnedActorReturnToPoolCommand(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            SpawnedActorPoolOrigin poolOrigin,
            SpawnedActorLifetimeState lifetimeState,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            PoolOrigin = poolOrigin;
            LifetimeState = lifetimeState;
            Reason = Normalize(reason);
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public SpawnedActorPoolOrigin PoolOrigin { get; }
        public SpawnedActorLifetimeState LifetimeState { get; }
        public string Reason { get; }
        public bool HasPoolOrigin => PoolOrigin.IsValid;
        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            HasPoolOrigin &&
            LifetimeState.IsValid &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct SpawnedActorReturnToPoolResult
    {
        public SpawnedActorReturnToPoolResult(
            SpawnedActorReturnToPoolResultKind kind,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string reason,
            string message)
        {
            Kind = kind;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public SpawnedActorReturnToPoolResultKind Kind { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public string Reason { get; }
        public string Message { get; }
        public bool IsValid => Kind != SpawnedActorReturnToPoolResultKind.Unknown;
        public bool IsSuccess => Kind == SpawnedActorReturnToPoolResultKind.Success;
        public bool IsSkipped => Kind == SpawnedActorReturnToPoolResultKind.Skipped;
        public bool IsFailed => Kind == SpawnedActorReturnToPoolResultKind.Failed;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct SpawnedActorResetCommand
    {
        public SpawnedActorResetCommand(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorSpawnedResetPolicy resetPolicy,
            ActorSnapshotPolicy snapshotPolicy,
            SpawnedActorPoolOrigin poolOrigin,
            SpawnedActorLifetimeState lifetimeState,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ResetPolicy = resetPolicy;
            SnapshotPolicy = snapshotPolicy;
            PoolOrigin = poolOrigin;
            LifetimeState = lifetimeState;
            Reason = Normalize(reason);
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorSpawnedResetPolicy ResetPolicy { get; }
        public ActorSnapshotPolicy SnapshotPolicy { get; }
        public SpawnedActorPoolOrigin PoolOrigin { get; }
        public SpawnedActorLifetimeState LifetimeState { get; }
        public string Reason { get; }
        public bool HasPoolOrigin => PoolOrigin.IsValid;
        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            HasPoolOrigin &&
            ResetPolicy != ActorSpawnedResetPolicy.Unknown &&
            SnapshotPolicy != ActorSnapshotPolicy.Unknown &&
            LifetimeState.IsValid &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct SpawnedActorResetFactPayload
    {
        public SpawnedActorResetFactPayload(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorSpawnedResetPolicy resetPolicy,
            ActorSnapshotPolicy snapshotPolicy,
            SpawnedActorPoolOrigin poolOrigin,
            SpawnedActorReturnToPoolResultKind resultKind,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ResetPolicy = resetPolicy;
            SnapshotPolicy = snapshotPolicy;
            PoolOrigin = poolOrigin;
            ResultKind = resultKind;
            Reason = Normalize(reason);
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorSpawnedResetPolicy ResetPolicy { get; }
        public ActorSnapshotPolicy SnapshotPolicy { get; }
        public SpawnedActorPoolOrigin PoolOrigin { get; }
        public SpawnedActorReturnToPoolResultKind ResultKind { get; }
        public string Reason { get; }
        public bool HasPoolOrigin => PoolOrigin.IsValid;
        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            HasPoolOrigin &&
            ResetPolicy != ActorSpawnedResetPolicy.Unknown &&
            SnapshotPolicy != ActorSnapshotPolicy.Unknown &&
            ResultKind != SpawnedActorReturnToPoolResultKind.Unknown &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
