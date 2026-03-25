using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Eater;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Spawn;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Spawn.Definitions;

namespace _ImmersiveGames.NewScripts.Modules.SceneReset.Spawn
{
    /// <summary>
    /// Factory explícita para criação de serviços de spawn baseados em definições.
    /// Responsabilidade atual: compor dependências resolvidas, validar entry e instanciar o serviço concreto.
    /// </summary>
    public sealed class WorldSpawnServiceFactory
    {
        private readonly WorldSpawnFactoryDependenciesResolver _dependenciesResolver = new();
        private readonly WorldSpawnEntryValidator _entryValidator = new();

        // ReSharper disable CognitiveComplexity
        public IWorldSpawnService Create(
            WorldDefinition.SpawnEntry entry,
            IDependencyProvider provider,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            if (!_dependenciesResolver.TryResolve(entry, provider, actorRegistry, context, out WorldSpawnFactoryDependencies dependencies))
            {
                return null;
            }

            if (!_entryValidator.TryValidate(entry))
            {
                return null;
            }

            switch (entry.Kind)
            {
                case WorldSpawnServiceKind.DummyActor:
                    return CreateDummy(entry, dependencies);

                case WorldSpawnServiceKind.Player:
                    return CreatePlayer(entry, dependencies);

                case WorldSpawnServiceKind.Eater:
                    return CreateEater(entry, dependencies);

                default:
                    DebugUtility.LogError(typeof(WorldSpawnServiceFactory),
                        $"WorldSpawnServiceKind não suportado: {entry.Kind}.");
                    return null;
            }
        }
        // ReSharper restore CognitiveComplexity

        private static IWorldSpawnService CreateDummy(
            WorldDefinition.SpawnEntry entry,
            WorldSpawnFactoryDependencies dependencies)
        {
            return new DummyActorSpawnService(
                dependencies.UniqueIdFactory,
                dependencies.ActorRegistry,
                dependencies.Context,
                entry.Prefab);
        }

        private static IWorldSpawnService CreatePlayer(
            WorldDefinition.SpawnEntry entry,
            WorldSpawnFactoryDependencies dependencies)
        {
            return new PlayerSpawnService(
                dependencies.UniqueIdFactory,
                dependencies.ActorRegistry,
                dependencies.Context,
                entry.Prefab,
                dependencies.GameplayStateService);
        }

        private static IWorldSpawnService CreateEater(
            WorldDefinition.SpawnEntry entry,
            WorldSpawnFactoryDependencies dependencies)
        {
            EaterActor eaterPrefab = entry.Prefab.GetComponent<EaterActor>();
            return new EaterSpawnService(
                dependencies.UniqueIdFactory,
                dependencies.ActorRegistry,
                dependencies.Context,
                eaterPrefab,
                dependencies.GameplayStateService);
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
