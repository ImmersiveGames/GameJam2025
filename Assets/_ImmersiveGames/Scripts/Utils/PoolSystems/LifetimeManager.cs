using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Warning), DefaultExecutionOrder(-10)]
    public class LifetimeManager : MonoBehaviour
    {
        public static LifetimeManager Instance { get; private set; }
        private readonly Dictionary<IPoolable, float> _activeObjects = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Criar uma cópia da coleção para iteração segura
            var objectsToProcess = new List<KeyValuePair<IPoolable, float>>(_activeObjects);
            var toRemove = new List<IPoolable>();
            var updatedLifetimes = new Dictionary<IPoolable, float>();

            // Iterar sobre a cópia
            foreach (var pair in objectsToProcess)
            {
                var obj = pair.Key;
                var timeLeft = pair.Value - Time.deltaTime;
                if (timeLeft <= 0)
                {
                    toRemove.Add(obj);
                }
                else
                {
                    updatedLifetimes[obj] = timeLeft;
                }
            }

            // Aplicar desativações após a iteração
            foreach (var obj in toRemove)
            {
                if (obj != null && _activeObjects.ContainsKey(obj))
                {
                    if (obj.GetGameObject() != null)
                    {
                        obj.Deactivate(); // Chama ReturnObject no PooledObject
                        DebugUtility.LogVerbose<LifetimeManager>($"Objeto '{obj.GetGameObject().name}' desativado pelo lifetime.", "blue", this);
                    }
                    _activeObjects.Remove(obj);
                }
            }

            // Aplicar atualizações de lifetime após a iteração
            foreach (var pair in updatedLifetimes)
            {
                if (_activeObjects.ContainsKey(pair.Key))
                {
                    _activeObjects[pair.Key] = pair.Value;
                }
            }
        }

        public void RegisterObject(IPoolable poolable, float lifetime)
        {
            if (poolable == null || lifetime <= 0) return;
            _activeObjects[poolable] = lifetime;
            DebugUtility.LogVerbose<LifetimeManager>($"Objeto '{poolable.GetGameObject().name}' registrado com lifetime {lifetime}.", "cyan", this);
        }

        public void UnregisterObject(IPoolable poolable)
        {
            if (_activeObjects.Remove(poolable))
            {
                DebugUtility.LogVerbose<LifetimeManager>($"Objeto '{poolable.GetGameObject().name}' desregistrado.", "cyan", this);
            }
        }
    }
}