using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera
{
    public static class ActorCameraBindingContributionBuilder
    {
        public static bool TryBuild(
            SessionActivityIdentity identity,
            ActorScanTarget target,
            PlayerActorCapabilityIdentity playerIdentity,
            IActorCameraTargetEndpoint endpoint,
            string source,
            string reason,
            out ActorCameraBindingContribution contribution)
        {
            contribution = default;
            if (!identity.IsValid || !target.IsValid || !playerIdentity.IsValid || endpoint == null)
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
                playerIdentity.PlayerActorId,
                playerIdentity.PlayerSlotId,
                ActivityCapabilityTransformPathUtility.BuildTransformPath(endpoint is Component component ? component.transform : null),
                endpoint,
                source,
                reason);
            return contribution.IsValid;
        }
    }
}
