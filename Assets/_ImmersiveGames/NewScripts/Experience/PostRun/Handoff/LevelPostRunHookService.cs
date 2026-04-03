using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Handoff
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelPostRunHookService : ILevelPostRunHookService
    {
        private readonly ILevelStagePresentationService _stagePresentationService;
        private readonly ILevelPostRunHookPresenterRegistry _presenterRegistry;
        private readonly IPostRunOwnershipService _postRunOwnershipService;

        public LevelPostRunHookService(
            ILevelStagePresentationService stagePresentationService,
            ILevelPostRunHookPresenterRegistry presenterRegistry,
            IPostRunOwnershipService postRunOwnershipService)
        {
            _stagePresentationService = stagePresentationService ?? throw new ArgumentNullException(nameof(stagePresentationService));
            _presenterRegistry = presenterRegistry ?? throw new ArgumentNullException(nameof(presenterRegistry));
            _postRunOwnershipService = postRunOwnershipService ?? throw new ArgumentNullException(nameof(postRunOwnershipService));
        }

        public async Task RunReactionAsync(LevelPostRunHookContext context, CancellationToken cancellationToken = default)
        {
            if (!_stagePresentationService.TryGetCurrentContract(out LevelStagePresentationContract contract) ||
                !contract.IsValid ||
                contract.LevelRef == null)
            {
                DebugUtility.LogVerbose<LevelPostRunHookService>(
                    $"[OBS][GameplaySessionFlow][PostRun] LevelPostRunHookSkipped reason='missing_current_level_contract' result='{context.Result}'.");
                return;
            }

            if (!contract.HasPostRunReactionHook)
            {
                DebugUtility.LogVerbose<LevelPostRunHookService>(
                    $"[OBS][GameplaySessionFlow][PostRun] LevelPostRunHookSkipped reason='hook_disabled' levelRef='{contract.LevelRef.name}' result='{context.Result}'.");
                return;
            }

            DebugUtility.Log<LevelPostRunHookService>(
                $"[OBS][GameplaySessionFlow][PostRun] LevelPostRunHookStarted levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                DebugUtility.Colors.Info);

            if (_presenterRegistry.TryEnsureCurrentPresenter(context, nameof(LevelPostRunHookService), out ILevelPostRunHookPresenter presenter))
            {
                DebugUtility.Log<LevelPostRunHookService>(
                    $"[OBS][GameplaySessionFlow][PostRun] LevelPostRunHookPresenterAdopted levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}' presenter='{presenter.GetType().FullName}'.",
                    DebugUtility.Colors.Info);

                DebugUtility.Log<LevelPostRunHookService>(
                    $"[OBS][GameplaySessionFlow][PostRun] LevelPostRunHookPresenterAwaitingCompletion levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}' presenter='{presenter.GetType().FullName}'.",
                    DebugUtility.Colors.Info);

                await presenter.WaitForCompletionAsync(cancellationToken).ConfigureAwait(true);
            }
            else
            {
                // Comentário: o presenter canônico local ainda pode não existir na cena.
                // Neste caso, mantemos o skip observável para smoke e validação arquitetural.
                DebugUtility.LogVerbose<LevelPostRunHookService>(
                    $"[OBS][GameplaySessionFlow][PostRun] LevelPostRunHookMockVisualSkipped levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}'.");
            }

            DebugUtility.Log<LevelPostRunHookService>(
                $"[OBS][GameplaySessionFlow][PostRun] LevelPostRunHookCompleted levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}'.",
                DebugUtility.Colors.Info);

            if (!_postRunOwnershipService.IsActive)
            {
                DebugUtility.LogWarning<LevelPostRunHookService>(
                    $"[OBS][GameplaySessionFlow][RunDecision] Ownership nao estava ativo ao final do PostRunHook. signature='{context.PostRunSignature}' levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}'.");
            }
            else
            {
                DebugUtility.LogVerbose<LevelPostRunHookService>(
                    $"[OBS][GameplaySessionFlow][RunDecision] Ownership already active when PostRunHook completed. signature='{context.PostRunSignature}' levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}'.");
            }
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
