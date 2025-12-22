using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Debug;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;

namespace _ImmersiveGames.NewScripts.Infrastructure.DI
{
    [DisallowMultipleComponent]
    public class DependencyManager : RegulatorSingleton<DependencyManager>, IDependencyProvider
    {
        public static IDependencyProvider Provider => Instance;

        [SerializeField] private int maxSceneServices = 8;

        private DependencyInjector _injector;
        private ObjectServiceRegistry _objectRegistry;
        private SceneServiceRegistry _sceneRegistry;
        private GlobalServiceRegistry _globalRegistry;

        public bool IsInTestMode { get; set; }

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            if (instance != this) return;

            _objectRegistry = new ObjectServiceRegistry();
            _sceneRegistry = new SceneServiceRegistry(maxSceneServices);
            _globalRegistry = new GlobalServiceRegistry();
            _injector = new DependencyInjector(_objectRegistry, _sceneRegistry, _globalRegistry);

            DebugUtility.LogVerbose(
                typeof(DependencyManager),
                $"DependencyManager inicializado ({gameObject.scene.name}).",
                DebugUtility.Colors.CrucialInfo);
        }

        public void RegisterGlobal<T>(T service, bool allowOverride = false) where T : class =>
            _globalRegistry.Register(null, service, allowOverride);

        public bool TryGetGlobal<T>(out T service) where T : class => _globalRegistry.TryGet(null, out service);

        public void RegisterForObject<T>(string objectId, T service, bool allowOverride = false) where T : class
        {
            if (string.IsNullOrEmpty(objectId))
                throw new ArgumentNullException(nameof(objectId), "objectId é nulo ou vazio.");
            _objectRegistry.Register(objectId, service, allowOverride);
        }

        public bool TryGetForObject<T>(string objectId, out T service) where T : class => _objectRegistry.TryGet(objectId, out service);

        public void RegisterForScene<T>(string sceneName, T service, bool allowOverride = false) where T : class =>
            _sceneRegistry.Register(sceneName, service, allowOverride);

        public bool TryGetForScene<T>(string sceneName, out T service) where T : class => _sceneRegistry.TryGet(sceneName, out service);

        public void GetAllForScene<T>(string sceneName, List<T> services) where T : class
        {
            if (string.IsNullOrWhiteSpace(sceneName) || services == null)
            {
                return;
            }

            services.Clear();

            foreach (T service in _sceneRegistry.GetAll<T>(sceneName))
            {
                services.Add(service);
            }
        }

        public bool TryGet<T>(out T service, string objectId = null) where T : class
        {
            service = null;
            if (objectId != null && _objectRegistry.TryGet(objectId, out service) ||
                _sceneRegistry.TryGet(SceneManager.GetActiveScene().name, out service) ||
                (objectId == null && _globalRegistry.TryGet(null, out service)))
            {
                DebugUtility.LogVerbose(typeof(DependencyManager), $"Serviço {typeof(T).Name} encontrado.");
                return true;
            }
            return false;
        }

        public void GetAll<T>(List<T> services) where T : class
        {
            services.Clear();
            services.AddRange(_globalRegistry.GetAll<T>());
            services.AddRange(_sceneRegistry.GetAll<T>(SceneManager.GetActiveScene().name));
            foreach (string objectId in _objectRegistry.GetAllObjectIds())
            {
                if (_objectRegistry.TryGet(objectId, out T service))
                    services.Add(service);
            }
            DebugUtility.LogVerbose(typeof(DependencyManager), $"Recuperados {services.Count} serviços do tipo {typeof(T).Name}.");
        }

        public void InjectDependencies(object target, string objectId = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            _injector.InjectDependencies(target, objectId);
        }

        public void ClearSceneServices(string sceneName) => _sceneRegistry.Clear(sceneName);
        public void ClearAllSceneServices() => _sceneRegistry.ClearAll();
        public void ClearObjectServices(string objectId) => _objectRegistry.Clear(objectId);
        public void ClearAllObjectServices() => _objectRegistry.ClearAll();
        public void ClearGlobalServices() => _globalRegistry.Clear(null);

        public List<Type> ListServicesForObject(string objectId) => _objectRegistry.ListServices(objectId);
        public List<Type> ListServicesForScene(string sceneName) => _sceneRegistry.ListServices(sceneName);
        public List<Type> ListGlobalServices() => _globalRegistry.ListServices(null);

        protected void OnDestroy()
        {
            if (instance != this) return;
            ClearAllObjectServices();
            ClearAllSceneServices();
            ClearGlobalServices();
            StopAllCoroutines();
            DebugUtility.LogVerbose(
                typeof(DependencyManager),
                "DependencyManager destruído e serviços limpos.",
                DebugUtility.Colors.Success);
        }

        private void OnApplicationQuit()
        {
            if (instance != this) return;
            ClearAllObjectServices();
            ClearAllSceneServices();
            ClearGlobalServices();
            DebugUtility.LogVerbose(
                typeof(DependencyManager),
                "Serviços limpos no fechamento do jogo.",
                DebugUtility.Colors.Success);
        }
    }
}
