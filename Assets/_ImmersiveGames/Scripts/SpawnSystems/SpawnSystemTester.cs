using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class SpawnSystemTester : MonoBehaviour
    {
        [SerializeField] private SpawnPoint spawnPoint;
        [SerializeField] private KeyCode resetKey = KeyCode.R;

        private EventBinding<SpawnTriggeredEvent> _triggeredBinding;
        private EventBinding<SpawnFailedEvent> _failedBinding;
        private EventBinding<PoolExhaustedEvent> _exhaustedBinding;

        private void Start()
        {
            if (spawnPoint == null)
            {
                DebugUtility.LogError<SpawnSystemTester>("SpawnPoint não configurado.", this);
                enabled = false;
                return;
            }

            _triggeredBinding = new EventBinding<SpawnTriggeredEvent>(e =>
                DebugUtility.Log<SpawnSystemTester>($"Spawn disparado em {e.Position} para '{e.PoolKey}'.", "cyan", this));
            _failedBinding = new EventBinding<SpawnFailedEvent>(e =>
                DebugUtility.Log<SpawnSystemTester>($"Spawn falhou para '{e.PoolKey}'.", "yellow", this));
            _exhaustedBinding = new EventBinding<PoolExhaustedEvent>(e =>
                DebugUtility.Log<SpawnSystemTester>($"Pool '{e.PoolKey}' esgotado.", "yellow", this));

            EventBus<SpawnTriggeredEvent>.Register(_triggeredBinding);
            EventBus<SpawnFailedEvent>.Register(_failedBinding);
            EventBus<PoolExhaustedEvent>.Register(_exhaustedBinding);
        }

        private void OnDestroy()
        {
            EventBus<SpawnTriggeredEvent>.Unregister(_triggeredBinding);
            EventBus<SpawnFailedEvent>.Unregister(_failedBinding);
            EventBus<PoolExhaustedEvent>.Unregister(_exhaustedBinding);
        }

        private void Update()
        {
            if (Input.GetKeyDown(resetKey))
            {
                spawnPoint.ResetSpawnPoint();
                DebugUtility.Log<SpawnSystemTester>("SpawnPoint resetado.", "green", this);
            }
        }
    }
}