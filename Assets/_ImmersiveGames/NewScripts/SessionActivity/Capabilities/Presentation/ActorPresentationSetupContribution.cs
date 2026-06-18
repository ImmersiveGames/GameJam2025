using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation
{
    [Serializable]
    public readonly struct ActorPresentationSetupContribution
    {
        public ActorPresentationSetupContribution(
            SessionActivityIdentity identity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorSourceKind actorSourceKind,
            string participationPolicy,
            string componentPath,
            ActorPresentationEndpoint endpoint,
            ActorPresentationProfileAsset profile,
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
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
        public ActorRole ActorRole { get; }
        public ActorScope ActorScope { get; }
        public ActorSourceKind ActorSourceKind { get; }
        public string ParticipationPolicy { get; }
        public string ComponentPath { get; }
        public ActorPresentationEndpoint Endpoint { get; }
        public ActorPresentationProfileAsset Profile { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            ActorKind != ActorKind.Unknown &&
            ActorRole != ActorRole.Unknown &&
            ActorScope != ActorScope.Unknown &&
            ActorSourceKind != ActorSourceKind.Unknown &&
            !string.IsNullOrWhiteSpace(ParticipationPolicy) &&
            !string.IsNullOrWhiteSpace(ComponentPath) &&
            Endpoint != null &&
            !string.IsNullOrWhiteSpace(Source);
    }
}
