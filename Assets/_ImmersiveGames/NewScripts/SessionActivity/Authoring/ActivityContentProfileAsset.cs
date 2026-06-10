using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [CreateAssetMenu(fileName = "ActivityContentProfile", menuName = "ImmersiveGames/SessionActivity/Activity Content Profile")]
    public sealed class ActivityContentProfileAsset : ScriptableObject
    {
        [SerializeField] private string contentProfileId;
        [SerializeField] private ActivitySceneDiscoveryMode discoveryMode = ActivitySceneDiscoveryMode.StrictDeclaredOnly;
        [SerializeField] private ActivityContentPreparationPolicy preparationPolicy = ActivityContentPreparationPolicy.LoadBeforeSetup;
        [SerializeField] private List<ActivityContentSceneEntry> contentScenes = new();
        [SerializeField] private ActivitySetupRequirementsAuthoring setupRequirements = new();

        public string ContentProfileId => Normalize(contentProfileId);
        public ActivitySceneDiscoveryMode DiscoveryMode => discoveryMode;
        public ActivityContentPreparationPolicy PreparationPolicy => preparationPolicy;
        public IReadOnlyList<ActivityContentSceneEntry> ContentScenes => contentScenes;
        public ActivitySetupRequirementsAuthoring SetupRequirements => setupRequirements;
        public bool HasContentScenes => contentScenes is { Count: > 0 };
        public bool HasSetupRequirements => setupRequirements is { HasRequirements: true };

        public void ValidateOrThrow(string source = null)
        {
            string validationSource = string.IsNullOrWhiteSpace(source)
                ? $"ActivityContentProfileAsset:{name}"
                : source.Trim();

            if (string.IsNullOrWhiteSpace(ContentProfileId))
            {
                throw new InvalidOperationException($"{validationSource} requires contentProfileId.");
            }

            if (!string.Equals(contentProfileId, contentProfileId.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{validationSource} contentProfileId cannot have leading or trailing spaces.");
            }

            if (ContentProfileId.Contains(" ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{validationSource} contentProfileId cannot contain spaces.");
            }

            if (discoveryMode == ActivitySceneDiscoveryMode.None)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit discoveryMode different from None.");
            }

            if (preparationPolicy == ActivityContentPreparationPolicy.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit preparationPolicy.");
            }

            if (preparationPolicy == ActivityContentPreparationPolicy.LoadBeforeSetup &&
                (contentScenes == null || contentScenes.Count == 0))
            {
                throw new InvalidOperationException($"{validationSource} requires at least one content scene when preparationPolicy=LoadBeforeSetup.");
            }

            if (setupRequirements == null)
            {
                throw new InvalidOperationException($"{validationSource} requires setupRequirements container. Use an empty container when there are no requirements.");
            }

            setupRequirements.ValidateOrThrow(validationSource);

            if (contentScenes == null)
            {
                return;
            }

            for (int index = 0; index < contentScenes.Count; index++)
            {
                ActivityContentSceneEntry entry = contentScenes[index];
                if (entry == null)
                {
                    throw new InvalidOperationException($"{validationSource} has null content scene entry at index {index}.");
                }

                entry.ValidateOrThrow(validationSource, index);
            }
        }

        private void OnValidate()
        {
            contentProfileId = Normalize(contentProfileId);

            if (setupRequirements == null)
            {
                setupRequirements = new ActivitySetupRequirementsAuthoring();
                return;
            }

            setupRequirements.PruneLegacyEmptyObjectEntryRequirements();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
