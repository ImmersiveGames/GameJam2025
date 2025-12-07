using _ImmersiveGames.Scripts.SceneManagement.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ImmersiveGames/GameConfig", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("Cenas (legado)")]
        [SerializeField] private string menuScene = "MenuScene";
        [SerializeField] private string gameplayScene = "GameplayScene";
        [SerializeField] private string uiScene = "UIScene";

        [Header("Scene Setups (opcional)")]
        [Tooltip("Setup de cenas para o menu principal. Se definido, tem prioridade sobre as strings de cena legadas.")]
        [SerializeField] private SceneSetup menuSetup;

        [Tooltip("Setup de cenas para o gameplay (normalmente Gameplay + UI). Se definido, tem prioridade sobre as strings de cena legadas.")]
        [SerializeField] private SceneSetup gameplaySetup;

        public string MenuScene => menuScene;
        public string GameplayScene => gameplayScene;
        public string UIScene => uiScene;

        public SceneSetup MenuSetup => menuSetup;
        public SceneSetup GameplaySetup => gameplaySetup;

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