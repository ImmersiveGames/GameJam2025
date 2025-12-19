using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Factory explícita para criação de serviços de spawn baseados em definições.
    /// </summary>
    public sealed class WorldSpawnServiceFactory
    {
        public IWorldSpawnService Create(
            WorldDefinition.SpawnEntry entry,
            IDependencyProvider provider,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            if (entry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "SpawnEntry nula ao criar serviço de spawn.");
                return null;
            }

            if (provider == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IDependencyProvider ausente ao criar serviço de spawn.");
                return null;
            }

            if (context == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IWorldSpawnContext ausente ao criar serviço de spawn.");
                return null;
            }

            switch (entry.Kind)
            {
                case WorldSpawnServiceKind.DummyActor:
                    return CreateDummyActorService(entry, provider, actorRegistry, context);
                case WorldSpawnServiceKind.Player:
                    return CreatePlayerService(entry, provider, actorRegistry, context);

                default:
                    DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                        $"WorldSpawnServiceKind não suportado: {entry.Kind}.");
                    return null;
            }
        }

        private IWorldSpawnService CreateDummyActorService(
            WorldDefinition.SpawnEntry entry,
            IDependencyProvider provider,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            if (actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IActorRegistry ausente. Não foi possível criar DummyActorSpawnService.");
                return null;
            }

            if (context == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IWorldSpawnContext ausente. DummyActorSpawnService não será criado.");
                return null;
            }

            provider.TryGetGlobal<IUniqueIdFactory>(out var uniqueIdFactory);
            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IUniqueIdFactory global ausente. DummyActorSpawnService não será criado.");
                return null;
            }

            return new DummyActorSpawnService(uniqueIdFactory, actorRegistry, context, entry.Prefab);
        }

        private IWorldSpawnService CreatePlayerService(
            WorldDefinition.SpawnEntry entry,
            IDependencyProvider provider,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            if (actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IActorRegistry ausente. Não foi possível criar PlayerSpawnService.");
                return null;
            }

            if (context == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IWorldSpawnContext ausente. PlayerSpawnService não será criado.");
                return null;
            }

            provider.TryGetGlobal<IUniqueIdFactory>(out var uniqueIdFactory);
            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IUniqueIdFactory global ausente. PlayerSpawnService não será criado.");
                return null;
            }

            return new PlayerSpawnService(uniqueIdFactory, actorRegistry, context, entry.Prefab);
        }
    }
}
