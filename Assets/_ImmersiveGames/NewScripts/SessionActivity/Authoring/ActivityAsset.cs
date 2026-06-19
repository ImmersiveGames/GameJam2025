using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [CreateAssetMenu(fileName = "Activity", menuName = "ImmersiveGames/SessionActivity/Activity Asset")]
    public sealed class ActivityAsset : ScriptableObject
    {
        [SerializeField] private string activityId;
        [SerializeField] private string displayName;
        [SerializeField] private ActivityContentMode activityContentMode = ActivityContentMode.None;
        [SerializeField] private ActivityContentProfileAsset activityContentProfile;
        [SerializeField] private ActivityPauseContentProfileAsset activityPauseContentProfile;
        [SerializeField] private ActivityWindowMode activationWindowMode = ActivityWindowMode.None;
        [SerializeField] private SceneKeyAsset activationWindowAdditiveSceneKey;
        [SerializeField] private ActivityWindowMode deactivationWindowMode = ActivityWindowMode.None;
        [SerializeField] private SceneKeyAsset deactivationWindowAdditiveSceneKey;
        [SerializeField] private ActivityAsset nextActivity;
        [SerializeField] private ActivityTransitionProfileSource nextActivityTransitionProfileSource = ActivityTransitionProfileSource.None;
        [SerializeField] private ActivityTransitionContinuePolicy nextActivityTransitionContinuePolicy = ActivityTransitionContinuePolicy.Unknown;
        [SerializeField] private ActivityTransitionProfileAsset nextActivityTransitionProfileOverride;

        public string ActivityId => activityId.TrimToEmpty();
        public string DisplayName => displayName.TrimToEmpty();
        public ActivityContentMode ActivityContentMode => activityContentMode;
        public ActivityContentProfileAsset ActivityContentProfile => activityContentProfile;
        public ActivityPauseContentProfileAsset ActivityPauseContentProfile => activityPauseContentProfile;
        public bool HasActivityContentProfile => activityContentProfile != null;
        public bool HasActivityPauseContentProfile => activityPauseContentProfile != null;
        public bool HasGameplayContent => activityContentMode == ActivityContentMode.Profile;
        public ActivityWindowMode ActivationWindowMode => activationWindowMode;
        public SceneKeyAsset ActivationWindowAdditiveSceneKey => activationWindowAdditiveSceneKey;
        public ActivityWindowMode DeactivationWindowMode => deactivationWindowMode;
        public SceneKeyAsset DeactivationWindowAdditiveSceneKey => deactivationWindowAdditiveSceneKey;
        public ActivityAsset NextActivity => nextActivity;
        public string NextActivityId => nextActivity != null ? nextActivity.ActivityId.TrimToEmpty() : string.Empty;
        public bool HasNextActivity => nextActivity != null;
        public ActivityTransitionProfileSource NextActivityTransitionProfileSource => nextActivityTransitionProfileSource;
        public ActivityTransitionContinuePolicy NextActivityTransitionContinuePolicy => nextActivityTransitionContinuePolicy;
        public ActivityTransitionProfileAsset NextActivityTransitionProfileOverride => nextActivityTransitionProfileOverride;

        public void ValidateOrThrow()
        {
            if (string.IsNullOrWhiteSpace(activityId))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires activityId.");
            }

            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires displayName.");
            }

            if (!string.Equals(activityId, activityId.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' activityId cannot have leading or trailing spaces.");
            }

            if (ActivityId.Contains(" ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' activityId cannot contain spaces.");
            }

            ValidateActivityContentOrThrow();
            ValidateActivityPauseContentOrThrow();

            if (activationWindowMode == ActivityWindowMode.AdditiveScene &&
                activationWindowAdditiveSceneKey == null)
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires activationWindowAdditiveSceneKey when activationWindowMode=AdditiveScene.");
            }

            if (activationWindowMode == ActivityWindowMode.AdditiveScene &&
                string.IsNullOrWhiteSpace(activationWindowAdditiveSceneKey.SceneName))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires activationWindowAdditiveSceneKey.SceneName when activationWindowMode=AdditiveScene.");
            }

            if (deactivationWindowMode == ActivityWindowMode.AdditiveScene &&
                deactivationWindowAdditiveSceneKey == null)
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires deactivationWindowAdditiveSceneKey when deactivationWindowMode=AdditiveScene.");
            }

            if (deactivationWindowMode == ActivityWindowMode.AdditiveScene &&
                string.IsNullOrWhiteSpace(deactivationWindowAdditiveSceneKey.SceneName))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires deactivationWindowAdditiveSceneKey.SceneName when deactivationWindowMode=AdditiveScene.");
            }

            if (nextActivityTransitionProfileSource == ActivityTransitionProfileSource.OverrideProfile &&
                nextActivityTransitionProfileOverride == null)
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires nextActivityTransitionProfileOverride when source=OverrideProfile.");
            }

            if (nextActivityTransitionContinuePolicy == ActivityTransitionContinuePolicy.Unknown)
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires explicit nextActivityTransitionContinuePolicy.");
            }

            if (HasNextActivity && string.IsNullOrWhiteSpace(NextActivityId))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' references nextActivity '{nextActivity.name}' with empty activityId.");
            }

            if (HasNextActivity && string.Equals(ActivityId, NextActivityId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' cannot reference itself as nextActivity.");
            }

            if (!HasNextActivity)
            {
                if (nextActivityTransitionProfileSource != ActivityTransitionProfileSource.None)
                {
                    throw new InvalidOperationException($"ActivityAsset '{name}' without nextActivity requires nextActivityTransitionProfileSource=None.");
                }

                if (nextActivityTransitionProfileOverride != null)
                {
                    throw new InvalidOperationException($"ActivityAsset '{name}' without nextActivity cannot define nextActivityTransitionProfileOverride.");
                }
            }

            if (nextActivityTransitionProfileOverride != null)
            {
                nextActivityTransitionProfileOverride.ValidateOrThrow($"ActivityAsset:{name}");
            }

        }


        private void ValidateActivityPauseContentOrThrow()
        {
            if (activityPauseContentProfile == null)
            {
                return;
            }

            activityPauseContentProfile.ValidateOrThrow($"ActivityAsset:{name}:ActivityPauseContentProfile");
        }

        private void ValidateActivityContentOrThrow()
        {
            if (activityContentMode != ActivityContentMode.None &&
                activityContentMode != ActivityContentMode.Profile)
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' has unsupported activityContentMode '{activityContentMode}'.");
            }

            if (activityContentMode == ActivityContentMode.None && activityContentProfile != null)
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' cannot reference activityContentProfile when activityContentMode=None.");
            }

            if (activityContentMode == ActivityContentMode.Profile && activityContentProfile == null)
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires activityContentProfile when activityContentMode=Profile.");
            }

            if (activityContentProfile != null)
            {
                activityContentProfile.ValidateOrThrow($"ActivityAsset:{name}:ActivityContentProfile");
            }
        }
    }
}
