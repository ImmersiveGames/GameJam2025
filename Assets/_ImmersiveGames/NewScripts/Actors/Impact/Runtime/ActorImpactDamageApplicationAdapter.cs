using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Damage.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    public sealed class ActorImpactDamageApplicationAdapter : IActorImpactDamageApplicationAdapter
    {
        private readonly IActorDamageSourceEndpoint _damageSourceEndpoint;
        private readonly IActorAttributeEventStream _actorAttributeEventStream;

        public ActorImpactDamageApplicationAdapter(
            IActorDamageSourceEndpoint damageSourceEndpoint,
            IActorAttributeEventStream actorAttributeEventStream)
        {
            _damageSourceEndpoint = damageSourceEndpoint;
            _actorAttributeEventStream = actorAttributeEventStream;
        }

        public bool IsConfigured =>
            _damageSourceEndpoint != null &&
            _damageSourceEndpoint.IsConfigured &&
            _actorAttributeEventStream != null;

        public bool TryApplyDamage(
            ActorImpactResult impactResult,
            float rawDamageAmount,
            string source,
            string reason,
            out ActorDamageSourceResult result)
        {
            string normalizedSource = source.TrimToEmpty();
            string normalizedReason = reason.TrimToEmpty();

            DebugUtility.LogVerbose(
                typeof(ActorImpactDamageApplicationAdapter),
                $"event='ActorImpactDamageApplicationRequested' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' rawDamageAmount='{rawDamageAmount:0.###}' impactKind='{impactResult.Intent.ImpactKind.TrimToEmpty()}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info);

            if (!impactResult.Registered || !impactResult.HasIntent || !impactResult.HasResolvedTargetActor)
            {
                result = ActorDamageSourceResult.Reject(
                    impactResult.Intent.OwnerActorId,
                    impactResult.Intent.OwnerActorInstanceRuntimeId,
                    impactResult.Intent.TargetActorId,
                    impactResult.Intent.TargetActorInstanceRuntimeId,
                    rawDamageAmount,
                    "impact_result_not_registered_or_target_unresolved");
                LogRejected(impactResult, rawDamageAmount, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!IsConfigured)
            {
                result = ActorDamageSourceResult.Reject(
                    impactResult.Intent.OwnerActorId,
                    impactResult.Intent.OwnerActorInstanceRuntimeId,
                    impactResult.Intent.TargetActorId,
                    impactResult.Intent.TargetActorInstanceRuntimeId,
                    rawDamageAmount,
                    "impact_damage_adapter_not_configured");
                LogRejected(impactResult, rawDamageAmount, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (rawDamageAmount <= 0f || float.IsNaN(rawDamageAmount) || float.IsInfinity(rawDamageAmount))
            {
                result = ActorDamageSourceResult.Reject(
                    impactResult.Intent.OwnerActorId,
                    impactResult.Intent.OwnerActorInstanceRuntimeId,
                    impactResult.Intent.TargetActorId,
                    impactResult.Intent.TargetActorInstanceRuntimeId,
                    rawDamageAmount,
                    "impact_damage_amount_invalid");
                LogRejected(impactResult, rawDamageAmount, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!TryResolveTargetDamageableEndpoint(impactResult.Target, out IActorDamageableEndpoint targetDamageableEndpoint, out string targetResolutionReason))
            {
                result = ActorDamageSourceResult.Reject(
                    impactResult.Intent.OwnerActorId,
                    impactResult.Intent.OwnerActorInstanceRuntimeId,
                    impactResult.Intent.TargetActorId,
                    impactResult.Intent.TargetActorInstanceRuntimeId,
                    rawDamageAmount,
                    targetResolutionReason);
                LogRejected(impactResult, rawDamageAmount, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            ActorDamageSourceIntent damageIntent = new(
                _damageSourceEndpoint.ActivityIdentity,
                impactResult.Intent.OwnerActorId,
                impactResult.Intent.OwnerActorInstanceRuntimeId,
                impactResult.Intent.TargetActorId,
                impactResult.Intent.TargetActorInstanceRuntimeId,
                rawDamageAmount,
                impactResult.Intent.ImpactKind,
                normalizedSource,
                normalizedReason);

            if (!_damageSourceEndpoint.TryEmitDamageIntent(damageIntent, targetDamageableEndpoint, out result) ||
                result.Rejected ||
                result.Failed)
            {
                LogRejected(impactResult, rawDamageAmount, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!result.HasDamageResult ||
                !result.DamageResult.HasMutationResult ||
                !result.DamageResult.MutationResult.HasApplyResult ||
                !result.DamageResult.MutationResult.ApplyResult.HasFact)
            {
                result = ActorDamageSourceResult.Fail(
                    impactResult.Intent.OwnerActorId,
                    impactResult.Intent.OwnerActorInstanceRuntimeId,
                    impactResult.Intent.TargetActorId,
                    impactResult.Intent.TargetActorInstanceRuntimeId,
                    rawDamageAmount,
                    "impact_damage_source_result_fact_missing");
                LogRejected(impactResult, rawDamageAmount, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            ActorAttributeApplyResult applyResult = result.DamageResult.MutationResult.ApplyResult;
            if (!ActorAttributeApplyResultEventPublisher.TryPublish(
                    _actorAttributeEventStream,
                    result.TargetActorId,
                    applyResult,
                    normalizedSource,
                    normalizedReason))
            {
                result = ActorDamageSourceResult.Fail(
                    impactResult.Intent.OwnerActorId,
                    impactResult.Intent.OwnerActorInstanceRuntimeId,
                    impactResult.Intent.TargetActorId,
                    impactResult.Intent.TargetActorInstanceRuntimeId,
                    rawDamageAmount,
                    "impact_damage_apply_result_publish_failed");
                LogRejected(impactResult, rawDamageAmount, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            ActorAttributeChangedFact fact = applyResult.Fact;
            DebugUtility.LogVerbose(
                typeof(ActorImpactDamageApplicationAdapter),
                $"event='ActorImpactDamageApplicationApplied' sourceActorId='{result.SourceActorId}' sourceActorInstanceRuntimeId='{result.SourceActorInstanceRuntimeId}' targetActorId='{result.TargetActorId}' targetActorInstanceRuntimeId='{result.TargetActorInstanceRuntimeId}' targetAttributeId='{fact.AttributeId}' rawDamageAmount='{rawDamageAmount:0.###}' effectiveDamageAmount='{result.DamageResult.EffectiveDamageAmount:0.###}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' thresholdFactCount='{applyResult.ThresholdFactCount}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Success);

            DebugUtility.LogVerbose(
                typeof(ActorImpactDamageApplicationAdapter),
                $"event='ActorImpactDamageApplicationPublished' sourceActorId='{result.SourceActorId}' targetActorId='{result.TargetActorId}' targetAttributeId='{fact.AttributeId}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Success);
            return true;
        }

        private static bool TryResolveTargetDamageableEndpoint(
            ActorImpactTarget target,
            out IActorDamageableEndpoint endpoint,
            out string reason)
        {
            endpoint = null;
            reason = string.Empty;

            if (!target.HasTargetActor)
            {
                reason = "impact_target_actor_missing_or_invalid";
                return false;
            }

            if (target.TargetActor.CapabilitySurface != null &&
                target.TargetActor.CapabilitySurface.TryGetEndpoint(out ActorDamageableEndpoint surfaceEndpoint) &&
                surfaceEndpoint != null &&
                surfaceEndpoint.IsConfigured)
            {
                endpoint = surfaceEndpoint;
                return true;
            }

            ActorDamageableEndpoint localEndpoint = target.TargetActor.GetComponentInChildren<ActorDamageableEndpoint>(includeInactive: true);
            if (localEndpoint != null && localEndpoint.IsConfigured)
            {
                endpoint = localEndpoint;
                return true;
            }

            reason = "impact_target_damageable_endpoint_not_ready";
            return false;
        }

        private static void LogRejected(
            ActorImpactResult impactResult,
            float rawDamageAmount,
            string rejectionReason,
            string source,
            string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActorImpactDamageApplicationAdapter),
                $"event='ActorImpactDamageApplicationRejected' impactActorId='{impactResult.Intent.ImpactActorId}' ownerActorId='{impactResult.Intent.OwnerActorId}' targetActorId='{impactResult.Intent.TargetActorId}' rawDamageAmount='{rawDamageAmount:0.###}' outcomeReason='{rejectionReason.TrimToEmpty()}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'",
                DebugUtility.Colors.Warning);
        }
}
}
