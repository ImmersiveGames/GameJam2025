using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelPostGameHookService : ILevelPostGameHookService
    {
        private readonly ILevelStagePresentationService _stagePresentationService;

        public LevelPostGameHookService(ILevelStagePresentationService stagePresentationService)
        {
            _stagePresentationService = stagePresentationService ?? throw new ArgumentNullException(nameof(stagePresentationService));
        }

        public Task RunReactionAsync(LevelPostGameHookContext context, CancellationToken cancellationToken = default)
        {
            if (!_stagePresentationService.TryGetCurrentContract(out LevelStagePresentationContract contract) ||
                !contract.IsValid ||
                contract.LevelRef == null)
            {
                DebugUtility.LogVerbose<LevelPostGameHookService>(
                    $"[OBS][PostGame] LevelPostGameHookSkipped reason='missing_current_level_contract' result='{context.Result}'.");
                return Task.CompletedTask;
            }

            if (!contract.HasPostGameReactionHook)
            {
                DebugUtility.LogVerbose<LevelPostGameHookService>(
                    $"[OBS][PostGame] LevelPostGameHookSkipped reason='hook_disabled' levelRef='{contract.LevelRef.name}' result='{context.Result}'.");
                return Task.CompletedTask;
            }

            DebugUtility.Log<LevelPostGameHookService>(
                $"[OBS][PostGame] LevelPostGameHookStarted levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                DebugUtility.Colors.Info);

            _ = cancellationToken;

            DebugUtility.LogVerbose<LevelPostGameHookService>(
                $"[OBS][PostGame] LevelPostGameHookMockVisualSkipped levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}'.");

            DebugUtility.Log<LevelPostGameHookService>(
                $"[OBS][PostGame] LevelPostGameHookCompleted levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}'.",
                DebugUtility.Colors.Info);

            return Task.CompletedTask;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
