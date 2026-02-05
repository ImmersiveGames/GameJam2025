namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Commands
{
    /// <summary>
    /// API oficial para disparar comandos de gameplay (pause/resume/victory/defeat/restart/exit-to-menu).
    /// </summary>
    public interface IGameCommands
    {
        void RequestPause(string reason = null);
        void RequestResume(string reason = null);
        void RequestVictory(string reason);
        void RequestDefeat(string reason);
        void RequestRestart(string reason);
        void RequestExitToMenu(string reason);
    }
}

