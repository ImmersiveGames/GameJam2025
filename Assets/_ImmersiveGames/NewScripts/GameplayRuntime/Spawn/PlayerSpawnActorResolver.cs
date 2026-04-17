using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Core;
using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Player;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.Spawn
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

