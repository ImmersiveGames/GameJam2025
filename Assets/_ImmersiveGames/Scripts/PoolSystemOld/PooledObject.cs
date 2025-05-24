using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PoolSystemOld
{
    public abstract class PooledObject : MonoBehaviour
    {
        protected ObjectPoolBase pool;

        public void SetPool(ObjectPoolBase pool)
        {
            this.pool = pool;
        }

        public virtual void ReturnSelfToPool()
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

        public virtual void ResetState() { }
    }
}