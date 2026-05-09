using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [CreateAssetMenu(fileName = "ActivityCatalog", menuName = "ImmersiveGames/SessionActivity/Activity Catalog Asset")]
    public sealed class ActivityCatalogAsset : ScriptableObject
    {
        [SerializeField] private string catalogId;
        [SerializeField] private List<ActivityAsset> activities = new();

        public string CatalogId => Normalize(catalogId);
        public IReadOnlyList<ActivityAsset> Activities => activities;

        public SessionActivityCatalog BuildRuntimeCatalog()
        {
            ValidateOrThrow();

            List<SessionActivityDefinition> definitions = new(activities.Count);
            string source = $"ActivityCatalogAsset:{CatalogId}";
            for (int index = 0; index < activities.Count; index++)
            {
                ActivityAsset current = activities[index];
                string nextActivityId = current.NextActivity != null ? current.NextActivity.ActivityId : string.Empty;
                definitions.Add(new SessionActivityDefinition(
                    activityId: current.ActivityId,
                    displayName: current.DisplayName,
                    activityOrdinal: index + 1,
                    hasActivation: current.HasActivation,
                    hasGameplayContent: current.HasGameplayContent,
                    hasActivityResult: current.HasActivityResult,
                    nextActivityId: nextActivityId,
                    source: source));
            }

            return new SessionActivityCatalog(definitions);
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
            HashSet<ActivityAsset> set = new();

            for (int index = 0; index < activities.Count; index++)
            {
                ActivityAsset activity = activities[index];
                if (activity == null)
                {
                    throw new InvalidOperationException($"ActivityCatalogAsset '{name}' has null activity at index {index}.");
                }

                activity.ValidateOrThrow();
                if (!ids.Add(activity.ActivityId))
                {
                    throw new InvalidOperationException($"ActivityCatalogAsset '{name}' has duplicate activityId '{activity.ActivityId}'.");
                }

                set.Add(activity);
            }

            for (int index = 0; index < activities.Count; index++)
            {
                ActivityAsset activity = activities[index];
                ActivityAsset next = activity.NextActivity;
                if (next == null)
                {
                    continue;
                }

                if (!set.Contains(next))
                {
                    throw new InvalidOperationException($"ActivityCatalogAsset '{name}' has nextActivity outside catalog for '{activity.ActivityId}'.");
                }
            }

            ValidateNoCyclesOrThrow();
        }

        private void ValidateNoCyclesOrThrow()
        {
            Dictionary<ActivityAsset, int> colors = new();
            for (int index = 0; index < activities.Count; index++)
            {
                ActivityAsset activity = activities[index];
                if (!colors.ContainsKey(activity))
                {
                    colors.Add(activity, 0);
                }
            }

            for (int index = 0; index < activities.Count; index++)
            {
                ActivityAsset activity = activities[index];
                if (colors[activity] == 0)
                {
                    VisitOrThrow(activity, colors);
                }
            }
        }

        private static void VisitOrThrow(ActivityAsset activity, Dictionary<ActivityAsset, int> colors)
        {
            colors[activity] = 1;
            ActivityAsset next = activity.NextActivity;
            if (next != null)
            {
                int color = colors[next];
                if (color == 1)
                {
                    throw new InvalidOperationException($"Cycle detected in nextActivity chain at '{activity.ActivityId}'.");
                }

                if (color == 0)
                {
                    VisitOrThrow(next, colors);
                }
            }

            colors[activity] = 2;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
