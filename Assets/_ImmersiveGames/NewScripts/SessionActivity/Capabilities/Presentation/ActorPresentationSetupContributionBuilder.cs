using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation
{
    public static class ActorPresentationSetupContributionBuilder
    {
        public static bool TryBuild(
            SessionActivityIdentity identity,
            ActorScanTarget target,
            ActorPresentationEndpoint endpoint,
            string source,
            string reason,
            out ActorPresentationSetupContribution contribution)
        {
            contribution = default;
            if (!identity.IsValid || !target.IsValid || endpoint == null)
            {
                return false;
            }

            contribution = new ActorPresentationSetupContribution(
                identity,
                new ActorId(target.ActorId),
                target.ActorInstanceId,
                target.ActorKind,
                target.ActorRole,
                target.ActorScope,
                target.ActorSourceKind,
                target.ParticipationPolicy,
                ActivityCapabilityTransformPathUtility.BuildTransformPath(endpoint.transform),
                endpoint,
                endpoint.Profile,
                source,
                reason);
            return contribution.IsValid;
        }
    }
}
