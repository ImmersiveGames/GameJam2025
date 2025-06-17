using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DefaultExecutionOrder(-1)]
    public class SpawnManager : MonoBehaviour
    {
        public static SpawnManager Instance { get; private set; }

        [SerializeField] private int maxSpawnsPerPoint = 5;

        private readonly List<SpawnPoint> _allSpawnPointsPool = new();
        private readonly Dictionary<SpawnPoint, ManagedSpawnData> _managedSpawnPoints = new();
        private bool _isResetting;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterSpawnPoint(SpawnPoint point, bool useManagerLocking)
        {
            if (!point || _allSpawnPointsPool.Contains(point))
                return;
            _allSpawnPointsPool.Add(point);
            if (useManagerLocking && !_managedSpawnPoints.ContainsKey(point))
            {
                _managedSpawnPoints.Add(point, new ManagedSpawnData());
            }
        }

        public bool CanSpawn(SpawnPoint point)
        {
            if (!_allSpawnPointsPool.Contains(point))
            {
                DebugUtility.LogError<SpawnManager>($"SpawnPoint '{point.name}' não registrado.", this);
                return false;
            }
            if (!_managedSpawnPoints.TryGetValue(point, out var data))
            {
                return true; // Independente
            }
            return !data.isLocked && data.spawnCount < maxSpawnsPerPoint;
        }

        public void RegisterSpawn(SpawnPoint point)
        {
            if (!_managedSpawnPoints.TryGetValue(point, out var data))
                return;
            data.spawnCount++;
            if (data.spawnCount >= maxSpawnsPerPoint)
            {
                LockSpawns(point);
            }
        }

        public void LockSpawns(SpawnPoint point)
        {
            if (!_managedSpawnPoints.TryGetValue(point, out var data))
                return;
            data.isLocked = true;
            DebugUtility.Log<SpawnManager>($"SpawnPoint '{point.name}' bloqueado.", "yellow", this);
        }

        public void UnlockSpawns(SpawnPoint point)
        {
            if (!_managedSpawnPoints.TryGetValue(point, out var data))
                return;
            data.isLocked = false;
            data.spawnCount = 0;
            DebugUtility.Log<SpawnManager>($"SpawnPoint '{point.name}' desbloqueado.", "green", this);
        }

        public void ResetSpawnPoint(SpawnPoint point)
        {
            if (!_allSpawnPointsPool.Contains(point))
                return;
            if (_managedSpawnPoints.TryGetValue(point, out var data))
            {
                data.spawnCount = 0;
                data.isLocked = false;
            }
            var pool = PoolManager.Instance.GetPool(point.GetPoolKey());
            if (pool)
            {
                var activeObjects = pool.GetActiveObjects().ToList();
                foreach (var obj in activeObjects)
                {
                    var pooledObject = obj as PooledObject;
                    if (pooledObject && pooledObject.Lifetime == 0)
                    {
                        DebugUtility.Log<SpawnManager>($"Objeto '{obj.GetGameObject().name}' com lifetime=0 ignorado no reset.", "yellow", this);
                        continue;
                    }
                    obj.Deactivate();
                }
            }
            point.TriggerReset();
            DebugUtility.Log<SpawnManager>($"SpawnPoint '{point.name}' resetado.", "green", this);
        }

        public void ResetAllSpawnPoints()
        {
            if (_isResetting)
                return;
            _isResetting = true;
            try
            {
                foreach (var point in _allSpawnPointsPool)
                {
                    ResetSpawnPoint(point);
                }
                DebugUtility.Log<SpawnManager>("Todos os SpawnPoints resetados.", "green", this);
            }
            finally
            {
                _isResetting = false;
            }
        }

        public void StopAllSpawnPoints()
        {
            foreach (var point in _allSpawnPointsPool.Where(point => _managedSpawnPoints.ContainsKey(point)))
            {
                LockSpawns(point);
            }
            DebugUtility.Log<SpawnManager>("SpawnPoints gerenciados parados.", "yellow", this);
        }

        public void StopAllSpawnPointsIncludingIndependent()
        {
            foreach (var point in _allSpawnPointsPool)
            {
                if (_managedSpawnPoints.ContainsKey(point))
                    LockSpawns(point);
                point.SetTriggerActive(false);
                DebugUtility.Log<SpawnManager>($"SpawnPoint '{point.name}' parado (global).", "yellow", this);
            }
            DebugUtility.Log<SpawnManager>("Todos os SpawnPoints parados.", "yellow", this);
        }
    }

    [System.Serializable]
    public class ManagedSpawnData
    {
        public int spawnCount;
        public bool isLocked;
    }
}