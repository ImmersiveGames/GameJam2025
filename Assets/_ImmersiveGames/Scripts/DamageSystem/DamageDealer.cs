using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Causa dano em colisão com objetos que tenham DamageReceiver.
    /// </summary>
    [RequireComponent(typeof(ActorMaster))]
    public class DamageDealer : MonoBehaviour, IDamageDealer
    {
        [Header("Configuração de Dano")]
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private ResourceType targetResource = ResourceType.Health;
        [SerializeField] private DamageType damageType = DamageType.Physical;

        [Header("Camadas válidas para causar dano")]
        [SerializeField] private LayerMask targetLayers;

        private ActorMaster actor;

        private void Awake() => actor = GetComponent<ActorMaster>();

        /// <summary>
        /// Colliders podem estar em filhos — por isso, este método é chamado no pai.
        /// </summary>
        /// <param name="collision"></param>
        public void OnChildCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        private void HandleCollision(Collision collision)
        {
            var other = collision.gameObject;
            if (!IsTargetLayerValid(other.layer)) return;

            var receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver == null) return;

            var ctx = new DamageContext(
                actor.ActorId,
                receiver.GetReceiverId(),
                baseDamage,
                targetResource,
                damageType,
                collision.contacts[0].point,
                collision.contacts[0].normal
            );

            DealDamage(receiver, ctx);
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
