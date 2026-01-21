using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actions;
using UnityEngine;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using _ImmersiveGames.NewScripts.Infrastructure.State;

namespace _ImmersiveGames.Tools
{
    // Small smoke test runner to exercise WorldSpawnServiceFactory and spawn/despawn flow.
    [DisallowMultipleComponent]
    public class WorldSpawnSmokeRunner : MonoBehaviour
    {
        [Tooltip("WorldDefinition asset with spawn entries to test")]
        public WorldDefinition worldDefinition;

        [Tooltip("Transform used as world root for instantiated prefabs")]
        public Transform worldRoot;

        private readonly WorldSpawnServiceFactory _factory = new();
        private IDependencyProvider _provider;
        private IActorRegistry _actorRegistry;
        private readonly List<IWorldSpawnService> _services = new();

        private void Awake()
        {
            if (worldRoot == null)
            {
                var go = new GameObject("WorldRoot");
                worldRoot = go.transform;
            }

            // minimal test provider
            _provider = new TestDependencyProvider();

            // register minimal implementations if needed (IUniqueIdFactory/IStateDependentService)
            _provider.RegisterGlobal<IUniqueIdFactory>(new SimpleUniqueIdFactory());
            _provider.RegisterGlobal<IStateDependentService>(new SimpleStateDependentService());

            _actorRegistry = new ActorRegistry();
        }

        private async void Start()
        {
            await SpawnAllAsync();
        }

        private async void OnDestroy()
        {
            await DespawnAllAsync();
        }

        /// <summary>
        /// Public method to spawn all services (can be called from editor buttons/context menu).
        /// </summary>
        public async Task SpawnAllAsync()
        {
            if (worldDefinition == null)
            {
                Debug.LogWarning("WorldDefinition não configurado no WorldSpawnSmokeRunner.");
                return;
            }

            // create services
            foreach (var entry in worldDefinition.Entries)
            {
                if (!entry.Enabled)
                {
                    continue;
                }

                var service = _factory.Create(entry, _provider, _actorRegistry, new WorldSpawnContext(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, worldRoot));
                if (service != null)
                {
                    _services.Add(service);
                    await service.SpawnAsync();
                }
            }

            Debug.Log($"SmokeRunner: spawn complete. Registry count: {_actorRegistry.Count}");
        }

        /// <summary>
        /// Public method to despawn all services (can be called from editor buttons/context menu).
        /// </summary>
        public async Task DespawnAllAsync()
        {
            foreach (var s in _services)
            {
                if (s != null)
                {
                    await s.DespawnAsync();
                }
            }

            _services.Clear();

            Debug.Log($"SmokeRunner: despawn complete. Registry count: {_actorRegistry.Count}");
        }

        [ContextMenu("Spawn All (smoke)")]
        private void SpawnAllContext()
        {
            // fire-and-forget; intended for editor context menu use
            _ = SpawnAllAsync();
        }

        [ContextMenu("Despawn All (smoke)")]
        private void DespawnAllContext()
        {
            _ = DespawnAllAsync();
        }

        // Minimal in-file test implementations to avoid coupling to project DI.
        private class TestDependencyProvider : IDependencyProvider
        {
            private readonly Dictionary<System.Type, object> _globals = new();

            public void RegisterGlobal<T>(T service, bool allowOverride = false) where T : class
            {
                var t = typeof(T);
                if (!allowOverride && _globals.ContainsKey(t))
                {
                    return;
                }
                _globals[t] = service;
            }

            public bool TryGetGlobal<T>(out T service) where T : class
            {
                if (_globals.TryGetValue(typeof(T), out object o) && o is T t)
                {
                    service = t;
                    return true;
                }

                service = null;
                return false;
            }

            public void RegisterForObject<T>(string objectId, T service, bool allowOverride = false) where T : class { }
            public bool TryGetForObject<T>(string objectId, out T service) where T : class { service = null; return false; }
            public void RegisterForScene<T>(string sceneName, T service, bool allowOverride = false) where T : class { }
            public bool TryGetForScene<T>(string sceneName, out T service) where T : class { service = null; return false; }
            public void GetAllForScene<T>(string sceneName, List<T> services) where T : class { }
            public bool TryGet<T>(out T service, string objectId = null) where T : class { service = null; return false; }
            public void GetAll<T>(List<T> services) where T : class { }
            public void InjectDependencies(object target, string objectId = null) { }
            public void ClearSceneServices(string sceneName) { }
            public void ClearAllSceneServices() { }
            public void ClearObjectServices(string objectId) { }
            public void ClearAllObjectServices() { }
            public void ClearGlobalServices() { _globals.Clear(); }
            public List<System.Type> ListServicesForObject(string objectId) { return new List<System.Type>(); }
            public List<System.Type> ListServicesForScene(string sceneName) { return new List<System.Type>(); }
            public List<System.Type> ListGlobalServices() { return new List<System.Type>(_globals.Keys); }
        }

        // Very small UniqueIdFactory for testing
        private class SimpleUniqueIdFactory : IUniqueIdFactory
        {
            private readonly Dictionary<string, int> _counts = new(System.StringComparer.Ordinal);
            private int _counter;

            public string GenerateId(GameObject owner, string prefix = null)
            {
                _counter++;
                string baseName = owner != null ? owner.name : "NullOwner";

                int c = _counts.GetValueOrDefault(baseName, 0);
                c++;
                _counts[baseName] = c;

                string id = $"test-A_{_counter}_{baseName}_{c}";
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    id = $"{id}_{prefix}";
                }
                return id;
            }

            public int GetInstanceCount(string actorName)
            {
                return string.IsNullOrEmpty(actorName) ? 0 : _counts.GetValueOrDefault(actorName, 0);
            }
        }

        // Minimal state-dependent service stub for smoke tests
        private class SimpleStateDependentService : IStateDependentService
        {
            public bool CanExecuteAction(ActionType action)
            {
                return true;
            }

            public bool IsGameActive()
            {
                return true;
            }

            public void Dispose() { }
        }
    }
}
