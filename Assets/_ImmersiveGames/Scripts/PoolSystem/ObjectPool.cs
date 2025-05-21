using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PoolSystem
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialPoolSize = 20;
        [SerializeField] private bool _expandable = true;
        [SerializeField] private bool _moveToSceneRoot = true; // Opção para mover objetos ativos para a raiz da cena

        private List<GameObject> _pooledObjects;
        private Transform _poolContainer;
        private static Transform _sceneContainer; // Container na raiz da cena para objetos ativos

        private void Awake()
        {
            // Criar um container global para todos os objetos ativos, se ainda não existir
            if (_sceneContainer == null)
            {
                GameObject containerObj = GameObject.Find("ActivePoolObjects");
                if (containerObj == null)
                {
                    containerObj = new GameObject("ActivePoolObjects");
                    DontDestroyOnLoad(containerObj); // Opcional: mantém o container entre cenas
                }
                _sceneContainer = containerObj.transform;
            }
            
            InitializePool();
        }

        private void InitializePool()
        {
            _pooledObjects = new List<GameObject>(_initialPoolSize);
            
            // Criar um container para organizar a hierarquia dos objetos inativos
            _poolContainer = new GameObject($"Pool_{_prefab.name}").transform;
            _poolContainer.SetParent(transform, false);

            // Preencher o pool com objetos iniciais
            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewPoolObject();
            }
        }

        private GameObject CreateNewPoolObject()
        {
            GameObject newObject = Instantiate(_prefab, _poolContainer);
            newObject.SetActive(false);
            _pooledObjects.Add(newObject);
            
            // Adicionar componente PooledObject para rastrear a qual pool este objeto pertence
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
            // Procurar por um objeto inativo no pool
            for (int i = 0; i < _pooledObjects.Count; i++)
            {
                if (!_pooledObjects[i].activeInHierarchy)
                {
                    GameObject obj = _pooledObjects[i];
                    
                    // Mover para a raiz da cena quando ativado
                    if (_moveToSceneRoot)
                    {
                        obj.transform.SetParent(_sceneContainer, true);
                    }
                    
                    return obj;
                }
            }

            // Se não encontrar nenhum objeto disponível e o pool for expansível, cria um novo
            if (_expandable)
            {
                GameObject newObj = CreateNewPoolObject();
                
                // Mover para a raiz da cena se necessário
                if (_moveToSceneRoot)
                {
                    newObj.transform.SetParent(_sceneContainer, true);
                }
                
                return newObj;
            }

            // Retorna null se não houver objetos disponíveis e o pool não for expansível
            return null;
        }
        
        // Método para devolver manualmente um objeto ao pool
        public void ReturnToPool(GameObject obj)
        {
            if (obj != null)
            {
                // Retornar o objeto para o container do pool
                obj.transform.SetParent(_poolContainer, false);
                
                // Redefinir posição e rotação (opcional)
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                
                // Desativar o objeto
                obj.SetActive(false);
            }
        }
    }
}
