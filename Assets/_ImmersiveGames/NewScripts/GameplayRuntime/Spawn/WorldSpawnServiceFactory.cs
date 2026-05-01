using System;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    /// <summary>
    /// Factory canônica de serviços de spawn via registry de archetype.
    /// </summary>
    public sealed class WorldSpawnServiceFactory
    {
        private readonly WorldSpawnFactoryDependenciesResolver _dependenciesResolver = new();
        private readonly IActorSpawnArchetypeRegistry _archetypeRegistry;

        public WorldSpawnServiceFactory(IActorSpawnArchetypeRegistry archetypeRegistry)
        {
            _archetypeRegistry = archetypeRegistry ?? throw new ArgumentNullException(nameof(archetypeRegistry));
        }

        public IWorldSpawnService CreateFromActorSpec(
            ActorSpecRecord actorSpec,
            IDependencyProvider provider,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            if (!actorSpec.IsValid)
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][ActorsExecution] Invalid ActorSpec while creating spawn service.");
            }

            if (!_dependenciesResolver.TryResolve(provider, actorRegistry, context, out WorldSpawnFactoryDependencies dependencies))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Missing spawn dependencies for actorSpecId='{actorSpec.ActorSpecId}'.");
            }

            if (!actorSpec.HasPlaceholderBody)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] ActorSpec without placeholder body actorSpecId='{actorSpec.ActorSpecId}'.");
            }

            if (!_archetypeRegistry.TryResolve(actorSpec.SpawnArchetypeId, out ActorSpawnArchetypeRegistration registration) ||
                !registration.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] spawnArchetypeId unresolved for actorSpecId='{actorSpec.ActorSpecId}' spawnArchetypeId='{actorSpec.SpawnArchetypeId}'.");
            }

            DebugUtility.Log(typeof(WorldSpawnServiceFactory),
                $"[OBS][ActorsExecution] CreateSpawnServiceViaActorSpec actorSpecId='{actorSpec.ActorSpecId}' spawnArchetypeId='{actorSpec.SpawnArchetypeId}' actorSetRef='<none>' recipe='{actorSpec.OperationalRecipeKind}' source='ActorSpec'.",
                DebugUtility.Colors.Info);

            IWorldSpawnService service = registration.Factory(actorSpec, dependencies);
            if (service == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Failed to instantiate spawn service actorSpecId='{actorSpec.ActorSpecId}' spawnArchetypeId='{actorSpec.SpawnArchetypeId}'.");
            }

            if (!string.Equals(service.SpawnArchetypeId, actorSpec.SpawnArchetypeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Spawn service archetype mismatch actorSpecId='{actorSpec.ActorSpecId}' expected='{actorSpec.SpawnArchetypeId}' service='{service.SpawnArchetypeId}'.");
            }

            return service;
        }
    }
}
