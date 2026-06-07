using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes
{
    [Serializable]
    public readonly struct ActorAttributeSetupContribution
    {
        public ActorAttributeSetupContribution(
            SessionActivityIdentity identity,
            ActorId actorId,
            ActorInstanceId actorInstanceRuntimeId,
            ActorKind actorKind,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorSourceKind actorSourceKind,
            string participationPolicy,
            string componentPath,
            ActorAttributeEndpoint endpoint,
            ActorAttributeProfileAsset profile,
            string source,
            string reason)
        {
            Identity = identity;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind;
            ActorRole = actorRole;
            ActorScope = actorScope;
            ActorSourceKind = actorSourceKind;
            ParticipationPolicy = Normalize(participationPolicy);
            ComponentPath = Normalize(componentPath);
            Endpoint = endpoint;
            Profile = profile;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId => Identity.ActivityId;
        public int EntrySequence => Identity.EntrySequence;
        public ActorId ActorId { get; }
        public ActorInstanceId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
        public ActorRole ActorRole { get; }
        public ActorScope ActorScope { get; }
        public ActorSourceKind ActorSourceKind { get; }
        public string ParticipationPolicy { get; }
        public string ComponentPath { get; }
        public ActorAttributeEndpoint Endpoint { get; }
        public ActorAttributeProfileAsset Profile { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(ComponentPath) &&
            Endpoint != null &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
