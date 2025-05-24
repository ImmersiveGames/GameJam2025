using UnityEngine;
using _ImmersiveGames.Scripts.PlayerControllerSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Tags;

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

            var modelRoot = GetOrCreateModelRoot();
            if (_modelInstance != null)
            {
                _modelInstance.SetActive(false); // Desativar modelo anterior
            }

            _modelInstance = Instantiate(_data.modelPrefab, modelRoot);
            _modelInstance.transform.localPosition = Vector3.zero;
            _modelInstance.transform.localRotation = Quaternion.identity;

            if (_modelInstance.GetComponentInChildren<Renderer>() == null)
            {
                DebugUtility.LogError<Enemy>($"ModelPrefab {_data.modelPrefab.name} não tem Renderer em {gameObject.name}.", this);
            }
        }

        private Transform GetOrCreateModelRoot()
        {
            var modelRoot = GetComponentInChildren<ModelRoot>()?.transform;
            if (modelRoot != null) return modelRoot;

            var rootObj = new GameObject("ModelRoot");
            var modelRootTr = rootObj.transform;
            modelRootTr.SetParent(transform, false);
            rootObj.AddComponent<ModelRoot>();

            // Adicionar Rigidbody (isKinematic = true)
            var rb = rootObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Adicionar SphereCollider (isTrigger = true)
            var collider = rootObj.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f; // Tamanho padrão, já que EnemyData não fornece tamanho

            return modelRootTr;
        }

        public override void ResetState()
        {
            base.ResetState();
            if (_movement != null)
            {
                _movement.ResetState(); // Resetar movimento
            }
            if (_modelInstance != null)
            {
                _modelInstance.SetActive(true); // Reativar modelo ao resetar
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