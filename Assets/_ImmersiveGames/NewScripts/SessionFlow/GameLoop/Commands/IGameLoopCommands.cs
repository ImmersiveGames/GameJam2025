namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Commands
{
    /// <summary>
    /// API oficial para disparar comandos de gameplay (pause/resume/victory/defeat/exit-to-menu).
    /// </summary>
    public interface IGameLoopCommands : IPauseCommands
    {
        void RequestVictory(string reason);
        void RequestDefeat(string reason);
        void RequestExitToMenu(string reason);
    }
}


