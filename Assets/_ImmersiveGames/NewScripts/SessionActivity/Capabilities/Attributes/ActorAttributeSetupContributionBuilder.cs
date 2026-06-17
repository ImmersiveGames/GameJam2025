using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes
{
    public static class ActorAttributeSetupContributionBuilder
    {
        public static bool TryBuild(
            SessionActivityIdentity identity,
            ActorScanTarget target,
            ActorAttributeEndpoint endpoint,
            string source,
            string reason,
            out ActorAttributeSetupContribution contribution)
        {
            contribution = default;
            if (!identity.IsValid || !target.IsValid || endpoint == null)
            {
                return false;
            }

            contribution = new ActorAttributeSetupContribution(
                identity,
                new ActorId(target.ActorId),
                target.ActorInstanceRuntimeId,
                target.ActorKind,
                target.ActorRole,
                target.ActorScope,
                target.ActorSourceKind,
                target.ParticipationPolicy,
                ActivityCapabilityTransformPathUtility.BuildTransformPath(endpoint.transform),
                endpoint,
                endpoint.AttributeProfile,
                source,
                reason);
            return contribution.IsValid;
        }
    }
}
