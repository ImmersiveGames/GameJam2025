using ImmersiveGames.GameJam2025.Core.Events;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Loading.Runtime
{
    public readonly struct SceneFlowRouteLoadingProgressEvent : IEvent
    {
        public SceneFlowRouteLoadingProgressEvent(
            string contextSignature,
            float normalizedProgress,
            string stepLabel,
            string reason = null)
        {
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
            NormalizedProgress = normalizedProgress;
            StepLabel = string.IsNullOrWhiteSpace(stepLabel) ? string.Empty : stepLabel.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public string ContextSignature { get; }
        public float NormalizedProgress { get; }
        public string StepLabel { get; }
        public string Reason { get; }
    }
}

