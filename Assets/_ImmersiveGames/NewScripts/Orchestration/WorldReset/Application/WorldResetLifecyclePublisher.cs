using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Application
{
    /// <summary>
    /// Publicador canônico do lifecycle de WorldReset.
    /// Centraliza observabilidade e emissão dos contratos Started/Completed.
    /// </summary>
    public sealed class WorldResetLifecyclePublisher
    {
        public void PublishStarted(WorldResetRequest request)
        {
            DebugUtility.LogVerbose<WorldResetLifecyclePublisher>(
                $"[{ResetLogTags.Start}][OBS][WorldReset] ResetStarted kind='{request.Kind}' routeId='{request.MacroRouteId}' signature='{request.ContextSignature}' targetScene='{request.TargetScene}' levelSignature='{request.LevelSignature}' reason='{request.Reason}' origin='{request.Origin}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldResetStartedEvent>.Raise(new WorldResetStartedEvent(
                request.Kind,
                request.MacroRouteId,
                request.Reason,
                request.ContextSignature,
                request.LevelSignature,
                request.Origin,
                request.TargetScene,
                request.SourceSignature));
        }

        public void PublishCompleted(WorldResetRequest request, WorldResetOutcome outcome, string detail)
        {
            string completionDetail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();

            DebugUtility.LogVerbose<WorldResetLifecyclePublisher>(
                $"[{ResetLogTags.Completed}][OBS][WorldReset] ResetCompleted kind='{request.Kind}' routeId='{request.MacroRouteId}' signature='{request.ContextSignature}' targetScene='{request.TargetScene}' levelSignature='{request.LevelSignature}' reason='{request.Reason}' outcome='{outcome}' detail='{completionDetail}' origin='{request.Origin}'.",
                outcome == WorldResetOutcome.Completed || outcome == WorldResetOutcome.SkippedByPolicy || outcome == WorldResetOutcome.SkippedValidation
                    ? DebugUtility.Colors.Success
                    : DebugUtility.Colors.Warning);

            EventBus<WorldResetCompletedEvent>.Raise(new WorldResetCompletedEvent(
                request.Kind,
                request.MacroRouteId,
                request.Reason,
                request.ContextSignature,
                request.LevelSignature,
                outcome,
                completionDetail,
                request.Origin,
                request.TargetScene,
                request.SourceSignature));
        }
    }
}
