using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actions.States;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Bindings.Eater;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Runtime;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Spawning;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Spawning.Definitions;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn
{
    /// <summary>
    /// Factory explícita para criação de serviços de spawn baseados em definições.
    /// Simplificada para evitar checagens repetidas por tipo.
    /// </summary>
    public sealed class WorldSpawnServiceFactory
    {
        // ReSharper disable CognitiveComplexity
        public IWorldSpawnService Create(
            WorldDefinition.SpawnEntry entry,
            IDependencyProvider provider,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            if (!TryPrepare(entry, provider, actorRegistry, context, out var uniqueIdFactory, out var stateService))
            {
                return null;
            }

            switch (entry.Kind)
            {
                case WorldSpawnServiceKind.DummyActor:
                    return CreateDummy(entry, uniqueIdFactory, actorRegistry, context);

                case WorldSpawnServiceKind.Player:
                    return CreatePlayer(entry, uniqueIdFactory, actorRegistry, context, stateService);

                case WorldSpawnServiceKind.Eater:
                    return CreateEater(entry, uniqueIdFactory, actorRegistry, context, stateService);

                default:
                    DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                        $"WorldSpawnServiceKind não suportado: {entry.Kind}.");
                    return null;
            }
        }
        // ReSharper, restore CognitiveComplexity

        private bool TryPrepare(
            WorldDefinition.SpawnEntry entry,
            IDependencyProvider provider,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            out IUniqueIdFactory uniqueIdFactory,
            out IStateDependentService stateService)
        {
            uniqueIdFactory = null;
            stateService = null;

            if (entry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "SpawnEntry nula ao criar serviço de spawn.");
                return false;
            }

            if (provider == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IDependencyProvider ausente ao criar serviço de spawn.");
                return false;
            }

            if (context == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IWorldSpawnContext ausente ao criar serviço de spawn.");
                return false;
            }

            if (actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IActorRegistry ausente ao criar serviço de spawn.");
                return false;
            }

            provider.TryGetGlobal(out uniqueIdFactory);
            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "IUniqueIdFactory global ausente. Serviço de spawn não será criado.");
                return false;
            }

            // obter optional state service (usado por Player/Eater)
            provider.TryGetGlobal(out stateService);

            return true;
        }

        private IWorldSpawnService CreateDummy(
            WorldDefinition.SpawnEntry entry,
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            if (entry.Prefab == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "Prefab não configurado para DummyActorSpawnService.");
                return null;
            }

            return new DummyActorSpawnService(uniqueIdFactory, actorRegistry, context, entry.Prefab);
        }

        private IWorldSpawnService CreatePlayer(
            WorldDefinition.SpawnEntry entry,
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            IStateDependentService stateService)
        {
            if (entry.Prefab == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "Prefab não configurado para PlayerSpawnService.");
                return null;
            }

            return new PlayerSpawnService(uniqueIdFactory, actorRegistry, context, entry.Prefab, stateService);
        }

        private IWorldSpawnService CreateEater(
            WorldDefinition.SpawnEntry entry,
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            IStateDependentService stateService)
        {
            var eaterPrefab = entry.Prefab != null ? entry.Prefab.GetComponent<EaterActor>() : null;
            if (eaterPrefab == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                    "Prefab sem EaterActor. EaterSpawnService não será criado.");
                return null;
            }

            return new EaterSpawnService(uniqueIdFactory, actorRegistry, context, eaterPrefab, stateService);
        }
    }
    /// <summary>
    /// Tipos conhecidos de serviços de spawn do mundo.
    /// Mantido explícito para evitar reflection e facilitar expansão futura.
    /// </summary>
    public enum WorldSpawnServiceKind
    {
        DummyActor = 0,
        Player = 1,
        Eater = 2
    }
}

