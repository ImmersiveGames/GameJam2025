using _ImmersiveGames.Scripts.PlayerControllerSystem;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EnemySystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class Enemy : DestructibleObject
    {
        private EnemyData _data;
        private GameObject _modelInstance;
        private EnemyMovement _movement;

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            if (_movement == null)
            {
                _movement = gameObject.AddComponent<EnemyMovement>();
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!IsAlive) return;

            var playerHealth = collision.gameObject.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive)
            {
                playerHealth.TakeDamage(_data.collisionDamage);
                TakeDamage(CurrentHealth); // Auto-destruir ao colidir
                DebugUtility.LogVerbose<Enemy>($"Enemy {gameObject.name} colidiu com o player e foi destruído.", "red", this);
            }
        }

        public void Setup(EnemyData data)
        {
            if (_data == data) return;

            _data = data;
            destructibleObject = data;
            base.Initialize();
            CreateModel();
        }

        public void Configure(Transform target)
        {
            _movement.Initialize(_data, target);
        }

        private void CreateModel()
        {
            if (_data?.modelPrefab == null)
            {
                DebugUtility.LogError<Enemy>($"ModelPrefab não definido no EnemyData para {gameObject.name}.", this);
                return;
            }

            if (_modelInstance != null)
            {
                Destroy(_modelInstance);
            }

            _modelInstance = Instantiate(_data.modelPrefab, transform);
            _modelInstance.transform.localPosition = Vector3.zero;
            _modelInstance.transform.localRotation = Quaternion.identity;

            if (_modelInstance.GetComponentInChildren<Renderer>() == null)
            {
                DebugUtility.LogError<Enemy>($"ModelPrefab {_data.modelPrefab.name} não tem Renderer em {gameObject.name}.", this);
            }
        }

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            if (!IsAlive)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            EnemyPooledObject pooledObj = GetComponent<EnemyPooledObject>();
            if (pooledObj != null)
            {
                pooledObj.ReturnSelfToPool();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        protected override void Die()
        {
            base.Die();
            if (_modelInstance != null)
            {
                _modelInstance.SetActive(false);
            }
        }
    }
}