using System;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline
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
