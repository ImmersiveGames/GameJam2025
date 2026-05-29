using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActorPresentationEndpointReference : IActivityCapabilityRuntimeReference
    {
        public ActorPresentationEndpointReference(
            string capabilityId,
            string ownerId,
            ActorInstanceId actorInstanceRuntimeId,
            string actorId,
            ActorKind actorKind,
            ActorRole actorRole,
            ActorScope actorScope,
            string componentPath,
            ActorPresentationEndpoint endpoint)
        {
            CapabilityId = Normalize(capabilityId);
            OwnerId = Normalize(ownerId);
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorId = Normalize(actorId);
            ActorKind = actorKind;
            ActorRole = actorRole;
            ActorScope = actorScope;
            ComponentPath = Normalize(componentPath);
            Endpoint = endpoint;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public ActorInstanceId ActorInstanceRuntimeId { get; }
        public string ActorId { get; }
        public ActorKind ActorKind { get; }
        public ActorRole ActorRole { get; }
        public ActorScope ActorScope { get; }
        public string ComponentPath { get; }
        public ActorPresentationEndpoint Endpoint { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(CapabilityId) &&
            !string.IsNullOrWhiteSpace(OwnerId) &&
            ActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(ActorId) &&
            ActorKind != ActorKind.Unknown &&
            ActorRole != ActorRole.Unknown &&
            ActorScope != ActorScope.Unknown &&
            Endpoint != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
