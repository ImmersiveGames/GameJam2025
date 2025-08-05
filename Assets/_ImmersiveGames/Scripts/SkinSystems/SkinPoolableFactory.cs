using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class SkinPoolableFactory : IPoolableFactory
    {
        public void Configure(GameObject target, PoolableObjectData data)
        {
            // Configurações específicas para skins
            if (data is SkinPoolableObjectData skinData)
            {
                // Exemplo: Ajustar rotação para visão top-down (XZ)
                target.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }

}