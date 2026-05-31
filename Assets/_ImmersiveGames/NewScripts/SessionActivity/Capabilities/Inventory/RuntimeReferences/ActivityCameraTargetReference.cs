using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActivityCameraTargetReference : IActivityCapabilityRuntimeReference
    {
        public ActivityCameraTargetReference(
            string capabilityId,
            string ownerId,
            ActorId actorId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId,
            string componentPath,
            Transform trackingTarget,
            Transform lookAtTarget)
        {
            CapabilityId = Normalize(capabilityId);
            OwnerId = Normalize(ownerId);
            ActorId = actorId;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
            ComponentPath = Normalize(componentPath);
            TrackingTarget = trackingTarget;
            LookAtTarget = lookAtTarget;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public ActorId ActorId { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public string ComponentPath { get; }
        public Transform TrackingTarget { get; }
        public Transform LookAtTarget { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CapabilityId) && ActorId.IsValid && PlayerActorId.IsValid && TrackingTarget != null;

        public bool Matches(PlayerActorRuntimeHandle handle)
        {
            return IsValid &&
                   handle.IsValid &&
                   PlayerActorId == handle.PlayerActorId &&
                   ActorId == handle.ActorId;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
