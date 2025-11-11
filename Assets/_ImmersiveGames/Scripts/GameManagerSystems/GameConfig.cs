using UnityEngine;
namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ImmersiveGames/GameConfig", order = 0)]
    public class GameConfig : ScriptableObject
    {
        public int timerGame = 300;
        public Rect gameArea = new(-50f, -50f, 100f, 100f); // x, z, width, height

        public bool DebugMode { get; set; }
    }
}