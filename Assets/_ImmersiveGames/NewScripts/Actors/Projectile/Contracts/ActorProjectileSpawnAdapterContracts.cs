using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Contracts
{
    public enum ActorProjectileSpawnAdapterResultKind
    {
        Unknown = 0,
        NotConfigured = 1,
        AcceptedNoSpawn = 2,
        Spawned = 3,
        Failed = 4,
    }

    public readonly struct ActorProjectileSpawnAdapterResult
    {
        public ActorProjectileSpawnAdapterResult(
            ActorProjectileSpawnAdapterResultKind kind,
            ActorProjectileFireCommand command,
            bool spawnExecuted,
            bool poolCalled,
            GameObject spawnedInstance,
            Actor spawnedActor,
            string reason,
            string message)
        {
            Kind = kind;
            Command = command;
            SpawnExecuted = spawnExecuted;
            PoolCalled = poolCalled;
            SpawnedInstance = spawnedInstance;
            SpawnedActor = spawnedActor;
            Reason = reason.TrimToEmpty();
            Message = message.TrimToEmpty();
        }

        public ActorProjectileSpawnAdapterResultKind Kind { get; }
        public ActorProjectileFireCommand Command { get; }
        public bool SpawnExecuted { get; }
        public bool PoolCalled { get; }
        public GameObject SpawnedInstance { get; }
        public Actor SpawnedActor { get; }
        public string Reason { get; }
        public string Message { get; }
        public bool IsValid => Kind != ActorProjectileSpawnAdapterResultKind.Unknown;
        public bool IsAccepted =>
            Kind == ActorProjectileSpawnAdapterResultKind.AcceptedNoSpawn ||
            Kind == ActorProjectileSpawnAdapterResultKind.Spawned;
        public bool IsFailed => Kind == ActorProjectileSpawnAdapterResultKind.Failed;

        public static ActorProjectileSpawnAdapterResult NotConfigured(
            ActorProjectileFireCommand command,
            string reason,
            string message)
        {
            return new ActorProjectileSpawnAdapterResult(
                ActorProjectileSpawnAdapterResultKind.NotConfigured,
                command,
                spawnExecuted: false,
                poolCalled: false,
                spawnedInstance: null,
                spawnedActor: null,
                reason,
                message);
        }

        public static ActorProjectileSpawnAdapterResult AcceptedNoSpawn(
            ActorProjectileFireCommand command,
            string reason,
            string message)
        {
            return new ActorProjectileSpawnAdapterResult(
                ActorProjectileSpawnAdapterResultKind.AcceptedNoSpawn,
                command,
                spawnExecuted: false,
                poolCalled: false,
                spawnedInstance: null,
                spawnedActor: null,
                reason,
                message);
        }

        public static ActorProjectileSpawnAdapterResult Spawned(
            ActorProjectileFireCommand command,
            GameObject spawnedInstance,
            Actor spawnedActor,
            string reason,
            string message)
        {
            return new ActorProjectileSpawnAdapterResult(
                ActorProjectileSpawnAdapterResultKind.Spawned,
                command,
                spawnExecuted: true,
                poolCalled: true,
                spawnedInstance,
                spawnedActor,
                reason,
                message);
        }

        public static ActorProjectileSpawnAdapterResult Failed(
            ActorProjectileFireCommand command,
            string reason,
            string message)
        {
            return Failed(
                command,
                poolCalled: false,
                spawnedInstance: null,
                spawnedActor: null,
                reason,
                message);
        }

        public static ActorProjectileSpawnAdapterResult Failed(
            ActorProjectileFireCommand command,
            bool poolCalled,
            GameObject spawnedInstance,
            Actor spawnedActor,
            string reason,
            string message)
        {
            return new ActorProjectileSpawnAdapterResult(
                ActorProjectileSpawnAdapterResultKind.Failed,
                command,
                spawnExecuted: false,
                poolCalled: poolCalled,
                spawnedInstance: spawnedInstance,
                spawnedActor: spawnedActor,
                reason,
                message);
        }
}

    public interface IActorProjectileSpawnAdapter
    {
        ActorProjectileSpawnAdapterResult Execute(ActorProjectileFireCommand command);
    }
}
