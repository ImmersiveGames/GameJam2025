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

        private IActor actor;
        private PooledObject pooledObject;

        private void Awake()
        {
            actor = GetComponent<IActor>();
            pooledObject = GetComponent<PooledObject>();

            if (actor == null && pooledObject == null)
            {
                DebugUtility.LogWarning<DamageDealer>(
                    "Nenhum IActor ou PooledObject encontrado. O dano ficará sem ator associado.", this);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        /// <summary>
        /// Chamado por filhos com colliders através de DamageChildCollider.
        /// </summary>
        public void OnChildCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        private void HandleCollision(Collision collision)
        {
            var other = collision.gameObject;
            if (!IsTargetLayerValid(other.layer)) return;

            var receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver == null) return;

            var contact = collision.contacts.Length > 0 ? collision.contacts[0] : default;
            var sourceActor = ResolveActor();
            var ctx = new DamageContext(
                sourceActor?.ActorId,
                receiver.GetReceiverId(),
                baseDamage,
                targetResource,
                damageType,
                contact.point,
                contact.normal
            );

            DealDamage(receiver, ctx);
        }

        private IActor ResolveActor()
        {
            if (actor != null)
                return actor;

            if (pooledObject != null && pooledObject.Spawner != null)
                return pooledObject.Spawner;

            return null;
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
