using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneComposition
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelSceneCompositionExecutor : ISceneCompositionExecutor
    {
        public async Task<SceneCompositionResult> ApplyAsync(SceneCompositionRequest request, CancellationToken ct = default)
        {
            if (request.Scope != SceneCompositionScope.Local)
            {
                HardFailFastH1.Trigger(typeof(LevelSceneCompositionExecutor),
                    $"[FATAL][H1][SceneComposition] Unsupported scope='{request.Scope}' for LevelSceneCompositionExecutor. correlationId='{request.CorrelationId}' reason='{request.Reason}'.");
            }

            if (request.IsClearRequest)
            {
                int clearedScenesRemoved = await LevelAdditiveSceneRuntimeApplier.ClearAsync(request.PreviousLevelRef, ct);

                DebugUtility.Log<LevelSceneCompositionExecutor>(
                    $"[OBS][SceneComposition] LocalCompositionCleared correlationId='{request.CorrelationId}' previousLevelRef='{(request.PreviousLevelRef != null ? request.PreviousLevelRef.name : "<none>")}' scenesRemoved={clearedScenesRemoved} reason='{request.Reason}'.",
                    DebugUtility.Colors.Info);

                return new SceneCompositionResult(
                    success: true,
                    scope: request.Scope,
                    reason: request.Reason,
                    correlationId: request.CorrelationId,
                    scenesAdded: 0,
                    scenesRemoved: clearedScenesRemoved);
            }

            if (request.TargetLevelRef == null)
            {
                HardFailFastH1.Trigger(typeof(LevelSceneCompositionExecutor),
                    $"[FATAL][H1][SceneComposition] TargetLevelRef is null for non-clear local composition. correlationId='{request.CorrelationId}' reason='{request.Reason}'.");
            }

            (int scenesAdded, int scenesRemoved) = await LevelAdditiveSceneRuntimeApplier.ApplyAsync(
                request.PreviousLevelRef,
                request.TargetLevelRef,
                ct);

            DebugUtility.Log<LevelSceneCompositionExecutor>(
                $"[OBS][SceneComposition] LocalCompositionApplied correlationId='{request.CorrelationId}' previousLevelRef='{(request.PreviousLevelRef != null ? request.PreviousLevelRef.name : "<none>")}' targetLevelRef='{request.TargetLevelRef.name}' scenesAdded={scenesAdded} scenesRemoved={scenesRemoved} reason='{request.Reason}'.",
                DebugUtility.Colors.Info);

            return new SceneCompositionResult(
                success: true,
                scope: request.Scope,
                reason: request.Reason,
                correlationId: request.CorrelationId,
                scenesAdded: scenesAdded,
                scenesRemoved: scenesRemoved);
        }
    }
}
