using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;

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

            provider.TryGetGlobal(out IGameplayStateGate stateService);
            provider.TryGetGlobal(out ISpawnResetParticipationReadPort participationReadPort);

            dependencies = new WorldSpawnFactoryDependencies(
                uniqueIdFactory,
                actorRegistry,
                context,
                stateService,
                participationReadPort);

            return true;
        }
    }

    public readonly struct WorldSpawnFactoryDependencies
    {
        public WorldSpawnFactoryDependencies(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            IGameplayStateGate gameplayStateService,
            ISpawnResetParticipationReadPort participationReadPort)
        {
            UniqueIdFactory = uniqueIdFactory;
            ActorRegistry = actorRegistry;
            Context = context;
            GameplayStateService = gameplayStateService;
            ParticipationReadPort = participationReadPort;
        }

        public IUniqueIdFactory UniqueIdFactory { get; }
        public IActorRegistry ActorRegistry { get; }
        public IWorldSpawnContext Context { get; }
        public IGameplayStateGate GameplayStateService { get; }
        public ISpawnResetParticipationReadPort ParticipationReadPort { get; }
    }
}
