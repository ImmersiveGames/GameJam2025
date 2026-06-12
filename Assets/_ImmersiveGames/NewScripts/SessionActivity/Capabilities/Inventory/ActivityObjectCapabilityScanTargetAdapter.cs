using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityObjectCapabilityScanTargetAdaptationResult
    {
        public ActivityObjectCapabilityScanTargetAdaptationResult(
            ActivityObjectContributorDiscoveryResult discoveryResult,
            IReadOnlyList<ActivityObjectCapabilityScanTarget> targets,
            IReadOnlyList<ActivityObjectContributionReport> unresolvedReports,
            string source,
            string reason)
        {
            DiscoveryResult = discoveryResult;
            Targets = targets ?? Array.Empty<ActivityObjectCapabilityScanTarget>();
            UnresolvedReports = unresolvedReports ?? Array.Empty<ActivityObjectContributionReport>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActivityObjectContributorDiscoveryResult DiscoveryResult { get; }
        public IReadOnlyList<ActivityObjectCapabilityScanTarget> Targets { get; }
        public IReadOnlyList<ActivityObjectContributionReport> UnresolvedReports { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => DiscoveryResult.IsValid && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class ActivityObjectCapabilityScanTargetAdapter
    {
        public ActivityObjectCapabilityScanTargetAdaptationResult Adapt(
            ActivityObjectContributorDiscoveryResult discoveryResult,
            string source,
            string reason)
        {
            if (!discoveryResult.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectCapabilityScanTargetAdapter requires a valid discovery result.");
            }

            List<ActivityObjectCapabilityScanTarget> targets = new(discoveryResult.Reports.Count);
            List<ActivityObjectContributionReport> unresolved = new();

            for (int index = 0; index < discoveryResult.Reports.Count; index++)
            {
                var report = discoveryResult.Reports[index];
                if (!report.IsValid)
                {
                    unresolved.Add(report);
                    continue;
                }

                if (!TryResolveContributorObject(report, out var contributor, out var targetObject))
                {
                    unresolved.Add(report);
                    continue;
                }

                targets.Add(new ActivityObjectCapabilityScanTarget(
                    report,
                    targetObject,
                    ActivityCapabilityTransformPathUtility.BuildTransformPath(targetObject.transform),
                    contributor.IncludeChildrenForEndpointDiscovery));
            }

            targets.Sort(CompareTargets);
            unresolved.Sort(CompareReports);

            return new ActivityObjectCapabilityScanTargetAdaptationResult(discoveryResult, targets, unresolved, source, reason);
        }

        private static bool TryResolveContributorObject(
            ActivityObjectContributionReport report,
            out ActivityObjectContributor resolvedContributor,
            out GameObject resolvedObject)
        {
            resolvedContributor = null;
            resolvedObject = null;

            var scene = SceneManager.GetSceneByName(report.SceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                ActivityObjectContributor[] contributors = roots[rootIndex].GetComponentsInChildren<ActivityObjectContributor>(true);
                for (int contributorIndex = 0; contributorIndex < contributors.Length; contributorIndex++)
                {
                    var contributor = contributors[contributorIndex];
                    if (contributor == null)
                    {
                        continue;
                    }

                    if (!string.Equals(contributor.TargetId, report.TargetId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    resolvedContributor = contributor;
                    resolvedObject = contributor.gameObject;
                    return true;
                }
            }

            return false;
        }

        private static int CompareTargets(ActivityObjectCapabilityScanTarget left, ActivityObjectCapabilityScanTarget right)
        {
            int sceneCompare = string.Compare(left.Contribution.SceneName, right.Contribution.SceneName, StringComparison.Ordinal);
            if (sceneCompare != 0)
            {
                return sceneCompare;
            }

            int targetCompare = string.Compare(left.Contribution.TargetId, right.Contribution.TargetId, StringComparison.Ordinal);
            if (targetCompare != 0)
            {
                return targetCompare;
            }

            int roleCompare = string.Compare(left.Contribution.RoleId, right.Contribution.RoleId, StringComparison.Ordinal);
            if (roleCompare != 0)
            {
                return roleCompare;
            }

            return string.Compare(left.TargetObjectPath, right.TargetObjectPath, StringComparison.Ordinal);
        }

        private static int CompareReports(ActivityObjectContributionReport left, ActivityObjectContributionReport right)
        {
            int sceneCompare = string.Compare(left.SceneName, right.SceneName, StringComparison.Ordinal);
            if (sceneCompare != 0)
            {
                return sceneCompare;
            }

            int targetCompare = string.Compare(left.TargetId, right.TargetId, StringComparison.Ordinal);
            if (targetCompare != 0)
            {
                return targetCompare;
            }

            return string.Compare(left.RoleId, right.RoleId, StringComparison.Ordinal);
        }
    }
}
