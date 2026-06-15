namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal interface IActivityRetainedParticipantLookup
    {
        ActivityRetainedParticipantLookupResult Execute(ActivityRetainedParticipantLookupCommand command);
    }
}
