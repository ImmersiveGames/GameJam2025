using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class PoolSetup : MonoBehaviour
    {
        [SerializeField] private PoolData bulletPoolData;

        private void Awake()
        {
            PoolManager.Instance.RegisterPool(bulletPoolData, FactoryType.Bullet);
        }
    }
}