using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Causa dano em colisão com objetos que tenham DamageReceiver.
    /// </summary>
    public class DamageDealer : MonoBehaviour, IDamageDealer
    {
        [Header("Configuração de Dano")]
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private ResourceType targetResource = ResourceType.Health;
        [SerializeField] private DamageType damageType = DamageType.Physical;

        [Header("Camadas válidas para causar dano")]
        [SerializeField] private LayerMask targetLayers;

        private IActor _actor;
        private PooledObject _pooledObject;
        private int _lastProcessedFrame = -1;
        private GameObject _lastProcessedTarget;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            _pooledObject = GetComponent<PooledObject>();

            if (_actor == null && _pooledObject == null)
            {
                DebugUtility.LogWarning<DamageDealer>(
                    "Nenhum IActor ou PooledObject encontrado. O dano ficará sem ator associado.", this);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            var contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            TryDealDamage(
                collision.gameObject,
                contact.point,
                contact.normal
            );
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleTriggerCollision(other);
        }

        /// <summary>
        /// Chamado por filhos com colliders através de DamageChildCollider.
        /// </summary>
        public void OnChildCollisionEnter(Collision collision)
        {
            OnCollisionEnter(collision);
        }

        /// <summary>
        /// Chamado por filhos com colliders através de DamageChildCollider.
        /// </summary>
        public void OnChildTriggerEnter(Collider other)
        {
            HandleTriggerCollision(other);
        }

        private void HandleTriggerCollision(Collider other)
        {
            if (other == null) return;
            var hitPoint = other.ClosestPoint(transform.position);
            Vector3? normal = null;

            var direction = transform.position - hitPoint;
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                normal = direction.normalized;
            }

            TryDealDamage(other.gameObject, hitPoint, normal);
        }

        private IActor ResolveActor()
        {
            if (_actor != null)
                return _actor;

            if (_pooledObject != null && _pooledObject.Spawner != null)
                return _pooledObject.Spawner;

            return null;
        }

        public bool TryDealDamage(GameObject other, Vector3? hitPoint = null, Vector3? hitNormal = null)
        {
            if (other == null) return false;
            if (!IsTargetLayerValid(other.layer)) return false;

            if (_lastProcessedFrame == Time.frameCount && _lastProcessedTarget == other)
                return false;

            var receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver == null) return false;

            var sourceActor = ResolveActor();
            var ctx = new DamageContext(
                sourceActor?.ActorId,
                receiver.GetReceiverId(),
                baseDamage,
                targetResource,
                damageType,
                hitPoint,
                hitNormal
            );

            DealDamage(receiver, ctx);

            _lastProcessedFrame = Time.frameCount;
            _lastProcessedTarget = other;

            return true;
        }



        public void DealDamage(IDamageReceiver target, DamageContext ctx)
        {
            target.ReceiveDamage(ctx);
        }

        private bool IsTargetLayerValid(int layer)
        {
            return (targetLayers.value & (1 << layer)) != 0;
        }
    }
}
