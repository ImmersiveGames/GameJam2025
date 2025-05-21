using UnityEngine;

namespace _ImmersiveGames.Scripts.PoolSystem
{
    /// <summary>
    /// Componente adicionado automaticamente a objetos gerenciados por ObjectPool
    /// para rastrear a qual pool eles pertencem.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        private ObjectPool _parentPool;
        
        public void SetPool(ObjectPool pool)
        {
            _parentPool = pool;
        }
        
        public ObjectPool GetPool()
        {
            return _parentPool;
        }
        
        // Método auxiliar para facilitar o retorno ao pool
        public void ReturnSelfToPool()
        {
            if (_parentPool != null)
            {
                _parentPool.ReturnToPool(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
