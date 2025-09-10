using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class Gun : MonoBehaviour
    {
        private ObjectPool _bulletPool;

        private void Start()
        {
            _bulletPool = PoolManager.Instance.GetPool(FactoryType.Bullet);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Space)) return;
            var bullet = _bulletPool.GetObject(transform.position + transform.forward * 2);
            if (bullet != null)
            {
                Debug.Log($"Spawned {bullet.GetGameObject().name}");
            }
        }
    }
}