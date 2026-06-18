using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    public sealed class ActorImpactTargetResolver : IActorImpactTargetResolver
    {
        public bool TryResolveImpactTarget(
            GameObject targetObject,
            Collider targetCollider,
            string source,
            string reason,
            out ActorImpactTarget target)
        {
            GameObject resolvedTargetObject = targetObject;
            if (resolvedTargetObject == null && targetCollider != null)
            {
                resolvedTargetObject = targetCollider.gameObject;
            }

            if (resolvedTargetObject == null)
            {
                target = ActorImpactTarget.Unresolved(null, targetCollider, "impact_target_object_missing");
                LogResolveSkipped(target, source, reason);
                return false;
            }

            Actor targetActor = resolvedTargetObject.GetComponentInParent<Actor>();
            if (targetActor == null || !targetActor.ActorIdValue.IsValid || !targetActor.RuntimeActorInstanceId.IsValid)
            {
                target = ActorImpactTarget.Unresolved(resolvedTargetObject, targetCollider, "impact_target_actor_missing_or_invalid");
                LogResolveSkipped(target, source, reason);
                return false;
            }

            target = new ActorImpactTarget(
                resolvedTargetObject,
                targetCollider,
                targetActor,
                targetActor.ActorIdValue,
                targetActor.RuntimeActorInstanceId,
                "impact_target_actor_resolved");

            DebugUtility.LogVerbose(
                typeof(ActorImpactTargetResolver),
                $"event='ActorImpactTargetResolved' targetActorId='{target.TargetActorId}' targetActorInstanceRuntimeId='{target.TargetActorInstanceRuntimeId}' targetObject='{target.TargetObjectName}' targetCollider='{target.TargetColliderName}' source='{Normalize(source)}' reason='{Normalize(reason)}'",
                DebugUtility.Colors.Success);

            return true;
        }

        private static void LogResolveSkipped(
            ActorImpactTarget target,
            string source,
            string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActorImpactTargetResolver),
                $"event='ActorImpactTargetResolveSkipped' targetObject='{target.TargetObjectName}' targetCollider='{target.TargetColliderName}' outcomeReason='{Normalize(target.Reason)}' source='{Normalize(source)}' reason='{Normalize(reason)}'",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
