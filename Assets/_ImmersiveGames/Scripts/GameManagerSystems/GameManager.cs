using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
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
    public sealed class GameManager : PersistentSingleton<GameManager>, IGameManager
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

        private Coroutine _resetRoutine;
        private bool _resetInProgress;

        // Infra moderna de cenas
        private ISceneLoader _sceneLoader;
        private ISceneTransitionPlanner _sceneTransitionPlanner;
        private ISceneTransitionService _sceneTransitionService;
        private IFadeAwaiter _fadeAwaiter;

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

        /// <summary>
        /// Reset completo do jogo utilizando o pipeline moderno de transição de cenas.
        /// Mantém a semântica anterior (eventos de reset e espera do MenuState),
        /// mas agora usa SceneState + Planner + SceneTransitionService.
        /// </summary>
        public void ResetGame()
        {
            if (_resetInProgress)
            {
                DebugUtility.LogWarning<GameManager>(
                    "Solicitação de reset ignorada: já existe um reset em andamento.");
                return;
            }

            DebugUtility.LogVerbose<GameManager>("Resetando o jogo (pipeline moderno de transição).");
            _resetRoutine = StartCoroutine(ResetGameRoutine());
        }

        /// <summary>
        /// Solicita retorno para o Menu usando o mesmo pipeline moderno de transição de cenas.
        /// </summary>
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

        #region Game Flow Event Handlers

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

        #region Context Menu Shortcuts

#if UNITY_EDITOR
        [ContextMenu("Game Loop/Requests/Start Game (From Menu State)")]
        private void Editor_StartGame()
        {
            EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
        }

        [ContextMenu("Game Loop/Requests/Pause Game")]
        private void Editor_PauseGame()
        {
            EventBus<GamePauseRequestedEvent>.Raise(new GamePauseRequestedEvent());
        }

        [ContextMenu("Game Loop/Requests/Resume Game")]
        private void Editor_ResumeGame()
        {
            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
        }

        [ContextMenu("Game Loop/Requests/Reset Game")]
        private void Editor_ResetGame()
        {
            ResetGame();
        }

        [ContextMenu("Game Loop/Requests/Go To Gameplay (Direct)")]
        private void Editor_GoToGameplay()
        {
            _ = StartGameplayFromCurrentSceneAsync();
        }

        [ContextMenu("Game Loop/Requests/Go To Menu (Direct)")]
        private void Editor_GoToMenu()
        {
            _ = GoToMenuFromCurrentSceneAsync();
        }

        [ContextMenu("Game Loop/Debug/Force GameOver")]
        private void Editor_ForceGameOver()
        {
            TryTriggerGameOver("Editor Debug");
        }

        [ContextMenu("Game Loop/Debug/Force Victory")]
        private void Editor_ForceVictory()
        {
            TryTriggerVictory("Editor Debug");
        }
