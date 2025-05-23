using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PoolSystem
{
    public abstract class PooledObject : MonoBehaviour
    {
        protected ObjectPoolBase pool;

        public void SetPool(ObjectPoolBase pool)
        {
            this.pool = pool;
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
                DebugUtility.LogWarning(GetType(), $"PooledObject em {gameObject.name} não tem um pool associado.", this);
            }
        }
    }
}