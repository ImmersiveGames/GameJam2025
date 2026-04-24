using System;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Eater;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    /// <summary>
    /// Factory canonica de servicos de spawn baseada em ActorSpec.
    /// </summary>
    public sealed class WorldSpawnServiceFactory
    {
        private readonly WorldSpawnFactoryDependenciesResolver _dependenciesResolver = new();

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

            WorldSpawnServiceKind kind = MapRecipeToWorldSpawnServiceKind(actorSpec.OperationalRecipeKind);
            if (kind == WorldSpawnServiceKind.Unknown)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Unsupported recipe for spawn service actorSpecId='{actorSpec.ActorSpecId}' recipe='{actorSpec.OperationalRecipeKind}'.");
            }

            DebugUtility.Log(typeof(WorldSpawnServiceFactory),
                $"[OBS][ActorsExecution] CreateSpawnServiceViaActorSpec actorSpecId='{actorSpec.ActorSpecId}' recipe='{actorSpec.OperationalRecipeKind}' kind='{kind}' orderSource='ActorSetRef'.",
                DebugUtility.Colors.Info);

            IWorldSpawnService service = CreateByKind(kind, actorSpec.PlaceholderBodyPrefab, dependencies);
            if (service == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Failed to instantiate spawn service actorSpecId='{actorSpec.ActorSpecId}' kind='{kind}'.");
            }

            return service;
        }

        private static IWorldSpawnService CreateDummy(
            GameObject prefab,
            WorldSpawnFactoryDependencies dependencies)
        {
            return new DummyActorSpawnService(
                dependencies.UniqueIdFactory,
                dependencies.ActorRegistry,
                dependencies.Context,
                prefab);
        }

        private static IWorldSpawnService CreatePlayer(
            GameObject prefab,
            WorldSpawnFactoryDependencies dependencies)
        {
            return new PlayerSpawnService(
                dependencies.UniqueIdFactory,
                dependencies.ActorRegistry,
                dependencies.Context,
                prefab,
                dependencies.GameplayStateService);
        }

        private static IWorldSpawnService CreateEater(
            GameObject prefab,
            WorldSpawnFactoryDependencies dependencies)
        {
            EaterActor eaterPrefab = prefab != null ? prefab.GetComponent<EaterActor>() : null;
            return new EaterSpawnService(
                dependencies.UniqueIdFactory,
                dependencies.ActorRegistry,
                dependencies.Context,
                eaterPrefab,
                dependencies.GameplayStateService);
        }

        private static IWorldSpawnService CreateByKind(
            WorldSpawnServiceKind kind,
            GameObject prefab,
            WorldSpawnFactoryDependencies dependencies)
        {
            return kind switch
            {
                WorldSpawnServiceKind.DummyActor => CreateDummy(prefab, dependencies),
                WorldSpawnServiceKind.Player => CreatePlayer(prefab, dependencies),
                WorldSpawnServiceKind.Eater => CreateEater(prefab, dependencies),
                _ => null
            };
        }

        private static WorldSpawnServiceKind MapRecipeToWorldSpawnServiceKind(ActorOperationalRecipeKind recipeKind)
        {
            return recipeKind switch
            {
                ActorOperationalRecipeKind.Player => WorldSpawnServiceKind.Player,
                ActorOperationalRecipeKind.Eater => WorldSpawnServiceKind.Eater,
                ActorOperationalRecipeKind.Dummy => WorldSpawnServiceKind.DummyActor,
                _ => WorldSpawnServiceKind.Unknown
            };
        }
    }

    /// <summary>
    /// Tipos conhecidos de servicos de spawn.
    /// </summary>
    public enum WorldSpawnServiceKind
    {
        Unknown = -1,
        DummyActor = 0,
        Player = 1,
        Eater = 2
    }
}
