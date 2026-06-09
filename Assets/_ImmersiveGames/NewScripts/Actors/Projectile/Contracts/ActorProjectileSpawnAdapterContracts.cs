using System;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Contracts
{
    public enum ActorProjectileSpawnAdapterResultKind
    {
        Unknown = 0,
        NotConfigured = 1,
        AcceptedNoSpawn = 2,
        Failed = 3,
    }

    public readonly struct ActorProjectileSpawnAdapterResult
    {
        public ActorProjectileSpawnAdapterResult(
            ActorProjectileSpawnAdapterResultKind kind,
            ActorProjectileFireCommand command,
            bool spawnExecuted,
            bool poolCalled,
            string reason,
            string message)
        {
            Kind = kind;
            Command = command;
            SpawnExecuted = spawnExecuted;
            PoolCalled = poolCalled;
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActorProjectileSpawnAdapterResultKind Kind { get; }
        public ActorProjectileFireCommand Command { get; }
        public bool SpawnExecuted { get; }
        public bool PoolCalled { get; }
        public string Reason { get; }
        public string Message { get; }
        public bool IsValid => Kind != ActorProjectileSpawnAdapterResultKind.Unknown;
        public bool IsAccepted => Kind == ActorProjectileSpawnAdapterResultKind.AcceptedNoSpawn || Kind == ActorProjectileSpawnAdapterResultKind.NotConfigured;
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
                reason,
                message);
        }

        public static ActorProjectileSpawnAdapterResult Failed(
            ActorProjectileFireCommand command,
            string reason,
            string message)
        {
            return new ActorProjectileSpawnAdapterResult(
                ActorProjectileSpawnAdapterResultKind.Failed,
                command,
                spawnExecuted: false,
                poolCalled: false,
                reason,
                message);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActorProjectileSpawnAdapter
    {
        ActorProjectileSpawnAdapterResult Execute(ActorProjectileFireCommand command);
    }
}
