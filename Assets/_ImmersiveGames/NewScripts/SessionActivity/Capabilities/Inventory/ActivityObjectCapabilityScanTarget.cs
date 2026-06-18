using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
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
            TargetObjectPath = targetObjectPath.TrimToEmpty();
            IncludeChildrenForEndpointDiscovery = includeChildrenForEndpointDiscovery;
        }

        public ActivityObjectContributionReport Contribution { get; }
        public GameObject TargetObject { get; }
        public string TargetObjectPath { get; }
        public bool IncludeChildrenForEndpointDiscovery { get; }
        public bool HasTargetObjectPath => !string.IsNullOrWhiteSpace(TargetObjectPath);
        public bool IsValid => Contribution.IsValid && TargetObject != null;
    }
}
