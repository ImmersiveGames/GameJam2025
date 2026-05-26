using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityObjectCapabilityScanTarget
    {
        public ActivityObjectCapabilityScanTarget(
            ActivityObjectContributionReport contribution,
            GameObject targetObject,
            string targetObjectPath,
            bool includeChildrenForEndpointDiscovery)
        {
            Contribution = contribution;
            TargetObject = targetObject;
            TargetObjectPath = Normalize(targetObjectPath);
            IncludeChildrenForEndpointDiscovery = includeChildrenForEndpointDiscovery;
        }

        public ActivityObjectContributionReport Contribution { get; }
        public GameObject TargetObject { get; }
        public string TargetObjectPath { get; }
        public bool IncludeChildrenForEndpointDiscovery { get; }
        public bool HasTargetObjectPath => !string.IsNullOrWhiteSpace(TargetObjectPath);
        public bool IsValid => Contribution.IsValid && TargetObject != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
