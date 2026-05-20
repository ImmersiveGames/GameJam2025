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

        public string ContentProfileId => Normalize(contentProfileId);
        public ActivitySceneDiscoveryMode DiscoveryMode => discoveryMode;
        public ActivityContentPreparationPolicy PreparationPolicy => preparationPolicy;
        public IReadOnlyList<ActivityContentSceneEntry> ContentScenes => contentScenes;
        public bool HasContentScenes => contentScenes != null && contentScenes.Count > 0;

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
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
