using System;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ActivityObjectDefaultResetEndpoint : MonoBehaviour, IActivityObjectResetEndpoint
    {
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

            // F6C: endpoint minimo observacional sem regra de gameplay.
            return new ActivityObjectResetResult(
                ActivityObjectResetResultKind.Applied,
                command,
                command.Source,
                command.Reason,
                $"Activity object reset applied targetId='{command.TargetId}' resetGroup='{command.ResetGroup}' endpoint='{nameof(ActivityObjectDefaultResetEndpoint)}'.");
        }
    }
}
