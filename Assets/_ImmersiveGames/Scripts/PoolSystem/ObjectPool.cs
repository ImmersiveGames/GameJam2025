using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PoolSystem
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab a ser usado no pool")]
        private GameObject prefab;

        [SerializeField, Tooltip("Tamanho inicial do pool")]
        private int initialPoolSize = 20;

        [SerializeField, Tooltip("Permite criar novos objetos se o pool estiver vazio")]
        private bool expandable = true;

        [SerializeField, Tooltip("Move objetos ativos para a raiz da cena")]
        private bool moveToSceneRoot = true;

        private List<GameObject> _pooledObjects;
        private Transform _poolContainer;
        private static Transform _sceneContainer;

        private void Awake()
        {
            if (_sceneContainer == null)
            {
                GameObject containerObj = new GameObject("ActivePoolObjects");
                DontDestroyOnLoad(containerObj);
                _sceneContainer = containerObj.transform;
            }

            InitializePool();
        }

        private void InitializePool()
        {
            if (prefab == null)
            {
                Debug.LogError($"Prefab não atribuído no ObjectPool de {gameObject.name}.");
                return;
            }

            _pooledObjects = new List<GameObject>(initialPoolSize);
            _poolContainer = new GameObject($"Pool_{prefab.name}").transform;
            _poolContainer.SetParent(transform, false);

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewPoolObject();
            }
        }

        private GameObject CreateNewPoolObject()
        {
            GameObject newObject = Instantiate(prefab, _poolContainer);
            newObject.SetActive(false);
            _pooledObjects.Add(newObject);

            PooledObject pooledComponent = newObject.GetComponent<PooledObject>();
            if (pooledComponent == null)
            {
                pooledComponent = newObject.AddComponent<PooledObject>();
            }
            pooledComponent.SetPool(this);

            return newObject;
        }

        public GameObject GetPooledObject()
        {
            foreach (GameObject obj in _pooledObjects)
            {
                if (!obj.activeInHierarchy)
                {
                    if (moveToSceneRoot)
                    {
                        obj.transform.SetParent(_sceneContainer, true);
                    }
                    return obj;
                }
            }

            if (expandable)
            {
                GameObject newObj = CreateNewPoolObject();
                if (moveToSceneRoot)
                {
                    newObj.transform.SetParent(_sceneContainer, true);
                }
                return newObj;
            }

            return null;
        }

        public void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.SetParent(_poolContainer, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.SetActive(false);
        }
    }
}