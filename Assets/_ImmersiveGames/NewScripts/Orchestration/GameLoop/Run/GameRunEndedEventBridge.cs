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
    /// Bridge do fim de run: GameRunEndedEvent -> PostStage -> PostGame.
    ///
    /// O GameLoop permanece owner do estado de fluxo e do terminal interno da run.
    /// O PostGame assume a projeção do resultado e a entrada do pós-run.
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
                    $"[OBS][ExitStage] ExitStageSkipped reason='scene_not_gameplay' scene='{SceneManager.GetActiveScene().name}'.");
                return;
            }

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

                if (!DependencyManager.Provider.TryGetGlobal<IPostStageCoordinator>(out var postStageCoordinator) || postStageCoordinator == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][ExitStage] GameRunEndedEvent recebido mas IPostStageCoordinator nao foi encontrado no escopo global.");
                    return;
                }

                if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        "[FATAL][ExitStage] GameRunEndedEvent recebido mas IGameLoopService nao foi encontrado no escopo global.");
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
                    $"[OBS][ExitStage] ExitStageStarted outcome={evt?.Outcome} reason='{reason}' scene='{sceneName}' frame={Time.frameCount}.");

                await postStageCoordinator.RunAsync(context);

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[OBS][ExitStage] ExitStageCompleted signature='{context.Signature}' outcome='{context.Outcome}' reason='{reason}' scene='{context.SceneName}' frame={context.Frame}.",
                    DebugUtility.Colors.Info);

                var postGameResultService = ResolveRequiredPostGameResultService(reason);
                var postGameOwnershipService = ResolveRequiredPostGameOwnershipService(reason);

                GameRunOutcome outcome = evt?.Outcome ?? GameRunOutcome.Unknown;
                if (!TryMapToPostGameResult(outcome, out PostGameResult postGameResult))
                {
                    DebugUtility.LogError<GameRunEndedEventBridge>(
                        $"[FATAL][ExitStage] GameRunEndedEvent recebido com outcome nao terminal='{outcome}' reason='{reason}'.");
                    return;
                }

                postGameResultService.TrySetRunOutcome(outcome, reason);

                postGameOwnershipService.OnPostGameEntered(new PostGameOwnershipContext(
                    signature: context.Signature,
                    sceneName: context.SceneName,
                    profile: string.Empty,
                    frame: context.Frame,
                    result: postGameResult,
                    reason: reason));

                DebugUtility.Log<GameRunEndedEventBridge>(
                    $"[OBS][ExitStage] DownstreamHandoffRequested target='PostGameOwnershipService.OnPostGameEntered' outcome='{context.Outcome}' reason='{reason}' scene='{context.SceneName}' frame={context.Frame}.",
                    DebugUtility.Colors.Info);
                gameLoopService.RequestRunEnd();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    $"[FATAL][ExitStage] Falha inesperada ao executar PostStage. ex='{ex.GetType().Name}: {ex.Message}'.");
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

        private static IPostGameResultService ResolveRequiredPostGameResultService(string reason)
        {
            if (DependencyManager.Provider.TryGetGlobal<IPostGameResultService>(out var resultService) && resultService != null)
            {
                return resultService;
            }

            HardFailFastH1.Trigger(typeof(GameRunEndedEventBridge),
                $"[FATAL][H1][PostGame] IPostGameResultService nao encontrado no escopo global. reason='{reason}'.");
            return null;
        }

        private static IPostGameOwnershipService ResolveRequiredPostGameOwnershipService(string reason)
        {
            if (DependencyManager.Provider.TryGetGlobal<IPostGameOwnershipService>(out var ownershipService) && ownershipService != null)
            {
                return ownershipService;
            }

            HardFailFastH1.Trigger(typeof(GameRunEndedEventBridge),
                $"[FATAL][H1][PostGame] IPostGameOwnershipService nao encontrado no escopo global. reason='{reason}'.");
            return null;
        }

        private static bool TryMapToPostGameResult(GameRunOutcome outcome, out PostGameResult result)
        {
            result = outcome switch
            {
                GameRunOutcome.Victory => PostGameResult.Victory,
                GameRunOutcome.Defeat => PostGameResult.Defeat,
                _ => PostGameResult.None,
            };

            return result != PostGameResult.None;
        }
    }
}
