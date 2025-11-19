// Arquivo: DependencyManager.cs (versão melhorada)
using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;

namespace _ImmersiveGames.Scripts.Utils.DependencySystems
{
    [DisallowMultipleComponent]
    public class DependencyManager : RegulatorSingleton<DependencyManager>, IDependencyProvider
    {
        public static IDependencyProvider Provider => Instance;

        [SerializeField] private int maxSceneServices = 2;

        private DependencyInjector _injector;
        private ObjectServiceRegistry _objectRegistry;
        private SceneServiceRegistry _sceneRegistry;
        private GlobalServiceRegistry _globalRegistry;

        public bool IsInTestMode { get; set; }

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            if (instance != this) return;

            _objectRegistry = new();
            _sceneRegistry = new(maxSceneServices);
            _globalRegistry = new();
            _injector = new(_objectRegistry, _sceneRegistry, _globalRegistry);

            DebugUtility.Log(
                typeof(DependencyManager),
                $"DependencyManager inicializado ({gameObject.scene.name}).",
                DebugUtility.Colors.CrucialInfo);
        }

        // Métodos da interface (apenas encaminham)
        public void RegisterGlobal<T>(T service) where T : class => _globalRegistry.Register(null, service);
        public bool TryGetGlobal<T>(out T service) where T : class => _globalRegistry.TryGet(null, out service);

        public void RegisterForObject<T>(string objectId, T service) where T : class
        {
            if (string.IsNullOrEmpty(objectId))
                throw new ArgumentNullException(nameof(objectId), "objectId é nulo ou vazio.");
            _objectRegistry.Register(objectId, service);
        }

        public bool TryGetForObject<T>(string objectId, out T service) where T : class => _objectRegistry.TryGet(objectId, out service);

        public void RegisterForScene<T>(string sceneName, T service, bool allowOverride = false) where T : class =>
            _sceneRegistry.Register(sceneName, service, allowOverride);

        public bool TryGetForScene<T>(string sceneName, out T service) where T : class => _sceneRegistry.TryGet(sceneName, out service);

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
            DebugUtility.Log(
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
            DebugUtility.Log(
                typeof(DependencyManager),
                "Serviços limpos no fechamento do jogo.",
                DebugUtility.Colors.Success);
        }
    }
}