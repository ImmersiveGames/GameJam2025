using System;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
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

