using System.Collections;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.LoaderSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [DefaultExecutionOrder(-101)]
    public sealed class GameManager : Singleton<GameManager>, IGameManager
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private DebugManager debugManager;
        [SerializeField] private Transform worldEater;
        
        public GameConfig GameConfig => gameConfig;
        public Transform WorldEater => worldEater;

        private EventBinding<GameStartEvent> _gameStartEvent;
        private EventBinding<GameStartRequestedEvent> _startRequestedBinding;
        private EventBinding<GamePauseRequestedEvent> _pauseRequestedBinding;
        private EventBinding<GameResumeRequestedEvent> _resumeRequestedBinding;
        private EventBinding<GameResetRequestedEvent> _resetRequestedBinding;
        private IActorResourceOrchestrator _orchestrator;
        private Coroutine _resetRoutine;
        private bool _resetInProgress;

        private const string StateGuardLogPrefix = "Solicitação ignorada: ciclo de jogo não está em estado de gameplay.";

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
            // Dispara game over respeitando validação de estado.
            TryTriggerGameOver("Context menu");
        }

        [ContextMenu("Game Loop/Force Victory")]
        private void ContextForceVictory()
        {
            // Dispara vitória respeitando validação de estado.
            TryTriggerVictory("Context menu");
        }

        protected override void Awake()
        {
            base.Awake();
            ConfigureDebug();
            if (!DependencyManager.Provider.TryGetGlobal(out _orchestrator))
            {
                _orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Provider.RegisterGlobal(_orchestrator);
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

            DebugUtility.Log<GameManager>(
                "GameManager inicializado.",
                DebugUtility.Colors.CrucialInfo);
        }
        

        private void OnDestroy()
        {
            if (_resetRoutine != null)
            {
                StopCoroutine(_resetRoutine);
                _resetRoutine = null;
                _resetInProgress = false;
            }

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

        public bool TryTriggerGameOver(string reason = null)
        {
            if (!IsCurrentState<PlayingState>())
            {
                DebugUtility.LogWarning<GameManager>(StateGuardLogPrefix);
                return false;
            }

            DebugUtility.LogVerbose<GameManager>($"Disparando GameOver. Razão: {reason ?? "(não informada)"}.");
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
            return true;
        }

        public bool TryTriggerVictory(string reason = null)
        {
            if (!IsCurrentState<PlayingState>())
            {
                DebugUtility.LogWarning<GameManager>(StateGuardLogPrefix);
                return false;
            }

            DebugUtility.LogVerbose<GameManager>($"Disparando Victory. Razão: {reason ?? "(não informada)"}.");
            EventBus<GameVictoryEvent>.Raise(new GameVictoryEvent());
            return true;
        }

        public void ResetGame()
        {
            if (_resetInProgress)
            {
                DebugUtility.LogWarning<GameManager>("Solicitação de reset ignorada: já existe um reset em andamento.");
                return;
            }

            DebugUtility.LogVerbose<GameManager>("Resetando o jogo (pipeline robusto).");
            _resetRoutine = StartCoroutine(ResetGameRoutine());
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

        private void ConfigureDebug()
        {
            var manager = ResolveDebugManager();
            if (manager != null)
            {
                manager.ApplyConfiguration(gameConfig);
            }
            else
            {
                DebugUtility.LogWarning<GameManager>(
                    "DebugManager não encontrado; aplicando configuração padrão do DebugUtility.");
            }
        }

        private DebugManager ResolveDebugManager()
        {
            if (debugManager != null)
            {
                return debugManager;
            }

            if (TryGetComponent(out DebugManager localManager))
            {
                debugManager = localManager;
                return debugManager;
            }

            debugManager = FindObjectOfType<DebugManager>();
            return debugManager;
        }

        private IEnumerator ResetGameRoutine()
        {
            _resetInProgress = true;

            // Permite que sistemas persistentes (ex.: pools, placares) limpem estado antes do reload.
            EventBus<GameResetStartedEvent>.Raise(new GameResetStartedEvent());

            // Dá uma folga para que os handlers do GameResetRequestedEvent processem paradas (timers, spawns, etc.).
            yield return new WaitForEndOfFrame();

            // Aguarda a FSM voltar para o menu para garantir execução de OnExit/OnEnter dos estados atuais.
            yield return WaitForMenuState();

            // Garante que o tempo global não fique travado após sair de estados que pausam o timeScale.
            Time.timeScale = 1f;

            // Recria a FSM para limpar bindings antigos antes de recarregar a cena.
            GameManagerStateMachine.Instance.Rebuild(this);

            // Recarrega a cena ativa e reanexa a UI.
            yield return SceneLoader.Instance.ReloadCurrentSceneAsync();

            EventBus<GameResetCompletedEvent>.Raise(new GameResetCompletedEvent());

            _resetInProgress = false;
            _resetRoutine = null;
        }

        private IEnumerator WaitForMenuState()
        {
            // Usa deltaTime não escalonado para respeitar pausas.
            const float timeoutSeconds = 1.5f;
            float elapsed = 0f;

            while (!IsCurrentState<MenuState>() && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
