using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    internal static class PlayerSpawnActorResolver
    {
        public static IActor ResolvePlayerActor(GameObject instance)
        {
            if (instance != null && instance.TryGetComponent(out PlayerActor playerActor))
            {
                return playerActor;
            }

            return null;
        }
    }
}

