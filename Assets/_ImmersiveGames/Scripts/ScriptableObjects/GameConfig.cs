using UnityEngine;
namespace _ImmersiveGames.Scripts.ScriptableObjects
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ImmersiveGames/GameConfig", order = 0)]
    public class GameConfig : ScriptableObject
    {
        public int timerGame = 300;

    }
}