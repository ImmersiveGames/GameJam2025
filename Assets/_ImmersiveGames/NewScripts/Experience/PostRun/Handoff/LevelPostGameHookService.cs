using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Handoff
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelPostRunHookService : ILevelPostRunHookService
    {
        private readonly ILevelStagePresentationService _stagePresentationService;

        public LevelPostRunHookService(ILevelStagePresentationService stagePresentationService)
        {
            _stagePresentationService = stagePresentationService ?? throw new ArgumentNullException(nameof(stagePresentationService));
        }

        public Task RunReactionAsync(LevelPostRunHookContext context, CancellationToken cancellationToken = default)
        {
            if (!_stagePresentationService.TryGetCurrentContract(out LevelStagePresentationContract contract) ||
                !contract.IsValid ||
                contract.LevelRef == null)
            {
                DebugUtility.LogVerbose<LevelPostRunHookService>(
                    $"[OBS][PostRun] LevelPostRunHookSkipped reason='missing_current_level_contract' result='{context.Result}'.");
                return Task.CompletedTask;
            }

            if (!contract.HasPostRunReactionHook)
            {
                DebugUtility.LogVerbose<LevelPostRunHookService>(
                    $"[OBS][PostRun] LevelPostRunHookSkipped reason='hook_disabled' levelRef='{contract.LevelRef.name}' result='{context.Result}'.");
                return Task.CompletedTask;
            }

            DebugUtility.Log<LevelPostRunHookService>(
                $"[OBS][PostRun] LevelPostRunHookStarted levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                DebugUtility.Colors.Info);

            _ = cancellationToken;

            DebugUtility.LogVerbose<LevelPostRunHookService>(
                $"[OBS][PostRun] LevelPostRunHookMockVisualSkipped levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}'.");

            DebugUtility.Log<LevelPostRunHookService>(
                $"[OBS][PostRun] LevelPostRunHookCompleted levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}'.",
                DebugUtility.Colors.Info);

            return Task.CompletedTask;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}

