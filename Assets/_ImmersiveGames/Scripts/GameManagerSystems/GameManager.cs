using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [DefaultExecutionOrder(-101)]
    public sealed partial class GameManager : PersistentSingleton<GameManager>, IGameManager
    {
        private const string StateGuardLogPrefix =
            "Operação inválida: o estado atual do GameManager não permite esta operação.";

        [Header("Configuração")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Debug")]
        [SerializeField] private DebugManager debugManager;

        public GameConfig GameConfig => gameConfig;

        private EventBinding<GameStartEvent> _gameStartEvent;
        private EventBinding<GameStartRequestedEvent> _startRequestedBinding;
        private EventBinding<GamePauseRequestedEvent> _pauseRequestedBinding;
        private EventBinding<GameResumeRequestedEvent> _resumeRequestedBinding;
        private EventBinding<GameResetRequestedEvent> _resetRequestedBinding;

        // Debounce por frame (neutraliza double subscription / double raise no mesmo frame)
        private int _lastStartRequestFrame = -1;
        private int _lastPauseRequestFrame = -1;
        private int _lastResumeRequestFrame = -1;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            ConfigureDebug();
            RegisterGameServices();
            Initialize();
        }

        private void Initialize()
        {
            GameManagerStateMachine.Instance.InitializeStateMachine(this);

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
            EventBus<GameStartEvent>.Unregister(_gameStartEvent);
            EventBus<GameStartRequestedEvent>.Unregister(_startRequestedBinding);
            EventBus<GamePauseRequestedEvent>.Unregister(_pauseRequestedBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_resumeRequestedBinding);
            EventBus<GameResetRequestedEvent>.Unregister(_resetRequestedBinding);
        }

        #endregion

        #region Public API

        public bool IsGameActive()
        {
            return GameManagerStateMachine.Instance.CurrentState?.IsGameActive() ?? false;
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

        #endregion

        #region Game Flow Event Handlers

        private void OnGameStart(GameStartEvent evt)
        {
            DebugUtility.LogVerbose<GameManager>("Evento de início de jogo recebido.");
        }

        private void OnStartRequested(GameStartRequestedEvent _)
        {
            if (Time.frameCount == _lastStartRequestFrame)
                return;

            if (!IsCurrentState<MenuState>())
                return;

            _lastStartRequestFrame = Time.frameCount;

            DebugUtility.LogVerbose<GameManager>("Solicitação de início de jogo recebida.");
            EventBus<GameStartEvent>.Raise(new GameStartEvent());
        }

        private void OnPauseRequested(GamePauseRequestedEvent _)
        {
            if (Time.frameCount == _lastPauseRequestFrame)
                return;

            if (!IsCurrentState<PlayingState>())
                return;

            _lastPauseRequestFrame = Time.frameCount;

            DebugUtility.LogVerbose<GameManager>("Solicitação de pausa recebida.");
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(true));
        }

        private void OnResumeRequested(GameResumeRequestedEvent _)
        {
            if (Time.frameCount == _lastResumeRequestFrame)
                return;

            if (!IsCurrentState<PausedState>())
                return;

            _lastResumeRequestFrame = Time.frameCount;

            DebugUtility.LogVerbose<GameManager>("Solicitação de retomada recebida.");
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(false));
        }

        private void OnResetRequested(GameResetRequestedEvent _)
        {
            DebugUtility.LogVerbose<GameManager>("Solicitação de reset recebida.");
            ResetGame(); // Implementado na partial SceneFlow
        }

        #endregion

        #region Internal helpers & DI

        private bool IsCurrentState<T>() where T : GameStateBase
        {
            return GameManagerStateMachine.Instance.CurrentState is T;
        }

        private void ConfigureDebug()
        {
            var manager = ResolveDebugManager();
            if (manager == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "DebugManager não encontrado; seguindo com configuração padrão.");
            }
        }

        private void RegisterGameServices()
        {
            var provider = DependencyManager.Provider;

            provider.RegisterGlobal<IGameManager>(this, allowOverride: true);

            if (gameConfig != null)
            {
                provider.RegisterGlobal(gameConfig, allowOverride: true);
            }

            if (!provider.TryGetGlobal<GameManagerStateMachine>(out _))
            {
                provider.RegisterGlobal(GameManagerStateMachine.Instance, allowOverride: true);
            }

            EnsureSceneTransitionServices(); // Implementado na partial SceneFlow
        }

        private DebugManager ResolveDebugManager()
        {
            if (debugManager != null)
                return debugManager;

            if (TryGetComponent(out DebugManager localManager))
            {
                debugManager = localManager;
                return debugManager;
            }

            if (DependencyManager.Provider.TryGetGlobal(out DebugManager globalManager))
            {
                debugManager = globalManager;
                return debugManager;
            }

            return null;
        }

        #endregion
    }
}
