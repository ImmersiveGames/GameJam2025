using UnityEngine;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    public interface IGameManager
    {
        GameConfig GameConfig { get; }
        Transform WorldEater { get; }
        bool IsGameActive();
        void ResetGame();
        bool TryTriggerGameOver(string reason = null);
        bool TryTriggerVictory(string reason = null);
    }
}
