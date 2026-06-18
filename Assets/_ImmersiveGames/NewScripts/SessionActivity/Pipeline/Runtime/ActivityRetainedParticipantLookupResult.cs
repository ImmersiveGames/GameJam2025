using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.UnityUtils;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    public enum ActivityRetainedParticipantLookupOutcomeKind
    {
        Unknown = 0,
        Resolved = 1,
        Missed = 2,
        RejectedStale = 3,
        RejectedForeign = 4,
    }

    public enum ActivityRetainedParticipantLookupSourceKind
    {
        Unknown = 0,
        None = 1,
        ActiveRegistry = 2,
        RouteRegistry = 3,
        SessionActorStore = 4,
    }

    public readonly struct ActivityRetainedParticipantLookupResult
    {
        public ActivityRetainedParticipantLookupResult(
            ActivityRetainedParticipantLookupCommand command,
            ActivityRetainedParticipantLookupOutcomeKind outcomeKind,
            ActivityRetainedParticipantLookupSourceKind sourceKind,
            PlayerActivityParticipantBinding participantBinding,
            PlayerActorRuntimeHandle retainedHandle,
            string detail)
        {
            Command = command;
            OutcomeKind = outcomeKind;
            SourceKind = sourceKind;
            ParticipantBinding = participantBinding;
            RetainedHandle = retainedHandle;
            Detail = detail.TrimToEmpty();
        }

        public ActivityRetainedParticipantLookupCommand Command { get; }
        public ActivityRetainedParticipantLookupOutcomeKind OutcomeKind { get; }
        public ActivityRetainedParticipantLookupSourceKind SourceKind { get; }
        public PlayerActivityParticipantBinding ParticipantBinding { get; }
        public PlayerActorRuntimeHandle RetainedHandle { get; }
        public string Detail { get; }

        public bool IsValid =>
            Command.IsValid &&
            OutcomeKind != ActivityRetainedParticipantLookupOutcomeKind.Unknown &&
            SourceKind != ActivityRetainedParticipantLookupSourceKind.Unknown;

        public bool IsResolved =>
            OutcomeKind == ActivityRetainedParticipantLookupOutcomeKind.Resolved &&
            ParticipantBinding.IsValid &&
            RetainedHandle.IsValid;

        public bool IsRejected =>
            OutcomeKind == ActivityRetainedParticipantLookupOutcomeKind.RejectedStale ||
            OutcomeKind == ActivityRetainedParticipantLookupOutcomeKind.RejectedForeign;

        public bool ShouldMaterializeNew => OutcomeKind == ActivityRetainedParticipantLookupOutcomeKind.Missed;
}
}
