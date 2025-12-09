using System.Threading.Tasks;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    /// <summary>
    /// Parte do GameManager responsável por orquestrar o fluxo de cenas
    /// usando o pipeline moderno (SceneFlowMap + SceneGroupProfile + SceneTransitionService).
    /// 
    /// Esta partial substitui o fluxo antigo baseado em corrotinas e listas de cenas
    /// e utiliza apenas Task/async-await.
    /// </summary>
    public sealed partial class GameManager
    {
        [Header("Scene Flow (Modern)")]
        [SerializeField]
        [Tooltip("Mapa lógico de grupos de cena usado para Menu, Gameplay e fluxos adicionais.")]
        private SceneFlowMap sceneFlowMap;

        private bool _resetInProgress;

        // Infra moderna de cenas
        private ISceneLoader _sceneLoader;
        private ISceneTransitionPlanner _sceneTransitionPlanner;
        private ISceneTransitionService _sceneTransitionService;

        #region Public Scene Flow API

        /// <summary>
        /// Solicita um reset completo de jogo.
        /// Implementação padrão:
        /// - Garante que estamos em estado de Menu;
        /// - Reconstrói a GameStateMachine;
        /// - Vai para o grupo de Gameplay configurado no SceneFlowMap.
        /// </summary>
        public void ResetGame()
        {
            if (_resetInProgress)
            {
                DebugUtility.LogWarning<GameManager>(
                    "Solicitação de reset ignorada: já existe um reset em andamento.");
                return;
            }

            DebugUtility.LogVerbose<GameManager>("Resetando o jogo (pipeline moderno de transição, Task-based).");
            _resetInProgress = true;

            _ = ResetGameAsync();
        }

        /// <summary>
        /// Solicita retorno ao Menu (grupo menuGroup do SceneFlowMap).
        /// </summary>
        public void ReturnToMenu()
        {
            _ = ReturnToMenuAsync();
        }

        #endregion

        #region Editor Shortcuts (Scene Flow)

#if UNITY_EDITOR
        [ContextMenu("Game Loop/Requests/Reset Game (Scene Flow)")]
        private void Editor_ResetGame()
        {
            ResetGame();
        }

        [ContextMenu("Game Loop/Requests/Go To Gameplay (Direct Scene Flow)")]
        private void Editor_GoToGameplay()
        {
            _ = StartGameplayFromCurrentSceneAsync();
        }

        [ContextMenu("Game Loop/Requests/Go To Menu (Direct Scene Flow)")]
        private void Editor_GoToMenu()
        {
            _ = GoToMenuFromCurrentSceneAsync();
        }
#endif

        #endregion

        #region Modern Scene Flow Helpers

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
        }

        private SceneGroupProfile GetGameplayGroup()
        {
            if (sceneFlowMap != null && sceneFlowMap.GameplayGroup != null)
                return sceneFlowMap.GameplayGroup;

            DebugUtility.LogWarning<GameManager>(
                "[SceneFlow] SceneFlowMap ou GameplayGroup não configurados. " +
                "Certifique-se de atribuir um SceneFlowMap ao GameManager no inspector.");
            return null;
        }

        private SceneGroupProfile GetMenuGroup()
        {
            if (sceneFlowMap != null && sceneFlowMap.MenuGroup != null)
                return sceneFlowMap.MenuGroup;

            DebugUtility.LogWarning<GameManager>(
                "[SceneFlow] SceneFlowMap ou MenuGroup não configurados. " +
                "Certifique-se de atribuir um SceneFlowMap ao GameManager no inspector.");
            return null;
        }

        /// <summary>
        /// A partir da cena atual, transita para o grupo de Gameplay definido no SceneFlowMap.
        /// </summary>
        private async Task StartGameplayFromCurrentSceneAsync()
        {
            EnsureSceneTransitionServices();

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "StartGameplayFromCurrentSceneAsync chamado sem infraestrutura de transição configurada.");
                return;
            }

            var targetGroup = GetGameplayGroup();
            if (targetGroup == null)
            {
                DebugUtility.LogError<GameManager>(
                    "[StartGameplayFromCurrentSceneAsync] GameplayGroup não configurado no SceneFlowMap.");
                return;
            }

            SceneState state = SceneState.Capture();
            DebugUtility.LogVerbose<GameManager>(
                $"[StartGameplayFromCurrentSceneAsync] SceneState atual: {state}");

            SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(
                state,
                targetGroup);

            DebugUtility.LogVerbose<GameManager>(
                "[StartGameplayFromCurrentSceneAsync] Contexto montado: " + context);

            await _sceneTransitionService.RunTransitionAsync(context);
        }

        /// <summary>
        /// A partir da cena atual, transita para o grupo de Menu definido no SceneFlowMap.
        /// </summary>
        private async Task GoToMenuFromCurrentSceneAsync()
        {
            EnsureSceneTransitionServices();

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "GoToMenuFromCurrentSceneAsync chamado sem infraestrutura de transição configurada.");
                return;
            }

            var targetGroup = GetMenuGroup();
            if (targetGroup == null)
            {
                DebugUtility.LogError<GameManager>(
                    "[GoToMenuFromCurrentSceneAsync] MenuGroup não configurado no SceneFlowMap.");
                return;
            }

            SceneState state = SceneState.Capture();
            DebugUtility.LogVerbose<GameManager>(
                $"[GoToMenuFromCurrentSceneAsync] SceneState atual: {state}");

            SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(
                state,
                targetGroup);

            DebugUtility.LogVerbose<GameManager>(
                "[GoToMenuFromCurrentSceneAsync] Contexto montado: " + context);

            await _sceneTransitionService.RunTransitionAsync(context);
        }

        #endregion

        #region Reset / ReturnToMenu Routines (Task-based)

        private async Task ResetGameAsync()
        {
            try
            {
                EventBus<GameResetStartedEvent>.Raise(new GameResetStartedEvent());

                // Garante que estamos em MenuState antes de reconstruir o jogo
                await Task.Yield();
                await WaitForMenuStateAsync();

                Time.timeScale = 1f;

                GameManagerStateMachine.Instance.Rebuild(this);

                EnsureSceneTransitionServices();

                if (_sceneTransitionPlanner == null || _sceneTransitionService == null)
                {
                    DebugUtility.LogWarning<GameManager>(
                        "Infra de transição de cenas não configurada; reset não alterou cenas.");
                }
                else
                {
                    var targetGroup = GetGameplayGroup();
                    if (targetGroup == null)
                    {
                        DebugUtility.LogError<GameManager>(
                            "[ResetGameAsync] GameplayGroup não configurado no SceneFlowMap.");
                    }
                    else
                    {
                        SceneState state = SceneState.Capture();

                        SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(
                            state,
                            targetGroup);

                        DebugUtility.LogVerbose<GameManager>(
                            "[ResetGameAsync] Contexto montado: " + context);

                        await _sceneTransitionService.RunTransitionAsync(context);
                    }
                }

                EventBus<GameResetCompletedEvent>.Raise(new GameResetCompletedEvent());
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError<GameManager>($"[ResetGameAsync] Exceção durante o reset: {ex}");
            }
            finally
            {
                _resetInProgress = false;
            }
        }

        private async Task ReturnToMenuAsync()
        {
            try
            {
                EnsureSceneTransitionServices();

                if (_sceneTransitionPlanner == null || _sceneTransitionService == null)
                {
                    DebugUtility.LogWarning<GameManager>(
                        "Infra de transição de cenas não configurada; ReturnToMenu ignorado.");
                    return;
                }

                var targetGroup = GetMenuGroup();
                if (targetGroup == null)
                {
                    DebugUtility.LogError<GameManager>(
                        "[ReturnToMenuAsync] MenuGroup não configurado no SceneFlowMap.");
                    return;
                }

                SceneState state = SceneState.Capture();

                SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(
                    state,
                    targetGroup);

                DebugUtility.LogVerbose<GameManager>(
                    "[ReturnToMenuAsync] Contexto montado: " + context);

                await _sceneTransitionService.RunTransitionAsync(context);
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError<GameManager>($"[ReturnToMenuAsync] Exceção durante retorno ao menu: {ex}");
            }
        }

        /// <summary>
        /// Aguardar até que o estado atual do GameManager seja MenuState,
        /// ou até um timeout de segurança.
        /// </summary>
        private async Task WaitForMenuStateAsync()
        {
            const float timeoutSeconds = 1.5f;
            float elapsed = 0f;

            while (!(GameManagerStateMachine.Instance.CurrentState is MenuState) && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }
        }

        #endregion
    }
}
