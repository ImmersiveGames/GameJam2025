using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ActivityObjectDefaultReleaseEndpoint : MonoBehaviour, IActivityObjectReleaseEndpoint, IActivityObjectLifecycleContributionProvider
    {
        public void CollectActivityObjectLifecycleContributions(
            ActivityObjectLifecycleContributionContext context,
            IList<IActivityObjectLifecycleContribution> contributions)
        {
            if (contributions == null || !context.IsValid)
            {
                return;
            }

            contributions.Add(new ActivityObjectReleaseContribution(
                $"activity_object.release:{context.TargetId}:{nameof(ActivityObjectDefaultReleaseEndpoint)}",
                400,
                this));
        }

        public bool Supports(ActivityReleaseRequirementKind releaseKind)
        {
            return releaseKind != ActivityReleaseRequirementKind.Unknown;
        }

        public ActivityObjectReleaseResult ApplyRelease(ActivityObjectReleaseCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectReleaseCommand is invalid.");
            }

            // Endpoint mínimo observacional. A decisão de quando liberar permanece no stage/pipeline.
            return new ActivityObjectReleaseResult(
                ActivityObjectReleaseResultKind.Applied,
                command,
                command.Source,
                command.Reason,
                $"Activity object release applied targetId='{command.TargetId}' releaseKind='{command.ReleaseKind}' endpoint='{nameof(ActivityObjectDefaultReleaseEndpoint)}'.");
        }
    }
}
