using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Handoff
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostRunHandoffService : IPostRunHandoffService
    {
        private readonly IPostStageCoordinator _postStageCoordinator;
        private readonly IPostRunResultService _postGameResultService;
        private readonly IPostRunOwnershipService _postGameOwnershipService;
        private readonly IGameLoopService _gameLoopService;

        public PostRunHandoffService(
            IPostStageCoordinator postStageCoordinator,
            IPostRunResultService postGameResultService,
            IPostRunOwnershipService postGameOwnershipService,
            IGameLoopService gameLoopService)
        {
            _postStageCoordinator = postStageCoordinator ?? throw new ArgumentNullException(nameof(postStageCoordinator));
            _postGameResultService = postGameResultService ?? throw new ArgumentNullException(nameof(postGameResultService));
            _postGameOwnershipService = postGameOwnershipService ?? throw new ArgumentNullException(nameof(postGameOwnershipService));
            _gameLoopService = gameLoopService ?? throw new ArgumentNullException(nameof(gameLoopService));
        }

        public async Task HandleRunEndedAsync(PostRunHandoffContext context, CancellationToken cancellationToken = default)
        {
            if (!context.IsGameplayScene)
            {
                DebugUtility.LogVerbose<PostRunHandoffService>(
                    $"[OBS][PostRun][Bridge] PostRunHandoffSkipped reason='scene_not_gameplay' scene='{Normalize(context.SceneName)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            var stageContext = new PostStageContext(
                signature: context.Signature,
                sceneName: context.SceneName,
                frame: context.Frame,
                outcome: context.Outcome,
                reason: context.Reason,
                isGameplayScene: context.IsGameplayScene);

            try
            {
                DebugUtility.Log<PostRunHandoffService>(
                    $"[OBS][ExitStage] PostRunHandoffStarted outcome={context.Outcome} reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                    DebugUtility.Colors.Info);

                await _postStageCoordinator.RunAsync(stageContext, cancellationToken);

                DebugUtility.Log<PostRunHandoffService>(
                    $"[OBS][ExitStage] PostStageCompleted signature='{stageContext.Signature}' outcome='{stageContext.Outcome}' reason='{Normalize(context.Reason)}' scene='{stageContext.SceneName}' frame={stageContext.Frame}.",
                    DebugUtility.Colors.Info);

                if (!TryMapToPostRunResult(context.Outcome, out PostRunResult postGameResult))
                {
                    DebugUtility.LogError<PostRunHandoffService>(
                        $"[FATAL][ExitStage] Handoff recebido com outcome nao terminal='{context.Outcome}' reason='{Normalize(context.Reason)}'.");
                    return;
                }

                _postGameResultService.TrySetRunOutcome(context.Outcome, context.Reason);

                _postGameOwnershipService.OnPostRunEntered(new PostRunOwnershipContext(
                    signature: stageContext.Signature,
                    sceneName: stageContext.SceneName,
                    profile: string.Empty,
                    frame: stageContext.Frame,
                    result: postGameResult,
                    reason: context.Reason));

                DebugUtility.Log<PostRunHandoffService>(
                    $"[OBS][ExitStage] DownstreamHandoffRequested target='PostRunOwnershipService.OnPostRunEntered' outcome='{stageContext.Outcome}' reason='{Normalize(context.Reason)}' scene='{stageContext.SceneName}' frame={stageContext.Frame}.",
                    DebugUtility.Colors.Info);

                _gameLoopService.RequestRunEnd();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<PostRunHandoffService>(
                    $"[FATAL][ExitStage] Falha inesperada ao executar PostRunHandoff. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static bool TryMapToPostRunResult(GameRunOutcome outcome, out PostRunResult result)
        {
            result = outcome switch
            {
                GameRunOutcome.Victory => PostRunResult.Victory,
                GameRunOutcome.Defeat => PostRunResult.Defeat,
                _ => PostRunResult.None,
            };

            return result != PostRunResult.None;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

