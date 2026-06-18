using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using PlayerActivityParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipationContext;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    public readonly struct ActivityRetainedParticipantLookupCommand
    {
        public ActivityRetainedParticipantLookupCommand(
            SessionActivityIdentity identity,
            PlayerActivityParticipantBinding participantBinding,
            PlayerActivityParticipationContext retainedParticipationContext,
            string source,
            string reason)
        {
            Identity = identity;
            ParticipantBinding = participantBinding;
            RetainedParticipationContext = retainedParticipationContext;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public PlayerActivityParticipantBinding ParticipantBinding { get; }
        public PlayerActivityParticipationContext RetainedParticipationContext { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ParticipantBinding.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
}
}
