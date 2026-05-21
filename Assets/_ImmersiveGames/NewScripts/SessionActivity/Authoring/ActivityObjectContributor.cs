using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ActivityObjectContributor : MonoBehaviour
    {
        [SerializeField] private string targetId;
        [SerializeField] private string roleId;
        [SerializeField] private ActivityObjectContributorKind contributorKind = ActivityObjectContributorKind.SceneObject;
        [SerializeField] private ActivitySetupRequirementRequiredness defaultRequiredness = ActivitySetupRequirementRequiredness.Required;
        [SerializeField] private List<ActivityStateResetGroup> supportedResetGroups = new();
        [SerializeField] private List<ActivityReleaseRequirementKind> supportedReleaseKinds = new();
        [SerializeField] private bool includeChildrenForEndpointDiscovery = true;
        [SerializeField] private string debugLabel;

        public string TargetId => Normalize(targetId);
        public string RoleId => Normalize(roleId);
        public ActivityObjectContributorKind ContributorKind => contributorKind;
        public ActivitySetupRequirementRequiredness DefaultRequiredness => defaultRequiredness;
        public IReadOnlyList<ActivityStateResetGroup> SupportedResetGroups => supportedResetGroups;
        public IReadOnlyList<ActivityReleaseRequirementKind> SupportedReleaseKinds => supportedReleaseKinds;
        public bool IncludeChildrenForEndpointDiscovery => includeChildrenForEndpointDiscovery;
        public string DebugLabel => Normalize(debugLabel);

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(TargetId) &&
            contributorKind != ActivityObjectContributorKind.Unknown &&
            defaultRequiredness != ActivitySetupRequirementRequiredness.Unknown &&
            supportedResetGroups != null &&
            supportedReleaseKinds != null;

        public void ValidateOrThrow(string source)
        {
            string validationSource = string.IsNullOrWhiteSpace(source)
                ? $"ActivityObjectContributor:{name}"
                : source.Trim();

            if (string.IsNullOrWhiteSpace(TargetId))
            {
                throw new InvalidOperationException($"{validationSource} requires targetId.");
            }

            if (contributorKind == ActivityObjectContributorKind.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit contributorKind.");
            }

            if (defaultRequiredness == ActivitySetupRequirementRequiredness.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit defaultRequiredness.");
            }

            ValidateResetGroupsOrThrow(validationSource);
            ValidateReleaseKindsOrThrow(validationSource);
        }

        private void ValidateResetGroupsOrThrow(string source)
        {
            if (supportedResetGroups == null)
            {
                throw new InvalidOperationException($"{source} supportedResetGroups cannot be null.");
            }

            for (int index = 0; index < supportedResetGroups.Count; index++)
            {
                if (supportedResetGroups[index] == ActivityStateResetGroup.Unknown)
                {
                    throw new InvalidOperationException($"{source}.supportedResetGroups[{index}] cannot be Unknown.");
                }
            }
        }

        private void ValidateReleaseKindsOrThrow(string source)
        {
            if (supportedReleaseKinds == null)
            {
                throw new InvalidOperationException($"{source} supportedReleaseKinds cannot be null.");
            }

            for (int index = 0; index < supportedReleaseKinds.Count; index++)
            {
                if (supportedReleaseKinds[index] == ActivityReleaseRequirementKind.Unknown)
                {
                    throw new InvalidOperationException($"{source}.supportedReleaseKinds[{index}] cannot be Unknown.");
                }
            }
        }

        private void OnValidate()
        {
            targetId = Normalize(targetId);
            roleId = Normalize(roleId);
            debugLabel = Normalize(debugLabel);

            if (supportedResetGroups == null)
            {
                supportedResetGroups = new List<ActivityStateResetGroup>();
            }

            if (supportedReleaseKinds == null)
            {
                supportedReleaseKinds = new List<ActivityReleaseRequirementKind>();
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
