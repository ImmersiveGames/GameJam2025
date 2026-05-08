using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    /// <summary>
    /// Resolve e valida dependencias minimas para criar servicos de spawn canonicos via ActorSpec.
    /// </summary>
    public sealed class WorldSpawnFactoryDependenciesResolver
    {
        public bool TryResolve(
            IDependencyProvider provider,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            out WorldSpawnFactoryDependencies dependencies)
        {
            dependencies = default;

            if (provider == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnFactoryDependenciesResolver),
                    "IDependencyProvider missing while creating spawn service.");
                return false;
            }

            if (context == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnFactoryDependenciesResolver),
                    "IWorldSpawnContext missing while creating spawn service.");
                return false;
            }

            if (actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnFactoryDependenciesResolver),
                    "IActorRegistry missing while creating spawn service.");
                return false;
            }

            provider.TryGetGlobal(out IUniqueIdFactory uniqueIdFactory);
            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnFactoryDependenciesResolver),
                    "IUniqueIdFactory missing. Spawn service cannot be created.");
                return false;
            }
            dependencies = new WorldSpawnFactoryDependencies(
                uniqueIdFactory,
                actorRegistry,
                context);

            return true;
        }
    }

    public readonly struct WorldSpawnFactoryDependencies
    {
        public WorldSpawnFactoryDependencies(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            UniqueIdFactory = uniqueIdFactory;
            ActorRegistry = actorRegistry;
            Context = context;
        }

        public IUniqueIdFactory UniqueIdFactory { get; }
        public IActorRegistry ActorRegistry { get; }
        public IWorldSpawnContext Context { get; }
    }
}
