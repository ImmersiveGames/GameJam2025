using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime
{
    public readonly struct LoadingProgressSnapshot
    {
        public LoadingProgressSnapshot(float normalizedProgress, string stepLabel, string reason = null)
        {
            NormalizedProgress = Mathf.Clamp01(normalizedProgress);
            Percentage = Mathf.Clamp(Mathf.RoundToInt(NormalizedProgress * 100f), 0, 100);
            StepLabel = string.IsNullOrWhiteSpace(stepLabel) ? "Loading..." : stepLabel.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public float NormalizedProgress { get; }
        public int Percentage { get; }
        public string StepLabel { get; }
        public string Reason { get; }

        public LoadingProgressSnapshot WithStepLabel(string stepLabel)
        {
            return new LoadingProgressSnapshot(NormalizedProgress, stepLabel, Reason);
        }

        public LoadingProgressSnapshot WithNormalizedProgress(float normalizedProgress)
        {
            return new LoadingProgressSnapshot(normalizedProgress, StepLabel, Reason);
        }
    }
}
