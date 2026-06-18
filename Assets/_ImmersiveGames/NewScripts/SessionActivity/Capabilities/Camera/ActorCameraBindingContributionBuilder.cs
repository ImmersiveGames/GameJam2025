using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera
{
    public static class ActorCameraBindingContributionBuilder
    {
        public static bool TryBuild(
            SessionActivityIdentity identity,
            ActorScanTarget target,
            IActorCameraTargetEndpoint endpoint,
            string source,
            string reason,
            out ActorCameraBindingContribution contribution)
        {
            contribution = default;
            if (!identity.IsValid || !target.IsValid || endpoint == null)
            {
                return false;
            }

            if (!TryResolvePlayerIdentity(target, out var playerActorId, out var playerSlotId))
            {
                return false;
            }

            contribution = new ActorCameraBindingContribution(
                identity,
                new ActorId(target.ActorId),
                target.ActorInstanceRuntimeId,
                target.ActorKind,
                target.ActorRole,
                target.ActorScope,
                playerActorId,
                playerSlotId,
                ActivityCapabilityTransformPathUtility.BuildTransformPath(endpoint is Component component ? component.transform : null),
                endpoint,
                source,
                reason);
            return contribution.IsValid;
        }

        private static bool TryResolvePlayerIdentity(
            ActorScanTarget target,
            out PlayerActorId playerActorId,
            out PlayerSlotId playerSlotId)
        {
            playerActorId = default;
            playerSlotId = default;

            if (target.RuntimeActor == null)
            {
                return false;
            }

            var playerIdentity = target.RuntimeActor.GetComponent<PlayerActorIdentity>();
            if (playerIdentity == null || !playerIdentity.IsValid)
            {
                return false;
            }

            playerActorId = playerIdentity.PlayerActorId;
            playerSlotId = playerIdentity.PlayerSlotId;
            return playerActorId.IsValid && playerSlotId.IsValid;
        }
    }
}
