using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Events
{
    public class PlayerDiedEvent: IEvent
    {
        public Vector3 Position { get; }
        public GameObject Eater { get; }
        public PlayerDiedEvent(Vector3 position, GameObject eater)
        {
            Position = position;
            Eater = eater;
        }
    }
}