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
        [SerializeField] private ActivityAsset nextActivity;

        public string ActivityId => Normalize(activityId);
        public string DisplayName => Normalize(displayName);
        public bool HasActivation => hasActivation;
        public bool HasGameplayContent => hasGameplayContent;
        public bool HasActivityResult => hasActivityResult;
        public ActivityAsset NextActivity => nextActivity;

        public void ValidateOrThrow()
        {
            if (string.IsNullOrWhiteSpace(ActivityId))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires activityId.");
            }

            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' requires displayName.");
            }

            if (ReferenceEquals(nextActivity, this))
            {
                throw new InvalidOperationException($"ActivityAsset '{name}' cannot point nextActivity to itself.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
