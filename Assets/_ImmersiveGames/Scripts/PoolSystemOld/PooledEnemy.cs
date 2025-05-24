using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PoolSystemOld
{
    public class PooledEnemy : MonoBehaviour
    {
        private EnemyObjectPool _parentPool;

        public void SetPool(EnemyObjectPool pool)
        {
            _parentPool = pool;
        }

        public EnemyObjectPool GetPool()
        {
            return _parentPool;
        }

        public void ReturnSelfToPool()
        {
            if (_parentPool != null)
            {
                _parentPool.ReturnToPool(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
                DebugUtility.LogWarning<PooledEnemy>($"PooledEnemy em {gameObject.name} não tem um pool associado.", this);
            }
        }
    }
}