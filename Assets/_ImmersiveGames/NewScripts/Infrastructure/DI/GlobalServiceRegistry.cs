using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
namespace _ImmersiveGames.NewScripts.Infrastructure.DI
{
    
    public class GlobalServiceRegistry : ServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new();

        public override void Register<T>(string key, T service, bool allowOverride = false)
        {
            if (!ValidateService(typeof(T), service, "RegisterGlobal", "global"))
            {
                throw new ArgumentNullException(nameof(service), $"Serviço nulo para o tipo {typeof(T).Name} no escopo global.");
            }

            var type = typeof(T);
            if (_services.TryGetValue(type, out object existing) && !allowOverride)
            {
                DebugUtility.LogError(typeof(GlobalServiceRegistry), $"Serviço {type.Name} já registrado no escopo global.");
                throw new InvalidOperationException($"Serviço {type.Name} já registrado no escopo global.");
            }

            if (allowOverride && existing != null && !ReferenceEquals(existing, service))
            {
                DisposeServiceIfNeeded(existing);
            }

            _services[type] = service;
            DebugUtility.LogVerbose(
                typeof(GlobalServiceRegistry),
                $"Serviço {type.Name} registrado no escopo global.",
                DebugUtility.Colors.Success);
        }
        public IEnumerable<T> GetAll<T>() where T : class
        {
            foreach (object svc in _services.Values)
            {
                if (svc is T typedService)
                    yield return typedService;
            }
        }

        public override bool TryGet<T>(string key, out T service)
        {
            service = null;
            var targetType = typeof(T);
            foreach (KeyValuePair<Type, object> kvp in _services)
            {
                if (targetType.IsAssignableFrom(kvp.Key))
                {
                    service = (T)kvp.Value;
                    DebugUtility.LogVerbose(typeof(GlobalServiceRegistry), $"Serviço {targetType.Name} encontrado no escopo global (tipo registrado: {kvp.Key.Name}).");
                    return true;
                }
            }
            return false;
        }

        public override void Clear(string key)
        {
            foreach (object service in _services.Values)
            {
                DisposeServiceIfNeeded(service);
            }

            int count = _services.Count;
            _services.Clear();
            DebugUtility.LogVerbose(
                typeof(GlobalServiceRegistry),
                $"Removidos {count} serviços do escopo global.",
                DebugUtility.Colors.Success);
        }

        public override void ClearAll()
        {
            Clear(null);
        }

        public override List<Type> ListServices(string key)
        {
            return new List<Type>(_services.Keys);
        }
    }
}
