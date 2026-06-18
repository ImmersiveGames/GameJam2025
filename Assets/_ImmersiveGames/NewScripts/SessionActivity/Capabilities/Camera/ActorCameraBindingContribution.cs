using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera
{
    [Serializable]
    public readonly struct ActorCameraBindingContribution
    {
        public ActorCameraBindingContribution(
            SessionActivityIdentity identity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            ActorRole actorRole,
            ActorScope actorScope,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId,
            string componentPath,
            IActorCameraTargetEndpoint endpoint,
            string source,
            string reason)
        {
            Identity = identity;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind;
            ActorRole = actorRole;
            ActorScope = actorScope;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
            ComponentPath = componentPath.TrimToEmpty();
            Endpoint = endpoint;
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
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public string ComponentPath { get; }
        public IActorCameraTargetEndpoint Endpoint { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            PlayerActorId.IsValid &&
            PlayerSlotId.IsValid &&
            !string.IsNullOrWhiteSpace(ComponentPath) &&
            Endpoint != null &&
            !string.IsNullOrWhiteSpace(Source);

        public bool Matches(PlayerActorRuntimeHandle handle)
        {
            return IsValid &&
                   handle.IsValid &&
                   ActorInstanceRuntimeId == handle.ActorInstanceRuntimeId &&
                   ActorId == handle.ActorId;
        }
}
}
