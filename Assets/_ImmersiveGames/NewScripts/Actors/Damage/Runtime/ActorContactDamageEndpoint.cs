using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Damage/Actor Contact Damage Endpoint")]
    public sealed class ActorContactDamageEndpoint : MonoBehaviour
    {
        [Header("Contact Damage")]
        [Tooltip("Quantidade de dano bruto aplicada quando este componente encosta em um receiver de dano valido.")]
        [SerializeField] private float damageAmount = 10f;

        [Tooltip("Tipo semantico do dano de contato.")]
        [SerializeField] private string damageKind = "contact";

        [Tooltip("Mascara de layers aceitos para receivers de dano por contato.")]
        [SerializeField] private LayerMask targetLayerMask = ~0;

        [Tooltip("Tempo minimo entre danos contra o mesmo receiver de dano.")]
        [SerializeField] private float cooldownSecondsPerReceiver = 0.5f;

        [Header("Physics")]
        [SerializeField] private bool detectTriggerEnter = true;
        [SerializeField] private bool detectCollisionEnter = true;
        [SerializeField] private bool installColliderRelaysInChildren = true;

        [Header("Runtime References")]
        [SerializeField] private ActorDamageSourceEndpoint damageSourceEndpoint;

        private readonly Dictionary<ActorInstanceRuntimeId, float> _nextAllowedDamageByReceiver = new();
        private ActorInstanceRuntimeId _sourceActorInstanceRuntimeId;
        private SessionActivityIdentity _activityIdentity;

        public bool IsConfigured { get; private set; }
        public ActorId SourceActorId { get; private set; }
        public ActorInstanceRuntimeId SourceActorInstanceRuntimeId => _sourceActorInstanceRuntimeId;
        public SessionActivityIdentity ActivityIdentity => _activityIdentity;

        public void Configure(
            ActorId sourceActorId,
            ActorInstanceRuntimeId sourceActorInstanceRuntimeId,
            SessionActivityIdentity activityIdentity,
            ActorDamageSourceEndpoint sourceEndpoint,
            string source,
            string reason)
        {
            if (!sourceActorId.IsValid)
            {
                throw new InvalidOperationException("ActorContactDamageEndpoint requires a valid source ActorId.");
            }

            if (!sourceActorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("ActorContactDamageEndpoint requires a valid source ActorInstanceRuntimeId.");
            }

            if (!activityIdentity.IsValid)
            {
                throw new InvalidOperationException("ActorContactDamageEndpoint requires a valid SessionActivityIdentity.");
            }

            if (sourceEndpoint == null || !sourceEndpoint.IsConfigured)
            {
                throw new InvalidOperationException("ActorContactDamageEndpoint requires a configured ActorDamageSourceEndpoint.");
            }

            SourceActorId = sourceActorId;
            _sourceActorInstanceRuntimeId = sourceActorInstanceRuntimeId;
            _activityIdentity = activityIdentity;
            damageSourceEndpoint = sourceEndpoint;
            IsConfigured = true;
            _nextAllowedDamageByReceiver.Clear();

            ConfigureColliderRelays(source, reason);

            DebugUtility.LogVerbose(
                typeof(ActorContactDamageEndpoint),
                $"event='ActorContactDamageEndpointConfigured' sourceActorId='{SourceActorId}' sourceActorInstanceRuntimeId='{_sourceActorInstanceRuntimeId}' activityId='{_activityIdentity.ActivityId}' entrySequence='{_activityIdentity.EntrySequence}' damageAmount='{damageAmount:0.###}' damageKind='{damageKind.TrimToEmpty()}' targetLayerMask='{targetLayerMask.value}' cooldownSecondsPerReceiver='{cooldownSecondsPerReceiver:0.###}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Success);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!detectTriggerEnter)
            {
                return;
            }

            TryApplyContactDamage(other == null ? null : other.gameObject, other, "ActorContactDamageEndpoint", "trigger_enter");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!detectCollisionEnter)
            {
                return;
            }

            var otherCollider = collision == null ? null : collision.collider;
            TryApplyContactDamage(otherCollider == null ? null : otherCollider.gameObject, otherCollider, "ActorContactDamageEndpoint", "collision_enter");
        }

        internal void NotifyTriggerEnterFromRelay(ActorContactDamageColliderRelay relay, Collider other, string source, string reason)
        {
            if (!detectTriggerEnter)
            {
                return;
            }

            TryApplyContactDamage(other == null ? null : other.gameObject, other, source, reason);
        }

        internal void NotifyCollisionEnterFromRelay(ActorContactDamageColliderRelay relay, Collision collision, string source, string reason)
        {
            if (!detectCollisionEnter)
            {
                return;
            }

            var otherCollider = collision == null ? null : collision.collider;
            TryApplyContactDamage(otherCollider == null ? null : otherCollider.gameObject, otherCollider, source, reason);
        }

        private bool TryApplyContactDamage(GameObject targetObject, Collider targetCollider, string source, string reason)
        {
            if (!IsConfigured || damageSourceEndpoint == null || !damageSourceEndpoint.IsConfigured)
            {
                LogRejected(null, targetObject, targetCollider, "contact_damage_endpoint_not_configured", source, reason);
                return false;
            }

            if (targetObject == null && targetCollider != null)
            {
                targetObject = targetCollider.gameObject;
            }

            if (targetObject == null)
            {
                LogRejected(null, targetObject, targetCollider, "contact_target_object_missing", source, reason);
                return false;
            }

            if (!IsLayerAllowed(targetObject.layer))
            {
                LogRejected(null, targetObject, targetCollider, "contact_target_layer_not_allowed", source, reason);
                return false;
            }

            var damageableEndpoint = ResolveDamageableEndpoint(targetObject, targetCollider);
            if (damageableEndpoint == null || !damageableEndpoint.IsConfigured)
            {
                LogRejected(damageableEndpoint, targetObject, targetCollider, "contact_target_damageable_missing_or_not_configured", source, reason);
                return false;
            }

            var targetActorId = damageableEndpoint.ActorId;
            var targetActorInstanceRuntimeId = damageableEndpoint.ActorInstanceRuntimeId;

            if (IsSelfReceiver(targetActorInstanceRuntimeId))
            {
                LogRejected(damageableEndpoint, targetObject, targetCollider, "contact_target_is_source_actor", source, reason);
                return false;
            }

            if (!IsCooldownElapsed(targetActorInstanceRuntimeId))
            {
                LogRejected(damageableEndpoint, targetObject, targetCollider, "contact_damage_cooldown_active", source, reason);
                return false;
            }

            float normalizedDamageAmount = damageAmount <= 0f || float.IsNaN(damageAmount) || float.IsInfinity(damageAmount)
                ? 0f
                : damageAmount;
            if (normalizedDamageAmount <= 0f)
            {
                LogRejected(damageableEndpoint, targetObject, targetCollider, "contact_damage_amount_invalid", source, reason);
                return false;
            }

            DebugUtility.LogVerbose(
                typeof(ActorContactDamageEndpoint),
                $"event='ActorContactDamageDetected' sourceActorId='{SourceActorId}' sourceActorInstanceRuntimeId='{_sourceActorInstanceRuntimeId}' targetActorId='{targetActorId}' targetActorInstanceRuntimeId='{targetActorInstanceRuntimeId}' targetObject='{ResolveObjectName(targetObject)}' targetCollider='{ResolveColliderName(targetCollider)}' receiver='{ResolveReceiverName(damageableEndpoint)}' damageAmount='{normalizedDamageAmount:0.###}' damageKind='{damageKind.TrimToEmpty()}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Info);

            ActorDamageSourceIntent intent = new(
                _activityIdentity,
                SourceActorId,
                _sourceActorInstanceRuntimeId,
                targetActorId,
                targetActorInstanceRuntimeId,
                normalizedDamageAmount,
                damageKind.TrimToEmpty(),
                source,
                reason);

            DebugUtility.LogVerbose(
                typeof(ActorContactDamageEndpoint),
                $"event='ActorContactDamageRegistered' sourceActorId='{SourceActorId}' sourceActorInstanceRuntimeId='{_sourceActorInstanceRuntimeId}' targetActorId='{targetActorId}' targetActorInstanceRuntimeId='{targetActorInstanceRuntimeId}' receiver='{ResolveReceiverName(damageableEndpoint)}' rawDamageAmount='{normalizedDamageAmount:0.###}' damageKind='{damageKind.TrimToEmpty()}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Success);

            if (!damageSourceEndpoint.TryEmitDamageIntent(intent, damageableEndpoint, out var result) || result.Rejected || result.Failed)
            {
                LogRejected(damageableEndpoint, targetObject, targetCollider, result.Reason, source, reason);
                return false;
            }

            MarkCooldown(targetActorInstanceRuntimeId);

            DebugUtility.LogVerbose(
                typeof(ActorContactDamageEndpoint),
                $"event='ActorContactDamageApplied' sourceActorId='{SourceActorId}' sourceActorInstanceRuntimeId='{_sourceActorInstanceRuntimeId}' targetActorId='{targetActorId}' targetActorInstanceRuntimeId='{targetActorInstanceRuntimeId}' receiver='{ResolveReceiverName(damageableEndpoint)}' rawDamageAmount='{normalizedDamageAmount:0.###}' changedFact='{result.HasChangedFact}' thresholdFacts='{result.HasThresholdFacts}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Success);

            return true;
        }

        private void ConfigureColliderRelays(string source, string reason)
        {
            if (!installColliderRelaysInChildren)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorContactDamageEndpoint),
                    $"event='ActorContactDamageColliderRelayConfigurationSkipped' sourceActorId='{SourceActorId}' sourceActorInstanceRuntimeId='{_sourceActorInstanceRuntimeId}' outcomeReason='relay_install_disabled' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                    DebugUtility.Colors.Info);
                return;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            int configuredCount = 0;
            int skippedCount = 0;
            for (int index = 0; index < colliders.Length; index++)
            {
                var collider = colliders[index];
                if (collider == null)
                {
                    skippedCount++;
                    continue;
                }

                var nearestEndpoint = collider.GetComponentInParent<ActorContactDamageEndpoint>();
                if (nearestEndpoint != this)
                {
                    skippedCount++;
                    continue;
                }

                if (collider.gameObject == gameObject)
                {
                    skippedCount++;
                    continue;
                }

                var relay = collider.GetComponent<ActorContactDamageColliderRelay>();
                if (relay == null)
                {
                    relay = collider.gameObject.AddComponent<ActorContactDamageColliderRelay>();
                }

                relay.Configure(this, collider, source, reason);
                configuredCount++;
            }

            DebugUtility.LogVerbose(
                typeof(ActorContactDamageEndpoint),
                $"event='ActorContactDamageColliderRelaysConfigured' sourceActorId='{SourceActorId}' sourceActorInstanceRuntimeId='{_sourceActorInstanceRuntimeId}' observedColliderCount='{colliders.Length}' configuredRelayCount='{configuredCount}' skippedColliderCount='{skippedCount}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Success);
        }

        private bool IsSelfReceiver(ActorInstanceRuntimeId receiverActorInstanceRuntimeId)
        {
            return receiverActorInstanceRuntimeId.IsValid &&
                _sourceActorInstanceRuntimeId.IsValid &&
                receiverActorInstanceRuntimeId == _sourceActorInstanceRuntimeId;
        }

        private bool IsCooldownElapsed(ActorInstanceRuntimeId receiverActorInstanceRuntimeId)
        {
            if (!receiverActorInstanceRuntimeId.IsValid)
            {
                return false;
            }

            float normalizedCooldown = cooldownSecondsPerReceiver < 0f || float.IsNaN(cooldownSecondsPerReceiver) || float.IsInfinity(cooldownSecondsPerReceiver)
                ? 0f
                : cooldownSecondsPerReceiver;
            if (normalizedCooldown <= 0f)
            {
                return true;
            }

            return !_nextAllowedDamageByReceiver.TryGetValue(receiverActorInstanceRuntimeId, out float nextAllowedTime) || Time.time >= nextAllowedTime;
        }

        private void MarkCooldown(ActorInstanceRuntimeId receiverActorInstanceRuntimeId)
        {
            if (!receiverActorInstanceRuntimeId.IsValid)
            {
                return;
            }

            float normalizedCooldown = cooldownSecondsPerReceiver < 0f || float.IsNaN(cooldownSecondsPerReceiver) || float.IsInfinity(cooldownSecondsPerReceiver)
                ? 0f
                : cooldownSecondsPerReceiver;
            if (normalizedCooldown <= 0f)
            {
                return;
            }

            _nextAllowedDamageByReceiver[receiverActorInstanceRuntimeId] = Time.time + normalizedCooldown;
        }

        private bool IsLayerAllowed(int layer)
        {
            int mask = targetLayerMask.value;
            return (mask & 1 << layer) != 0;
        }

        private static ActorDamageableEndpoint ResolveDamageableEndpoint(GameObject targetObject, Collider targetCollider)
        {
            if (targetCollider != null)
            {
                var endpointFromColliderParent = targetCollider.GetComponentInParent<ActorDamageableEndpoint>(true);
                if (endpointFromColliderParent != null)
                {
                    return endpointFromColliderParent;
                }
            }

            if (targetObject == null)
            {
                return null;
            }

            var endpoint = targetObject.GetComponent<ActorDamageableEndpoint>();
            if (endpoint != null)
            {
                return endpoint;
            }

            endpoint = targetObject.transform.GetComponentInParent<ActorDamageableEndpoint>(true);
            if (endpoint != null)
            {
                return endpoint;
            }

            return targetObject.GetComponentInChildren<ActorDamageableEndpoint>(true);
        }

        private void LogRejected(
            ActorDamageableEndpoint damageableEndpoint,
            GameObject targetObject,
            Collider targetCollider,
            string outcomeReason,
            string source,
            string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActorContactDamageEndpoint),
                $"event='ActorContactDamageRejected' sourceActorId='{SourceActorId}' sourceActorInstanceRuntimeId='{_sourceActorInstanceRuntimeId}' targetActorId='{ResolveReceiverActorId(damageableEndpoint)}' targetActorInstanceRuntimeId='{ResolveReceiverActorInstanceRuntimeId(damageableEndpoint)}' targetObject='{ResolveObjectName(targetObject)}' targetCollider='{ResolveColliderName(targetCollider)}' receiver='{ResolveReceiverName(damageableEndpoint)}' outcomeReason='{outcomeReason.TrimToEmpty()}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Warning);
        }

        private static string ResolveObjectName(GameObject targetObject)
        {
            return targetObject == null ? string.Empty : targetObject.name;
        }

        private static string ResolveColliderName(Collider targetCollider)
        {
            return targetCollider == null ? string.Empty : targetCollider.name;
        }

        private static string ResolveReceiverName(ActorDamageableEndpoint damageableEndpoint)
        {
            return damageableEndpoint == null ? string.Empty : damageableEndpoint.name;
        }

        private static ActorId ResolveReceiverActorId(ActorDamageableEndpoint damageableEndpoint)
        {
            return damageableEndpoint == null ? default : damageableEndpoint.ActorId;
        }

        private static ActorInstanceRuntimeId ResolveReceiverActorInstanceRuntimeId(ActorDamageableEndpoint damageableEndpoint)
        {
            return damageableEndpoint == null ? default : damageableEndpoint.ActorInstanceRuntimeId;
        }

    }
}
