namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    public interface IGameManager
    {
        GameConfig GameConfig { get; }
        bool IsGameActive();
        void ResetGame();
        bool TryTriggerGameOver(string reason = null);
        bool TryTriggerVictory(string reason = null);
    }
}
