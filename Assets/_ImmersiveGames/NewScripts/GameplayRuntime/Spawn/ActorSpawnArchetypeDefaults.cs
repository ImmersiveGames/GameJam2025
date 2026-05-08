using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    public static class ActorSpawnArchetypeDefaults
    {
        public const string Player = "spawn.player";
        public const string Dummy = "spawn.dummy";
        public const string Eater = "spawn.eater";

        public static void RegisterDefaults(IActorSpawnArchetypeRegistry registry)
        {
            registry.Register(new ActorSpawnArchetypeRegistration(
                Player,
                static (actorSpec, dependencies) => new PlayerSpawnService(
                    dependencies.UniqueIdFactory,
                    dependencies.ActorRegistry,
                    dependencies.Context,
                    actorSpec,
                    actorSpec.PlaceholderBodyPrefab)));

            registry.Register(new ActorSpawnArchetypeRegistration(
                Dummy,
                static (actorSpec, dependencies) => new DummyActorSpawnService(
                    dependencies.UniqueIdFactory,
                    dependencies.ActorRegistry,
                    dependencies.Context,
                    actorSpec,
                    actorSpec.PlaceholderBodyPrefab)));

            registry.Register(new ActorSpawnArchetypeRegistration(
                Eater,
                static (actorSpec, dependencies) => new EaterSpawnService(
                    dependencies.UniqueIdFactory,
                    dependencies.ActorRegistry,
                    dependencies.Context,
                    actorSpec,
                    actorSpec.PlaceholderBodyPrefab)));
        }

        public static ActorKind MapRecipeToActorKind(ActorOperationalRecipeKind recipeKind)
        {
            return recipeKind switch
            {
                ActorOperationalRecipeKind.Player => ActorKind.Player,
                ActorOperationalRecipeKind.Dummy => ActorKind.Dummy,
                ActorOperationalRecipeKind.Eater => ActorKind.Eater,
                _ => ActorKind.Unknown
            };
        }
    }
}
