using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    [CreateAssetMenu(
        fileName = "RuntimeLoadingProfileAsset",
        menuName = "ImmersiveGames/Infrastructure/RuntimeMode/RuntimeLoadingProfileAsset",
        order = 22)]
    public sealed class RuntimeLoadingProfileAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string profileId;

        [Header("Behavior")]
        [SerializeField] private bool showImmediately = true;
        [SerializeField] private bool hideAfterCompletion = true;
        [SerializeField] [Min(0f)] private float minimumVisibleSeconds;
        [SerializeField] [Min(0f)] private float finalProgressHoldSeconds;

        public string ProfileId => profileId.TrimToEmpty();
        public bool ShowImmediately => showImmediately;
        public bool HideAfterCompletion => hideAfterCompletion;
        public float MinimumVisibleSeconds => minimumVisibleSeconds < 0f ? 0f : minimumVisibleSeconds;
        public float FinalProgressHoldSeconds => finalProgressHoldSeconds;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnValidate()
        {
            profileId = profileId.TrimToEmpty();
            minimumVisibleSeconds = Mathf.Max(0f, minimumVisibleSeconds);

            if (TryValidate(out string errorMessage) || string.IsNullOrWhiteSpace(errorMessage))
            {
                return;
            }

            DebugUtility.LogWarning(
                typeof(RuntimeLoadingProfileAsset),
                $"[Config][Editor] profileId='{ProfileId}' invalido. detail='{errorMessage}'");
        }
#endif

        public bool TryValidate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(ProfileId))
            {
                errorMessage = "profileId is required.";
                return false;
            }

            if (minimumVisibleSeconds < 0f)
            {
                errorMessage = "minimumVisibleSeconds cannot be negative.";
                return false;
            }

            if (finalProgressHoldSeconds < 0f)
            {
                errorMessage = "finalProgressHoldSeconds cannot be negative.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
