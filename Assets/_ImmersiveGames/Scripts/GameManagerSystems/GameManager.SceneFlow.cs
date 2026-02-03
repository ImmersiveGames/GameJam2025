using System.Threading.Tasks;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.SceneManagement.OldTransition;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    public sealed partial class GameManager
    {
        [Header("Scene Flow (Modern)")]
        [SerializeField]
        [Tooltip("Mapa lógico de grupos de cena usado para Menu, Gameplay e fluxos adicionais.")]
        private SceneFlowMap sceneFlowMap;

        private bool _resetInProgress;
        private bool _qaFlowInProgress;

        private ISceneLoader _sceneLoader;
        private ISceneTransitionPlanner _sceneTransitionPlanner;
        private IOldSceneTransitionService _sceneTransitionService;

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

        #region QA Public API (Flow real)

        public void QA_GoToMenuFromAnywhere() => _ = QA_GoToMenuFromAnywhereAsync();
        public void QA_GoToGameplayFromAnywhere() => _ = QA_GoToGameplayFromAnywhereAsync();
        public void QA_ForceGameOverFromAnywhere(string reason = null) => _ = QA_ForceGameOverFromAnywhereAsync(reason);
        public void QA_ForceVictoryFromAnywhere(string reason = null) => _ = QA_ForceVictoryFromAnywhereAsync(reason);

        public Task QA_GoToMenuFromAnywhereAsync() => QA_GoToMenuFromAnywhereImplAsync();
        public Task QA_GoToGameplayFromAnywhereAsync() => QA_GoToGameplayFromAnywhereImplAsync();
        public Task QA_ForceGameOverFromAnywhereAsync(string reason = null) => QA_ForceGameOverFromAnywhereImplAsync(reason);
        public Task QA_ForceVictoryFromAnywhereAsync(string reason = null) => QA_ForceVictoryFromAnywhereImplAsync(reason);

        #endregion

        #region Modern Scene Flow Helpers

        private void EnsureSceneTransitionServices()
        {
            var provider = DependencyManager.Provider;

            if (_sceneLoader == null && provider.TryGetGlobal(out ISceneLoader loader))
                _sceneLoader = loader;

            if (_sceneTransitionPlanner == null && provider.TryGetGlobal(out ISceneTransitionPlanner planner))
                _sceneTransitionPlanner = planner;

            if (_sceneTransitionService == null && provider.TryGetGlobal(out IOldSceneTransitionService service))
                _sceneTransitionService = service;
        }

        private SceneGroupProfile GetGameplayGroup()
        {
            if (sceneFlowMap != null && sceneFlowMap.GameplayGroup != null)
                return sceneFlowMap.GameplayGroup;

            DebugUtility.LogWarning<GameManager>(
                "[SceneFlow] SceneFlowMap ou GameplayGroup não configurados.");
            return null;
        }

        private SceneGroupProfile GetMenuGroup()
        {
            if (sceneFlowMap != null && sceneFlowMap.MenuGroup != null)
                return sceneFlowMap.MenuGroup;

            DebugUtility.LogWarning<GameManager>(
                "[SceneFlow] SceneFlowMap ou MenuGroup não configurados.");
            return null;
        }

        private bool IsGameplayReady()
        {
            var current = OldGameManagerStateMachine.Instance.CurrentState;
            return current is OldPlayingState || current is OldPausedState;
        }

        private bool IsMenuReady()
        {
            // Critério mínimo e seguro:
            // - já estamos em OldMenuState
            // - e a cena ativa é Menu (ou o grupo já está carregado)
            var current = OldGameManagerStateMachine.Instance.CurrentState;
            if (current is not OldMenuState)
                return false;

            var menuGroup = GetMenuGroup();
            if (menuGroup == null)
                return true; // se não há config, ao menos não reentra no OldMenuState.

            SceneState state = SceneState.Capture();

            // Se todas as cenas do grupo já estão carregadas, consideramos "ready".
            if (menuGroup.SceneNames != null)
            {
                foreach (var s in menuGroup.SceneNames)
                {
                    if (string.IsNullOrWhiteSpace(s))
                        continue;

                    if (!state.LoadedScenes.Contains(s))
                        return false;
                }
            }

            // Se houver ActiveSceneName, valida.
            if (!string.IsNullOrWhiteSpace(menuGroup.ActiveSceneName))
            {
                if (state.ActiveSceneName != menuGroup.ActiveSceneName)
                    return false;
            }

            return true;
        }

        #endregion

        #region Reset / ReturnToMenu Routines

        private async Task ResetGameAsync()
        {
            try
            {
                EventBus<GameResetStartedEvent>.Raise(new GameResetStartedEvent());

                await Task.Yield();
                await WaitForMenuStateAsync();

                Time.timeScale = 1f;

                OldGameManagerStateMachine.Instance.Rebuild(this);

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
                        SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(state, targetGroup);

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
                SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(state, targetGroup);

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

            while (!(OldGameManagerStateMachine.Instance.CurrentState is OldMenuState) && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }
        }

        private async Task WaitForPlayingStateAsync()
        {
            const float timeoutSeconds = 1.5f;
            float elapsed = 0f;

            while (!(OldGameManagerStateMachine.Instance.CurrentState is OldPlayingState) && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }
        }

        #endregion

        #region QA Flow Implementations

        private async Task QA_GoToMenuFromAnywhereImplAsync()
        {
            if (_qaFlowInProgress)
            {
                DebugUtility.LogWarning<GameManager>("[QA] Fluxo já em andamento; ignorando solicitação.");
                return;
            }

            _qaFlowInProgress = true;

            try
            {
                EnsureSceneTransitionServices();

                if (_sceneTransitionPlanner == null || _sceneTransitionService == null)
                {
                    DebugUtility.LogWarning<GameManager>(
                        "[QA] Infra de transição não configurada (Menu).");
                    return;
                }

                Time.timeScale = 1f;

                // Idempotência: se já está em menu e as cenas já estão corretas, não faz churn.
                if (IsMenuReady())
                {
                    DebugUtility.LogVerbose<GameManager>(
                        "[QA] Menu já está pronto (OldMenuState + cenas ok). Ignorando transição/rebuild.");
                    return;
                }

                // Só rebuild se não for OldMenuState (reduz churn e token spam)
                if (!(OldGameManagerStateMachine.Instance.CurrentState is OldMenuState))
                    OldGameManagerStateMachine.Instance.Rebuild(this);

                var targetGroup = GetMenuGroup();
                if (targetGroup == null)
                {
                    DebugUtility.LogError<GameManager>(
                        "[QA] MenuGroup não configurado no SceneFlowMap.");
                    return;
                }

                SceneState state = SceneState.Capture();
                SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(state, targetGroup);

                DebugUtility.LogVerbose<GameManager>(
                    "[QA] Indo para MENU (fluxo real). Contexto: " + context);

                await _sceneTransitionService.RunTransitionAsync(context);
                await Task.Yield();
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError<GameManager>($"[QA_GoToMenuFromAnywhereAsync] Exceção: {ex}");
            }
            finally
            {
                _qaFlowInProgress = false;
            }
        }

        private async Task QA_GoToGameplayFromAnywhereImplAsync()
        {
            if (_qaFlowInProgress)
            {
                DebugUtility.LogWarning<GameManager>("[QA] Fluxo já em andamento; ignorando solicitação.");
                return;
            }

            _qaFlowInProgress = true;

            try
            {
                EnsureSceneTransitionServices();

                if (_sceneTransitionPlanner == null || _sceneTransitionService == null)
                {
                    DebugUtility.LogWarning<GameManager>(
                        "[QA] Infra de transição não configurada (Gameplay).");
                    return;
                }

                if (IsGameplayReady())
                {
                    DebugUtility.LogVerbose<GameManager>(
                        "[QA] Gameplay já está pronto (Playing/Paused). Ignorando transição.");
                    return;
                }

                Time.timeScale = 1f;

                // Só rebuild se não for OldMenuState (reduz churn e token spam)
                if (!(OldGameManagerStateMachine.Instance.CurrentState is OldMenuState))
                    OldGameManagerStateMachine.Instance.Rebuild(this);

                var targetGroup = GetGameplayGroup();
                if (targetGroup == null)
                {
                    DebugUtility.LogError<GameManager>(
                        "[QA] GameplayGroup não configurado no SceneFlowMap.");
                    return;
                }

                SceneState state = SceneState.Capture();
                SceneTransitionContext context = _sceneTransitionPlanner.BuildContext(state, targetGroup);

                DebugUtility.LogVerbose<GameManager>(
                    "[QA] Indo para GAMEPLAY (fluxo real). Contexto: " + context);

                await _sceneTransitionService.RunTransitionAsync(context);

                EventBus<OldGameStartRequestedEvent>.Raise(new OldGameStartRequestedEvent());

                await Task.Yield();
                await WaitForPlayingStateAsync();
                await Task.Yield();
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError<GameManager>($"[QA_GoToGameplayFromAnywhereAsync] Exceção: {ex}");
            }
            finally
            {
                _qaFlowInProgress = false;
            }
        }

        private async Task QA_ForceGameOverFromAnywhereImplAsync(string reason)
        {
            await QA_GoToGameplayFromAnywhereImplAsync();
            TryTriggerGameOver(reason ?? "QA ForceGameOver");
            await Task.Yield();
        }

        private async Task QA_ForceVictoryFromAnywhereImplAsync(string reason)
        {
            await QA_GoToGameplayFromAnywhereImplAsync();
            TryTriggerVictory(reason ?? "QA ForceVictory");
            await Task.Yield();
        }

        #endregion
    }
}


