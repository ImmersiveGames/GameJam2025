using System;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ActivityObjectDefaultReleaseEndpoint : MonoBehaviour, IActivityObjectReleaseEndpoint
    {
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

            // F6D: endpoint minimo observacional sem regra de gameplay.
            return new ActivityObjectReleaseResult(
                ActivityObjectReleaseResultKind.Applied,
                command,
                command.Source,
                command.Reason,
                $"Activity object release applied targetId='{command.TargetId}' releaseKind='{command.ReleaseKind}' endpoint='{nameof(ActivityObjectDefaultReleaseEndpoint)}'.");
        }
    }
}
