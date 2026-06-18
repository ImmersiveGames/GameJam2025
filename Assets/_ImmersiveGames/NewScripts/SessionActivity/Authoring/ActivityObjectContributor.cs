using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
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
        [SerializeField] private ActivityResetBoundaryEligibility resetBoundaryEligibility = ActivityResetBoundaryEligibility.RuntimeAll;
        [SerializeField] private List<ActivityReleaseRequirementKind> supportedReleaseKinds = new();
        [SerializeField] private bool includeChildrenForEndpointDiscovery = true;
        [SerializeField] private string debugLabel;

        public string TargetId => targetId.TrimToEmpty();
        public string RoleId => roleId.TrimToEmpty();
        public ActivityObjectContributorKind ContributorKind => contributorKind;
        public ActivitySetupRequirementRequiredness DefaultRequiredness => defaultRequiredness;
        public ActivityResetBoundaryEligibility ResetBoundaryEligibility => resetBoundaryEligibility;
        public IReadOnlyList<ActivityReleaseRequirementKind> SupportedReleaseKinds => supportedReleaseKinds;
        public bool IncludeChildrenForEndpointDiscovery => includeChildrenForEndpointDiscovery;
        public string DebugLabel => debugLabel.TrimToEmpty();

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(TargetId) &&
            contributorKind != ActivityObjectContributorKind.Unknown &&
            defaultRequiredness != ActivitySetupRequirementRequiredness.Unknown &&
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

            ValidateReleaseKindsOrThrow(validationSource);
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
            targetId = targetId.TrimToEmpty();
            roleId = roleId.TrimToEmpty();
            debugLabel = debugLabel.TrimToEmpty();

            if (supportedReleaseKinds == null)
            {
                supportedReleaseKinds = new List<ActivityReleaseRequirementKind>();
            }
        }
    }
}
