using System;
using UnityEngine;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine.SceneManagement;
using UnityUtils;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts
{
    [DefaultExecutionOrder(-100), DebugLevel(DebugLevel.Verbose)]
    public sealed class GameManager : Singleton<GameManager>
    {
        [SerializeField] public GameConfig gameConfig;
        [SerializeField] private Transform player;
        [SerializeField] private Transform worldEater;
        public Transform Player => player;
        public Transform WorldEater => worldEater;
        public string Score { get; private set; }
        private bool _isPlaying;
        private bool _isGameOver;
        private bool _isVictory;
        private bool _isInitialized;
        
        public event Action EventStartGame;
        public event Action EventGameOver;
        public event Action EventVictory;
        public event Action<bool> EventPauseGame;

        protected override void Awake()
        {
            base.Awake();
            // Inicializa o gerenciador de estados
            GameManagerStateMachine.Instance.InitializeStateMachine(this);
            if(!SceneManager.GetSceneByName("UI").isLoaded)
                SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
        }

        private void OnDestroy()
        {
            // Limpar eventos para evitar memory leaks
            EventStartGame = null;
            EventGameOver = null;
            EventVictory = null;
            EventPauseGame = null;

            // Destruir o GameManagerStateMachine
            if (GameManagerStateMachine.Instance != null)
            {
                Destroy(GameManagerStateMachine.Instance.gameObject);
            }
        }

        public bool ShouldPlayingGame() => _isPlaying && !_isVictory && !_isGameOver;

        public void ForceReset()
        {
            DebugUtility.LogVerbose<GameManager>($"Forçou o reset do jogo, Initialized={_isInitialized}, Playing={_isPlaying}, Victory={_isVictory}, GameOver={_isGameOver}");
            _isInitialized = false;
            SetPlayGame(true);
        }
       
        public void SetGameOver(bool gameOver)
        {
            _isGameOver = gameOver;
            _isPlaying = !gameOver;
            _isVictory = !gameOver;
            if (!gameOver) return;
            _isInitialized = false; // Reset para próximo jogo
            EventGameOver?.Invoke();
            DebugUtility.LogVerbose<GameManager>($"Setou o Fim do Jogo");
        }

        public void SetVictory(bool victory)
        {
            _isGameOver = !victory;
            _isPlaying = !victory;
            _isVictory = victory;
            if (!victory) return;
            _isInitialized = false; // Reset para próximo jogo
            EventVictory?.Invoke();
            DebugUtility.LogVerbose<GameManager>($"Setou a vitória do jogo");
        }
        
        public void SetPlayGame(bool play)
        {
            _isPlaying = play;
            _isGameOver = !play;
            _isVictory = !play;

            if (play)
            {
                if (!_isInitialized)
                {
                    _isInitialized = true;
                    EventStartGame?.Invoke();
                    DebugUtility.LogVerbose<GameManager>($"Iniciou um novo jogo");
                }
                else
                {
                    EventPauseGame?.Invoke(false);
                    DebugUtility.LogVerbose<GameManager>($"Despausou o jogo");
                }
            }
            else
            {
                EventPauseGame?.Invoke(true);
                DebugUtility.LogVerbose<GameManager>($"Pausou o jogo");
            }
        }
        
        public bool CheckGameOver() => !_isPlaying && _isGameOver && !_isVictory;

        public bool CheckVictory() => !_isPlaying && _isVictory && !_isGameOver;

        public void SetScore(string score)
        {
            Score = score;
            DebugUtility.LogVerbose<GameManager>($"Pontuação: {score}");
        }
    }
}