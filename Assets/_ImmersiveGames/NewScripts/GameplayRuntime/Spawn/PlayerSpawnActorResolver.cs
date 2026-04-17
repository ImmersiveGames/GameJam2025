using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    internal static class PlayerSpawnActorResolver
    {
        public static IActor ResolvePlayerActor(GameObject instance)
        {
            if (instance == null)
            {
                return null;
            }

            // Try PlayerActor
            if (instance.TryGetComponent(out PlayerActor playerActor))
            {
                return playerActor;
            }

            // Try any existing IActor
            if (instance.TryGetComponent(out IActor existingActor) && existingActor != null)
            {
                return existingActor;
            }

            // Fallback: add PlayerActor
            var fallback = instance.AddComponent<PlayerActor>();
            return fallback;
        }
    }
}

