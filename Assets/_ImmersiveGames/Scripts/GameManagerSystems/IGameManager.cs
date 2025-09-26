using _ImmersiveGames.Scripts.ScriptableObjects;
namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    public interface IGameManager
    {
        GameConfig GameConfig { get; }
        bool IsGameActive();
        void ResetGame();
    }
}