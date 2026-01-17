using UnityEngine;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn
{
    internal static class PlayerSpawnResolver
    {
        public static IActor Resolve(
            GameObject instance,
            System.Func<PlayerActor, GameObject, bool> ensurePlayer,
            System.Func<PlayerActorAdapter, GameObject, bool> ensureAdapter)
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

            // Try adapter
            if (instance.TryGetComponent(out PlayerActorAdapter adapter))
            {
                return !ensureAdapter(adapter, instance) ? null : adapter;
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
