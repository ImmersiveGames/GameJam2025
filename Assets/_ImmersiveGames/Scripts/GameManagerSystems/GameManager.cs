using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.LoaderSystems;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [DefaultExecutionOrder(-100), DebugLevel(DebugLevel.Verbose)]
    public sealed class GameManager : Singleton<GameManager>, IGameManager
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Transform worldEater;

        public GameConfig GameConfig => gameConfig;
        public Transform WorldEater => worldEater;

        private EventBinding<GameStartEvent> _gameStartEvent;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Initialize()
        {
            // Inicializa a FSM
            GameManagerStateMachine.Instance.InitializeStateMachine(this);

            // Carrega UI para debug, se configurado
            if (gameConfig.DebugMode)
            {
                var loadUISceneAdditive = SceneLoader.Instance.LoadUISceneAdditive("UI");
            }

            // Registra listener para GameStart
            _gameStartEvent = new EventBinding<GameStartEvent>(OnGameStart);
            EventBus<GameStartEvent>.Register(_gameStartEvent);

            DebugUtility.LogVerbose<GameManager>("GameManager inicializado.");
        }

        private void OnDestroy()
        {
            EventBus<GameStartEvent>.Unregister(_gameStartEvent);
        }

        public bool IsGameActive()
        {
            return GameManagerStateMachine.Instance.CurrentState?.IsGameActive() ?? false;
        }

        public void ResetGame()
        {
            DebugUtility.LogVerbose<GameManager>("Resetando o jogo.");
            GameManagerStateMachine.Instance.InitializeStateMachine(this); // Reinicializa a FSM
            SceneLoader.Instance.ReloadCurrentScene();
            EventBus<GameStartEvent>.Raise(new GameStartEvent());
        }

        private void OnGameStart(GameStartEvent evt)
        {
            DebugUtility.LogVerbose<GameManager>("Evento de início de jogo recebido.");
            // Pode ser usado para inicializar sistemas adicionais
        }
    }
}