using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.FadeSystem;
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
        private Coroutine _resetRoutine;
        private bool _resetInProgress;

        // Infra moderna de cenas
        private ISceneLoader _sceneLoader;
        private ISceneTransitionPlanner _sceneTransitionPlanner;
        private ISceneTransitionService _sceneTransitionService;
        private IFadeAwaiter _fadeAwaiter;

        #region Public Scene Flow API

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

        private (List<string> scenesToLoad, string activeScene, bool useFade) BuildGameplayTargetFromConfig()
        {
            if (GameConfig == null)
            {
                return (null, null, true);
            }

            // Preferência: usar SceneSetup se configurado
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

            // Fallback: usa strings antigas do GameConfig
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

            // Fallback: usa string antiga do GameConfig
            if (string.IsNullOrWhiteSpace(GameConfig.MenuScene))
                return (null, null, true);

            var legacyScenes = new List<string> { GameConfig.MenuScene };
            return (legacyScenes, GameConfig.MenuScene, true);
        }

        /// <summary>
        /// Helper interno para transicionar da cena atual para o Gameplay+UI
        /// usando SceneState + Planner + SceneTransitionService.
        /// Usa SceneSetup do GameConfig se configurado.
        /// </summary>
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

        /// <summary>
        /// Helper interno para transicionar da cena atual para o Menu usando
        /// SceneState + Planner + SceneTransitionService.
        /// Usa SceneSetup do GameConfig se configurado.
        /// </summary>
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
                        "[ResetGameRoutine] Configuração de cenas inválida no GameConfig/SceneSetup.");
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
                        "[ResetGameRoutine] Contexto montado: " + context);

                    Task task = _sceneTransitionService.RunTransitionAsync(context);
                    yield return WaitForTask(task);
                }
            }

            EventBus<GameResetCompletedEvent>.Raise(new GameResetCompletedEvent());

            _resetInProgress = false;
            _resetRoutine = null;
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            EnsureSceneTransitionServices();

            if (_sceneTransitionPlanner == null || _sceneTransitionService == null || GameConfig == null)
            {
                DebugUtility.LogWarning<GameManager>(
                    "Infra de transição de cenas não configurada; ReturnToMenu ignorado.");
                yield break;
            }

            var (scenesToLoad, activeScene, useFade) = BuildMenuTargetFromConfig();
            if (scenesToLoad == null || scenesToLoad.Count == 0 || string.IsNullOrWhiteSpace(activeScene))
            {
                DebugUtility.LogError<GameManager>(
                    "[ReturnToMenuRoutine] Configuração de cenas inválida no GameConfig/SceneSetup.");
                yield break;
            }

            SceneState state = SceneState.FromSceneManager();

            SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(
                state,
                scenesToLoad,
                activeScene,
                useFade);

            DebugUtility.LogVerbose<GameManager>(
                "[ReturnToMenuRoutine] Contexto montado: " + context);

            Task task = _sceneTransitionService.RunTransitionAsync(context);
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

            while (!(GameManagerStateMachine.Instance.CurrentState is MenuState) && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        #endregion
    }
}
