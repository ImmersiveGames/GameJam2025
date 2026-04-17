using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Core.Identifiers;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Core;
using ImmersiveGames.GameJam2025.Game.Content.Definitions.Worlds.Config;
using ImmersiveGames.GameJam2025.Game.Gameplay.State.Core;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SessionIntegration.Runtime;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.Spawn
{
    /// <summary>
    /// ResolvePlayerActor e valida as dependências mínimas necessárias para criar serviços de spawn.
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
            provider.TryGetGlobal(out IGameplayStateGate stateService);
            provider.TryGetGlobal(out ISessionIntegrationContextService sessionIntegrationContextService);

            dependencies = new WorldSpawnFactoryDependencies(
                uniqueIdFactory,
                actorRegistry,
                context,
                stateService,
                sessionIntegrationContextService);

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
            ISessionIntegrationContextService sessionIntegrationContextService)
        {
            UniqueIdFactory = uniqueIdFactory;
            ActorRegistry = actorRegistry;
            Context = context;
            GameplayStateService = gameplayStateService;
            SessionIntegrationContextService = sessionIntegrationContextService;
        }

        public IUniqueIdFactory UniqueIdFactory { get; }

        public IActorRegistry ActorRegistry { get; }

        public IWorldSpawnContext Context { get; }

        public IGameplayStateGate GameplayStateService { get; }

        public ISessionIntegrationContextService SessionIntegrationContextService { get; }
    }
}

