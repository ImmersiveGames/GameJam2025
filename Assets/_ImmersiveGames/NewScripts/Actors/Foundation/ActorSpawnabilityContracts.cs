using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.Actors.Foundation
{
    public enum ActorMaterializationKind
    {
        Unknown = 0,
        SceneAuthored = 1,
        SessionParticipant = 2,
        RuntimeSpawned = 3
    }

    public readonly struct ActorLifetimePolicy
    {
        // ActorScope define ownership/lifecycle amplo; esta policy define regra explícita de fim de vida/materialização quando necessário.
        public enum PolicyKind
        {
            Unknown = 0,
            ActivityBound = 1,
            RouteBound = 2,
            SessionBound = 3,
            RuntimeTransient = 4
        }

        public ActorLifetimePolicy(PolicyKind kind)
        {
            Kind = kind;
        }

        public PolicyKind Kind { get; }
        public bool IsValid => Kind != PolicyKind.Unknown;
        public bool IsRuntimeTransient => Kind == PolicyKind.RuntimeTransient;

        public override string ToString()
        {
            return Kind.ToString();
        }
    }

    public enum ActorSpawnedResetPolicy
    {
        Unknown = 0,
        ReturnToOriginPool = 1,
        ClearTransientState = 2,
        ClearLifetime = 3,
        SkipExplicit = 4
    }

    public enum ActorSnapshotPolicy
    {
        Unknown = 0,
        SkipRuntimeTransient = 1,
        SaveIfMarked = 2,
        CheckpointRelevant = 3,
        PersistUntilConsumed = 4
    }

    public readonly struct SpawnedActorPoolOrigin
    {
        public SpawnedActorPoolOrigin(string poolOriginId, string poolDefinitionId)
        {
            PoolOriginId = poolOriginId.TrimToEmpty();
            PoolDefinitionId = poolDefinitionId.TrimToEmpty();
        }

        public string PoolOriginId { get; }
        public string PoolDefinitionId { get; }
        public bool HasPoolOriginId => !string.IsNullOrWhiteSpace(PoolOriginId);
        public bool HasPoolDefinitionId => !string.IsNullOrWhiteSpace(PoolDefinitionId);
        public bool IsValid => HasPoolOriginId || HasPoolDefinitionId;

        public override string ToString()
        {
            return HasPoolOriginId ? PoolOriginId : PoolDefinitionId;
        }
    }

    public readonly struct SpawnedActorLifetimeState
    {
        public SpawnedActorLifetimeState(float lifetimeSeconds, float elapsedSeconds)
        {
            LifetimeSeconds = lifetimeSeconds < 0f ? 0f : lifetimeSeconds;
            ElapsedSeconds = elapsedSeconds < 0f ? 0f : elapsedSeconds;
        }

        public float LifetimeSeconds { get; }
        public float ElapsedSeconds { get; }
        public float RemainingSeconds => HasLifetime ? LifetimeSeconds - ElapsedSeconds > 0f ? LifetimeSeconds - ElapsedSeconds : 0f : 0f;
        public bool HasLifetime => LifetimeSeconds > 0f;
        public bool IsExpired => HasLifetime && ElapsedSeconds >= LifetimeSeconds;
        public bool IsValid => LifetimeSeconds >= 0f && ElapsedSeconds >= 0f;

        // Estado puro/local: não executa reset nem retorna ao pool.
        public SpawnedActorLifetimeState Clear()
        {
            return default;
        }

        public SpawnedActorLifetimeState WithElapsedSeconds(float elapsedSeconds)
        {
            return new SpawnedActorLifetimeState(LifetimeSeconds, elapsedSeconds);
        }
    }

    public readonly struct ActorSpawnability
    {
        public ActorSpawnability(
            bool isSpawnable,
            ActorMaterializationKind materializationKind,
            ActorLifetimePolicy.PolicyKind lifetimePolicy,
            ActorSpawnedResetPolicy resetPolicy,
            ActorSnapshotPolicy snapshotPolicy,
            SpawnedActorPoolOrigin? poolOrigin = null)
        {
            IsSpawnable = isSpawnable;
            MaterializationKind = materializationKind;
            LifetimePolicy = lifetimePolicy;
            ResetPolicy = resetPolicy;
            SnapshotPolicy = snapshotPolicy;
            PoolOrigin = poolOrigin ?? default;
            HasPoolOrigin = poolOrigin is { IsValid: true };
        }

        public bool IsSpawnable { get; }
        public ActorMaterializationKind MaterializationKind { get; }
        public ActorLifetimePolicy.PolicyKind LifetimePolicy { get; }
        public ActorSpawnedResetPolicy ResetPolicy { get; }
        public ActorSnapshotPolicy SnapshotPolicy { get; }
        public SpawnedActorPoolOrigin PoolOrigin { get; }
        public bool HasPoolOrigin { get; }
        public bool RequiresPoolOrigin =>
            MaterializationKind == ActorMaterializationKind.RuntimeSpawned ||
            ResetPolicy == ActorSpawnedResetPolicy.ReturnToOriginPool;

        public bool IsValid =>
            (!IsSpawnable || MaterializationKind != ActorMaterializationKind.Unknown &&
                LifetimePolicy != ActorLifetimePolicy.PolicyKind.Unknown &&
                ResetPolicy != ActorSpawnedResetPolicy.Unknown &&
                SnapshotPolicy != ActorSnapshotPolicy.Unknown &&
                (!RequiresPoolOrigin || HasPoolOrigin) &&
                (LifetimePolicy != ActorLifetimePolicy.PolicyKind.RuntimeTransient ||
                    SnapshotPolicy != ActorSnapshotPolicy.Unknown)) &&
            (!HasPoolOrigin || PoolOrigin.IsValid);
    }
}
