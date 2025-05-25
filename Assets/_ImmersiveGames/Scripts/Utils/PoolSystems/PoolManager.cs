using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DefaultExecutionOrder(-100)]
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager _instance;
        private readonly Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();

        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PoolManager");
                    _instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                    DebugUtility.LogVerbose<PoolManager>("PoolManager inicializado.", "blue", _instance);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                DebugUtility.LogWarning<PoolManager>("Outra instância de PoolManager encontrada. Destruindo esta.", this);
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            DebugUtility.LogVerbose<PoolManager>("PoolManager Awake executado.", "blue", this);
        }

        public void RegisterPool(PoolableObjectData data)
        {
            if (data == null)
            {
                DebugUtility.LogError<PoolManager>("PoolableObjectData é nulo.", this);
                return;
            }

            if (_pools.ContainsKey(data.ObjectName))
            {
                DebugUtility.LogWarning<PoolManager>($"Pool para '{data.ObjectName}' já registrado.", this);
                return;
            }

            DebugUtility.LogVerbose<PoolManager>($"Iniciando registro do pool para '{data.ObjectName}'.", "blue", this);
            GameObject poolGo = new GameObject($"Pool_{data.ObjectName}");
            poolGo.transform.SetParent(transform);
            ObjectPool pool = poolGo.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.Initialize();
            _pools.Add(data.ObjectName, pool);
            DebugUtility.Log<PoolManager>($"Pool '{data.ObjectName}' registrado com sucesso.", "green", this);
        }

        public ObjectPool GetPool(string objectName)
        {
            DebugUtility.LogVerbose<PoolManager>($"Tentando obter pool para '{objectName}'. Pools disponíveis: {string.Join(", ", _pools.Keys)}", "blue", this);
            if (_pools.TryGetValue(objectName, out var pool))
            {
                return pool;
            }
            DebugUtility.LogError<PoolManager>($"Nenhum pool encontrado para '{objectName}'.", this);
            return null;
        }

        public IPoolable GetObject(string objectName, Vector3 position)
        {
            DebugUtility.LogVerbose<PoolManager>($"Obtendo objeto para '{objectName}' na posição {position}.", "blue", this);
            var pool = GetPool(objectName);
            if (pool == null)
            {
                return null;
            }
            var obj = pool.GetObject(position);
            DebugUtility.LogVerbose<PoolManager>($"Objeto {(obj != null ? "obtido" : "nulo")} para '{objectName}'.", "blue", this);
            return obj;
        }
    }
}