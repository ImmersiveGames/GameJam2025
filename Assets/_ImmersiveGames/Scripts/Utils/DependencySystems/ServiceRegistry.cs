using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.Utils.DependencySystems
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
            if (_dictionaryPool.Count > 0)
            {
                DebugUtility.LogVerbose(typeof(ServiceRegistry), "Dicionário obtido do pool para serviços.", "yellow");
                return _dictionaryPool.Pop();
            }
            return new Dictionary<Type, object>();
        }

        protected void ReturnDictionaryToPool(Dictionary<Type, object> dictionary)
        {
            dictionary.Clear();
            _dictionaryPool.Push(dictionary);
            DebugUtility.LogVerbose(typeof(ServiceRegistry), $"Dicionário retornado ao pool. Tamanho do pool: {_dictionaryPool.Count}.", "yellow");
        }

        protected bool ValidateService(Type type, object service, string method, string key)
        {
            if (service == null)
            {
                DebugUtility.LogError(typeof(ServiceRegistry), $"{method}: Tentativa de registrar serviço nulo para o tipo {type.Name} com chave {key}.");
                return false;
            }
            return true;
        }
    }
}