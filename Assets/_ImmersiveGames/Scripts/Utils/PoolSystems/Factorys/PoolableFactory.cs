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
            // Ponto de extensão para configurações específicas do objeto
        }
    }
}