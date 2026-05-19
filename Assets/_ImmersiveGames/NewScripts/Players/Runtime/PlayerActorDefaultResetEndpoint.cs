using System;
using _ImmersiveGames.NewScripts.Players.ActivitySetup;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorDefaultResetEndpoint : MonoBehaviour, IPlayerActorResetEndpoint
    {
        public bool Supports(PlayerActorResetGroup group)
        {
            return group == PlayerActorResetGroup.Placement ||
                group == PlayerActorResetGroup.ActivityParticipation;
        }

        public void ApplyReset(PlayerActorResetContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("PlayerActorDefaultResetEndpoint received invalid reset context.");
            }

            if (context.Group == PlayerActorResetGroup.Placement)
            {
                if (context.PlacementRequired && !context.HasPlacement)
                {
                    throw new InvalidOperationException($"Placement reset is required but missing for playerActorId='{context.ActorIdentity.PlayerActorId}'.");
                }

                if (!context.HasPlacement)
                {
                    return;
                }

                transform.localPosition = context.PlacementLocalPosition;
                transform.localRotation = Quaternion.Euler(context.PlacementLocalEulerAngles);
                return;
            }

            if (context.Group == PlayerActorResetGroup.ActivityParticipation)
            {
                PlayerActorParticipationState participation = GetComponent<PlayerActorParticipationState>();
                if (participation == null)
                {
                    participation = gameObject.AddComponent<PlayerActorParticipationState>();
                }

                participation.MarkActiveInActivity(context.PipelineIdentity.ActivityId, context.PipelineIdentity.EntrySequence);
            }
        }
    }
}
