using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ActivityObjectDefaultResetEndpoint : MonoBehaviour, IActivityObjectResetEndpoint, IActivityObjectLifecycleContributionProvider
    {
        public void CollectActivityObjectLifecycleContributions(
            ActivityObjectLifecycleContributionContext context,
            IList<IActivityObjectLifecycleContribution> contributions)
        {
            if (contributions == null || !context.IsValid)
            {
                return;
            }

            contributions.Add(new ActivityObjectResetContribution(
                $"activity_object.reset:{context.TargetId}:{nameof(ActivityObjectDefaultResetEndpoint)}",
                100,
                this));
        }

        public bool Supports(ActivityStateResetGroup resetGroup)
        {
            return resetGroup != ActivityStateResetGroup.Unknown;
        }

        public ActivityObjectResetResult ApplyReset(ActivityObjectResetCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectResetCommand is invalid.");
            }

            // Endpoint mínimo observacional. A decisão de quando resetar permanece no stage/pipeline.
            return new ActivityObjectResetResult(
                ActivityObjectResetResultKind.Applied,
                command,
                command.Source,
                command.Reason,
                $"Activity object reset applied targetId='{command.TargetId}' resetGroup='{command.ResetGroup}' endpoint='{nameof(ActivityObjectDefaultResetEndpoint)}'.");
        }
    }
}
