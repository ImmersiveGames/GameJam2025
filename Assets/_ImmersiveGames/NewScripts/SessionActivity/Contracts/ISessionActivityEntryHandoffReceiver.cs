namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public interface ISessionActivityEntryHandoffReceiver
    {
        string SessionId { get; }

        SessionActivityCommandResult StartFromPreparedHandoff(
            SessionActivityEntryHandoff handoff,
            string source,
            string reason);
    }
}
