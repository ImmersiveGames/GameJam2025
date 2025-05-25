using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public interface IPoolableFactory
    {
        void Configure(GameObject target, PoolableObjectData data);
    }

    public sealed class PoolableFactory : IPoolableFactory
    {
        public void Configure(GameObject target, PoolableObjectData data)
        {
            // Configurações padrão, se necessário
            DebugUtility.LogVerbose(typeof(ObjectPoolFactory),$"Configuração aplicada para '{data?.ObjectName}'.", "blue");
        }
    }
}