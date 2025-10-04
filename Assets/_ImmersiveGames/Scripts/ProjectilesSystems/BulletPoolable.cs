using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ProjectilesSystems
{
    [RequireComponent(typeof(Rigidbody)), DebugLevel(DebugLevel.Error)]
    public class BulletPoolable : PooledObject
    {
        private Rigidbody _rb;
        private BulletObjectData _data;
        [SerializeField] private LayerMask collisionLayers = -1;

        protected override void OnConfigured(PoolableObjectData config, IActor spawner)
        {
            _rb = GetComponent<Rigidbody>();
            _data = config as BulletObjectData;

            if (_rb == null)
                DebugUtility.LogError<BulletPoolable>($"No Rigidbody on {name}", this);
        }

        protected override void OnActivated(Vector3 pos, Vector3? direction, IActor spawner)
        {
            if (_rb == null || _data == null) return;

            // Usa a direção fornecida, ou Vector3.zero se não houver direção
            var dir = direction ?? Vector3.zero;
            _rb.linearVelocity = dir.normalized * _data.Speed;
        }

        protected override void OnDeactivated()
        {
            if (_rb != null) 
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        protected override void OnReset()
        {
            if (_rb == null) return;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        protected override void OnReconfigured(PoolableObjectData config)
        {
            _data = config as BulletObjectData;
        }
        // Método para configurar a layer mask via código
        public void SetCollisionLayers(LayerMask layerMask)
        {
            collisionLayers = layerMask;
        }

        // Detecção de colisão por Trigger
        private void OnTriggerEnter(Collider other)
        {
            HandleCollision(other.gameObject);
        }

        // Detecção de colisão física
        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision.gameObject);
        }

        private void HandleCollision(GameObject other)
        {
            // Verifica se a layer do objeto colidido está na layer mask
            if ((collisionLayers.value & (1 << other.layer)) != 0)
            {
                // Usa o método Deactivate() que já existe no PooledObject
                // Isso fará o objeto retornar ao pool automaticamente
                Deactivate();
            }
        }
    }
}