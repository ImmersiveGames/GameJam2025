using System.Collections.Generic;
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
    public sealed partial class GameManager
    {
        private bool _resetInProgress;

        // Infra moderna de cenas
        private ISceneLoader _sceneLoader;
        private ISceneTransitionPlanner _sceneTransitionPlanner;
        private ISceneTransitionService _sceneTransitionService;

        #region Public Scene Flow API

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

        private (List<string> scenesToLoad, string activeScene, bool useFade) BuildGameplayTargetFromConfig()
        {
            if (GameConfig == null)
            {
                return (null, null, true);
            }

            SceneSetup setup = GameConfig.GameplaySetup;
            if (setup != null && setup.Scenes != null && setup.Scenes.Count > 0)
            {
                var scenes = new List<string>(setup.Scenes.Count);
                foreach (var s in setup.Scenes)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        scenes.Add(s);
                }

                if (scenes.Count == 0)
                    return (null, null, setup.UseFade);

                string active = string.IsNullOrWhiteSpace(setup.ActiveScene)
                    ? scenes[0]
                    : setup.ActiveScene;

                return (scenes, active, setup.UseFade);
            }

            var legacyScenes = new List<string>();
            if (!string.IsNullOrWhiteSpace(GameConfig.GameplayScene))
                legacyScenes.Add(GameConfig.GameplayScene);
            if (!string.IsNullOrWhiteSpace(GameConfig.UIScene))
                legacyScenes.Add(GameConfig.UIScene);

            if (legacyScenes.Count == 0)
                return (null, null, true);

            return (legacyScenes, GameConfig.GameplayScene, true);
        }

        private (List<string> scenesToLoad, string activeScene, bool useFade) BuildMenuTargetFromConfig()
        {
            if (GameConfig == null)
            {
                return (null, null, true);
            }

            SceneSetup setup = GameConfig.MenuSetup;
            if (setup != null && setup.Scenes != null && setup.Scenes.Count > 0)
            {
                var scenes = new List<string>(setup.Scenes.Count);
                foreach (var s in setup.Scenes)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        scenes.Add(s);
                }

                if (scenes.Count == 0)
                    return (null, null, setup.UseFade);

                string active = string.IsNullOrWhiteSpace(setup.ActiveScene)
                    ? scenes[0]
                    : setup.ActiveScene;

                return (scenes, active, setup.UseFade);
            }

            if (string.IsNullOrWhiteSpace(GameConfig.MenuScene))
                return (null, null, true);

            var legacyScenes = new List<string> { GameConfig.MenuScene };
            return (legacyScenes, GameConfig.MenuScene, true);
        }

        private async Task StartGameplayFromCurrentSceneAsync()
        {
            EnsureSceneTransitionServices();

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null || GameConfig == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "StartGameplayFromCurrentSceneAsync chamado sem infraestrutura de transição configurada.");
                return;
            }

            var (scenesToLoad, activeScene, useFade) = BuildGameplayTargetFromConfig();
            if (scenesToLoad == null || scenesToLoad.Count == 0 || string.IsNullOrWhiteSpace(activeScene))
            {
                DebugUtility.LogError<GameManager>(
                    "[StartGameplayFromCurrentSceneAsync] Configuração de cenas inválida no GameConfig/SceneSetup.");
                return;
            }

            SceneState state = SceneState.FromSceneManager();
            DebugUtility.LogVerbose<GameManager>(
                $"[StartGameplayFromCurrentSceneAsync] SceneState atual: {state}");

            SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(
                state,
                scenesToLoad,
                activeScene,
                useFade);

            DebugUtility.LogVerbose<GameManager>(
                "[StartGameplayFromCurrentSceneAsync] Contexto montado: " + context);

            await _sceneTransitionService.RunTransitionAsync(context);
        }

        private async Task GoToMenuFromCurrentSceneAsync()
        {
            EnsureSceneTransitionServices();

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null || GameConfig == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "GoToMenuFromCurrentSceneAsync chamado sem infraestrutura de transição configurada.");
                return;
            }

            var (scenesToLoad, activeScene, useFade) = BuildMenuTargetFromConfig();
            if (scenesToLoad == null || scenesToLoad.Count == 0 || string.IsNullOrWhiteSpace(activeScene))
            {
                DebugUtility.LogError<GameManager>(
                    "[GoToMenuFromCurrentSceneAsync] Configuração de cenas inválida no GameConfig/SceneSetup.");
                return;
            }

            SceneState state = SceneState.FromSceneManager();
            DebugUtility.LogVerbose<GameManager>(
                $"[GoToMenuFromCurrentSceneAsync] SceneState atual: {state}");

            SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(
                state,
                scenesToLoad,
                activeScene,
                useFade);

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

                await Task.Yield();
                await WaitForMenuStateAsync();

                Time.timeScale = 1f;

                GameManagerStateMachine.Instance.Rebuild(this);

                EnsureSceneTransitionServices();

                if (_sceneTransitionPlanner == null || _sceneTransitionService == null || GameConfig == null)
                {
                    DebugUtility.LogWarning<GameManager>(
                        "Infra de transição de cenas não configurada; reset não alterou cenas.");
                }
                else
                {
                    var (scenesToLoad, activeScene, useFade) = BuildGameplayTargetFromConfig();
                    if (scenesToLoad == null || scenesToLoad.Count == 0 || string.IsNullOrWhiteSpace(activeScene))
                    {
                        DebugUtility.LogError<GameManager>(
                            "[ResetGameAsync] Configuração de cenas inválida no GameConfig/SceneSetup.");
                    }
                    else
                    {
                        SceneState state = SceneState.FromSceneManager();

                        SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(
                            state,
                            scenesToLoad,
                            activeScene,
                            useFade);

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

                if (_sceneTransitionPlanner == null || _sceneTransitionService == null || GameConfig == null)
                {
                    DebugUtility.LogWarning<GameManager>(
                        "Infra de transição de cenas não configurada; ReturnToMenu ignorado.");
                    return;
                }

                var (scenesToLoad, activeScene, useFade) = BuildMenuTargetFromConfig();
                if (scenesToLoad == null || scenesToLoad.Count == 0 || string.IsNullOrWhiteSpace(activeScene))
                {
                    DebugUtility.LogError<GameManager>(
                        "[ReturnToMenuAsync] Configuração de cenas inválida no GameConfig/SceneSetup.");
                    return;
                }

                SceneState state = SceneState.FromSceneManager();

                SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(
                    state,
                    scenesToLoad,
                    activeScene,
                    useFade);

                DebugUtility.LogVerbose<GameManager>(
                    "[ReturnToMenuAsync] Contexto montado: " + context);

                await _sceneTransitionService.RunTransitionAsync(context);
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError<GameManager>($"[ReturnToMenuAsync] Exceção durante retorno ao menu: {ex}");
            }
        }

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
