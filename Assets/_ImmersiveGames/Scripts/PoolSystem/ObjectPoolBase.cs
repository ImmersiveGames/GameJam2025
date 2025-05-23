using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.Scripts.PoolSystem
{
    public abstract class ObjectPoolBase : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab do objeto a ser instanciado")]
        protected GameObject prefab;

        [SerializeField, Tooltip("Tamanho inicial do pool")]
        protected int initialPoolSize = 10;

        [SerializeField, Tooltip("Permitir expansão do pool se necessário")]
        protected bool expandable = true;

        protected readonly List<GameObject> _pool = new();
        private int _activeObjects;

        protected void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform); // Filho do pool
                obj.SetActive(false);
                _pool.Add(obj);
            }
        }

        protected GameObject GetObject(Vector3 position, Quaternion rotation, int maxObjects)
        {
            if (_activeObjects >= maxObjects)
                return null;

            foreach (var obj in _pool.Where(obj => !obj.activeSelf))
            {
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.transform.SetParent(null); // Desligar do pai para movimento independente
                ConfigureObject(obj);
                obj.SetActive(true);
                _activeObjects++;
                return obj;
            }

            if (!expandable) return null;
            {
                var obj = Instantiate(prefab, position, rotation, null); // Criar sem pai
                _pool.Add(obj);
                ConfigureObject(obj);
                obj.SetActive(true);
                _activeObjects++;
                return obj;
            }

        }

        public void ReturnToPool(GameObject obj)
        {
            if (_pool.Contains(obj))
            {
                obj.transform.SetParent(transform, false); // Voltar como filho do pool, preservando escala
                obj.transform.localPosition = Vector3.zero; // Opcional: resetar posição relativa
                obj.SetActive(false);
                _activeObjects--;
            }
            else
            {
                Debug.LogWarning($"Objeto {obj.name} não pertence a este pool ({gameObject.name}).", this);
            }
        }

        protected abstract void ConfigureObject(GameObject obj);
    }
}