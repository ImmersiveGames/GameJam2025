using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class ObjectPoolFactory
    {
        public IPoolable CreateObject(PoolableObjectData data, Transform parent, Vector3 position, string name, ObjectPool pool)
        {
            if (!data?.LogicPrefab)
            {
                DebugUtility.LogError(typeof(ObjectPoolFactory),$"LogicPrefab nulo para '{data?.ObjectName}'.");
                return null;
            }

            // Criar o objeto raiz
            var obj = Object.Instantiate(data.LogicPrefab, position, Quaternion.identity, parent);
            obj.name = name;
            var poolable = obj.GetComponent<IPoolable>();
            if (poolable == null)
            {
                DebugUtility.LogError(typeof(ObjectPoolFactory),$"LogicPrefab '{data.ObjectName}' não possui IPoolable.");
                Object.Destroy(obj);
                return null;
            }

            // Construir a hierarquia visual
            var modelRoot = ModelBuilder.BuildModel(obj, data);
            if (!modelRoot)
            {
                Object.Destroy(obj);
                return null;
            }

            // Inicializar o poolable
            poolable.Initialize(data, pool);
            var factory = FactoryRegistry.GetFactory(data.FactoryType);
            factory?.Configure(obj, data); // Configurações específicas, se necessário
            return poolable;
        }
    }
}