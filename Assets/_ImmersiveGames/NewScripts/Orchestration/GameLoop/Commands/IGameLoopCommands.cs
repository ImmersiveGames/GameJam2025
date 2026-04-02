namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.Commands
{
    /// <summary>
    /// API oficial para disparar comandos de gameplay (pause/resume/victory/defeat/restart/exit-to-menu).
    /// </summary>
    public interface IGameLoopCommands : IPauseCommands
    {
        void RequestVictory(string reason);
        void RequestDefeat(string reason);
        void RequestRestart(string reason);
        void RequestExitToMenu(string reason);
    }
}

