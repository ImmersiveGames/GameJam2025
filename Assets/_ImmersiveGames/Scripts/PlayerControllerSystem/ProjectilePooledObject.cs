using UnityEngine;
using _ImmersiveGames.Scripts.PoolSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class ProjectilePooledObject : MonoBehaviour
    {
        private ProjectileObjectPool pool;

        public void SetPool(ProjectileObjectPool poolRef)
        {
            pool = poolRef;
        }

        public void ReturnSelfToPool()
        {
            if (pool != null)
            {
                pool.ReturnToPool(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
                Debug.LogWarning($"ProjectilePooledObject em {gameObject.name} não tem pool atribuído.", this);
            }
        }
    }
}