using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.Scripts.Utils;
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
            WorldSpawnServiceKind kind,
            IDependencyProvider provider,
            IActorRegistry actorRegistry)
        {
            if (provider == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IDependencyProvider ausente ao criar serviço de spawn.");
                return null;
            }

            switch (kind)
            {
                case WorldSpawnServiceKind.DummyActor:
                    return CreateDummyActorService(provider, actorRegistry);

                default:
                    DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                        $"WorldSpawnServiceKind não suportado: {kind}.");
                    return null;
            }
        }

        private IWorldSpawnService CreateDummyActorService(
            IDependencyProvider provider,
            IActorRegistry actorRegistry)
        {
            if (actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IActorRegistry ausente. Não foi possível criar DummyActorSpawnService.");
                return null;
            }

            provider.TryGetGlobal<IUniqueIdFactory>(out var uniqueIdFactory);
            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IUniqueIdFactory global ausente. DummyActorSpawnService não será criado.");
                return null;
            }

            return new DummyActorSpawnService(uniqueIdFactory, actorRegistry);
        }
    }
}
