using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;
namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [DefaultExecutionOrder(-100), DebugLevel(DebugLevel.Verbose)]
    public sealed class GameManager : Singleton<GameManager>
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Transform player;
        [SerializeField] private Transform worldEater;
        public Transform Player => player;
        public Transform WorldEater => worldEater;
        public GameConfig GameConfig => gameConfig;
        
        
        private bool _isPlaying;
        private bool _isGameOver;
        private bool _isVictory;
        private bool _isInitialized;
        
        protected override void Awake()
        {
            base.Awake();
            // Inicializa o gerenciador de estados
            GameManagerStateMachine.Instance.InitializeStateMachine(this);
            if(!SceneManager.GetSceneByName("UI").isLoaded)
                SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
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
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
            DebugUtility.LogVerbose<GameManager>($"Setou o Fim do Jogo");
        }

        public void SetVictory(bool victory)
        {
            _isGameOver = !victory;
            _isPlaying = !victory;
            _isVictory = victory;
            if (!victory) return;
            _isInitialized = false; // Reset para próximo jogo
            EventBus<GameVictoryEvent>.Raise(new GameVictoryEvent());
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
                    EventBus<GameStartEvent>.Raise(new GameStartEvent());
                    DebugUtility.LogVerbose<GameManager>($"Iniciou um novo jogo");
                }
                else
                {
                    EventBus<GamePauseEvent>.Raise(new GamePauseEvent(false));
                    DebugUtility.LogVerbose<GameManager>($"Despausou o jogo");
                }
            }
            else
            {
                EventBus<GamePauseEvent>.Raise(new GamePauseEvent(true));
                DebugUtility.LogVerbose<GameManager>($"Pausou o jogo");
            }
        }
        
        public bool CheckGameOver() => !_isPlaying && _isGameOver && !_isVictory;

        public bool CheckVictory() => !_isPlaying && _isVictory && !_isGameOver;
        
        
    }
}