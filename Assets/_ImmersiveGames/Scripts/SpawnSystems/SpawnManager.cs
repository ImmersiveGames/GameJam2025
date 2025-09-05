using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using System.Linq;
using _ImmersiveGames.Scripts.SpawnSystems.Events;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DefaultExecutionOrder(-1), DebugLevel(DebugLevel.Warning)]
    public class SpawnManager : MonoBehaviour
    {
        public static SpawnManager Instance { get; private set; }

        [SerializeField, Tooltip("Número máximo de spawns por SpawnPoint antes de ser bloqueado.")]
        private int maxSpawnsPerPoint = 100;

        private readonly List<SpawnPoint> _allSpawnPoints = new();
        private readonly Dictionary<SpawnPoint, ManagedSpawnData> _managedSpawnPoints = new();
        private readonly Dictionary<SpawnPoint, int> _spawnCounts = new();
        //private EventBinding<PoolRestoredEvent> _poolRestoredBinding;

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

            //_poolRestoredBinding = new EventBinding<PoolRestoredEvent>(HandlePoolRestored);
        }

       

       

        public void RegisterSpawnPoint(SpawnPoint point, bool useManagerLocking)
        {
            if (point == null || _allSpawnPoints.Contains(point))
            {
                DebugUtility.LogVerbose<SpawnManager>($"Tentativa de registrar SpawnPoint nulo ou duplicado '{point?.name}'.");
                return;
            }

            _allSpawnPoints.Add(point);
            if (useManagerLocking)
                _managedSpawnPoints[point] = new ManagedSpawnData();

            var poolManager = PoolManager.Instance;
            if (poolManager && point.GetPoolableData() != null)
            {
                //poolManager.RegisterPool(point.GetPoolableData());
                DebugUtility.LogVerbose<SpawnManager>($"Pool '{point.GetPoolKey()}' registrado para SpawnPoint '{point.name}'.", "blue", this);
            }
            else
            {
                DebugUtility.LogWarning<SpawnManager>($"PoolManager ou PoolableData não encontrado para SpawnPoint '{point.name}'.");
            }

            DebugUtility.LogVerbose<SpawnManager>($"SpawnPoint '{point.name}' registrado com locking={(useManagerLocking ? "ativado" : "desativado")}.", "green", this);
        }

        public void RegisterSpawn(SpawnPoint point)
        {
            if (!_managedSpawnPoints.ContainsKey(point))
                return;

            _spawnCounts.TryAdd(point, 0);

            _spawnCounts[point]++;
            if (_spawnCounts[point] >= maxSpawnsPerPoint)
            {
                _managedSpawnPoints[point].IsLocked = true;
                EventBus<SpawnPointLockedEvent>.Raise(new SpawnPointLockedEvent(point));
                DebugUtility.LogVerbose<SpawnManager>($"SpawnPoint '{point.name}' bloqueado após atingir {maxSpawnsPerPoint} spawns.", "yellow", this);
            }
        }

        public bool CanSpawn(SpawnPoint point)
        {
            if (!_allSpawnPoints.Contains(point))
            {
                DebugUtility.LogWarning<SpawnManager>($"SpawnPoint '{point?.name}' não registrado.");
                return false;
            }

            if (!_managedSpawnPoints.TryGetValue(point, out var spawnPoint))
                return true;

            return !spawnPoint.IsLocked;
        }

        public bool IsLocked(SpawnPoint point)
        {
            return _managedSpawnPoints.ContainsKey(point) && _managedSpawnPoints[point].IsLocked;
        }

        public int GetSpawnCount(SpawnPoint point)
        {
            return _spawnCounts.GetValueOrDefault(point, 0);
        }

        public void ResetSpawnPoint(SpawnPoint point)
        {
            if (!_allSpawnPoints.Contains(point))
            {
                DebugUtility.LogWarning<SpawnManager>($"Tentativa de resetar SpawnPoint não registrado '{point?.name}'.");
                return;
            }

            if (_managedSpawnPoints.TryGetValue(point, out var spawnPoint))
            {
                spawnPoint.IsLocked = false;
                _spawnCounts[point] = 0;
            }

            DebugUtility.LogVerbose<SpawnManager>($"Resetando SpawnPoint '{point.name}'.", "blue", this);
            EventBus<SpawnPointResetEvent>.Raise(new SpawnPointResetEvent(point));
        }
        public void DeactivateAllTriggers()
        {
            foreach (var point in _allSpawnPoints)
            {
                point.SetTriggerActive(false);
            }
            DebugUtility.LogVerbose<SpawnManager>("Todos os triggers foram desativados.", "yellow", this);
        }

        public void ResetAllSpawnPoints()
        {
            var points = _allSpawnPoints.ToList();
            foreach (var point in points)
            {
                ResetSpawnPoint(point);
            }
            DebugUtility.LogVerbose<SpawnManager>("Todos os SpawnPoints foram resetados.", "blue", this);
        }

        /*private void HandlePoolRestored(PoolRestoredEvent evt)
        {
            var points = _managedSpawnPoints.Keys.ToList();
            foreach (var point in points)
            {
                if (point.GetPoolKey() == evt.PoolKey && IsLocked(point))
                {
                    _managedSpawnPoints[point].IsLocked = false;
                    _spawnCounts[point] = 0;
                    EventBus<SpawnPointUnlockedEvent>.Raise(new SpawnPointUnlockedEvent(point));
                    DebugUtility.LogVerbose<SpawnManager>($"SpawnPoint '{point.name}' desbloqueado devido à restauração do pool '{evt.PoolKey}'.", "green", this);
                }
            }
        }*/

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            _allSpawnPoints.Clear();
            _managedSpawnPoints.Clear();
            _spawnCounts.Clear();

            // Notificar PoolManager, se necessário
            var poolManager = PoolManager.Instance;
            if (poolManager)
            {
                foreach (var point in _allSpawnPoints)
                {
                    if (point.GetPoolableData() != null)
                    {
                        // Opcional: Desregistrar pools, se o PoolManager suportar
                        // poolManager.UnregisterPool(point.GetPoolKey());
                    }
                }
            }

            DebugUtility.LogVerbose<SpawnManager>("SpawnManager destruído e limpo.", "yellow", this);
        }
    }

    public class ManagedSpawnData
    {
        public bool IsLocked { get; set; }
    }
}