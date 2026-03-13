using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Dev;
#endif
using _ImmersiveGames.NewScripts.Modules.PostGame;

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

        public async Task RunReactionAsync(LevelPostGameHookContext context, CancellationToken cancellationToken = default)
        {
            if (!_stagePresentationService.TryGetCurrentContract(out LevelStagePresentationContract contract) ||
                !contract.IsValid ||
                contract.LevelRef == null)
            {
                DebugUtility.LogVerbose<LevelPostGameHookService>(
                    $"[OBS][PostGame] LevelPostGameHookSkipped reason='missing_current_level_contract' result='{context.Result}'.");
                return;
            }

            if (!contract.HasPostGameReactionHook)
            {
                DebugUtility.LogVerbose<LevelPostGameHookService>(
                    $"[OBS][PostGame] LevelPostGameHookSkipped reason='hook_disabled' levelRef='{contract.LevelRef.name}' result='{context.Result}'.");
                return;
            }

            DebugUtility.Log<LevelPostGameHookService>(
                $"[OBS][PostGame] LevelPostGameHookStarted levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                DebugUtility.Colors.Info);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            await IntroStageRuntimeDebugGui.RunPostGameReactionAsync(
                contract.LevelRef.name,
                context.Result,
                context.Reason,
                cancellationToken);
#endif

            DebugUtility.Log<LevelPostGameHookService>(
                $"[OBS][PostGame] LevelPostGameHookCompleted levelRef='{contract.LevelRef.name}' result='{context.Result}' reason='{Normalize(context.Reason)}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
