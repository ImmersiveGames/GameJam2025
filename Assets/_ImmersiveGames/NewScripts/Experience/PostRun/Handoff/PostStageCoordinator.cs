using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Presentation;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Handoff
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostStageCoordinator : IPostStageCoordinator
    {
        private readonly IPostStageControlService _controlService;
        private readonly IPostStagePresenterRegistry _presenterRegistry;

        public PostStageCoordinator(
            IPostStageControlService controlService,
            IPostStagePresenterRegistry presenterRegistry)
        {
            _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
            _presenterRegistry = presenterRegistry ?? throw new ArgumentNullException(nameof(presenterRegistry));
        }

        public async Task RunAsync(PostStageContext context, CancellationToken cancellationToken = default)
        {
            if (!context.IsGameplayScene)
            {
                DebugUtility.LogVerbose<PostStageCoordinator>(
                    $"[OBS][PostRun][Bridge] PostStageSkipped reason='scene_not_gameplay' scene='{Normalize(context.SceneName)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!_controlService.TryBegin(context))
            {
                await _controlService.WaitForCompletionAsync(cancellationToken);
                return;
            }

            DebugUtility.Log<PostStageCoordinator>(
                $"[OBS][PostRun][Bridge] PostStageStartRequested signature='{Normalize(context.Signature)}' outcome='{context.Outcome}' reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                DebugUtility.Colors.Info);
            EventBus<PostStageStartRequestedEvent>.Raise(new PostStageStartRequestedEvent(context));

            DebugUtility.Log<PostStageCoordinator>(
                $"[OBS][PostRun][Bridge] PostStageStarted signature='{Normalize(context.Signature)}' outcome='{context.Outcome}' reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                DebugUtility.Colors.Info);
            EventBus<PostStageStartedEvent>.Raise(new PostStageStartedEvent(context));

            if (!_presenterRegistry.TryEnsureCurrentPresenter(context, nameof(PostStageCoordinator), out IPostStagePresenter presenter))
            {
                // Comentário: ausência de presenter aqui é um skip opcional intencional.
                // O rail de PostStage continua canonico mesmo quando nao existe UI de apresentacao para a cena.
                DebugUtility.Log<PostStageCoordinator>(
                    $"[OBS][PostRun][Bridge] PostStageSkipped reason='PostStage/NoPresenter' signature='{Normalize(context.Signature)}' outcome='{context.Outcome}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                    DebugUtility.Colors.Info);
                _controlService.TrySkip("PostStage/NoPresenter");
            }
            else
            {
                if (!presenter.IsReady)
                {
                    HardFailFastH1.Trigger(typeof(PostStageCoordinator),
                        $"[FATAL][H1][PostRun] IPostStagePresenter resolved but not ready after bind. signature='{Normalize(context.Signature)}' scene='{Normalize(context.SceneName)}' presenter='{presenter.GetType().FullName}'.");
                }

                DebugUtility.Log<PostStageCoordinator>(
                    $"[OBS][PostRun][Bridge] PostStageWaitingForPresenterConfirmation signature='{Normalize(context.Signature)}' outcome='{context.Outcome}' reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame} presenter='{presenter.GetType().FullName}'.",
                    DebugUtility.Colors.Info);
            }

            PostStageCompletionResult completion = await _controlService.WaitForCompletionAsync(cancellationToken);

            DebugUtility.Log<PostStageCoordinator>(
                $"[OBS][PostRun][Bridge] PostStageCompleted signature='{Normalize(context.Signature)}' kind='{completion.Kind}' reason='{Normalize(completion.Reason)}' outcome='{context.Outcome}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                DebugUtility.Colors.Info);
            EventBus<PostStageCompletedEvent>.Raise(new PostStageCompletedEvent(context, completion));
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

