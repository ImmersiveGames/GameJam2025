using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [Serializable]
    public sealed class ActivitySetupRequirementsAuthoring
    {
        [SerializeField] private List<ActivityParticipantRequirementAuthoring> participantRequirements = new();
        [SerializeField] private List<ActivityObjectEntryRequirementAuthoring> objectEntryRequirements = new();
        [SerializeField] private List<ActivitySceneContributorRequirementAuthoring> sceneContributorRequirements = new();
        [SerializeField] private List<ActivityPlacementRequirementAuthoring> placementRequirements = new();
        [SerializeField] private List<ActivityCameraBindingRequirementAuthoring> cameraBindingRequirements = new();
        [SerializeField] private List<ActivityInteractionBindingRequirementAuthoring> interactionBindingRequirements = new();
        [SerializeField] private List<ActivityHudBindingRequirementAuthoring> hudBindingRequirements = new();
        [SerializeField] private List<ActivityWarmupRequirementAuthoring> warmupRequirements = new();
        [SerializeField] private List<ActivityStateResetRequirementAuthoring> stateResetRequirements = new();
        [SerializeField] private List<ActivityReleaseRequirementAuthoring> releaseRequirements = new();

        public IReadOnlyList<ActivityParticipantRequirementAuthoring> ParticipantRequirements => participantRequirements;
        public IReadOnlyList<ActivityObjectEntryRequirementAuthoring> ObjectEntryRequirements => objectEntryRequirements;
        public IReadOnlyList<ActivitySceneContributorRequirementAuthoring> SceneContributorRequirements => sceneContributorRequirements;
        public IReadOnlyList<ActivityPlacementRequirementAuthoring> PlacementRequirements => placementRequirements;
        public IReadOnlyList<ActivityCameraBindingRequirementAuthoring> CameraBindingRequirements => cameraBindingRequirements;
        public IReadOnlyList<ActivityInteractionBindingRequirementAuthoring> InteractionBindingRequirements => interactionBindingRequirements;
        public IReadOnlyList<ActivityHudBindingRequirementAuthoring> HudBindingRequirements => hudBindingRequirements;
        public IReadOnlyList<ActivityWarmupRequirementAuthoring> WarmupRequirements => warmupRequirements;
        public IReadOnlyList<ActivityStateResetRequirementAuthoring> StateResetRequirements => stateResetRequirements;
        public IReadOnlyList<ActivityReleaseRequirementAuthoring> ReleaseRequirements => releaseRequirements;

        public int TotalRequirementCount =>
            Count(participantRequirements) +
            Count(objectEntryRequirements) +
            Count(sceneContributorRequirements) +
            Count(placementRequirements) +
            Count(cameraBindingRequirements) +
            Count(interactionBindingRequirements) +
            Count(hudBindingRequirements) +
            Count(warmupRequirements) +
            Count(stateResetRequirements) +
            Count(releaseRequirements);

        public bool HasRequirements => TotalRequirementCount > 0;

        public void ValidateOrThrow(string source)
        {
            string validationSource = source.TrimToEmpty();

            ValidateList(participantRequirements, validationSource, nameof(participantRequirements));
            ValidateList(objectEntryRequirements, validationSource, nameof(objectEntryRequirements));
            ValidateList(sceneContributorRequirements, validationSource, nameof(sceneContributorRequirements));
            ValidateList(placementRequirements, validationSource, nameof(placementRequirements));
            ValidateList(cameraBindingRequirements, validationSource, nameof(cameraBindingRequirements));
            ValidateList(interactionBindingRequirements, validationSource, nameof(interactionBindingRequirements));
            ValidateList(hudBindingRequirements, validationSource, nameof(hudBindingRequirements));
            ValidateList(warmupRequirements, validationSource, nameof(warmupRequirements));
            ValidateList(stateResetRequirements, validationSource, nameof(stateResetRequirements));
            ValidateList(releaseRequirements, validationSource, nameof(releaseRequirements));
        }

        public int PruneLegacyEmptyObjectEntryRequirements()
        {
            if (objectEntryRequirements == null || objectEntryRequirements.Count == 0)
            {
                return 0;
            }

            int removed = 0;
            for (int index = objectEntryRequirements.Count - 1; index >= 0; index--)
            {
                var entry = objectEntryRequirements[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.RequirementId))
                {
                    objectEntryRequirements.RemoveAt(index);
                    removed += 1;
                }
            }

            return removed;
        }

        private static void ValidateList<T>(IReadOnlyList<T> entries, string source, string listName)
            where T : class, IActivitySetupRequirementAuthoring
        {
            if (entries == null)
            {
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    throw new InvalidOperationException($"{source}.{listName}[{index}] cannot be null.");
                }

                entry.ValidateOrThrow(source, listName, index);
            }
        }

        private static int Count<T>(IReadOnlyList<T> entries)
        {
            return entries?.Count ?? 0;
        }
    }

    public interface IActivitySetupRequirementAuthoring
    {
        string RequirementId { get; }
        ActivitySetupRequirementRequiredness Requiredness { get; }
        void ValidateOrThrow(string source, string listName, int index);
    }

    [Serializable]
    public abstract class ActivitySetupRequirementAuthoringBase : IActivitySetupRequirementAuthoring
    {
        [SerializeField] private string requirementId;
        [SerializeField] private ActivitySetupRequirementRequiredness requiredness = ActivitySetupRequirementRequiredness.Required;

        public string RequirementId => requirementId.TrimToEmpty();
        public ActivitySetupRequirementRequiredness Requiredness => requiredness;

        public void ValidateOrThrow(string source, string listName, int index)
        {
            string validationSource = $"{source.TrimToEmpty()}.{listName}[{index}]";

            if (string.IsNullOrWhiteSpace(RequirementId))
            {
                throw new InvalidOperationException($"{validationSource} requires requirementId.");
            }

            if (!string.Equals(requirementId, requirementId.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{validationSource} requirementId cannot have leading or trailing spaces.");
            }

            if (RequirementId.Contains(" ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{validationSource} requirementId cannot contain spaces.");
            }

            if (requiredness == ActivitySetupRequirementRequiredness.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit requiredness.");
            }

            ValidateSpecificOrThrow(validationSource);
        }

        protected abstract void ValidateSpecificOrThrow(string validationSource);

    }

    [Serializable]
    public sealed class ActivityParticipantRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private ActivityParticipantRequirementKind participantKind = ActivityParticipantRequirementKind.ControllablePlayer;
        [SerializeField] private string participantId;
        [SerializeField] private SessionParticipantRole expectedSessionRole = SessionParticipantRole.PrimaryPlayer;
        [SerializeField] private string placementRequirementId;

        public ActivityParticipantRequirementKind ParticipantKind => participantKind;
        public SessionParticipantId SessionParticipantId => new(participantId.TrimToEmpty());
        public string ParticipantId => SessionParticipantId.ToString();
        public SessionParticipantRole ExpectedSessionRole => expectedSessionRole;
        public string PlacementRequirementId => placementRequirementId.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (participantKind == ActivityParticipantRequirementKind.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit participantKind.");
            }

            if (string.IsNullOrWhiteSpace(ParticipantId))
            {
                throw new InvalidOperationException($"{validationSource} requires participantId.");
            }

            if (expectedSessionRole == SessionParticipantRole.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit expectedSessionRole.");
            }
        }
    }

    [Serializable]
    public sealed class ActivityObjectEntryRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private ActivityObjectEntryRequirementKind objectEntryKind = ActivityObjectEntryRequirementKind.SceneObject;
        [SerializeField] private string objectId;
        [SerializeField] private string objectTypeId;
        [SerializeField] private string placementRequirementId;

        public ActivityObjectEntryRequirementKind ObjectEntryKind => objectEntryKind;
        public string ObjectId => objectId.TrimToEmpty();
        public string ObjectTypeId => objectTypeId.TrimToEmpty();
        public string PlacementRequirementId => placementRequirementId.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (objectEntryKind == ActivityObjectEntryRequirementKind.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit objectEntryKind.");
            }

            if (string.IsNullOrWhiteSpace(ObjectId))
            {
                throw new InvalidOperationException($"{validationSource} requires objectId.");
            }
        }
    }

    [Serializable]
    public sealed class ActivitySceneContributorRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private string contributorId;
        [SerializeField] private string contributorRole;
        [SerializeField] private string sceneName;

        public string ContributorId => contributorId.TrimToEmpty();
        public string ContributorRole => contributorRole.TrimToEmpty();
        public string SceneName => sceneName.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (string.IsNullOrWhiteSpace(ContributorId))
            {
                throw new InvalidOperationException($"{validationSource} requires contributorId.");
            }
        }
    }

    [Serializable]
    public sealed class ActivityPlacementRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private ActivityPlacementRequirementKind placementKind = ActivityPlacementRequirementKind.Marker;
        [SerializeField] private string targetId;
        [SerializeField] private string markerId;
        [SerializeField] private string sceneName;

        public ActivityPlacementRequirementKind PlacementKind => placementKind;
        public string TargetId => targetId.TrimToEmpty();
        public string MarkerId => markerId.TrimToEmpty();
        public string SceneName => sceneName.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (placementKind == ActivityPlacementRequirementKind.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit placementKind.");
            }

            if (string.IsNullOrWhiteSpace(TargetId))
            {
                throw new InvalidOperationException($"{validationSource} requires targetId.");
            }

            if (placementKind == ActivityPlacementRequirementKind.Marker && string.IsNullOrWhiteSpace(MarkerId))
            {
                throw new InvalidOperationException($"{validationSource} requires markerId when placementKind=Marker.");
            }
        }
    }

    [Serializable]
    public sealed class ActivityCameraBindingRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private ActivityCameraBindingRequirementKind cameraBindingKind = ActivityCameraBindingRequirementKind.ActivityCamera;
        [SerializeField] private string bindingId;
        [SerializeField] private string targetId;
        [SerializeField] private string profileId;

        public ActivityCameraBindingRequirementKind CameraBindingKind => cameraBindingKind;
        public string BindingId => bindingId.TrimToEmpty();
        public string TargetId => targetId.TrimToEmpty();
        public string ProfileId => profileId.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (cameraBindingKind == ActivityCameraBindingRequirementKind.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit cameraBindingKind.");
            }

            if (string.IsNullOrWhiteSpace(BindingId))
            {
                throw new InvalidOperationException($"{validationSource} requires bindingId.");
            }
        }
    }

    [Serializable]
    public sealed class ActivityInteractionBindingRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private ActivityInteractionBindingRequirementKind interactionBindingKind = ActivityInteractionBindingRequirementKind.InteractionMap;
        [SerializeField] private string bindingId;
        [SerializeField] private string targetId;
        [SerializeField] private string profileId;

        public ActivityInteractionBindingRequirementKind InteractionBindingKind => interactionBindingKind;
        public string BindingId => bindingId.TrimToEmpty();
        public string TargetId => targetId.TrimToEmpty();
        public string ProfileId => profileId.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (interactionBindingKind == ActivityInteractionBindingRequirementKind.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit interactionBindingKind.");
            }

            if (string.IsNullOrWhiteSpace(BindingId))
            {
                throw new InvalidOperationException($"{validationSource} requires bindingId.");
            }
        }
    }

    [Serializable]
    public sealed class ActivityHudBindingRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private ActivityHudBindingRequirementKind hudBindingKind = ActivityHudBindingRequirementKind.ActivityHud;
        [SerializeField] private string bindingId;
        [SerializeField] private string targetId;
        [SerializeField] private string profileId;

        public ActivityHudBindingRequirementKind HudBindingKind => hudBindingKind;
        public string BindingId => bindingId.TrimToEmpty();
        public string TargetId => targetId.TrimToEmpty();
        public string ProfileId => profileId.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (hudBindingKind == ActivityHudBindingRequirementKind.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit hudBindingKind.");
            }

            if (string.IsNullOrWhiteSpace(BindingId))
            {
                throw new InvalidOperationException($"{validationSource} requires bindingId.");
            }
        }
    }

    [Serializable]
    public sealed class ActivityWarmupRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private ActivityWarmupRequirementKind warmupKind = ActivityWarmupRequirementKind.AdapterWarmup;
        [SerializeField] private string targetId;
        [SerializeField] private string profileId;

        public ActivityWarmupRequirementKind WarmupKind => warmupKind;
        public string TargetId => targetId.TrimToEmpty();
        public string ProfileId => profileId.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (warmupKind == ActivityWarmupRequirementKind.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit warmupKind.");
            }

            if (string.IsNullOrWhiteSpace(TargetId))
            {
                throw new InvalidOperationException($"{validationSource} requires targetId.");
            }
        }
    }

    [Serializable]
    public sealed class ActivityStateResetRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private string targetId;

        public string TargetId => targetId.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (string.IsNullOrWhiteSpace(TargetId))
            {
                throw new InvalidOperationException($"{validationSource} requires targetId.");
            }
        }
    }

    [Serializable]
    public sealed class ActivityReleaseRequirementAuthoring : ActivitySetupRequirementAuthoringBase
    {
        [SerializeField] private ActivityReleaseRequirementKind releaseKind = ActivityReleaseRequirementKind.Unbind;
        [SerializeField] private string targetId;
        [SerializeField] private string policyId;

        public ActivityReleaseRequirementKind ReleaseKind => releaseKind;
        public string TargetId => targetId.TrimToEmpty();
        public string PolicyId => policyId.TrimToEmpty();

        protected override void ValidateSpecificOrThrow(string validationSource)
        {
            if (releaseKind == ActivityReleaseRequirementKind.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} requires explicit releaseKind.");
            }

            if (string.IsNullOrWhiteSpace(TargetId))
            {
                throw new InvalidOperationException($"{validationSource} requires targetId.");
            }
        }
    }
}
