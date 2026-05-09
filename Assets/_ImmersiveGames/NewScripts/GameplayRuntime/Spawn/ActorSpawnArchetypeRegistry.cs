using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    public delegate IWorldSpawnService ActorSpawnArchetypeFactory(ActorSpecRecord actorSpec, WorldSpawnFactoryDependencies dependencies);

    public readonly struct ActorSpawnArchetypeRegistration
    {
        public ActorSpawnArchetypeRegistration(string spawnArchetypeId, ActorSpawnArchetypeFactory factory)
        {
            SpawnArchetypeId = string.IsNullOrWhiteSpace(spawnArchetypeId) ? string.Empty : spawnArchetypeId.Trim();
            Factory = factory;
        }

        public string SpawnArchetypeId { get; }
        public ActorSpawnArchetypeFactory Factory { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(SpawnArchetypeId) && Factory != null;
    }

    public interface IActorSpawnArchetypeRegistry
    {
        void Register(ActorSpawnArchetypeRegistration registration);
        bool TryResolve(string spawnArchetypeId, out ActorSpawnArchetypeRegistration registration);
    }

    public sealed class ActorSpawnArchetypeRegistry : IActorSpawnArchetypeRegistry
    {
        private readonly Dictionary<string, ActorSpawnArchetypeRegistration> _byArchetypeId = new(StringComparer.Ordinal);

        public void Register(ActorSpawnArchetypeRegistration registration)
        {
            if (!registration.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsExecution] Invalid actor spawn archetype registration.");
            }

            if (_byArchetypeId.ContainsKey(registration.SpawnArchetypeId))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Duplicate spawn archetype registration spawnArchetypeId='{registration.SpawnArchetypeId}'.");
            }

            _byArchetypeId.Add(registration.SpawnArchetypeId, registration);
        }

        public bool TryResolve(string spawnArchetypeId, out ActorSpawnArchetypeRegistration registration)
        {
            registration = default;
            if (string.IsNullOrWhiteSpace(spawnArchetypeId))
            {
                return false;
            }

            return _byArchetypeId.TryGetValue(spawnArchetypeId.Trim(), out registration);
        }
    }
}
