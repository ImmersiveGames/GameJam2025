using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private List<SpawnPoint> spawnPoints; // Lista de SpawnPoints gerenciados
        [SerializeField] private int maxSpawns = 3; // Máximo de tentativas de spawn por SpawnPoint
        [SerializeField] private float cooldownAfterExhausted = 5f; // Cooldown após esgotamento

        private Dictionary<SpawnPoint, int> spawnCounts; // Contador de spawns por SpawnPoint
        private Dictionary<SpawnPoint, float> cooldownEndTimes; // Tempo de cooldown por SpawnPoint
        private EventBinding<ObjectSpawnedEvent> spawnEventBinding; // Escuta eventos de spawn

        private void Awake()
        {
            spawnCounts = new Dictionary<SpawnPoint, int>();
            cooldownEndTimes = new Dictionary<SpawnPoint, float>();
            foreach (var point in spawnPoints)
            {
                spawnCounts[point] = 0;
                cooldownEndTimes[point] = 0f;
            }
            spawnEventBinding = new EventBinding<ObjectSpawnedEvent>(OnObjectSpawned);
        }

        private void OnEnable()
        {
            EventBus<ObjectSpawnedEvent>.Register(spawnEventBinding);
        }

        private void OnDisable()
        {
            EventBus<ObjectSpawnedEvent>.Unregister(spawnEventBinding);
        }

        private void Update()
        {
            foreach (var point in spawnPoints)
            {
                // Verifica cooldown
                if (Time.time < cooldownEndTimes[point])
                {
                    continue;
                }

                // Verifica se atingiu maxSpawns ou está esgotado
                if (spawnCounts[point] >= maxSpawns || IsPoolExhausted(point))
                {
                    StartCooldown(point);
                    continue;
                }

                // Verifica se objetos retornaram ao pool
                if (HasAvailableObjects(point))
                {
                    point.ResetSpawnPoint(); // Reativa o SpawnPoint
                }
            }
        }

        private void OnObjectSpawned(ObjectSpawnedEvent evt)
        {
            // Incrementa contador para o SpawnPoint correspondente
            foreach (var point in spawnPoints)
            {
                if (evt.Object.GetGameObject().name.Contains(point.name))
                {
                    spawnCounts[point]++;
                    break;
                }
            }
        }

        private bool IsPoolExhausted(SpawnPoint point)
        {
            var pool = PoolManager.Instance.GetPool(point.name);
            return pool == null || pool.GetAvailableCount() == 0;
        }

        private bool HasAvailableObjects(SpawnPoint point)
        {
            var pool = PoolManager.Instance.GetPool(point.name);
            return pool != null && pool.GetAvailableCount() > 0;
        }

        private void StartCooldown(SpawnPoint point)
        {
            cooldownEndTimes[point] = Time.time + cooldownAfterExhausted;
            DebugUtility.Log<SpawnManager>($"Cooldown iniciado para SpawnPoint '{point.name}' por {cooldownAfterExhausted}s.", "yellow", this);
        }

        public void ResetSpawnPoint(SpawnPoint point)
        {
            spawnCounts[point] = 0;
            cooldownEndTimes[point] = 0f;
            point.ResetSpawnPoint();
            DebugUtility.Log<SpawnManager>($"SpawnPoint '{point.name}' resetado.", "green", this);
        }
    }
}