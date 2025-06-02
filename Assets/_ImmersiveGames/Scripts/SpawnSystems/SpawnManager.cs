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

        private readonly List<SpawnPoint> _allSpawnPool = new();
        private readonly Dictionary<SpawnPoint, ManagedSpawnData> _managedSpawnPoints = new();
        private bool _isResetting;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterSpawnPoint(SpawnPoint point, bool useManagerLocking)
        {
            if (point == null || _allSpawnPool.Contains(point))
                return;
            _allSpawnPool.Add(point);
            if (useManagerLocking && !_managedSpawnPoints.ContainsKey(point))
            {
                _managedSpawnPoints.Add(point, new ManagedSpawnData());
            }
        }

        public bool CanSpawn(SpawnPoint point)
        {
            if (!_allSpawnPool.Contains(point))
            {
                DebugUtility.LogError<SpawnManager>($"SpawnPoint '{point.name}' não registrado.", this);
                return false;
            }
            if (!_managedSpawnPoints.ContainsKey(point))
            {
                return true; // Independente
            }
            var data = _managedSpawnPoints[point];
            return !data.isLocked && data.spawnCount < maxSpawnsPerPoint;
        }

        public void RegisterSpawn(SpawnPoint point)
        {
            if (!_managedSpawnPoints.ContainsKey(point))
                return;
            var data = _managedSpawnPoints[point];
            data.spawnCount++;
            if (data.spawnCount >= maxSpawnsPerPoint)
            {
                LockSpawns(point);
            }
        }

        public void LockSpawns(SpawnPoint point)
        {
            if (!_managedSpawnPoints.ContainsKey(point))
                return;
            var data = _managedSpawnPoints[point];
            data.isLocked = true;
            DebugUtility.Log<SpawnManager>($"SpawnPoint '{point.name}' bloqueado.", "yellow", this);
        }

        public void UnlockSpawns(SpawnPoint point)
        {
            if (!_managedSpawnPoints.ContainsKey(point))
                return;
            var data = _managedSpawnPoints[point];
            data.isLocked = false;
            data.spawnCount = 0;
            DebugUtility.Log<SpawnManager>($"SpawnPoint '{point.name}' desbloqueado.", "green", this);
        }

        public void ResetSpawnPoint(SpawnPoint point)
        {
            if (!_allSpawnPool.Contains(point))
                return;
            if (_managedSpawnPoints.ContainsKey(point))
            {
                var data = _managedSpawnPoints[point];
                data.spawnCount = 0;
                data.isLocked = false;
            }
            var pool = PoolManager.Instance.GetPool(point.GetPoolKey());
            if (pool != null)
            {
                var activeObjects = pool.GetActiveObjects().ToList();
                foreach (var obj in activeObjects)
                {
                    var pooledObject = obj as PooledObject;
                    if (pooledObject != null && pooledObject.Lifetime == 0)
                    {
                        DebugUtility.Log<SpawnManager>($"Objeto '{obj.GetGameObject().name}' com lifetime=0 ignorado no reset.", "yellow", this);
                        continue;
                    }
                    var mover = obj.GetGameObject().GetComponent<PooledObjectMover>();
                    if (mover)
                        mover.StopMovement();
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
                foreach (var point in _allSpawnPool)
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
            foreach (var point in _allSpawnPool)
            {
                if (_managedSpawnPoints.ContainsKey(point))
                    LockSpawns(point);
            }
            DebugUtility.Log<SpawnManager>("SpawnPoints gerenciados parados.", "yellow", this);
        }

        public void StopAllSpawnPointsIncludingIndependent()
        {
            foreach (var point in _allSpawnPool)
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