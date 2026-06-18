using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Damage/Actor Contact Damage Collider Relay")]
    public sealed class ActorContactDamageColliderRelay : MonoBehaviour
    {
        private ActorContactDamageEndpoint _endpoint;
        private Collider _collider;

        public bool IsConfigured => _endpoint != null && _collider != null;
        public Collider RelayCollider => _collider;

        public void Configure(
            ActorContactDamageEndpoint endpoint,
            Collider relayCollider,
            string source,
            string reason)
        {
            _endpoint = endpoint;
            _collider = relayCollider;

            DebugUtility.LogVerbose(
                typeof(ActorContactDamageColliderRelay),
                $"event='ActorContactDamageColliderRelayConfigured' relay='{name}' collider='{(_collider == null ? string.Empty : _collider.name)}' endpoint='{(_endpoint == null ? string.Empty : _endpoint.name)}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Info);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_endpoint == null)
            {
                return;
            }

            _endpoint.NotifyTriggerEnterFromRelay(this, other, "ActorContactDamageColliderRelay", "trigger_enter_relay");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_endpoint == null)
            {
                return;
            }

            _endpoint.NotifyCollisionEnterFromRelay(this, collision, "ActorContactDamageColliderRelay", "collision_enter_relay");
        }

    }
}
