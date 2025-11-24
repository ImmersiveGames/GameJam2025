using UnityEngine;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ImmersiveGames/GameConfig", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("Tempo e Área")]
        [SerializeField] private int timerGame = 300;
        [SerializeField] private Rect gameArea = new(-50f, -50f, 100f, 100f); // x, z, width, height

        [Header("Depuração")]
        [Tooltip("Quando marcado, força verbosidade e níveis de log voltados a QA.")]
        [SerializeField] private bool debugMode;

        public int TimerSeconds => Mathf.Max(timerGame, 0);
        public Rect GameArea => gameArea;
        public bool DebugMode => debugMode;
    }
}
