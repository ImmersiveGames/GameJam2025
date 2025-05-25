using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    public class TestPoolable : MonoBehaviour, IPoolable
    {
        private PoolableObjectData _data;
        private ObjectPool _pool;
        private GameObject _model;

        public bool IsActive { get; private set; }

        public void Initialize(PoolableObjectData data, ObjectPool pool)
        {
            _data = data;
            _pool = pool;
            IsActive = false;
        }
        
        public void Activate(Vector3 position)
        {
            IsActive = true;
            gameObject.transform.position = position;
            gameObject.SetActive(true);
            OnObjectSpawned();
        }

        public void Deactivate()
        {
            IsActive = false;
            gameObject.SetActive(false);
        }

        public void OnObjectReturned()
        {
            Debug.Log($"Objeto {gameObject.name} retornado ao pool.");
        }

        public void OnObjectSpawned()
        {
            Debug.Log($"Objeto {gameObject.name} ativado.");
        }

        public GameObject GetGameObject() => gameObject;

        public void SetModel(GameObject model)
        {
            _model = model;
        }
    }
}