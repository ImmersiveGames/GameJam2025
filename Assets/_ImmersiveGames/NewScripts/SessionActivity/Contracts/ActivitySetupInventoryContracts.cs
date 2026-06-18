using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivitySetupSubplanKind
    {
        Unknown = 0,
        Participant = 1,
        ObjectEntry = 2,
        SceneContributor = 3,
        Placement = 4,
        CameraBinding = 5,
        InteractionBinding = 6,
        HudBinding = 7,
        Warmup = 8,
        StateReset = 9,
        Release = 10,
    }

    public enum ActivitySetupRequirementRequiredness
    {
        Unknown = 0,
        Optional = 1,
        Required = 2,
    }

    public enum ActivitySetupRequirementStatus
    {
        Unknown = 0,
        Planned = 1,
        Resolved = 2,
        Skipped = 3,
        Missing = 4,
        Failed = 5,
        Completed = 6,
        Unsupported = 7,
    }

    public enum ActivityParticipantRequirementKind
    {
        Unknown = 0,
        ControllablePlayer = 1,
        Actor = 2,
        SessionParticipant = 3,
    }

    public enum ActivityObjectEntryRequirementKind
    {
        Unknown = 0,
        SceneObject = 1,
        RuntimeSpawn = 2,
        PooledInstance = 3,
    }

    public enum ActivityPlacementRequirementKind
    {
        Unknown = 0,
        Marker = 1,
        TransformSnapshot = 2,
        PreserveCurrent = 3,
    }

    public enum ActivityCameraBindingRequirementKind
    {
        Unknown = 0,
        ActivityCamera = 1,
        RouteCamera = 2,
        WindowCamera = 3,
    }

    public enum ActivityInteractionBindingRequirementKind
    {
        Unknown = 0,
        InteractionMap = 1,
        InteractorBinding = 2,
    }

    public enum ActivityHudBindingRequirementKind
    {
        Unknown = 0,
        ActivityHud = 1,
        RouteHud = 2,
    }

    public enum ActivityWarmupRequirementKind
    {
        Unknown = 0,
        AdapterWarmup = 1,
        PoolWarmup = 2,
    }

    public enum ActivityReleaseRequirementKind
    {
        Unknown = 0,
        UnloadActivityContentScene = 1,
        ReturnToPool = 2,
        DestroyRuntimeInstance = 3,
        Unbind = 4,
    }

    public enum ActivitySetupInventoryBuildResultKind
    {
        Unknown = 0,
        Built = 1,
        SkippedNoRequirements = 2,
        Failed = 3,
    }

    public readonly struct ActivitySetupRequirement
    {
        public ActivitySetupRequirement(
            SessionActivityIdentity identity,
            ActivitySetupSubplanKind subplanKind,
            string requirementId,
            ActivitySetupRequirementRequiredness requiredness,
            ActivitySetupRequirementStatus status,
            string source,
            string reason)
        {
            Identity = identity;
            SubplanKind = subplanKind;
            RequirementId = requirementId.TrimToEmpty();
            Requiredness = requiredness;
            Status = status;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public ActivitySetupSubplanKind SubplanKind { get; }
        public string RequirementId { get; }
        public ActivitySetupRequirementRequiredness Requiredness { get; }
        public ActivitySetupRequirementStatus Status { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsRequired => Requiredness == ActivitySetupRequirementRequiredness.Required;
        public bool IsOptional => Requiredness == ActivitySetupRequirementRequiredness.Optional;
        public bool IsValid =>
            Identity.IsValid &&
            SubplanKind != ActivitySetupSubplanKind.Unknown &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            Requiredness != ActivitySetupRequirementRequiredness.Unknown &&
            Status != ActivitySetupRequirementStatus.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', subplanKind='{SubplanKind}', requirementId='{RequirementId}', requiredness='{Requiredness}', status='{Status}', source='{Source}', reason='{Reason}'";
        }
}

    public readonly struct ParticipantRequirement
    {
        public ParticipantRequirement(
            ActivitySetupRequirement requirement,
            ActivityParticipantRequirementKind participantKind,
            SessionParticipantId sessionParticipantId,
            SessionParticipantRole expectedSessionRole,
            string placementRequirementId)
        {
            Requirement = requirement;
            ParticipantKind = participantKind;
            SessionParticipantId = sessionParticipantId;
            ExpectedSessionRole = expectedSessionRole;
            PlacementRequirementId = placementRequirementId.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public ActivityParticipantRequirementKind ParticipantKind { get; }
        public SessionParticipantId SessionParticipantId { get; }
        public string ParticipantId => SessionParticipantId.ToString();
        public SessionParticipantRole ExpectedSessionRole { get; }
        public string PlacementRequirementId { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.Participant } &&
            ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
            SessionParticipantId.IsValid &&
            ExpectedSessionRole != SessionParticipantRole.Unknown;

        public override string ToString()
        {
            return $"requirement='{Requirement}', participantKind='{ParticipantKind}', participantId='{ParticipantId}', expectedSessionRole='{ExpectedSessionRole}', placementRequirementId='{PlacementRequirementId}'";
        }
}

    public readonly struct ObjectEntryRequirement
    {
        public ObjectEntryRequirement(
            ActivitySetupRequirement requirement,
            ActivityObjectEntryRequirementKind objectEntryKind,
            string objectId,
            string objectTypeId,
            string placementRequirementId)
        {
            Requirement = requirement;
            ObjectEntryKind = objectEntryKind;
            ObjectId = objectId.TrimToEmpty();
            ObjectTypeId = objectTypeId.TrimToEmpty();
            PlacementRequirementId = placementRequirementId.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public ActivityObjectEntryRequirementKind ObjectEntryKind { get; }
        public string ObjectId { get; }
        public string ObjectTypeId { get; }
        public string PlacementRequirementId { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.ObjectEntry } &&
            ObjectEntryKind != ActivityObjectEntryRequirementKind.Unknown &&
            !string.IsNullOrWhiteSpace(ObjectId);

        public override string ToString()
        {
            return $"requirement='{Requirement}', objectEntryKind='{ObjectEntryKind}', objectId='{ObjectId}', objectTypeId='{ObjectTypeId}', placementRequirementId='{PlacementRequirementId}'";
        }
}

    public readonly struct SceneContributorRequirement
    {
        public SceneContributorRequirement(
            ActivitySetupRequirement requirement,
            string contributorId,
            string contributorRole,
            string sceneName)
        {
            Requirement = requirement;
            ContributorId = contributorId.TrimToEmpty();
            ContributorRole = contributorRole.TrimToEmpty();
            SceneName = sceneName.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public string ContributorId { get; }
        public string ContributorRole { get; }
        public string SceneName { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.SceneContributor } &&
            !string.IsNullOrWhiteSpace(ContributorId);

        public override string ToString()
        {
            return $"requirement='{Requirement}', contributorId='{ContributorId}', contributorRole='{ContributorRole}', sceneName='{SceneName}'";
        }
}

    public readonly struct PlacementRequirement
    {
        public PlacementRequirement(
            ActivitySetupRequirement requirement,
            ActivityPlacementRequirementKind placementKind,
            string targetId,
            string markerId,
            string sceneName)
        {
            Requirement = requirement;
            PlacementKind = placementKind;
            TargetId = targetId.TrimToEmpty();
            MarkerId = markerId.TrimToEmpty();
            SceneName = sceneName.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public ActivityPlacementRequirementKind PlacementKind { get; }
        public string TargetId { get; }
        public string MarkerId { get; }
        public string SceneName { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.Placement } &&
            PlacementKind != ActivityPlacementRequirementKind.Unknown &&
            !string.IsNullOrWhiteSpace(TargetId);

        public override string ToString()
        {
            return $"requirement='{Requirement}', placementKind='{PlacementKind}', targetId='{TargetId}', markerId='{MarkerId}', sceneName='{SceneName}'";
        }
}

    public readonly struct CameraBindingRequirement
    {
        public CameraBindingRequirement(
            ActivitySetupRequirement requirement,
            ActivityCameraBindingRequirementKind cameraBindingKind,
            string bindingId,
            string targetId,
            string profileId)
        {
            Requirement = requirement;
            CameraBindingKind = cameraBindingKind;
            BindingId = bindingId.TrimToEmpty();
            TargetId = targetId.TrimToEmpty();
            ProfileId = profileId.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public ActivityCameraBindingRequirementKind CameraBindingKind { get; }
        public string BindingId { get; }
        public string TargetId { get; }
        public string ProfileId { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.CameraBinding } &&
            CameraBindingKind != ActivityCameraBindingRequirementKind.Unknown &&
            !string.IsNullOrWhiteSpace(BindingId);

        public override string ToString()
        {
            return $"requirement='{Requirement}', cameraBindingKind='{CameraBindingKind}', bindingId='{BindingId}', targetId='{TargetId}', profileId='{ProfileId}'";
        }
}

    public readonly struct InteractionBindingRequirement
    {
        public InteractionBindingRequirement(
            ActivitySetupRequirement requirement,
            ActivityInteractionBindingRequirementKind interactionBindingKind,
            string bindingId,
            string targetId,
            string profileId)
        {
            Requirement = requirement;
            InteractionBindingKind = interactionBindingKind;
            BindingId = bindingId.TrimToEmpty();
            TargetId = targetId.TrimToEmpty();
            ProfileId = profileId.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public ActivityInteractionBindingRequirementKind InteractionBindingKind { get; }
        public string BindingId { get; }
        public string TargetId { get; }
        public string ProfileId { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.InteractionBinding } &&
            InteractionBindingKind != ActivityInteractionBindingRequirementKind.Unknown &&
            !string.IsNullOrWhiteSpace(BindingId);

        public override string ToString()
        {
            return $"requirement='{Requirement}', interactionBindingKind='{InteractionBindingKind}', bindingId='{BindingId}', targetId='{TargetId}', profileId='{ProfileId}'";
        }
}

    public readonly struct HudBindingRequirement
    {
        public HudBindingRequirement(
            ActivitySetupRequirement requirement,
            ActivityHudBindingRequirementKind hudBindingKind,
            string bindingId,
            string targetId,
            string profileId)
        {
            Requirement = requirement;
            HudBindingKind = hudBindingKind;
            BindingId = bindingId.TrimToEmpty();
            TargetId = targetId.TrimToEmpty();
            ProfileId = profileId.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public ActivityHudBindingRequirementKind HudBindingKind { get; }
        public string BindingId { get; }
        public string TargetId { get; }
        public string ProfileId { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.HudBinding } &&
            HudBindingKind != ActivityHudBindingRequirementKind.Unknown &&
            !string.IsNullOrWhiteSpace(BindingId);

        public override string ToString()
        {
            return $"requirement='{Requirement}', hudBindingKind='{HudBindingKind}', bindingId='{BindingId}', targetId='{TargetId}', profileId='{ProfileId}'";
        }
}

    public readonly struct WarmupRequirement
    {
        public WarmupRequirement(
            ActivitySetupRequirement requirement,
            ActivityWarmupRequirementKind warmupKind,
            string targetId,
            string profileId)
        {
            Requirement = requirement;
            WarmupKind = warmupKind;
            TargetId = targetId.TrimToEmpty();
            ProfileId = profileId.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public ActivityWarmupRequirementKind WarmupKind { get; }
        public string TargetId { get; }
        public string ProfileId { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.Warmup } &&
            WarmupKind != ActivityWarmupRequirementKind.Unknown &&
            !string.IsNullOrWhiteSpace(TargetId);

        public override string ToString()
        {
            return $"requirement='{Requirement}', warmupKind='{WarmupKind}', targetId='{TargetId}', profileId='{ProfileId}'";
        }
}

    public readonly struct StateResetRequirement
    {
        public StateResetRequirement(
            ActivitySetupRequirement requirement,
            string targetId)
        {
            Requirement = requirement;
            TargetId = targetId.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public string TargetId { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.StateReset } &&
            !string.IsNullOrWhiteSpace(TargetId);

        public override string ToString()
        {
            return $"requirement='{Requirement}', targetId='{TargetId}', resetDescriptor='endpoint_inventory'";
        }
}

    public readonly struct ReleaseRequirement
    {
        public ReleaseRequirement(
            ActivitySetupRequirement requirement,
            ActivityReleaseRequirementKind releaseKind,
            string targetId,
            string policyId)
        {
            Requirement = requirement;
            ReleaseKind = releaseKind;
            TargetId = targetId.TrimToEmpty();
            PolicyId = policyId.TrimToEmpty();
        }

        public ActivitySetupRequirement Requirement { get; }
        public ActivityReleaseRequirementKind ReleaseKind { get; }
        public string TargetId { get; }
        public string PolicyId { get; }

        public bool IsValid =>
            Requirement is { IsValid: true, SubplanKind: ActivitySetupSubplanKind.Release } &&
            ReleaseKind != ActivityReleaseRequirementKind.Unknown &&
            !string.IsNullOrWhiteSpace(TargetId);

        public override string ToString()
        {
            return $"requirement='{Requirement}', releaseKind='{ReleaseKind}', targetId='{TargetId}', policyId='{PolicyId}'";
        }
}

    public readonly struct ActivitySetupInventory
    {
        public ActivitySetupInventory(
            SessionActivityIdentity identity,
            string inventoryId,
            IReadOnlyList<ParticipantRequirement> participantRequirements,
            IReadOnlyList<ObjectEntryRequirement> objectEntryRequirements,
            IReadOnlyList<SceneContributorRequirement> sceneContributorRequirements,
            IReadOnlyList<PlacementRequirement> placementRequirements,
            IReadOnlyList<CameraBindingRequirement> cameraBindingRequirements,
            IReadOnlyList<InteractionBindingRequirement> interactionBindingRequirements,
            IReadOnlyList<HudBindingRequirement> hudBindingRequirements,
            IReadOnlyList<WarmupRequirement> warmupRequirements,
            IReadOnlyList<StateResetRequirement> stateResetRequirements,
            IReadOnlyList<ReleaseRequirement> releaseRequirements,
            string source,
            string reason)
        {
            Identity = identity;
            InventoryId = inventoryId.TrimToEmpty();
            ParticipantRequirements = participantRequirements ?? Array.Empty<ParticipantRequirement>();
            ObjectEntryRequirements = objectEntryRequirements ?? Array.Empty<ObjectEntryRequirement>();
            SceneContributorRequirements = sceneContributorRequirements ?? Array.Empty<SceneContributorRequirement>();
            PlacementRequirements = placementRequirements ?? Array.Empty<PlacementRequirement>();
            CameraBindingRequirements = cameraBindingRequirements ?? Array.Empty<CameraBindingRequirement>();
            InteractionBindingRequirements = interactionBindingRequirements ?? Array.Empty<InteractionBindingRequirement>();
            HudBindingRequirements = hudBindingRequirements ?? Array.Empty<HudBindingRequirement>();
            WarmupRequirements = warmupRequirements ?? Array.Empty<WarmupRequirement>();
            StateResetRequirements = stateResetRequirements ?? Array.Empty<StateResetRequirement>();
            ReleaseRequirements = releaseRequirements ?? Array.Empty<ReleaseRequirement>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string InventoryId { get; }
        public IReadOnlyList<ParticipantRequirement> ParticipantRequirements { get; }
        public IReadOnlyList<ObjectEntryRequirement> ObjectEntryRequirements { get; }
        public IReadOnlyList<SceneContributorRequirement> SceneContributorRequirements { get; }
        public IReadOnlyList<PlacementRequirement> PlacementRequirements { get; }
        public IReadOnlyList<CameraBindingRequirement> CameraBindingRequirements { get; }
        public IReadOnlyList<InteractionBindingRequirement> InteractionBindingRequirements { get; }
        public IReadOnlyList<HudBindingRequirement> HudBindingRequirements { get; }
        public IReadOnlyList<WarmupRequirement> WarmupRequirements { get; }
        public IReadOnlyList<StateResetRequirement> StateResetRequirements { get; }
        public IReadOnlyList<ReleaseRequirement> ReleaseRequirements { get; }
        public string Source { get; }
        public string Reason { get; }

        public int TotalRequirementCount =>
            ParticipantRequirements.Count +
            ObjectEntryRequirements.Count +
            SceneContributorRequirements.Count +
            PlacementRequirements.Count +
            CameraBindingRequirements.Count +
            InteractionBindingRequirements.Count +
            HudBindingRequirements.Count +
            WarmupRequirements.Count +
            StateResetRequirements.Count +
            ReleaseRequirements.Count;

        public bool HasRequirements => TotalRequirementCount > 0;

        public bool IsValid =>
            Identity is { IsValid: true, Stage: SessionActivityStage.ActivitySetupStarted } &&
            !string.IsNullOrWhiteSpace(InventoryId) &&
            !string.IsNullOrWhiteSpace(Source) &&
            ParticipantRequirements != null &&
            ObjectEntryRequirements != null &&
            SceneContributorRequirements != null &&
            PlacementRequirements != null &&
            CameraBindingRequirements != null &&
            InteractionBindingRequirements != null &&
            HudBindingRequirements != null &&
            WarmupRequirements != null &&
            StateResetRequirements != null &&
            ReleaseRequirements != null;

        public override string ToString()
        {
            return $"identity='{Identity}', inventoryId='{InventoryId}', totalRequirements='{TotalRequirementCount}', participants='{ParticipantRequirements.Count}', objects='{ObjectEntryRequirements.Count}', contributors='{SceneContributorRequirements.Count}', placements='{PlacementRequirements.Count}', cameras='{CameraBindingRequirements.Count}', interactions='{InteractionBindingRequirements.Count}', hud='{HudBindingRequirements.Count}', warmup='{WarmupRequirements.Count}', reset='{StateResetRequirements.Count}', release='{ReleaseRequirements.Count}', source='{Source}', reason='{Reason}'";
        }
}

    public readonly struct ActivitySetupInventoryBuildResult
    {
        public ActivitySetupInventoryBuildResult(
            ActivitySetupInventoryBuildResultKind kind,
            ActivitySetupInventory inventory,
            string source,
            string reason,
            string message)
        {
            Kind = kind;
            Inventory = inventory;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
            Message = message.TrimToEmpty();
        }

        public ActivitySetupInventoryBuildResultKind Kind { get; }
        public ActivitySetupInventory Inventory { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsBuilt => Kind == ActivitySetupInventoryBuildResultKind.Built;
        public bool IsSkipped => Kind == ActivitySetupInventoryBuildResultKind.SkippedNoRequirements;
        public bool IsFailed => Kind == ActivitySetupInventoryBuildResultKind.Failed;
        public bool IsValid =>
            Kind != ActivitySetupInventoryBuildResultKind.Unknown &&
            !string.IsNullOrWhiteSpace(Source) &&
            (IsFailed || Inventory.IsValid);

        public override string ToString()
        {
            return $"kind='{Kind}', inventory='{Inventory}', source='{Source}', reason='{Reason}', message='{Message}'";
        }
}

}
