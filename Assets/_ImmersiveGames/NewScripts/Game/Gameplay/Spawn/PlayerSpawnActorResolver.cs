using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Player;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Spawn
{
    internal static class PlayerSpawnActorResolver
    {
        public static IActor ResolvePlayerActor(
            GameObject instance,
            System.Func<PlayerActor, GameObject, bool> ensurePlayer)
        {
            if (instance == null)
            {
                return null;
            }

            // Try PlayerActor
            if (instance.TryGetComponent(out PlayerActor playerActor))
            {
                return !ensurePlayer(playerActor, instance) ? null : playerActor;
            }

            // Try any existing IActor
            if (instance.TryGetComponent(out IActor existingActor) && existingActor != null)
            {
                return string.IsNullOrWhiteSpace(existingActor.ActorId) ? null : existingActor;
            }

            // Fallback: add PlayerActor
            var fallback = instance.AddComponent<PlayerActor>();
            return !ensurePlayer(fallback, instance) ? null : fallback;
        }
    }
}
