using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DefaultExecutionOrder(-1)]
    public class SpawnManager : MonoBehaviour
    {
        public static SpawnManager Instance { get; private set; }
        private readonly List<SpawnPoint> _allSpawnPoints = new List<SpawnPoint>();
        private readonly Dictionary<SpawnPoint, ManagedSpawnData> _managedSpawnPoints = new Dictionary<SpawnPoint, ManagedSpawnData>();
        private readonly Dictionary<SpawnPoint, int> _spawnCounts = new Dictionary<SpawnPoint, int>();
        private int _maxSpawnsPerPoint = 100;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterSpawnPoint(SpawnPoint point, bool useManagerLocking)
        {
            if (point == null || _allSpawnPoints.Contains(point))
                return;

            _allSpawnPoints.Add(point);
            if (useManagerLocking)
                _managedSpawnPoints[point] = new ManagedSpawnData();

            var poolManager = PoolManager.Instance;
            if (poolManager && point.GetSpawnData()?.PoolableData != null)
            {
                poolManager.RegisterPool(point.GetSpawnData().PoolableData);
            }
            DebugUtility.Log<SpawnManager>($"SpawnPoint '{point.name}' registrado.", "green", this);
        }

        public void RegisterSpawn(SpawnPoint point)
        {
            if (!_managedSpawnPoints.ContainsKey(point)) return;

            if (!_spawnCounts.ContainsKey(point))
                _spawnCounts[point] = 0;

            _spawnCounts[point]++;
            if (_spawnCounts[point] >= _maxSpawnsPerPoint)
            {
                _managedSpawnPoints[point].IsLocked = true;
                EventBus<SpawnPointLockedEvent>.Raise(new SpawnPointLockedEvent(point));
            }
        }

        public bool CanSpawn(SpawnPoint point)
        {
            if (!_allSpawnPoints.Contains(point))
                return false;

            if (!_managedSpawnPoints.ContainsKey(point))
                return true;

            return !_managedSpawnPoints[point].IsLocked;
        }

        public bool IsLocked(SpawnPoint point)
        {
            return _managedSpawnPoints.ContainsKey(point) && _managedSpawnPoints[point].IsLocked;
        }

        public int GetSpawnCount(SpawnPoint point)
        {
            return _spawnCounts.ContainsKey(point) ? _spawnCounts[point] : 0;
        }

        public void ResetSpawnPoint(SpawnPoint point)
        {
            if (!_allSpawnPoints.Contains(point)) return;
            DebugUtility.Log<SpawnManager>($"Resetando SpawnPoint '{point.name}'.", "blue", this);
            EventBus<SpawnPointResetEvent>.Raise(new SpawnPointResetEvent(point));
        }

        public void ResetAllSpawnPoints()
        {
            // Criar cópia para evitar modificação durante iteração
            var points = _allSpawnPoints.ToList();
            foreach (var point in points)
            {
                ResetSpawnPoint(point);
            }
        }

        private void Update()
        {
            // Exemplo: evitar modificações em Update
            var points = _allSpawnPoints.ToList();
            foreach (var point in points)
            {
                if (point == null) continue;
                // Lógica de Update, se houver
            }
        }

        private void OnDestroy()
        {
            _allSpawnPoints.Clear();
            _managedSpawnPoints.Clear();
            _spawnCounts.Clear();
        }
    }

    public class ManagedSpawnData
    {
        public bool IsLocked { get; set; }
    }
}