using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes
{
    [Serializable]
    public readonly struct ActorAttributeSetupContribution
    {
        public ActorAttributeSetupContribution(
            SessionActivityIdentity identity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
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
            ParticipationPolicy = participationPolicy.TrimToEmpty();
            ComponentPath = componentPath.TrimToEmpty();
            Endpoint = endpoint;
            Profile = profile;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId => Identity.ActivityId;
        public int EntrySequence => Identity.EntrySequence;
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
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
    }
}
