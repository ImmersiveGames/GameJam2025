using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/SessionActivity/Activity Scene Contract Authoring")]
    public sealed class ActivitySceneContractAuthoring : MonoBehaviour
    {
        [SerializeField] private string activitySceneId;
        [SerializeField] private ActivitySceneDiscoveryMode discoveryMode = ActivitySceneDiscoveryMode.None;
        [SerializeField] private ActivitySceneRevealSafety revealSafety = ActivitySceneRevealSafety.Unknown;
        [SerializeField] private bool allowUndeclaredContributors;
        [SerializeField] private List<string> declaredContributorIds = new();

        public string ActivitySceneId => activitySceneId.TrimToEmpty();
        public ActivitySceneDiscoveryMode DiscoveryMode => discoveryMode;
        public ActivitySceneRevealSafety RevealSafety => revealSafety;
        public bool AllowUndeclaredContributors => allowUndeclaredContributors;
        public IReadOnlyList<string> DeclaredContributorIds => declaredContributorIds;

        public ActivitySceneContractSnapshot BuildSnapshotOrThrow()
        {
            ValidateOrThrow();

            List<ActivitySceneContractContributorEntry> contributors = new();
            for (int index = 0; index < declaredContributorIds.Count; index++)
            {
                string contributorId = declaredContributorIds[index].TrimToEmpty();
                if (string.IsNullOrWhiteSpace(contributorId))
                {
                    continue;
                }

                contributors.Add(new ActivitySceneContractContributorEntry(contributorId));
            }

            return new ActivitySceneContractSnapshot(
                ActivitySceneId,
                discoveryMode,
                revealSafety,
                allowUndeclaredContributors,
                contributors);
        }

        public void ValidateOrThrow()
        {
            if (string.IsNullOrWhiteSpace(ActivitySceneId))
            {
                throw new InvalidOperationException($"ActivitySceneContractAuthoring '{name}' requires activitySceneId.");
            }

            if (discoveryMode == ActivitySceneDiscoveryMode.None)
            {
                throw new InvalidOperationException($"ActivitySceneContractAuthoring '{name}' requires discoveryMode != None.");
            }

            if (revealSafety == ActivitySceneRevealSafety.Unknown)
            {
                throw new InvalidOperationException($"ActivitySceneContractAuthoring '{name}' requires revealSafety != Unknown.");
            }

            HashSet<string> dedupe = new(StringComparer.Ordinal);
            for (int index = 0; index < declaredContributorIds.Count; index++)
            {
                string contributorId = declaredContributorIds[index].TrimToEmpty();
                if (string.IsNullOrWhiteSpace(contributorId))
                {
                    throw new InvalidOperationException($"ActivitySceneContractAuthoring '{name}' has empty declaredContributorIds[{index}].");
                }

                if (!dedupe.Add(contributorId))
                {
                    throw new InvalidOperationException($"ActivitySceneContractAuthoring '{name}' has duplicate declaredContributorId='{contributorId}'.");
                }
            }
        }
    }
}
