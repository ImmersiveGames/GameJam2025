using System;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [CreateAssetMenu(fileName = "Activity", menuName = "ImmersiveGames/SessionActivity/Activity Asset")]
    public sealed class ActivityAsset : ScriptableObject
    {
        [SerializeField] private string activityId;
        [SerializeField] private string displayName;
        [SerializeField] private bool hasGameplayContent;
        [SerializeField] private ActivityWindowMode activationWindowMode = ActivityWindowMode.None;
        [SerializeField] private SceneKeyAsset activationWindowAdditiveSceneKey;
        [SerializeField] private ActivityWindowMode deactivationWindowMode = ActivityWindowMode.None;
        [SerializeField] private SceneKeyAsset deactivationWindowAdditiveSceneKey;
        [SerializeField] private ActivityTransitionProfileSource nextActivityTransitionProfileSource = ActivityTransitionProfileSource.None;
        [SerializeField] private ActivityTransitionContinuePolicy nextActivityTransitionContinuePolicy = ActivityTransitionContinuePolicy.Unknown;
        [SerializeField] private ActivityTransitionProfileAsset nextActivityTransitionProfileOverride;
        [SerializeField] private PlayerSetDefinitionAsset playerSetDefinition;

        public string ActivityId => Normalize(activityId);
        public string DisplayName => Normalize(displayName);
        public bool HasGameplayContent => hasGameplayContent;
        public ActivityWindowMode ActivationWindowMode => activationWindowMode;
        public SceneKeyAsset ActivationWindowAdditiveSceneKey => activationWindowAdditiveSceneKey;
        public ActivityWindowMode DeactivationWindowMode => deactivationWindowMode;
        public SceneKeyAsset DeactivationWindowAdditiveSceneKey => deactivationWindowAdditiveSceneKey;
        public ActivityTransitionProfileSource NextActivityTransitionProfileSource => nextActivityTransitionProfileSource;
        public ActivityTransitionContinuePolicy NextActivityTransitionContinuePolicy => nextActivityTransitionContinuePolicy;
        public ActivityTransitionProfileAsset NextActivityTransitionProfileOverride => nextActivityTransitionProfileOverride;
        public PlayerSetDefinitionAsset PlayerSetDefinition => playerSetDefinition;
        public bool RequiresPlayerActor => playerSetDefinition != null;

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

            if (nextActivityTransitionProfileOverride != null)
            {
                nextActivityTransitionProfileOverride.ValidateOrThrow($"ActivityAsset:{name}");
            }

            if (playerSetDefinition != null &&
                !playerSetDefinition.TryValidate(out string playerSetValidationError))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' invalid playerSetDefinition. detail='{playerSetValidationError}'.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
