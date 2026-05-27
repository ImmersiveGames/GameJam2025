using System;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActorPresentationEndpointReference : IActivityCapabilityRuntimeReference
    {
        public ActorPresentationEndpointReference(
            string capabilityId,
            string ownerId,
            string actorId,
            int actorInstanceId,
            string componentPath,
            ActorPresentationEndpoint endpoint)
        {
            CapabilityId = Normalize(capabilityId);
            OwnerId = Normalize(ownerId);
            ActorId = Normalize(actorId);
            ActorInstanceId = actorInstanceId;
            ComponentPath = Normalize(componentPath);
            Endpoint = endpoint;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public string ActorId { get; }
        public int ActorInstanceId { get; }
        public string ComponentPath { get; }
        public ActorPresentationEndpoint Endpoint { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(CapabilityId) &&
            !string.IsNullOrWhiteSpace(OwnerId) &&
            !string.IsNullOrWhiteSpace(ActorId) &&
            ActorInstanceId != 0 &&
            Endpoint != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
