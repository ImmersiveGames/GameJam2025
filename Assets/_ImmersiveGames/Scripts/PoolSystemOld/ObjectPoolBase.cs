using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PoolSystemOld
{
    public abstract class ObjectPoolBase : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab do objeto a ser instanciado")]
        protected GameObject prefab;

        [SerializeField, Tooltip("Tamanho inicial do pool")]
        protected int initialPoolSize = 10;

        [SerializeField, Tooltip("Permitir expansão do pool se necessário")]
        protected bool expandable = true;

        [SerializeField, Tooltip("Tamanho máximo do pool (se expansível)")]
        protected int maxPoolSize = 100;

        protected readonly List<GameObject> _pool = new();
        protected readonly Queue<GameObject> _inactiveObjects = new();
        protected int _activeObjects;

        protected virtual void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                obj.SetActive(false);
                _pool.Add(obj);
                _inactiveObjects.Enqueue(obj);
            }
        }

        public virtual GameObject GetObject(Vector3 position, Quaternion rotation, int maxObjects)
        {
            if (_activeObjects >= maxObjects)
            {
                DebugUtility.LogWarning(GetType(), $"Limite de objetos ativos ({maxObjects}) atingido.", this);
                return null;
            }

            if (_inactiveObjects.Count > 0)
            {
                var obj = _inactiveObjects.Dequeue();
                obj.transform.SetPositionAndRotation(new Vector3(position.x, 0, position.z), rotation);
                obj.transform.SetParent(null);
                ConfigureObject(obj);
                ResetObject(obj); // Resetar estado antes de ativar
                obj.SetActive(true);
                _activeObjects++;
                return obj;
            }

            if (expandable && _pool.Count < maxPoolSize)
            {
                var obj = Instantiate(prefab, new Vector3(position.x, 0, position.z), rotation, null);
                _pool.Add(obj);
                ConfigureObject(obj);
                ResetObject(obj);
                obj.SetActive(true);
                _activeObjects++;
                return obj;
            }

            DebugUtility.LogWarning(GetType(), $"Pool atingiu o limite máximo ({maxPoolSize}).", this);
            return null;
        }

        public virtual void ReturnToPool(GameObject obj)
        {
            if (_pool.Contains(obj))
            {
                ResetObject(obj); // Resetar antes de desativar
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = Vector3.zero;
                obj.SetActive(false);
                _activeObjects--;
                _inactiveObjects.Enqueue(obj);
            }
            else
            {
                DebugUtility.LogWarning(GetType(), $"Objeto {obj.name} não pertence a este pool ({gameObject.name}).", this);
            }
        }

        protected abstract void ConfigureObject(GameObject obj);
        protected virtual void ResetObject(GameObject obj) { }
    }
}