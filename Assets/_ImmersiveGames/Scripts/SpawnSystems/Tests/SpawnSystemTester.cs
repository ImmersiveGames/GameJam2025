using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.EventBus;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Tests
{
    public class SpawnSystemTester : MonoBehaviour
    {
        [SerializeField] private SpawnPoint spawnPoint;
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private KeyCode unlockKey = KeyCode.U;
        [SerializeField] private KeyCode resetAllKey = KeyCode.T;
        [SerializeField] private KeyCode stopAllKey = KeyCode.Y;
        [SerializeField] private KeyCode stopAllIncludingIndependentKey = KeyCode.I;

        private EventBinding<SpawnTriggeredEvent> _triggeredBinding;
        private EventBinding<SpawnFailedEvent> _failedBinding;
        private EventBinding<PoolExhaustedEvent> _exhaustedBinding;

        private void Start()
        {
            if (spawnPoint == null || SpawnManager.Instance == null)
            {
                DebugUtility.LogError<SpawnSystemTester>("SpawnPoint ou SpawnManager não configurado.", this);
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
                SpawnManager.Instance.ResetSpawnPoint(spawnPoint);
                DebugUtility.Log<SpawnSystemTester>($"Reset SpawnPoint '{spawnPoint.name}' (Gerenciado: {spawnPoint.useManagerLocking}).", "green", this);
            }
            if (Input.GetKeyDown(unlockKey))
            {
                SpawnManager.Instance.UnlockSpawns(spawnPoint);
                DebugUtility.Log<SpawnSystemTester>($"Desbloqueado SpawnPoint '{spawnPoint.name}'.", "green", this);
            }
            if (Input.GetKeyDown(resetAllKey))
            {
                SpawnManager.Instance.ResetAllSpawnPoints();
                DebugUtility.Log<SpawnSystemTester>("Reset global executado.", "green", this);
            }
            if (Input.GetKeyDown(stopAllKey))
            {
                SpawnManager.Instance.StopAllSpawnPoints();
                DebugUtility.Log<SpawnSystemTester>("Bloqueio global executado (apenas gerenciados).", "yellow", this);
            }
            if (Input.GetKeyDown(stopAllIncludingIndependentKey))
            {
                SpawnManager.Instance.StopAllSpawnPointsIncludingIndependent();
                DebugUtility.Log<SpawnSystemTester>("Bloqueio global executado (incluindo independentes).", "yellow", this);
            }
        }
    }
}