using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Commands
{
    public interface IPauseCommands
    {
        void RequestPause(string reason = null);
        void RequestResume(string reason = null);
    }

    /// <summary>
    /// Fachada legada temporaria para pause/resume.
    /// Victory/Defeat pertencem ao Run Pipeline.
    /// ExitToMenu pertence a Route/SessionOperational.
    /// </summary>
    public sealed class GameLoopCommands : IGameLoopCommands
    {
        public void RequestPause(string reason = null)
        {
            DebugUtility.Log(typeof(GameLoopCommands),
                $"[GameLoopCommands] RequestPause reason='{GameLoopReasonFormatter.Format(reason)}'");

            EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true, reason));
        }

        public void RequestResume(string reason = null)
        {
            DebugUtility.Log(typeof(GameLoopCommands),
                $"[GameLoopCommands] RequestResume reason='{GameLoopReasonFormatter.Format(reason)}'");

            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent(reason));
        }
    }
}
