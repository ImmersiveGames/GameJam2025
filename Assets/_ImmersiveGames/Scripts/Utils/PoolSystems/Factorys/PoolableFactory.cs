using _ImmersiveGames.Scripts.SpawnSystem.ProjectSystems;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public interface IPoolableFactory
    {
        void Configure(GameObject target, PoolableObjectData data);
    }

    public class PoolableFactory : IPoolableFactory
    {
        public virtual void Configure(GameObject target, PoolableObjectData data)
        {
            // Configurações padrão, se necessário
            var projectile = target.GetComponent<ProjectileObject>();
            if (projectile)
            {
                var modelRoot = target.GetComponentInChildren<ModelRoot>();
                projectile.SetModelInstance(modelRoot ? modelRoot.gameObject : null);
            }
            DebugUtility.LogVerbose(typeof(ObjectPoolFactory),$"Configuração aplicada para '{data?.ObjectName}'.", "blue");
        }
    }
}