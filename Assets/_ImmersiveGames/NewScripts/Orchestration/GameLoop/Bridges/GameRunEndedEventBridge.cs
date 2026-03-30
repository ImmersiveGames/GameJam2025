using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Readiness.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bridges
{
    /// <summary>
    /// Bridge fino do fim de run.
    ///
    /// O contrato explicito do handoff vive em IPostRunHandoffService.
    /// Este componente apenas traduz o evento do GameLoop para o seam canonico do PostRun.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunEndedEventBridge : MonoBehaviour
    {
        private EventBinding<GameRunEndedEvent> _binding;
        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _registered;
        private bool _postStagePending;

        private void Awake()
        {
            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
            RegisterBinding();
        }

        private void OnEnable()
        {
            RegisterBinding();
        }

        private void OnDisable()
        {
            UnregisterBinding();
        }

        private void OnDestroy()
        {
            UnregisterBinding();
        }

        private void RegisterBinding()
        {
            if (_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Register(_binding);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            _registered = true;
        }

        private void UnregisterBinding()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Unregister(_binding);
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            _registered = false;
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            if (_postStagePending)
            {
                DebugUtility.LogVerbose<GameRunEndedEventBridge>(
                    "[OBS][ExitStage] ExitStageRunEndIgnored reason='already_pending'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _postStagePending = true;
            _ = HandleGameRunEndedAsync(evt);
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            _postStagePending = false;
        }

        private async Task HandleGameRunEndedAsync(GameRunEndedEvent evt)
        {
            try
            {
                await Task.Yield();

                if (!DependencyManager.Provider.TryGetGlobal<IPostRunHandoffService>(out var postRunHandoffService) || postRunHandoffService == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][ExitStage] GameRunEndedEvent recebido mas IPostRunHandoffService nao foi encontrado no escopo global.");
                    return;
                }

                string reason = GameLoopReasonFormatter.Format(evt?.Reason);
                string sceneName = SceneManager.GetActiveScene().name;
                var context = new PostRunHandoffContext(
                    signature: sceneName,
                    sceneName: sceneName,
                    frame: Time.frameCount,
                    outcome: evt?.Outcome ?? GameRunOutcome.Unknown,
                    reason: reason,
                    isGameplayScene: IsGameplayScene());

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[OBS][ExitStage] ExitStageDispatchRequested outcome={evt?.Outcome} reason='{reason}' scene='{sceneName}' frame={Time.frameCount}.");

                await postRunHandoffService.HandleRunEndedAsync(context);

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[OBS][ExitStage] ExitStageCompleted signature='{context.Signature}' outcome='{context.Outcome}' reason='{reason}' scene='{context.SceneName}' frame={context.Frame}.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    $"[FATAL][ExitStage] Falha inesperada ao executar PostRunHandoff. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static bool IsGameplayScene()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier) && classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            return false;
        }
    }
}

