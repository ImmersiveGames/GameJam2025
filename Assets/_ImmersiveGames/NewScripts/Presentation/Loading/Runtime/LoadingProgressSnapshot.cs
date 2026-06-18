using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Presentation.Loading.Runtime
{
    public readonly struct LoadingProgressSnapshot
    {
        public LoadingProgressSnapshot(float normalizedProgress, string stepLabel, string reason = null)
        {
            NormalizedProgress = Mathf.Clamp01(normalizedProgress);
            Percentage = Mathf.Clamp(Mathf.RoundToInt(NormalizedProgress * 100f), 0, 100);
            StepLabel = stepLabel.TrimToOrDefault("Loading...");
            Reason = reason.TrimToEmpty();
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