#endif

        #endregion

        #region Modern Scene Flow Helpers

        /// <summary>
        /// Resolve serviços de transição se ainda não estiverem populados.
        /// </summary>
        private void EnsureSceneTransitionServices()
        {
            var provider = DependencyManager.Provider;

            if (_sceneLoader == null)
            {
                if (provider.TryGetGlobal(out ISceneLoader loader))
                {
                    _sceneLoader = loader;
                    DebugUtility.LogVerbose<GameManager>("[DI] ISceneLoader resolvido com sucesso.");
                }
                else
                {
                    DebugUtility.LogWarning<GameManager>("[DI] ISceneLoader não encontrado.");
                }
            }

            if (_sceneTransitionPlanner == null)
            {
                if (provider.TryGetGlobal(out ISceneTransitionPlanner planner))
                {
                    _sceneTransitionPlanner = planner;
                    DebugUtility.LogVerbose<GameManager>("[DI] ISceneTransitionPlanner resolvido com sucesso.");
                }
                else
                {
                    DebugUtility.LogWarning<GameManager>("[DI] ISceneTransitionPlanner não encontrado.");
                }
            }

            if (_sceneTransitionService == null)
            {
                if (provider.TryGetGlobal(out ISceneTransitionService transitionService))
                {
                    _sceneTransitionService = transitionService;
                    DebugUtility.LogVerbose<GameManager>("[DI] ISceneTransitionService resolvido com sucesso.");
                }
                else
                {
                    DebugUtility.LogWarning<GameManager>("[DI] ISceneTransitionService não encontrado.");
                }
            }

            if (_fadeAwaiter == null)
            {
                if (provider.TryGetGlobal(out IFadeAwaiter fadeAwaiter))
                {
                    _fadeAwaiter = fadeAwaiter;
                    DebugUtility.LogVerbose<GameManager>("[DI] IFadeAwaiter resolvido com sucesso.");
                }
                else
                {
                    // Não é crítico: o SceneTransitionService funciona sem fadeAwaiter (só não vai usar fade).
                    DebugUtility.LogVerbose<GameManager>("[DI] IFadeAwaiter não encontrado (fade assíncrono será ignorado).");
                }
            }
        }

        /// <summary>
        /// Helper interno para transicionar da cena atual para o Gameplay+UI
        /// usando SceneState + Planner + SceneTransitionService.
        /// Usado pelos context menus e pode ser reutilizado por botões de UI.
        /// </summary>
        private async Task StartGameplayFromCurrentSceneAsync()
        {
            EnsureSceneTransitionServices();

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null || gameConfig == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "StartGameplayFromCurrentSceneAsync chamado sem infraestrutura de transição configurada.");
                return;
            }

            var state = SceneState.FromSceneManager();
            DebugUtility.LogVerbose<GameManager>(
                $"[StartGameplayFromCurrentSceneAsync] SceneState atual: {state}");

            var targetScenes = new List<string>
            {
                gameConfig.GameplayScene,
                gameConfig.UIScene
            };

            var context = _sceneTransitionPlanner.BuildContext(
                state,
                targetScenes,
                gameConfig.GameplayScene,
                useFade: true);

            DebugUtility.LogVerbose<GameManager>(
                "[StartGameplayFromCurrentSceneAsync] Contexto montado: " + context);

            await _sceneTransitionService.RunTransitionAsync(context);
        }

        /// <summary>
        /// Helper interno para transicionar da cena atual para o Menu usando
        /// SceneState + Planner + SceneTransitionService.
        /// </summary>
        private async Task GoToMenuFromCurrentSceneAsync()
        {
            EnsureSceneTransitionServices();

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null || gameConfig == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "GoToMenuFromCurrentSceneAsync chamado sem infraestrutura de transição configurada.");
                return;
            }

            var state = SceneState.FromSceneManager();
            DebugUtility.LogVerbose<GameManager>(
                $"[GoToMenuFromCurrentSceneAsync] SceneState atual: {state}");

            var targetScenes = new List<string>
            {
                gameConfig.MenuScene
            };

            var context = _sceneTransitionPlanner.BuildContext(
                state,
                targetScenes,
                gameConfig.MenuScene,
                useFade: true);

            DebugUtility.LogVerbose<GameManager>(
                "[GoToMenuFromCurrentSceneAsync] Contexto montado: " + context);

            await _sceneTransitionService.RunTransitionAsync(context);
        }

        #endregion

        #region Reset / ReturnToMenu Routines (bridge para eventos legados)

        private IEnumerator ResetGameRoutine()
        {
            _resetInProgress = true;

            EventBus<GameResetStartedEvent>.Raise(new GameResetStartedEvent());
            yield return new WaitForEndOfFrame();
            yield return WaitForMenuState();

            Time.timeScale = 1f;

            GameManagerStateMachine.Instance.Rebuild(this);

            EnsureSceneTransitionServices();

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null || gameConfig == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "Infra de transição de cenas não configurada; reset não alterou cenas.");
            }
            else
            {
                var state = SceneState.FromSceneManager();
                var targetScenes = new List<string>
                {
                    gameConfig.GameplayScene,
                    gameConfig.UIScene
                };

                var context = _sceneTransitionPlanner.BuildContext(
                    state,
                    targetScenes,
                    gameConfig.GameplayScene,
                    useFade: true);

                DebugUtility.LogVerbose<GameManager>(
                    "[ResetGameRoutine] Contexto montado: " + context);

                var task = _sceneTransitionService.RunTransitionAsync(context);
                yield return WaitForTask(task);
            }

            EventBus<GameResetCompletedEvent>.Raise(new GameResetCompletedEvent());

            _resetInProgress = false;
            _resetRoutine = null;
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            EnsureSceneTransitionServices();

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null || gameConfig == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "Infra de transição de cenas não configurada; ReturnToMenu ignorado.");
                yield break;
            }

            var state = SceneState.FromSceneManager();

            var targetScenes = new List<string>
            {
                gameConfig.MenuScene
            };

            var context = _sceneTransitionPlanner.BuildContext(
                state,
                targetScenes,
                gameConfig.MenuScene,
                useFade: true);

            DebugUtility.LogVerbose<GameManager>(
                "[ReturnToMenuRoutine] Contexto montado: " + context);

            var task = _sceneTransitionService.RunTransitionAsync(context);
            yield return WaitForTask(task);
        }

        private static IEnumerator WaitForTask(Task task)
        {
            if (task == null)
                yield break;

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                DebugUtility.LogError<GameManager>(
                    $"Task de transição falhou: {task.Exception}");
            }
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

            // Resolve serviços de cena se já existirem (podem vir do DependencyBootstrapper
            // ou de outro ponto de inicialização).
            EnsureSceneTransitionServices();
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