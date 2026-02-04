using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Core.Composition
{

    public abstract class ServiceRegistry
    {
        private readonly Stack<Dictionary<Type, object>> _dictionaryPool = new();

        public abstract void Register<T>(string key, T service, bool allowOverride = false) where T : class;
        public abstract bool TryGet<T>(string key, out T service) where T : class;
        public abstract void Clear(string key);
        public abstract void ClearAll();
        public abstract List<Type> ListServices(string key);

        public virtual T Get<T>(string key = null) where T : class
        {
            if (TryGet(key, out T service))
            {
                return service;
            }
            DebugUtility.LogError(typeof(ServiceRegistry), $"Serviço {typeof(T).Name} não encontrado para a chave '{key ?? "global"}'.");
            throw new KeyNotFoundException($"Serviço {typeof(T).Name} não encontrado.");
        }

        protected Dictionary<Type, object> GetPooledDictionary()
        {
            if (_dictionaryPool.Count <= 0)
            {
                return new Dictionary<Type, object>();
            }
            DebugUtility.LogVerbose(typeof(ServiceRegistry), "Dicionário obtido do pool para serviços.");
            return _dictionaryPool.Pop();
        }

        protected void ReturnDictionaryToPool(Dictionary<Type, object> dictionary)
        {
            dictionary.Clear();
            _dictionaryPool.Push(dictionary);
            DebugUtility.LogVerbose(typeof(ServiceRegistry), $"Dicionário retornado ao pool. Tamanho do pool: {_dictionaryPool.Count}.");
        }

        protected static void DisposeServiceIfNeeded(object service)
        {
            if (service is not IDisposable disposable)
            {
                return;
            }
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(ServiceRegistry), $"Falha ao descartar serviço {service.GetType().Name}: {ex}");
            }
        }

        protected static bool ValidateService(Type type, object service, string method, string key)
        {
            if (service != null)
            {
                return true;
            }
            DebugUtility.LogError(typeof(ServiceRegistry), $"{method}: Tentativa de registrar serviço nulo para o tipo {type.Name} com chave {key}.");
            return false;
        }
    }
}
