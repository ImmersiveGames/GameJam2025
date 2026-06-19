using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    /// <summary>
    /// Relay fino para colliders materializados em filhos/presentation.
    /// Não decide impacto, dano, pool ou lifecycle; apenas encaminha eventos físicos ao endpoint owner.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Impact/Actor Impact Collider Relay")]
    public sealed class ActorImpactColliderRelay : MonoBehaviour
    {
        private ActorImpactEndpoint _endpoint;

        public bool IsConfigured => _endpoint != null;
        public Collider RelayCollider { get; private set; }

        public void Configure(
            ActorImpactEndpoint endpoint,
            Collider relayCollider,
            string source,
            string reason)
        {
            _endpoint = endpoint;
            RelayCollider = relayCollider != null ? relayCollider : GetComponent<Collider>();

            DebugUtility.LogVerbose(
                typeof(ActorImpactColliderRelay),
                $"event='ActorImpactColliderRelayConfigured' relay='{name}' collider='{(RelayCollider != null ? RelayCollider.name : string.Empty)}' endpoint='{(_endpoint != null ? _endpoint.name : string.Empty)}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Info);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_endpoint == null)
            {
                return;
            }

            _endpoint.NotifyTriggerEnterFromRelay(
                this,
                other,
                nameof(ActorImpactColliderRelay),
                "relay_trigger_enter");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_endpoint == null)
            {
                return;
            }

            _endpoint.NotifyCollisionEnterFromRelay(
                this,
                collision,
                nameof(ActorImpactColliderRelay),
                "relay_collision_enter");
        }
    }
}
