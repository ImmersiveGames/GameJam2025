using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.LoaderSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [DefaultExecutionOrder(-101)]
    public sealed class GameManager : PersistentSingleton<GameManager>, IGameManager
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private DebugManager debugManager;

        public GameConfig GameConfig => gameConfig;

        private EventBinding<GameStartEvent> _gameStartEvent;
        private EventBinding<GameStartRequestedEvent> _startRequestedBinding;
        private EventBinding<GamePauseRequestedEvent> _pauseRequestedBinding;
        private EventBinding<GameResumeRequestedEvent> _resumeRequestedBinding;
        private EventBinding<GameResetRequestedEvent> _resetRequestedBinding;

        private IActorResourceOrchestrator _orchestrator;
        private Coroutine _resetRoutine;
        private bool _resetInProgress;

        // Infraestrutura do novo pipeline de transição de cenas
        private ISceneLoader _sceneLoaderCore;
        private ISceneTransitionPlanner _sceneTransitionPlanner;
        private ISceneTransitionService _sceneTransitionService;
        private IFadeAwaiter _fadeAwaiter;

        private const string StateGuardLogPrefix =
            "Solicitação ignorada: ciclo de jogo não está em estado de gameplay.";

        #region Context Menus

        [ContextMenu("Game Loop/Requests/Start Game")]
        private void ContextRequestStart()
        {
            EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
        }

        [ContextMenu("Game Loop/Requests/Pause Game")]
        private void ContextRequestPause()
        {
            EventBus<GamePauseRequestedEvent>.Raise(new GamePauseRequestedEvent());
        }

        [ContextMenu("Game Loop/Requests/Resume Game")]
        private void ContextRequestResume()
        {
            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
        }

        [ContextMenu("Game Loop/Requests/Reset Game")]
        private void ContextRequestReset()
        {
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
        }

        [ContextMenu("Game Loop/Requests/Go To Gameplay (Direct)")]
        private void ContextRequestGoToGameplay()
        {
            StartGameplayFromCurrentScene();
        }

        [ContextMenu("Game Loop/Requests/Go To Menu (Direct)")]
        private void ContextRequestGoToMenu()
        {
            GoToMenuFromCurrentScene();
        }

        [ContextMenu("Game Loop/Force/Game Over")]
        private void ContextForceGameOver()
        {
            TryTriggerGameOver("Context menu");
        }

        [ContextMenu("Game Loop/Force/Victory")]
        private void ContextForceVictory()
        {
            TryTriggerVictory("Context menu");
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            ConfigureDebug();
            RegisterGameServices();

            if (!DependencyManager.Provider.TryGetGlobal(out _orchestrator))
            {
                _orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Provider.RegisterGlobal(_orchestrator);
            }

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
            if (_resetRoutine != null)
            {
                StopCoroutine(_resetRoutine);
                _resetRoutine = null;
            }

            _resetInProgress = false;

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

            DebugUtility.LogVerbose<GameManager>(
                $"Disparando GameOver. Razão: {reason ?? "(não informada)"}.");
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

            DebugUtility.LogVerbose<GameManager>(
                $"Disparando Victory. Razão: {reason ?? "(não informada)"}.");
            EventBus<GameVictoryEvent>.Raise(new GameVictoryEvent());
            return true;
        }

        /// <summary>
        /// Entrada direta para transitar da cena atual para o setup de Gameplay
        /// usando o pipeline novo (SceneState + Planner + TransitionService).
        /// Alvo: GameplayScene + UIScene, com GameplayScene ativa.
        /// </summary>
        public void StartGameplayFromCurrentScene()
        {
            if (_resetInProgress)
            {
                DebugUtility.LogWarning<GameManager>(
                    "StartGameplayFromCurrentScene ignorado: reset em andamento.");
                return;
            }

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null ||
                _sceneLoaderCore == null || gameConfig == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "Infra de transição de cenas não está configurada; StartGameplayFromCurrentScene ignorado.");
                return;
            }

            DebugUtility.LogVerbose<GameManager>(
                "Iniciando transição para Gameplay a partir da cena atual (StartGameplayFromCurrentScene).");

            _ = StartGameplayFromCurrentSceneAsync();
        }

        /// <summary>
        /// Entrada direta para transitar da cena atual para o MenuScene
        /// usando o pipeline novo (SceneState + Planner + TransitionService).
        /// Alvo: apenas MenuScene, como cena ativa.
        /// </summary>
        public void GoToMenuFromCurrentScene()
        {
            if (_resetInProgress)
            {
                DebugUtility.LogWarning<GameManager>(
                    "GoToMenuFromCurrentScene ignorado: reset em andamento.");
                return;
            }

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null ||
                _sceneLoaderCore == null || gameConfig == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "Infra de transição de cenas não está configurada; GoToMenuFromCurrentScene ignorado.");
                return;
            }

            DebugUtility.LogVerbose<GameManager>(
                "Iniciando transição para Menu a partir da cena atual (GoToMenuFromCurrentScene).");

            _ = GoToMenuFromCurrentSceneAsync();
        }

        public void ResetGame()
        {
            if (_resetInProgress)
            {
                DebugUtility.LogWarning<GameManager>(
                    "Solicitação de reset ignorada: já existe um reset em andamento.");
                return;
            }

            DebugUtility.LogVerbose<GameManager>("Resetando o jogo (pipeline robusto).");
            _resetRoutine = StartCoroutine(ResetGameRoutine());
        }

        public void ReturnToMenu()
        {
            if (_resetRoutine != null)
            {
                StopCoroutine(_resetRoutine);
                _resetRoutine = null;
            }

            _resetRoutine = StartCoroutine(ReturnToMenuRoutine());
        }

        #endregion

        #region Internals

        private bool IsCurrentState<TState>() where TState : class, IState
        {
            return GameManagerStateMachine.Instance.CurrentState is TState;
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

        private void RegisterGameServices()
        {
            var provider = DependencyManager.Provider;

            // GameManager global
            provider.RegisterGlobal<IGameManager>(this, allowOverride: true);

            // GameConfig global
            if (gameConfig != null)
            {
                provider.RegisterGlobal(gameConfig, allowOverride: true);
            }

            // StateMachine global
            if (!provider.TryGetGlobal<GameManagerStateMachine>(out _))
            {
                provider.RegisterGlobal(GameManagerStateMachine.Instance, allowOverride: true);
            }

            // Infra do novo pipeline de transição de cenas (para acesso via GameManager)
            if (!provider.TryGetGlobal(out _sceneLoaderCore))
            {
                _sceneLoaderCore = new SceneLoaderCore();
                provider.RegisterGlobal(_sceneLoaderCore, allowOverride: true);
            }

            if (!provider.TryGetGlobal(out _sceneTransitionPlanner))
            {
                _sceneTransitionPlanner = new SimpleSceneTransitionPlanner();
                provider.RegisterGlobal(_sceneTransitionPlanner, allowOverride: true);
            }

            // FadeAwaiter é opcional: só configuramos se os serviços existirem
            if (provider.TryGetGlobal(out IFadeService fadeService) &&
                provider.TryGetGlobal(out ICoroutineRunner runner))
            {
                _fadeAwaiter = new FadeAwaiter(fadeService, runner);
            }

            if (!provider.TryGetGlobal(out _sceneTransitionService))
            {
                _sceneTransitionService = new SceneTransitionService(_sceneLoaderCore, _fadeAwaiter);
                provider.RegisterGlobal(_sceneTransitionService, allowOverride: true);
            }
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

            debugManager = FindFirstObjectByType<DebugManager>();
            return debugManager;
        }

        private async Task StartGameplayFromCurrentSceneAsync()
        {
            var state = SceneState.FromSceneManager();

            var targetScenes = new List<string>
            {
                GameConfig.GameplayScene,
                GameConfig.UIScene
            };

            var context = _sceneTransitionPlanner.BuildContext(
                state,
                targetScenes,
                GameConfig.GameplayScene,
                useFade: true);

            DebugUtility.LogVerbose<GameManager>(
                "[StartGameplayFromCurrentSceneAsync] Contexto: " + context);

            await _sceneTransitionService.RunTransitionAsync(context);
        }

        private async Task GoToMenuFromCurrentSceneAsync()
        {
            var state = SceneState.FromSceneManager();

            var targetScenes = new List<string>
            {
                GameConfig.MenuScene
            };

            var context = _sceneTransitionPlanner.BuildContext(
                state,
                targetScenes,
                GameConfig.MenuScene,
                useFade: true);

            DebugUtility.LogVerbose<GameManager>(
                "[GoToMenuFromCurrentSceneAsync] Contexto: " + context);

            await _sceneTransitionService.RunTransitionAsync(context);
        }

        private IEnumerator ResetGameRoutine()
        {
            _resetInProgress = true;

            EventBus<GameResetStartedEvent>.Raise(new GameResetStartedEvent());
            yield return new WaitForEndOfFrame();
            yield return WaitForMenuState();

            Time.timeScale = 1f;

            GameManagerStateMachine.Instance.Rebuild(this);

            if (DependencyManager.Provider.TryGetGlobal(out ISceneLoaderService loader))
            {
                var gameplay = GameConfig.GameplayScene;
                var ui = GameConfig.UIScene;

                var scenesToLoad = new[]
                {
                    new SceneLoadData(gameplay, LoadSceneMode.Single),
                    new SceneLoadData(ui, LoadSceneMode.Additive)
                };

                yield return loader.LoadScenesWithFadeAsync(scenesToLoad);
            }

            EventBus<GameResetCompletedEvent>.Raise(new GameResetCompletedEvent());

            _resetInProgress = false;
            _resetRoutine = null;
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            if (!DependencyManager.Provider.TryGetGlobal(out ISceneLoaderService loader))
                yield break;

            var menu = GameConfig.MenuScene;

            var scenesToLoad = new[]
            {
                new SceneLoadData(menu, LoadSceneMode.Single)
            };

            yield return loader.LoadScenesWithFadeAsync(scenesToLoad);
        }

        private IEnumerator WaitForMenuState()
        {
            const float timeoutSeconds = 1.5f;
            float elapsed = 0f;

            while (!IsCurrentState<MenuState>() && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        #endregion

        #region Event Handlers

        private void OnGameStart(GameStartEvent evt)
        {
            DebugUtility.LogVerbose<GameManager>("Evento de início de jogo recebido.");
        }

        private void OnStartRequested(GameStartRequestedEvent _)
        {
            if (!IsCurrentState<MenuState>())
                return;

            DebugUtility.LogVerbose<GameManager>("Solicitação de início de jogo recebida.");
            EventBus<GameStartEvent>.Raise(new GameStartEvent());
        }

        private void OnPauseRequested(GamePauseRequestedEvent _)
        {
            if (!IsCurrentState<PlayingState>())
                return;

            DebugUtility.LogVerbose<GameManager>("Solicitação de pausa recebida.");
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(true));
        }

        private void OnResumeRequested(GameResumeRequestedEvent _)
        {
            if (!IsCurrentState<PausedState>())
                return;

            DebugUtility.LogVerbose<GameManager>("Solicitação de retomada recebida.");
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(false));
        }

        private void OnResetRequested(GameResetRequestedEvent _)
        {
            DebugUtility.LogVerbose<GameManager>("Solicitação de reset recebida.");
            ResetGame();
        }

        #endregion
    }
}
