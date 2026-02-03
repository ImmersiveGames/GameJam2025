using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;
using UnityEngine.Rendering;
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
        private EventBinding<OldGameStartRequestedEvent> _startRequestedBinding;
        private EventBinding<GamePauseRequestedEvent> _pauseRequestedBinding;
        private EventBinding<OldGameResumeRequestedEvent> _resumeRequestedBinding;
        private EventBinding<OldGameResetRequestedEvent> _resetRequestedBinding;
        private EventBinding<GameReturnToMenuRequestedEvent> _returnToMenuRequestedBinding;

        // Debounce por frame (neutraliza double subscription / double raise no mesmo frame)
        private int _lastStartRequestFrame = -1;
        private int _lastPauseRequestFrame = -1;
        private int _lastResumeRequestFrame = -1;
        private int _lastReturnToMenuRequestFrame = -1;

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
            OldGameManagerStateMachine.Instance.InitializeStateMachine(this);

            _gameStartEvent = new EventBinding<GameStartEvent>(OnGameStart);
            EventBus<GameStartEvent>.Register(_gameStartEvent);

            _startRequestedBinding = new EventBinding<OldGameStartRequestedEvent>(OnStartRequested);
            EventBus<OldGameStartRequestedEvent>.Register(_startRequestedBinding);

            _pauseRequestedBinding = new EventBinding<GamePauseRequestedEvent>(OnPauseRequested);
            EventBus<GamePauseRequestedEvent>.Register(_pauseRequestedBinding);

            _resumeRequestedBinding = new EventBinding<OldGameResumeRequestedEvent>(OnResumeRequested);
            EventBus<OldGameResumeRequestedEvent>.Register(_resumeRequestedBinding);

            _resetRequestedBinding = new EventBinding<OldGameResetRequestedEvent>(OnResetRequested);
            EventBus<OldGameResetRequestedEvent>.Register(_resetRequestedBinding);

            _returnToMenuRequestedBinding = new EventBinding<GameReturnToMenuRequestedEvent>(OnReturnToMenuRequested);
            EventBus<GameReturnToMenuRequestedEvent>.Register(_returnToMenuRequestedBinding);

            DebugUtility.Log<GameManager>(
                "GameManager inicializado.",
                DebugUtility.Colors.CrucialInfo);
        }

        private void OnDestroy()
        {
            EventBus<GameStartEvent>.Unregister(_gameStartEvent);
            EventBus<OldGameStartRequestedEvent>.Unregister(_startRequestedBinding);
            EventBus<GamePauseRequestedEvent>.Unregister(_pauseRequestedBinding);
            EventBus<OldGameResumeRequestedEvent>.Unregister(_resumeRequestedBinding);
            EventBus<OldGameResetRequestedEvent>.Unregister(_resetRequestedBinding);
            EventBus<GameReturnToMenuRequestedEvent>.Unregister(_returnToMenuRequestedBinding);
        }

        #endregion

        #region Public API

        public bool IsGameActive()
        {
            return OldGameManagerStateMachine.Instance.CurrentState?.IsGameActive() ?? false;
        }

        public bool TryTriggerGameOver(string reason = null)
        {
            if (!IsCurrentState<OldPlayingState>())
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
            if (!IsCurrentState<OldPlayingState>())
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

        private void OnStartRequested(OldGameStartRequestedEvent _)
        {
            if (Time.frameCount == _lastStartRequestFrame)
                return;

            if (!IsCurrentState<OldMenuState>())
                return;

            _lastStartRequestFrame = Time.frameCount;

            DebugUtility.LogVerbose<GameManager>("Solicitação de início de jogo recebida.");
            EventBus<GameStartEvent>.Raise(new GameStartEvent());
        }

        private void OnPauseRequested(GamePauseRequestedEvent _)
        {
            if (Time.frameCount == _lastPauseRequestFrame)
                return;

            if (!IsCurrentState<OldPlayingState>())
                return;

            _lastPauseRequestFrame = Time.frameCount;

            DebugUtility.LogVerbose<GameManager>("Solicitação de pausa recebida.");
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(true));
        }

        private void OnResumeRequested(OldGameResumeRequestedEvent _)
        {
            if (Time.frameCount == _lastResumeRequestFrame)
                return;

            if (!IsCurrentState<OldPausedState>())
                return;

            _lastResumeRequestFrame = Time.frameCount;

            DebugUtility.LogVerbose<GameManager>("Solicitação de retomada recebida.");
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(false));
        }

        private void OnResetRequested(OldGameResetRequestedEvent _)
        {
            DebugUtility.LogVerbose<GameManager>("Solicitação de reset recebida.");
            ResetGame(); // Implementado na partial SceneFlow
        }

        private void OnReturnToMenuRequested(GameReturnToMenuRequestedEvent _)
        {
            if (Time.frameCount == _lastReturnToMenuRequestFrame)
                return;

            _lastReturnToMenuRequestFrame = Time.frameCount;

            DebugUtility.LogVerbose<GameManager>("Solicitação de retorno ao menu recebida.");
            ReturnToMenu(); // Implementado na partial SceneFlow
        }

        #endregion

        #region Internal helpers & DI

        private bool IsCurrentState<T>() where T : OldGameStateBase
        {
            return OldGameManagerStateMachine.Instance.CurrentState is T;
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

            if (!provider.TryGetGlobal<OldGameManagerStateMachine>(out _))
            {
                provider.RegisterGlobal(OldGameManagerStateMachine.Instance, allowOverride: true);
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


