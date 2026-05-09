using System;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [CreateAssetMenu(fileName = "Activity", menuName = "ImmersiveGames/SessionActivity/Activity Asset")]
    public sealed class ActivityAsset : ScriptableObject
    {
        [SerializeField] private string activityId;
        [SerializeField] private string displayName;
        [SerializeField] private bool hasActivation;
        [SerializeField] private bool hasGameplayContent;
        [SerializeField] private bool hasActivityResult;

        public string ActivityId => Normalize(activityId);
        public string DisplayName => Normalize(displayName);
        public bool HasActivation => hasActivation;
        public bool HasGameplayContent => hasGameplayContent;
        public bool HasActivityResult => hasActivityResult;

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
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
