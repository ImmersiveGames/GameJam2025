using _ImmersiveGames.Scripts.PlayerControllerSystem.EventBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class PlayerHealth : HealthResource
    {
        public override void Deafeat(Vector3 position)
        {
            base.Deafeat(position);
            EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent(position, gameObject));
            DebugUtility.LogVerbose<PlayerHealth>($"Jogador derrotado na posição {position}.");
        }
    }
}