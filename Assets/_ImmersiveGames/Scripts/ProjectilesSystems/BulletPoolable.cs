using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ProjectilesSystems
{
    [RequireComponent(typeof(Rigidbody)), DebugLevel(DebugLevel.Error)]
    public class BulletPoolable : PooledObject, IHasSkin
    {
        private ModelRoot _modelRoot;
        private Rigidbody _rb;
        private BulletObjectData _data;
        private DamageDealer _damageDealer;
        [SerializeField] private LayerMask collisionLayers = -1;

        protected override void OnConfigured(PoolableObjectData config, IActor spawner)
        {
            _rb = GetComponent<Rigidbody>();
            _data = config as BulletObjectData;
            _damageDealer = GetComponent<DamageDealer>();

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
            HandleTrigger(other);
        }

        // Detecção de colisão física
        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        private void HandleTrigger(Collider other)
        {
            if (other == null) return;
            if (!IsLayerValid(other.gameObject.layer)) return;

            var closestPoint = other.ClosestPoint(transform.position);
            Vector3? hitNormal = null;

            var direction = transform.position - closestPoint;
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                hitNormal = direction.normalized;
            }

            TryDealDamage(other.gameObject, closestPoint, hitNormal);
            Deactivate();
        }

        private void HandleCollision(Collision collision)
        {
            if (collision == null) return;
            if (!IsLayerValid(collision.gameObject.layer)) return;

            Vector3? hitPoint = null;
            Vector3? hitNormal = null;

            if (collision.contactCount > 0)
            {
                var contact = collision.GetContact(0);
                hitPoint = contact.point;
                hitNormal = contact.normal;
            }

            TryDealDamage(collision.gameObject, hitPoint, hitNormal);
            Deactivate();
        }

        private void TryDealDamage(GameObject target, Vector3? hitPoint, Vector3? hitNormal)
        {
            if (_damageDealer == null || target == null) return;
            _damageDealer.TryDealDamage(target, hitPoint, hitNormal);
        }

        private bool IsLayerValid(int layer)
        {
            return (collisionLayers.value & (1 << layer)) != 0;
        }

        public ModelRoot ModelRoot => _modelRoot ??= this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
        public Transform ModelTransform => ModelRoot.transform;
        public void SetSkinActive(bool active)
        {
            if (_modelRoot != null)
            {
                _modelRoot.gameObject.SetActive(active);
            }
        }
    }
}
