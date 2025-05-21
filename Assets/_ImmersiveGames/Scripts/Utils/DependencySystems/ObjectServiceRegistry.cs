﻿using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.Utils.DependencySystems
{
    [DebugLevel(DebugLevel.None)]
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

            if (!_objectServices.TryGetValue(key, out var services))
            {
                services = GetPooledDictionary();
                _objectServices[key] = services;
            }

            var type = typeof(T);
            if (services.ContainsKey(type) && !allowOverride)
            {
                DebugUtility.LogError(typeof(ObjectServiceRegistry), $"Serviço {type.Name} já registrado para o ID {key}.");
                throw new InvalidOperationException($"Serviço {type.Name} já registrado para o ID {key}.");
            }

            services[type] = service;
            DebugUtility.LogVerbose(typeof(ObjectServiceRegistry), $"Serviço {type.Name} registrado para o ID {key}.", "green");
        }

        public override bool TryGet<T>(string key, out T service)
        {
            service = null;
            if (string.IsNullOrEmpty(key))
            {
                DebugUtility.LogError(typeof(ObjectServiceRegistry), "TryGet: objectId é nulo ou vazio.");
                return false;
            }

            if (_objectServices.TryGetValue(key, out var services))
            {
                var targetType = typeof(T);
                foreach (var kvp in services)
                {
                    if (targetType.IsAssignableFrom(kvp.Key))
                    {
                        service = (T)kvp.Value;
                        DebugUtility.LogVerbose(typeof(ObjectServiceRegistry), $"Serviço {targetType.Name} encontrado para o ID {key} (tipo registrado: {kvp.Key.Name}).", "cyan");
                        return true;
                    }
                }
            }

            return false;
        }

        public override void Clear(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                DebugUtility.LogError(typeof(ObjectServiceRegistry), "Clear: objectId é nulo ou vazio.");
                throw new ArgumentNullException(nameof(key));
            }

            if (_objectServices.TryGetValue(key, out var services))
            {
                int count = services.Count;
                _objectServices.Remove(key);
                ReturnDictionaryToPool(services);
                DebugUtility.LogVerbose(typeof(ObjectServiceRegistry), $"Removidos {count} serviços para o ID {key}.", "yellow");
            }
            DebugUtility.LogVerbose(typeof(ObjectServiceRegistry), $"Removidos serviços para o ID {key}.", "yellow");
        }

        public override void ClearAll()
        {
            int totalCount = 0;
            foreach (var services in _objectServices.Values)
            {
                totalCount += services.Count;
                ReturnDictionaryToPool(services);
            }
            _objectServices.Clear();
            DebugUtility.LogVerbose(typeof(ObjectServiceRegistry), $"Removidos {totalCount} serviços de todos os objetos.", "yellow");
        }

        public override List<Type> ListServices(string key)
        {
            if (_objectServices.TryGetValue(key, out var services))
                return new List<Type>(services.Keys);
            return new List<Type>();
        }
    }
}