using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.Utils.DependencySystems
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
            if (_services.ContainsKey(type) && !allowOverride)
            {
                DebugUtility.LogError(typeof(GlobalServiceRegistry), $"Serviço {type.Name} já registrado no escopo global.");
                throw new InvalidOperationException($"Serviço {type.Name} já registrado no escopo global.");
            }

            _services[type] = service;
            DebugUtility.LogVerbose(typeof(GlobalServiceRegistry), $"Serviço {type.Name} registrado no escopo global.", "green");
        }

        public override bool TryGet<T>(string key, out T service)
        {
            service = null;
            var targetType = typeof(T);
            foreach (var kvp in _services)
            {
                if (targetType.IsAssignableFrom(kvp.Key))
                {
                    service = (T)kvp.Value;
                    DebugUtility.LogVerbose(typeof(GlobalServiceRegistry), $"Serviço {targetType.Name} encontrado no escopo global (tipo registrado: {kvp.Key.Name}).", "cyan");
                    return true;
                }
            }
            return false;
        }

        public override void Clear(string key)
        {
            int count = _services.Count;
            _services.Clear();
            DebugUtility.LogVerbose(typeof(GlobalServiceRegistry), $"Removidos {count} serviços do escopo global.", "yellow");
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