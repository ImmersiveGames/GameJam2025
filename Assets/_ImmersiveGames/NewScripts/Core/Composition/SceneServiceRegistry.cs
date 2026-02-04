using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Core.Composition
{

    public class SceneServiceRegistry : ServiceRegistry
    {
        private readonly Dictionary<string, Dictionary<Type, object>> _sceneServices = new();
        private readonly int _maxSceneServices;
        private readonly SceneServiceCleaner _cleaner;
        public event Action<string> OnSceneServicesCleared;

        public SceneServiceRegistry(int maxSceneServices = 0)
        {
            _maxSceneServices = maxSceneServices;
            _cleaner = new SceneServiceCleaner(this);
        }

        public override void Register<T>(string key, T service, bool allowOverride = false) where T : class
        {
            ValidateKeyOrThrow(key);
            WarnIfSceneNotInBuild(key);

            var type = typeof(T);
            ValidateServiceOrThrow(type, service, key);

            Dictionary<Type, object> services = GetOrCreateServicesForScene(key);
            if (!CanRegisterService(key, type, services, allowOverride))
            {
                return;
            }

            HandleOverrideIfNeeded(key, type, services, allowOverride);
            services[type] = service;
            DebugUtility.LogVerbose(
                typeof(SceneServiceRegistry),
                $"Serviço {type.Name} registrado para a cena {key}.",
                DebugUtility.Colors.Success);
        }

        public override bool TryGet<T>(string key, out T service)
        {
            service = null;
            if (string.IsNullOrEmpty(key))
            {
                DebugUtility.LogError(typeof(SceneServiceRegistry), "TryGet: sceneName é nulo ou vazio.");
                return false;
            }

            if (_sceneServices.TryGetValue(key, out Dictionary<Type, object> services))
            {
                var targetType = typeof(T);
                foreach (KeyValuePair<Type, object> kvp in services)
                {
                    if (targetType.IsAssignableFrom(kvp.Key))
                    {
                        service = (T)kvp.Value;
                        DebugUtility.LogVerbose(typeof(SceneServiceRegistry), $"Serviço {targetType.Name} encontrado para a cena {key} (tipo registrado: {kvp.Key.Name}).");
                        return true;
                    }
                }
            }

            return false;
        }
        public IEnumerable<T> GetAll<T>(string sceneName) where T : class
        {
            if (_sceneServices.TryGetValue(sceneName, out Dictionary<Type, object> sceneServices))
            {
                foreach (object svc in sceneServices.Values)
                {
                    if (svc is T typedService)
                    {
                        yield return typedService;
                    }
                }
            }
        }
        public override void Clear(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                DebugUtility.LogError(typeof(SceneServiceRegistry), "Clear: sceneName é nulo ou vazio.");
                throw new ArgumentNullException(nameof(key));
            }

            if (_sceneServices.TryGetValue(key, out Dictionary<Type, object> services))
            {
                foreach (object service in services.Values)
                {
                    DisposeServiceIfNeeded(service);
                }

                int count = services.Count;
                _sceneServices.Remove(key);
                ReturnDictionaryToPool(services);
                DebugUtility.LogVerbose(
                    typeof(SceneServiceRegistry),
                    $"Removidos {count} serviços para a cena {key}.",
                    DebugUtility.Colors.Success);
                OnSceneServicesCleared?.Invoke(key);
            }
        }

        public override void ClearAll()
        {
            int totalCount = 0;
            foreach (Dictionary<Type, object> services in _sceneServices.Values)
            {
                foreach (object service in services.Values)
                {
                    DisposeServiceIfNeeded(service);
                }

                totalCount += services.Count;
                ReturnDictionaryToPool(services);
            }
            _sceneServices.Clear();
            DebugUtility.LogVerbose(
                typeof(SceneServiceRegistry),
                $"Removidos {totalCount} serviços de todas as cenas.",
                DebugUtility.Colors.Success);
            OnSceneServicesCleared?.Invoke(null);
        }

        public override List<Type> ListServices(string key)
        {
            if (_sceneServices.TryGetValue(key, out Dictionary<Type, object> services))
            {
                return new List<Type>(services.Keys);
            }
            return new List<Type>();
        }

        private bool ValidateSceneName(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = Path.GetFileNameWithoutExtension(path);
                if (name == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        private void ValidateKeyOrThrow(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                DebugUtility.LogError(typeof(SceneServiceRegistry), "Register: sceneName é nulo ou vazio.");
                throw new ArgumentNullException(nameof(key));
            }
        }

        private void WarnIfSceneNotInBuild(string key)
        {
            if (!ValidateSceneName(key))
            {
                DebugUtility.LogWarning(typeof(SceneServiceRegistry), $"Cena '{key}' não encontrada na build.");
            }
        }

        private void ValidateServiceOrThrow(Type type, object service, string key)
        {
            if (!ValidateService(type, service, "RegisterForScene", key))
            {
                throw new ArgumentNullException(nameof(service), $"Serviço nulo para o tipo {type.Name} com chave {key}.");
            }
        }

        private Dictionary<Type, object> GetOrCreateServicesForScene(string key)
        {
            if (!_sceneServices.TryGetValue(key, out Dictionary<Type, object> services))
            {
                services = GetPooledDictionary();
                _sceneServices[key] = services;
            }
            return services;
        }

        private bool CanRegisterService(string key, Type type, Dictionary<Type, object> services, bool allowOverride)
        {
            if (_maxSceneServices <= 0)
            {
                return true;
            }

            HashSet<Type> distinctTypes = services.Keys.ToHashSet();
            if (distinctTypes.Contains(type) && !allowOverride)
            {
                DebugUtility.LogWarning(typeof(SceneServiceRegistry), $"Serviço {type.Name} já registrado para a cena {key}. Registro ignorado.");
                return false;
            }

            if (!distinctTypes.Contains(type) && distinctTypes.Count >= _maxSceneServices)
            {
                DebugUtility.LogWarning(typeof(SceneServiceRegistry), $"Limite de {_maxSceneServices} serviços distintos atingido para a cena {key}. Registro de {type.Name} ignorado.");
                return false;
            }

            return true;
        }

        private void HandleOverrideIfNeeded(string key, Type type, Dictionary<Type, object> services, bool allowOverride)
        {
            if (services.TryGetValue(type, out object existing) && allowOverride)
            {
                DisposeServiceIfNeeded(existing);
                DebugUtility.LogWarning(typeof(SceneServiceRegistry), $"Sobrescrevendo serviço {type.Name} para a cena {key}.");
            }
        }

        public void Dispose()
        {
            _cleaner?.Dispose();
        }
    }
}
