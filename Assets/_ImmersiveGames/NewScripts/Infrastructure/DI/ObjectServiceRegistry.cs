using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
namespace _ImmersiveGames.NewScripts.Infrastructure.DI
{
    
    public class ObjectServiceRegistry : ServiceRegistry
    {
        private readonly Dictionary<string, Dictionary<Type, object>> _objectServices = new();

        public override void Register<T>(string key, T service, bool allowOverride = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                DebugUtility.LogError(typeof(ObjectServiceRegistry), "Register: objectId é nulo ou vazio.");
                throw new ArgumentNullException(nameof(key));
            }
            if (!ValidateService(typeof(T), service, "RegisterForObject", key))
            {
                throw new ArgumentNullException(nameof(service), $"Serviço nulo para o tipo {typeof(T).Name} com chave {key}.");
            }

            if (!_objectServices.TryGetValue(key, out Dictionary<Type, object> services))
            {
                services = GetPooledDictionary();
                _objectServices[key] = services;
            }

            var type = typeof(T);
            if (services.TryGetValue(type, out object existing) && !allowOverride)
            {
                DebugUtility.LogError(typeof(ObjectServiceRegistry), $"Serviço {type.Name} já registrado para o ID {key}.");
                throw new InvalidOperationException($"Serviço {type.Name} já registrado para o ID {key}.");
            }

            if (allowOverride && existing != null && !ReferenceEquals(existing, service))
            {
                DisposeServiceIfNeeded(existing);
            }

            services[type] = service;
            DebugUtility.LogVerbose(
                typeof(ObjectServiceRegistry),
                $"Serviço {type.Name} registrado para o ID {key}.",
                DebugUtility.Colors.Success);
        }

        public override bool TryGet<T>(string key, out T service)
        {
            service = null;
            if (string.IsNullOrEmpty(key))
            {
                DebugUtility.LogError(typeof(ObjectServiceRegistry), "TryGet: objectId é nulo ou vazio.");
                return false;
            }

            if (_objectServices.TryGetValue(key, out Dictionary<Type, object> services))
            {
                var targetType = typeof(T);
                foreach (KeyValuePair<Type, object> kvp in services)
                {
                    if (targetType.IsAssignableFrom(kvp.Key))
                    {
                        service = (T)kvp.Value;
                        DebugUtility.LogVerbose(typeof(ObjectServiceRegistry), $"Serviço {targetType.Name} encontrado para o ID {key} (tipo registrado: {kvp.Key.Name}).");
                        return true;
                    }
                }
            }

            return false;
        }
        public IEnumerable<string> GetAllObjectIds()
        {
            return _objectServices.Keys;
        }
        public override void Clear(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                DebugUtility.LogError(typeof(ObjectServiceRegistry), "Clear: objectId é nulo ou vazio.");
                throw new ArgumentNullException(nameof(key));
            }

            if (_objectServices.TryGetValue(key, out Dictionary<Type, object> services))
            {
                foreach (object service in services.Values)
                {
                    DisposeServiceIfNeeded(service);
                }

                int count = services.Count;
                _objectServices.Remove(key);
                ReturnDictionaryToPool(services);
                DebugUtility.LogVerbose(
                    typeof(ObjectServiceRegistry),
                    $"Removidos {count} serviços para o ID {key}.",
                    DebugUtility.Colors.Success);
            }
            DebugUtility.LogVerbose(typeof(ObjectServiceRegistry), $"Removidos serviços para o ID {key}.");
        }

        public override void ClearAll()
        {
            int totalCount = 0;
            foreach (Dictionary<Type, object> services in _objectServices.Values)
            {
                foreach (object service in services.Values)
                {
                    DisposeServiceIfNeeded(service);
                }

                totalCount += services.Count;
                ReturnDictionaryToPool(services);
            }
            _objectServices.Clear();
            DebugUtility.LogVerbose(
                typeof(ObjectServiceRegistry),
                $"Removidos {totalCount} serviços de todos os objetos.",
                DebugUtility.Colors.Success);
        }

        public override List<Type> ListServices(string key)
        {
            if (_objectServices.TryGetValue(key, out Dictionary<Type, object> services))
            {
                return new List<Type>(services.Keys);
            }
            return new List<Type>();
        }
    }
}
