using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class SpawnListener : MonoBehaviour
    {
        private EventBinding<ObjectSpawnedEvent> _binding;

        private void Awake()
        {
            _binding = new EventBinding<ObjectSpawnedEvent>(e => Debug.Log($"Objeto spawnado em {e.Position}"));
        }

        private void OnEnable()
        {
            EventBus<ObjectSpawnedEvent>.Register(_binding);
        }

        private void OnDisable()
        {
            EventBus<ObjectSpawnedEvent>.Unregister(_binding);
        }
    }
}