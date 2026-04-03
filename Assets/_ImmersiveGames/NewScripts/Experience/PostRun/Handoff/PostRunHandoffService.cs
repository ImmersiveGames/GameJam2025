using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Handoff
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostRunHandoffService : IPostRunHandoffService
    {
        private readonly IPostStageCoordinator _postStageCoordinator;
        private readonly IPostRunResultService _postRunResultService;
        private readonly IPostRunOwnershipService _postRunOwnershipService;

        public PostRunHandoffService(
            IPostStageCoordinator postStageCoordinator,
            IPostRunResultService postRunResultService,
            IPostRunOwnershipService postRunOwnershipService)
        {
            _postStageCoordinator = postStageCoordinator ?? throw new ArgumentNullException(nameof(postStageCoordinator));
            _postRunResultService = postRunResultService ?? throw new ArgumentNullException(nameof(postRunResultService));
            _postRunOwnershipService = postRunOwnershipService ?? throw new ArgumentNullException(nameof(postRunOwnershipService));
        }

        public async Task HandleRunEndedAsync(PostRunHandoffContext context, CancellationToken cancellationToken = default)
        {
            if (!context.IsGameplayScene)
            {
                DebugUtility.LogVerbose<PostRunHandoffService>(
                    $"[OBS][GameplaySessionFlow][Bridge] PostRunHandoffSkipped reason='scene_not_gameplay' scene='{Normalize(context.SceneName)}'.",
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
                    $"[OBS][GameplaySessionFlow] PostRunHandoffStarted outcome={context.Outcome} reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                    DebugUtility.Colors.Info);

                await _postStageCoordinator.RunAsync(stageContext, cancellationToken);

                DebugUtility.Log<PostRunHandoffService>(
                    $"[OBS][GameplaySessionFlow] PostStageCompleted signature='{stageContext.Signature}' outcome='{stageContext.Outcome}' reason='{Normalize(context.Reason)}' scene='{stageContext.SceneName}' frame={stageContext.Frame}.",
                    DebugUtility.Colors.Info);

                if (!TryMapToPostRunResult(context.Outcome, out PostRunResult postRunResult))
                {
                    DebugUtility.LogError<PostRunHandoffService>(
                        $"[FATAL][GameplaySessionFlow] Handoff recebido com outcome nao terminal='{context.Outcome}' reason='{Normalize(context.Reason)}'.");
                    return;
                }

                _postRunResultService.TrySetRunOutcome(context.Outcome, context.Reason);

                DebugUtility.Log<PostRunHandoffService>(
                    $"[OBS][GameplaySessionFlow][RunOutcome] RunOutcomeAccepted outcome='{context.Outcome}' result='{postRunResult}' reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                    DebugUtility.Colors.Info);

                DebugUtility.Log<PostRunHandoffService>(
                    $"[OBS][GameplaySessionFlow] PostRunStarted signature='{stageContext.Signature}' outcome='{postRunResult}' reason='{Normalize(context.Reason)}' scene='{stageContext.SceneName}' frame={stageContext.Frame}.",
                    DebugUtility.Colors.Info);

                if (TryResolveCurrentLevelContract(out LevelStagePresentationContract contract) &&
                    contract.IsValid &&
                    contract.LevelRef != null &&
                    contract.HasPostRunReactionHook)
                {
                    if (!DependencyManager.Provider.TryGetGlobal<ILevelPostRunHookService>(out var levelPostRunHookService) || levelPostRunHookService == null)
                    {
                        DebugUtility.LogVerbose<PostRunHandoffService>(
                            $"[OBS][GameplaySessionFlow] PostRunCompletedBypass reason='level_postrun_hook_service_missing' signature='{stageContext.Signature}' outcome='{postRunResult}' scene='{stageContext.SceneName}' frame={stageContext.Frame}.",
                            DebugUtility.Colors.Info);

                        DebugUtility.Log<PostRunHandoffService>(
                            $"[OBS][GameplaySessionFlow] PostRunCompleted signature='{stageContext.Signature}' outcome='{postRunResult}' reason='{Normalize(context.Reason)}' scene='{stageContext.SceneName}' frame={stageContext.Frame}.",
                            DebugUtility.Colors.Info);

                        _postRunOwnershipService.OnPostRunEntered(new PostRunOwnershipContext(
                            signature: stageContext.Signature,
                            sceneName: stageContext.SceneName,
                            profile: string.Empty,
                            frame: stageContext.Frame,
                            result: postRunResult,
                            reason: context.Reason));
                    }
                    else
                    {
                        await levelPostRunHookService.RunReactionAsync(new LevelPostRunHookContext(
                            contract.LevelRef,
                            contract.LevelSignature,
                            stageContext.Signature,
                            stageContext.SceneName,
                            postRunResult,
                            context.Reason,
                            stageContext.Frame),
                            cancellationToken);
                    }
                }
                else
                {
                    DebugUtility.LogVerbose<PostRunHandoffService>(
                        $"[OBS][GameplaySessionFlow] PostRunCompletedBypass reason='missing_current_level_contract_or_hook_disabled' signature='{stageContext.Signature}' outcome='{postRunResult}' scene='{stageContext.SceneName}' frame={stageContext.Frame}.",
                        DebugUtility.Colors.Info);

                    DebugUtility.Log<PostRunHandoffService>(
                        $"[OBS][GameplaySessionFlow] PostRunCompleted signature='{stageContext.Signature}' outcome='{postRunResult}' reason='{Normalize(context.Reason)}' scene='{stageContext.SceneName}' frame={stageContext.Frame}.",
                        DebugUtility.Colors.Info);

                    _postRunOwnershipService.OnPostRunEntered(new PostRunOwnershipContext(
                        signature: stageContext.Signature,
                        sceneName: stageContext.SceneName,
                        profile: string.Empty,
                        frame: stageContext.Frame,
                        result: postRunResult,
                        reason: context.Reason));
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<PostRunHandoffService>(
                    $"[FATAL][GameplaySessionFlow] Falha inesperada ao executar PostRunHandoff. ex='{ex.GetType().Name}: {ex.Message}'.");
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

        private static bool TryResolveCurrentLevelContract(out LevelStagePresentationContract contract)
        {
            contract = default;

            if (!DependencyManager.Provider.TryGetGlobal<ILevelStagePresentationService>(out var stagePresentationService) ||
                stagePresentationService == null)
            {
                return false;
            }

            return stagePresentationService.TryGetCurrentContract(out contract);
        }
    }
}
