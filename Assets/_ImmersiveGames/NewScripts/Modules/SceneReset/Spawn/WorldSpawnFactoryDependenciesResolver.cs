using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actions.States;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Spawning.Definitions;

namespace _ImmersiveGames.NewScripts.Modules.SceneReset.Spawn
{
    /// <summary>
    /// Resolve e valida as dependências mínimas necessárias para criar serviços de spawn.
    /// Mantém a semântica atual da factory: falha retorna false e o call site decide o tratamento.
    /// </summary>
    public sealed class WorldSpawnFactoryDependenciesResolver
    {
        public bool TryResolve(
            WorldDefinition.SpawnEntry entry,
            IDependencyProvider provider,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            out WorldSpawnFactoryDependencies dependencies)
        {
            dependencies = default;

            if (entry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnFactoryDependenciesResolver),
                    "SpawnEntry nula ao criar serviço de spawn.");
                return false;
            }

            if (provider == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnFactoryDependenciesResolver),
                    "IDependencyProvider ausente ao criar serviço de spawn.");
                return false;
            }

            if (context == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnFactoryDependenciesResolver),
                    "IWorldSpawnContext ausente ao criar serviço de spawn.");
                return false;
            }

            if (actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnFactoryDependenciesResolver),
                    "IActorRegistry ausente ao criar serviço de spawn.");
                return false;
            }

            provider.TryGetGlobal(out IUniqueIdFactory uniqueIdFactory);
            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnFactoryDependenciesResolver),
                    "IUniqueIdFactory global ausente. Serviço de spawn não será criado.");
                return false;
            }

            // Serviço opcional: Player/Eater podem usar quando disponível.
            provider.TryGetGlobal(out IStateDependentService stateService);

            dependencies = new WorldSpawnFactoryDependencies(
                uniqueIdFactory,
                actorRegistry,
                context,
                stateService);

            return true;
        }
    }

    public readonly struct WorldSpawnFactoryDependencies
    {
        public WorldSpawnFactoryDependencies(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            IStateDependentService stateService)
        {
            UniqueIdFactory = uniqueIdFactory;
            ActorRegistry = actorRegistry;
            Context = context;
            StateService = stateService;
        }

        public IUniqueIdFactory UniqueIdFactory { get; }

        public IActorRegistry ActorRegistry { get; }

        public IWorldSpawnContext Context { get; }

        public IStateDependentService StateService { get; }
    }
}
