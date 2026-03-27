using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.PostGame;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Run
{
    /// <summary>
    /// Bridge do fim de run: GameRunEndedEvent -> PostStage -> IGameLoopService.RequestRunEnd().
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
            if (!IsGameplayScene())
            {
                DebugUtility.LogWarning<GameRunEndedEventBridge>(
                    $"[OBS][PostGame] PostStageSkipped reason='scene_not_gameplay' scene='{SceneManager.GetActiveScene().name}'.");
                return;
            }

            if (_postStagePending)
            {
                DebugUtility.LogVerbose<GameRunEndedEventBridge>(
                    "[OBS][PostGame] PostStageRunEndIgnored reason='already_pending'.",
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

                if (!DependencyManager.Provider.TryGetGlobal<IPostStageCoordinator>(out var postStageCoordinator) || postStageCoordinator == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][PostGame] GameRunEndedEvent recebido mas IPostStageCoordinator nao foi encontrado no escopo global.");
                    return;
                }

                if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][GameLoop] GameRunEndedEvent recebido mas IGameLoopService nao foi encontrado no escopo global.");
                    return;
                }

                string reason = GameLoopReasonFormatter.Format(evt?.Reason);
                string sceneName = SceneManager.GetActiveScene().name;
                var context = new PostStageContext(
                    signature: sceneName,
                    sceneName: sceneName,
                    frame: Time.frameCount,
                    outcome: evt?.Outcome ?? GameRunOutcome.Unknown,
                    reason: reason,
                    isGameplayScene: IsGameplayScene());

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[GameLoop] GameRunEndedEvent recebido. Outcome={evt?.Outcome}, Reason='{reason}'. Iniciando PostStage.");

                await postStageCoordinator.RunAsync(context);

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[OBS][PostGame] PostStageRunEndHandoff signature='{context.Signature}' outcome='{context.Outcome}' reason='{reason}' scene='{context.SceneName}' frame={context.Frame}.",
                    DebugUtility.Colors.Info);

                gameLoopService.RequestRunEnd();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    $"[FATAL][PostGame] Falha inesperada ao executar PostStage. ex='{ex.GetType().Name}: {ex.Message}'.");
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
