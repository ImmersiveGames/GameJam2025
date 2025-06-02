using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetHealth : HealthResource
    {
        public override void Deafeat(Vector3 position)
        {
            modelRoot.SetActive(false);
            // Dispara DeathEvent com a posição do objeto
            EventBus<DeathEvent>.Raise(new DeathEvent(position, gameObject));
            GameManager.Instance.SetGameOver(true);
        }

    }
}