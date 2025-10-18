using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.LoaderSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [DefaultExecutionOrder(-101), DebugLevel(DebugLevel.Verbose)]
    public sealed class GameManager : Singleton<GameManager>, IGameManager
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Transform worldEater;
        
        
        public GameConfig GameConfig => gameConfig;
        public Transform WorldEater => worldEater;

        private EventBinding<GameStartEvent> _gameStartEvent;
        private EventBinding<GameStartRequestedEvent> _startRequestedBinding;
        private EventBinding<GamePauseRequestedEvent> _pauseRequestedBinding;
        private EventBinding<GameResumeRequestedEvent> _resumeRequestedBinding;
        private EventBinding<GameResetRequestedEvent> _resetRequestedBinding;
        private IActorResourceOrchestrator _orchestrator;

        [ContextMenu("Game Loop/Request Start")]
        private void ContextRequestStart()
        {
            // Facilita testes editoriais disparando o mesmo fluxo usado pelos botões de UI.
            EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
        }

        [ContextMenu("Game Loop/Request Pause")]
        private void ContextRequestPause()
        {
            // Usa o pipeline de eventos para respeitar as validações de estado atuais.
            EventBus<GamePauseRequestedEvent>.Raise(new GamePauseRequestedEvent());
        }

        [ContextMenu("Game Loop/Request Resume")]
        private void ContextRequestResume()
        {
            // Retoma o jogo seguindo o mesmo mecanismo da UI.
            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
        }

        [ContextMenu("Game Loop/Request Reset")]
        private void ContextRequestReset()
        {
            // Solicita o reset completo da sessão via FSM.
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
        }

        [ContextMenu("Game Loop/Force Game Over")]
        private void ContextForceGameOver()
        {
            // Dispara diretamente o evento de game over para testes.
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }

        [ContextMenu("Game Loop/Force Victory")]
        private void ContextForceVictory()
        {
            // Dispara diretamente o evento de vitória para validar fluxos.
            EventBus<GameVictoryEvent>.Raise(new GameVictoryEvent());
        }

        protected override void Awake()
        {
            base.Awake();
            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                _orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Instance.RegisterGlobal(_orchestrator);
            }
            if (!SceneManager.GetSceneByName("UI").isLoaded)
            {
                DebugUtility.LogVerbose<GameManager>($"Carregando cena de UI em modo aditivo.");
                SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
            }
            Initialize();
        }

        

        private void Initialize()
        {
            // Inicializa a FSM
            GameManagerStateMachine.Instance.InitializeStateMachine(this);

            // Registra listener para GameStart
            _gameStartEvent = new EventBinding<GameStartEvent>(OnGameStart);
            EventBus<GameStartEvent>.Register(_gameStartEvent);

            _startRequestedBinding = new EventBinding<GameStartRequestedEvent>(OnStartRequested);
            EventBus<GameStartRequestedEvent>.Register(_startRequestedBinding);

            _pauseRequestedBinding = new EventBinding<GamePauseRequestedEvent>(OnPauseRequested);
            EventBus<GamePauseRequestedEvent>.Register(_pauseRequestedBinding);

            _resumeRequestedBinding = new EventBinding<GameResumeRequestedEvent>(OnResumeRequested);
            EventBus<GameResumeRequestedEvent>.Register(_resumeRequestedBinding);

            _resetRequestedBinding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
            EventBus<GameResetRequestedEvent>.Register(_resetRequestedBinding);

            DebugUtility.LogVerbose<GameManager>("GameManager inicializado.");
        }
        

        private void OnDestroy()
        {
            EventBus<GameStartEvent>.Unregister(_gameStartEvent);
            EventBus<GameStartRequestedEvent>.Unregister(_startRequestedBinding);
            EventBus<GamePauseRequestedEvent>.Unregister(_pauseRequestedBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_resumeRequestedBinding);
            EventBus<GameResetRequestedEvent>.Unregister(_resetRequestedBinding);
        }

        public bool IsGameActive()
        {
            return GameManagerStateMachine.Instance.CurrentState?.IsGameActive() ?? false;
        }

        private bool IsCurrentState<TState>() where TState : class, IState
        {
            return GameManagerStateMachine.Instance.CurrentState is TState;
        }

        public void ResetGame()
        {
            DebugUtility.LogVerbose<GameManager>("Resetando o jogo.");
            GameManagerStateMachine.Instance.Rebuild(this); // Reinicializa a FSM sem recriar o singleton
            SceneLoader.Instance.ReloadCurrentScene();
        }

        private void OnGameStart(GameStartEvent evt)
        {
            DebugUtility.LogVerbose<GameManager>("Evento de início de jogo recebido.");
            // Pode ser usado para inicializar sistemas adicionais
        }

        private void OnStartRequested(GameStartRequestedEvent _)
        {
            if (!IsCurrentState<MenuState>())
            {
                return;
            }

            DebugUtility.LogVerbose<GameManager>("Solicitação de início de jogo recebida.");
            EventBus<GameStartEvent>.Raise(new GameStartEvent());
        }

        private void OnPauseRequested(GamePauseRequestedEvent _)
        {
            if (!IsCurrentState<PlayingState>())
            {
                return;
            }

            DebugUtility.LogVerbose<GameManager>("Solicitação de pausa recebida.");
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(true));
        }

        private void OnResumeRequested(GameResumeRequestedEvent _)
        {
            if (!IsCurrentState<PausedState>())
            {
                return;
            }

            DebugUtility.LogVerbose<GameManager>("Solicitação de retomada recebida.");
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(false));
        }

        private void OnResetRequested(GameResetRequestedEvent _)
        {
            DebugUtility.LogVerbose<GameManager>("Solicitação de reset recebida.");
            ResetGame();
        }
    }
}