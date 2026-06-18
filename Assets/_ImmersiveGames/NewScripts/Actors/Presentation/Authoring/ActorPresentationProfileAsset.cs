using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Authoring
{
    [CreateAssetMenu(
        fileName = "ActorPresentationProfile",
        menuName = "ImmersiveGames/Actors/Presentation/Actor Presentation Profile")]
    public sealed class ActorPresentationProfileAsset : ScriptableObject
    {
        [SerializeField] private string profileId = "actor.presentation.default";
        [SerializeField] private ActorPresentationRequiredness requiredness = ActorPresentationRequiredness.Optional;
        [SerializeField] private ActorPresentationReleasePolicy releasePolicy = ActorPresentationReleasePolicy.ReleaseOnActivityExit;
        [SerializeField] private ActorPresentationResetPolicy resetPolicy = ActorPresentationResetPolicy.ReapplyResolvedPlan;
        [SerializeField] private ActorPresentationVariationPolicy variationPolicy = ActorPresentationVariationPolicy.None;
        [SerializeField] private int variationSeed = 0;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private ActorPresentationSlotKind primarySlotKind = ActorPresentationSlotKind.VisualRoot;
        [SerializeField] private string primarySlotId = "visual.root";
        [SerializeField] private List<ActorPresentationSlotRequirement> slotRequirements = new();

        public string ProfileId => profileId.TrimToEmpty();
        public ActorPresentationRequiredness Requiredness => requiredness;
        public ActorPresentationReleasePolicy ReleasePolicy => releasePolicy;
        public ActorPresentationResetPolicy ResetPolicy => resetPolicy;
        public ActorPresentationVariationPolicy VariationPolicy => variationPolicy;
        public int VariationSeed => variationSeed;
        public GameObject VisualPrefab => visualPrefab;
        public ActorPresentationSlotKind PrimarySlotKind => primarySlotKind;
        public string PrimarySlotId => primarySlotId.TrimToEmpty();
        public IReadOnlyList<ActorPresentationSlotRequirement> SlotRequirements => (IReadOnlyList<ActorPresentationSlotRequirement>)slotRequirements ?? Array.Empty<ActorPresentationSlotRequirement>();
        public bool IsRequired => requiredness == ActorPresentationRequiredness.Required;
        public bool IsOptional => requiredness == ActorPresentationRequiredness.Optional;
        public bool HasVisualPrefab => visualPrefab != null;
        public bool HasPrimarySlot =>
            primarySlotKind != ActorPresentationSlotKind.Unknown &&
            !string.IsNullOrWhiteSpace(PrimarySlotId);

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProfileId) &&
            requiredness != ActorPresentationRequiredness.Unknown &&
            releasePolicy != ActorPresentationReleasePolicy.Unknown &&
            resetPolicy != ActorPresentationResetPolicy.Unknown &&
            variationPolicy != ActorPresentationVariationPolicy.Unknown &&
            slotRequirements != null;

        public void ValidateOrThrow(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"ActorPresentationProfileAsset:{name}"
                : source.Trim();

            if (string.IsNullOrWhiteSpace(ProfileId))
            {
                throw new InvalidOperationException($"{origin} requires non-empty profileId.");
            }

            if (requiredness == ActorPresentationRequiredness.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit requiredness.");
            }

            if (releasePolicy == ActorPresentationReleasePolicy.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit releasePolicy.");
            }

            if (resetPolicy == ActorPresentationResetPolicy.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit resetPolicy.");
            }

            if (variationPolicy == ActorPresentationVariationPolicy.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit variationPolicy.");
            }

            if (IsRequired && visualPrefab == null)
            {
                throw new InvalidOperationException($"{origin} requires visualPrefab when requiredness=Required.");
            }

            if (visualPrefab != null && !HasPrimarySlot)
            {
                throw new InvalidOperationException($"{origin} requires primary slot when visualPrefab is assigned.");
            }

            if (slotRequirements == null)
            {
                throw new InvalidOperationException($"{origin} requires slotRequirements list.");
            }

            var observedKeys = new HashSet<string>(StringComparer.Ordinal);
            bool hasPrimarySlotRequirement = false;

            for (int index = 0; index < slotRequirements.Count; index++)
            {
                var requirement = slotRequirements[index];
                if (!requirement.IsValid)
                {
                    throw new InvalidOperationException($"{origin} has invalid slot requirement at index '{index}'.");
                }

                string key = $"{requirement.SlotKind}:{requirement.SlotId}";
                if (!observedKeys.Add(key))
                {
                    throw new InvalidOperationException($"{origin} has duplicate slot requirement '{key}'.");
                }

                if (requirement.SlotKind == primarySlotKind &&
                    string.Equals(requirement.SlotId, PrimarySlotId, StringComparison.Ordinal))
                {
                    hasPrimarySlotRequirement = true;
                }
            }

            if (visualPrefab != null && !hasPrimarySlotRequirement)
            {
                throw new InvalidOperationException(
                    $"{origin} requires a slot requirement matching primary slot '{primarySlotKind}:{PrimarySlotId}' when visualPrefab is assigned.");
            }
        }

        private void OnValidate()
        {
            profileId = profileId.TrimToEmpty();
            primarySlotId = primarySlotId.TrimToEmpty();
            slotRequirements ??= new List<ActorPresentationSlotRequirement>();
        }
    }
}
