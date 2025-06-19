using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class ObjectPoolFactory
    {
        public IPoolable CreateObject(PoolableObjectData data, Transform parent, Vector3 position, string name, ObjectPool pool)
        {
            if (!data?.Prefab)
            {
                DebugUtility.LogError(typeof(ObjectPoolFactory), $"LogicPrefab nulo para '{data?.ObjectName}'.");
                return null;
            }

            // Instanciar o prefab desativado para evitar que apareça ativo no Scene View
            var obj = Object.Instantiate(data.Prefab, position, Quaternion.identity, parent);
            obj.SetActive(false); // Força desativação imediata
            obj.name = name;
            var poolable = obj.GetComponent<IPoolable>();
            if (poolable == null)
            {
                DebugUtility.LogError(typeof(ObjectPoolFactory), $"LogicPrefab '{data.ObjectName}' não possui IPoolable.");
                Object.Destroy(obj);
                return null;
            }

            var modelRoot = ModelBuilder.BuildModel(obj, data);
            if (!modelRoot)
            {
                Object.Destroy(obj);
                return null;
            }

            poolable.Initialize(data, pool, null);
            var factory = FactoryRegistry.GetFactory(data.FactoryType);
            factory?.Configure(obj, data);
            return poolable;
        }
    }
}