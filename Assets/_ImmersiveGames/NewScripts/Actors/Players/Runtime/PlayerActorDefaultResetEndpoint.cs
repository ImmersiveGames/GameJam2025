using System;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorDefaultResetEndpoint : MonoBehaviour, IActorResetEndpoint
    {
        public bool Supports(ActorResetGroup group)
        {
            return group == ActorResetGroup.Placement ||
                group == ActorResetGroup.ActivityParticipation;
        }

        public void ApplyReset(ActorResetContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("PlayerActorDefaultResetEndpoint received invalid reset context.");
            }

            if (context.Group == ActorResetGroup.Placement)
            {
                if (context.PlacementRequired && !context.HasPlacement)
                {
                    throw new InvalidOperationException($"Placement reset is required but missing for actorId='{context.Actor.ActorId}'.");
                }

                if (!context.HasPlacement)
                {
                    return;
                }

                transform.localPosition = context.PlacementPosition;
                transform.localRotation = Quaternion.Euler(context.PlacementEulerAngles);
                return;
            }

            if (context.Group == ActorResetGroup.ActivityParticipation)
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
