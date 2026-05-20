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
        [SerializeField] private ActivityCatalogAdvanceAtEndMode advanceAtEndMode = ActivityCatalogAdvanceAtEndMode.StopAtEnd;

        public string CatalogId => Normalize(catalogId);
        public IReadOnlyList<ActivityAsset> Activities => activities;
        public ActivityCatalogAdvanceAtEndMode AdvanceAtEndMode => advanceAtEndMode;

        public SessionActivityCatalog BuildRuntimeCatalog()
        {
            ValidateOrThrow();

            List<SessionActivityDefinition> definitions = new(activities.Count);
            string source = $"ActivityCatalogAsset:{CatalogId}";
            for (int index = 0; index < activities.Count; index++)
            {
                ActivityAsset current = activities[index];
                string nextActivityId = index + 1 < activities.Count
                    ? activities[index + 1].ActivityId
                    : string.Empty;
                definitions.Add(new SessionActivityDefinition(
                    activityId: current.ActivityId,
                    displayName: current.DisplayName,
                    activityOrdinal: index + 1,
                    activityContentMode: current.ActivityContentMode,
                    activityContentProfile: current.ActivityContentProfile,
                    activationWindowMode: current.ActivationWindowMode,
                    activationWindowAdditiveSceneKey: current.ActivationWindowAdditiveSceneKey,
                    deactivationWindowMode: current.DeactivationWindowMode,
                    deactivationWindowAdditiveSceneKey: current.DeactivationWindowAdditiveSceneKey,
                    nextActivityTransitionProfileSource: current.NextActivityTransitionProfileSource,
                    nextActivityTransitionContinuePolicy: current.NextActivityTransitionContinuePolicy,
                    nextActivityTransitionProfileOverride: current.NextActivityTransitionProfileOverride,
                    nextActivityId: nextActivityId,
                    source: source));
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
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
