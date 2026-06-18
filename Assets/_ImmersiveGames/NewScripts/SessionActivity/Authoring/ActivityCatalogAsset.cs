using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [CreateAssetMenu(fileName = "ActivityCatalog", menuName = "ImmersiveGames/SessionActivity/Activity Catalog Asset")]
    public sealed class ActivityCatalogAsset : ScriptableObject
    {
        [SerializeField] private string catalogId;
        [SerializeField] private List<ActivityAsset> activities = new();
        [SerializeField] private ActivityCatalogAdvanceAtEndMode advanceAtEndMode = ActivityCatalogAdvanceAtEndMode.StopAtEnd;

        public string CatalogId => catalogId.TrimToEmpty();
        public IReadOnlyList<ActivityAsset> Activities => activities;
        public ActivityCatalogAdvanceAtEndMode AdvanceAtEndMode => advanceAtEndMode;

        public SessionActivityCatalog BuildRuntimeCatalog()
        {
            ValidateOrThrow();

            List<SessionActivityDefinition> definitions = new(activities.Count);
            string source = $"ActivityCatalogAsset:{CatalogId}";
            for (int index = 0; index < activities.Count; index++)
            {
                var current = activities[index];
                string nextActivityId = current.HasNextActivity ? current.NextActivityId : string.Empty;
                definitions.Add(new SessionActivityDefinition(
                    current.ActivityId,
                    current.DisplayName,
                    index + 1,
                    current.ActivityContentMode,
                    current.ActivityContentProfile,
                    current.ActivationWindowMode,
                    current.ActivationWindowAdditiveSceneKey,
                    current.DeactivationWindowMode,
                    current.DeactivationWindowAdditiveSceneKey,
                    current.NextActivityTransitionProfileSource,
                    current.NextActivityTransitionContinuePolicy,
                    current.NextActivityTransitionProfileOverride,
                    nextActivityId,
                    source));
            }

            return new SessionActivityCatalog(definitions, advanceAtEndMode);
        }

        public void ValidateOrThrow()
        {
            if (string.IsNullOrWhiteSpace(CatalogId))
            {
                throw new InvalidOperationException($"ActivityCatalogAsset '{name}' requires catalogId.");
            }

            if (activities == null || activities.Count == 0)
            {
                throw new InvalidOperationException($"ActivityCatalogAsset '{name}' requires at least one activity.");
            }

            HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < activities.Count; index++)
            {
                var activity = activities[index];
                if (activity == null)
                {
                    throw new InvalidOperationException($"ActivityCatalogAsset '{name}' has null activity at index {index}.");
                }

                activity.ValidateOrThrow();
                if (!ids.Add(activity.ActivityId))
                {
                    throw new InvalidOperationException($"ActivityCatalogAsset '{name}' has duplicate activityId '{activity.ActivityId}'.");
                }
            }

            for (int index = 0; index < activities.Count; index++)
            {
                var activity = activities[index];
                if (activity == null || !activity.HasNextActivity)
                {
                    continue;
                }

                var next = activity.NextActivity;
                if (next == null)
                {
                    throw new InvalidOperationException($"ActivityCatalogAsset '{name}' has activity '{activity.ActivityId}' with null nextActivity reference.");
                }

                if (!ids.Contains(next.ActivityId))
                {
                    throw new InvalidOperationException($"ActivityCatalogAsset '{name}' has activity '{activity.ActivityId}' referencing nextActivityId '{next.ActivityId}' outside this catalog.");
                }
            }
        }
    }
}
