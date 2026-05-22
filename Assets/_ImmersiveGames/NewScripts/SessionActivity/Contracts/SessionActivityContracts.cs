using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityCatalogAdvanceAtEndMode
    {
        StopAtEnd = 0,
        LoopToFirst = 1,
    }

    public enum ActivityWindowMode
    {
        None = 0,
        AdditiveScene = 1,
    }

    public enum ActivityTransitionMode
    {
        None = 0,
        CutWithCurtain = 1,
        Seamless = 2,
    }

    public enum ActivityTransitionProfileSource
    {
        None = 0,
        OverrideProfile = 1,
        InheritRouteProfile = 2,
    }

    public enum ActivityTransitionContinuePolicy
    {
        Unknown = 0,
        AutoContinue = 1,
        ManualContinue = 2,
    }

    public enum ActivitySceneDiscoveryMode
    {
        None = 0,
        StrictDeclaredOnly = 1,
        AllowOptionalDiscovered = 2,
        Open = 3,
    }

    public enum ActivitySceneRevealSafety
    {
        Unknown = 0,
        SafeForCutWithCurtain = 1,
        SafeForSeamlessCandidate = 2,
    }

    public enum SessionActivityStage
    {
        Unknown = 0,
        ActivityActivationStarted = 1,
        ActivationWindowStarted = 2,
        ActivationWindowSceneLoading = 30,
        ActivationWindowAdditiveSceneLoadStarted = 3,
        ActivationWindowAdditiveSceneLoaded = 4,
        ActivationWindowReady = 5,
        ActivationWindowCompleted = 6,
        ActivationWindowSceneUnloading = 31,
        ActivationWindowAdditiveSceneUnloadStarted = 7,
        ActivationWindowAdditiveSceneUnloaded = 8,
        ActivationWindowSkippedNoContent = 9,
        ActivityRunning = 10,
        ActivityCompletionRequested = 11,
        ActivityCompleting = 12,
        PlayerActorParticipationExitStageStarted = 34,
        PlayerActorParticipationExitStageCompleted = 35,
        DeactivationWindowStarted = 13,
        DeactivationWindowSceneLoading = 32,
        DeactivationWindowAdditiveSceneLoadStarted = 14,
        DeactivationWindowAdditiveSceneLoaded = 15,
        DeactivationWindowReady = 16,
        DeactivationWindowCompleted = 17,
        DeactivationWindowSceneUnloading = 33,
        DeactivationWindowAdditiveSceneUnloadStarted = 18,
        DeactivationWindowAdditiveSceneUnloaded = 19,
        DeactivationWindowSkippedNoContent = 20,
        Deactivation = 21,
        Completed = 22,
        ClosedForRouteExit = 23,
        ActivitySetupStarted = 24,
        ActivitySetupSkippedNoContent = 25,
        ActivitySetupCompleted = 26,
        NextActivitySetupStarted = 27,
        NextActivitySetupSkippedNoContent = 28,
        NextActivitySetupCompleted = 29,
        ActivityContentProfileResolved = 36,
        ActivityContentLoadStarted = 37,
        ActivityContentSceneLoading = 38,
        ActivityContentSceneLoaded = 39,
        ActivityContentLoadedSetReady = 40,
        ActivityContentLoadSkippedNoContent = 41,
        ActivityContentLoadFailed = 42,
        ActivitySetupInventoryBuildStarted = 43,
        ActivitySetupInventoryBuilt = 44,
        ActivitySetupInventoryValidated = 45,
        ActivitySetupInventorySkippedNoRequirements = 46,
        ActivitySetupInventoryValidationFailed = 47,
        ActivityParticipantBindingStarted = 48,
        ActivityParticipantBindingSkippedNoRequirements = 49,
        ActivityParticipantBindingCompleted = 50,
        ActivityParticipantBindingFailed = 51,
        ActivityContentRetentionPlanResolved = 52,
        ActivityContentReleaseStarted = 53,
        ActivityContentSceneUnloading = 54,
        ActivityContentSceneUnloaded = 55,
        ActivityContentReleaseSkippedNoContent = 56,
        ActivityContentReleaseCompleted = 57,
        ActivityContentReleaseFailed = 58,
        PlayerActorReadinessStarted = 59,
        PlayerActorReadinessSkippedNoRequiredParticipant = 60,
        PlayerActorReadinessValidatedMaterializedOnly = 61,
        PlayerActorReadinessFailed = 62,
        PlayerActorReadinessCompleted = 63,
        PlayerInputBindingStarted = 64,
        PlayerInputBindingSkippedNoRequiredInput = 65,
        PlayerInputBindingFailed = 66,
        PlayerInputBindingCompleted = 67,
        MovementBindingStarted = 68,
        MovementBindingSkippedNoRequiredMovement = 69,
        MovementBindingFailed = 70,
        MovementBindingCompleted = 71,
        CameraBindingStarted = 72,
        CameraBindingFailed = 73,
        CameraBindingSkippedNoRequiredCamera = 74,
        CameraBindingCompleted = 75,
        ActorPresentationSetupStarted = 76,
        ActorPresentationPlanResolved = 77,
        ActorPresentationMaterialized = 78,
        ActorPresentationReady = 79,
        ActorPresentationSetupSkippedOptional = 80,
        ActorPresentationSetupFailed = 81,
        ActorPresentationSetupCompleted = 82,
        ActorPresentationReleaseStarted = 83,
        ActorPresentationReleased = 84,
        ActorPresentationReleaseSkipped = 85,
        ActorPresentationReleaseFailed = 86,
        ActorPresentationReleaseCompleted = 87,
        ActorPresentationRetained = 88,
        ActorPresentationRetentionSkipped = 89,
        ActorPresentationRetentionFailed = 90,
        ActorPresentationResetSkipped = 91,
    }

    public enum ActivityExecutionState
    {
        Unknown = 0,
        Stopped = 1,
        Running = 2,
        Paused = 3,
    }

    public readonly struct SessionActivityIdentity : IEquatable<SessionActivityIdentity>
    {
        public SessionActivityIdentity(
            string pipelineId,
            string sessionStateId,
            string activityId,
            int activityOrdinal,
            int entrySequence,
            SessionActivityStage stage,
            string source)
        {
            PipelineId = Normalize(pipelineId);
            SessionId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            Stage = stage;
            Source = Normalize(source);
            CycleSignature = BuildCycleSignature(PipelineId, SessionId, ActivityId, ActivityOrdinal, EntrySequence, Stage);
        }

        public string PipelineId { get; }
        public string SessionId { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public SessionActivityStage Stage { get; }
        public string Source { get; }
        public string CycleSignature { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            Stage != SessionActivityStage.Unknown &&
            !string.IsNullOrWhiteSpace(CycleSignature);

        public static SessionActivityIdentity Empty => default;

        public bool Equals(SessionActivityIdentity other)
        {
            return string.Equals(CycleSignature, other.CycleSignature, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionActivityIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(CycleSignature ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid
                ? $"pipelineId='{PipelineId}', sessionStateId='{SessionId}', activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', entrySequence='{EntrySequence}', stage='{Stage}'"
                : "<none>";
        }

        public static bool operator ==(SessionActivityIdentity left, SessionActivityIdentity right) => left.Equals(right);
        public static bool operator !=(SessionActivityIdentity left, SessionActivityIdentity right) => !left.Equals(right);

        private static string BuildCycleSignature(string pipelineId, string sessionStateId, string activityId, int activityOrdinal, int entrySequence, SessionActivityStage stage)
        {
            return $"{pipelineId}|{sessionStateId}|{activityId}|{activityOrdinal}|{entrySequence}|{stage}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivityDefinition
    {
        public SessionActivityDefinition(
            string activityId,
            string displayName,
            int activityOrdinal,
            ActivityContentMode activityContentMode,
            ActivityContentProfileAsset activityContentProfile,
            ActivityWindowMode activationWindowMode,
            SceneKeyAsset activationWindowAdditiveSceneKey,
            ActivityWindowMode deactivationWindowMode,
            SceneKeyAsset deactivationWindowAdditiveSceneKey,
            ActivityTransitionProfileSource nextActivityTransitionProfileSource,
            ActivityTransitionContinuePolicy nextActivityTransitionContinuePolicy,
            ActivityTransitionProfileAsset nextActivityTransitionProfileOverride,
            string nextActivityId,
            string source)
        {
            ActivityId = Normalize(activityId);
            DisplayName = Normalize(displayName);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            ActivityContentMode = activityContentMode;
            ActivityContentProfile = activityContentProfile;
            ActivationWindowMode = activationWindowMode;
            ActivationWindowAdditiveSceneKey = activationWindowAdditiveSceneKey;
            DeactivationWindowMode = deactivationWindowMode;
            DeactivationWindowAdditiveSceneKey = deactivationWindowAdditiveSceneKey;
            NextActivityTransitionProfileSource = nextActivityTransitionProfileSource;
            NextActivityTransitionContinuePolicy = nextActivityTransitionContinuePolicy;
            NextActivityTransitionProfileOverride = nextActivityTransitionProfileOverride;
            NextActivityId = Normalize(nextActivityId);
            Source = Normalize(source);
        }

        public string ActivityId { get; }
        public string DisplayName { get; }
        public int ActivityOrdinal { get; }
        public ActivityContentMode ActivityContentMode { get; }
        public ActivityContentProfileAsset ActivityContentProfile { get; }
        public bool HasGameplayContent => ActivityContentMode == ActivityContentMode.Profile;
        public ActivityWindowMode ActivationWindowMode { get; }
        public SceneKeyAsset ActivationWindowAdditiveSceneKey { get; }
        public ActivityWindowMode DeactivationWindowMode { get; }
        public SceneKeyAsset DeactivationWindowAdditiveSceneKey { get; }
        public ActivityTransitionProfileSource NextActivityTransitionProfileSource { get; }
        public ActivityTransitionContinuePolicy NextActivityTransitionContinuePolicy { get; }
        public ActivityTransitionProfileAsset NextActivityTransitionProfileOverride { get; }
        public string NextActivityId { get; }
        public string Source { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ActivityId) &&
            !string.IsNullOrWhiteSpace(DisplayName) &&
            ActivityOrdinal > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            IsActivityContentConfigurationValid;

        public bool HasNextActivity => !string.IsNullOrWhiteSpace(NextActivityId);
        public bool HasActivityContentProfile => ActivityContentProfile != null;
        public string ActivityContentProfileId => HasActivityContentProfile ? ActivityContentProfile.ContentProfileId : string.Empty;
        public bool HasActivationWindowAdditiveSceneKey => ActivationWindowAdditiveSceneKey != null;
        public bool HasDeactivationWindowAdditiveSceneKey => DeactivationWindowAdditiveSceneKey != null;
        public bool HasNextActivityTransitionProfileOverride => NextActivityTransitionProfileOverride != null;
        public bool HasValidNextActivityTransitionContinuePolicy => NextActivityTransitionContinuePolicy != ActivityTransitionContinuePolicy.Unknown;

        private bool IsActivityContentConfigurationValid =>
            (ActivityContentMode == ActivityContentMode.None && ActivityContentProfile == null) ||
            (ActivityContentMode == ActivityContentMode.Profile && ActivityContentProfile != null);

        public override string ToString()
        {
            return $"activityId='{ActivityId}', displayName='{DisplayName}', ordinal='{ActivityOrdinal}', activityContentMode='{ActivityContentMode}', activityContentProfile='{(HasActivityContentProfile ? ActivityContentProfile.name : "<none>")}', activityContentProfileId='{(HasActivityContentProfile ? ActivityContentProfileId : "<none>")}', activationWindowMode='{ActivationWindowMode}', activationWindowAdditiveSceneKey='{(HasActivationWindowAdditiveSceneKey ? ActivationWindowAdditiveSceneKey.name : "<none>")}', deactivationWindowMode='{DeactivationWindowMode}', deactivationWindowAdditiveSceneKey='{(HasDeactivationWindowAdditiveSceneKey ? DeactivationWindowAdditiveSceneKey.name : "<none>")}', nextActivityTransitionProfileSource='{NextActivityTransitionProfileSource}', nextActivityTransitionContinuePolicy='{NextActivityTransitionContinuePolicy}', nextActivityTransitionProfileOverride='{(HasNextActivityTransitionProfileOverride ? NextActivityTransitionProfileOverride.name : "<none>")}', nextActivityId='{(HasNextActivity ? NextActivityId : "<none>")}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionActivityCommandKind
    {
        Unknown = 0,
        StartActivity = 1,
        CompleteActivationWindow = 2,
        CompleteDeactivationWindow = 3,
        CompleteCurrentActivity = 4,
        ContinueToNextActivity = 5,
        GoToNextActivity = 6,
        GoToPreviousActivity = 7,
        RestartCurrentActivity = 8,
        GoToActivity = 9,
        PauseRequested = 10,
        ResumeRequested = 11,
        PauseSimulation = 12,
        ResumeSimulation = 13,
        CloseForRouteExit = 14,
    }

    public enum SessionActivityPendingOperationKind
    {
        Unknown = 0,
        CompleteActivationWindow = 1,
        CompleteCurrentActivity = 2,
        CompleteDeactivationWindow = 3,
        ContinueToNextActivity = 4,
        ActivationWindowSceneLoad = 10,
        ActivationWindowSceneUnload = 11,
        DeactivationWindowSceneLoad = 12,
        DeactivationWindowSceneUnload = 13,
        ActivityContentSceneLoad = 20,
        ActivityContentSceneUnload = 21,
    }

    public enum SessionActivityPendingWindowKind
    {
        None = 0,
        ActivationWindow = 1,
        DeactivationWindow = 2,
    }

    public enum SessionActivityRailKind
    {
        None = 0,
        ActivityEntryRail = 1,
        ActivityCompletionRail = 2,
        ActivityRestartRail = 3,
        ActivityNavigationRail = 4,
        ActivityRouteExitRail = 5,
    }

    public enum SessionActivityRailStatus
    {
        None = 0,
        Requested = 1,
        Started = 2,
        InProgress = 3,
        BlockedOnPendingOperation = 4,
        Completed = 5,
        Failed = 6,
    }

    public readonly struct SessionActivityPendingOperation
    {
        public SessionActivityPendingOperation(
            string operationId,
            string pipelineId,
            string sessionStateId,
            string activityId,
            int activityOrdinal,
            int entrySequence,
            SessionActivityPendingWindowKind windowKind,
            SessionActivityPendingOperationKind operationKind,
            string sceneKey,
            string sceneName,
            string source,
            string reason)
        {
            OperationId = Normalize(operationId);
            PipelineId = Normalize(pipelineId);
            SessionStateId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal;
            EntrySequence = entrySequence;
            WindowKind = windowKind;
            OperationKind = operationKind;
            SceneKey = Normalize(sceneKey);
            SceneName = Normalize(sceneName);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string OperationId { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public SessionActivityPendingWindowKind WindowKind { get; }
        public SessionActivityPendingOperationKind OperationKind { get; }
        public string SceneKey { get; }
        public string SceneName { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(OperationId) &&
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            OperationKind != SessionActivityPendingOperationKind.Unknown;

        public override string ToString()
        {
            return $"operationId='{OperationId}', pipelineId='{PipelineId}', sessionStateId='{SessionStateId}', activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', entrySequence='{EntrySequence}', windowKind='{WindowKind}', operationKind='{OperationKind}', sceneKey='{(string.IsNullOrWhiteSpace(SceneKey) ? "<none>" : SceneKey)}', sceneName='{(string.IsNullOrWhiteSpace(SceneName) ? "<none>" : SceneName)}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivityCommand
    {
        public SessionActivityCommand(
            SessionActivityCommandKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string targetActivityId = null)
        {
            Kind = kind;
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
            TargetActivityId = Normalize(targetActivityId);
        }

        public SessionActivityCommandKind Kind { get; }
        public SessionActivityIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }
        public string TargetActivityId { get; }

        public bool IsValid =>
            Kind != SessionActivityCommandKind.Unknown &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            (Kind != SessionActivityCommandKind.GoToActivity || !string.IsNullOrWhiteSpace(TargetActivityId));

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', source='{Source}', reason='{Reason}', targetActivityId='{(string.IsNullOrWhiteSpace(TargetActivityId) ? "<none>" : TargetActivityId)}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionActivityFactKind
    {
        Unknown = 0,
        PipelineStarted = 1,
        ActivityActivationStarted = 2,
        ActivationWindowStarted = 3,
        ActivationWindowAdditiveSceneLoadStarted = 4,
        ActivationWindowAdditiveSceneLoaded = 5,
        ActivationWindowReady = 6,
        ActivationWindowCompleted = 7,
        ActivationWindowAdditiveSceneUnloadStarted = 8,
        ActivationWindowAdditiveSceneUnloaded = 9,
        ActivationWindowSkippedNoContent = 10,
        ActivityRunningEntered = 11,
        GameplayContentSkippedNoContent = 12,
        ActivityCompletionRequested = 13,
        ActivityCompleting = 14,
        DeactivationWindowStarted = 15,
        DeactivationWindowAdditiveSceneLoadStarted = 16,
        DeactivationWindowAdditiveSceneLoaded = 17,
        DeactivationWindowReady = 18,
        DeactivationWindowCompleted = 19,
        DeactivationWindowAdditiveSceneUnloadStarted = 20,
        DeactivationWindowAdditiveSceneUnloaded = 21,
        DeactivationWindowSkippedNoContent = 22,
        ActivityDeactivated = 23,
        ContinueAccepted = 24,
        ActivityHandoffPrepared = 25,
        PipelineCompleted = 27,
        CommandRejected = 28,
        SimulationPaused = 29,
        SimulationResumed = 30,
        PauseResolved = 31,
        ResumeResolved = 32,
        PauseRejected = 33,
        ResumeRejected = 34,
        ActivityCatalogLooped = 35,
        ActivityNavigationExitRequested = 36,
        ActivityRouteExitRequested = 37,
        ActivityRouteExitCompleted = 38,
        ActivityTransitionProfileSelected = 39,
        ActivityTransitionProfileResolved = 40,
        ActivitySetupStarted = 41,
        ActivitySetupSkippedNoContent = 42,
        ActivitySetupCompleted = 43,
        NextActivitySetupStarted = 44,
        NextActivitySetupSkippedNoContent = 45,
        NextActivitySetupCompleted = 46,
        ActivityTransitionFadeInStarted = 47,
        ActivityTransitionFadeInCompleted = 48,
        ActivityTransitionFadeOutStarted = 49,
        ActivityTransitionFadeOutCompleted = 50,
        ActivityTransitionLoadingStarted = 51,
        ActivityTransitionLoadingProgress = 52,
        ActivityTransitionLoadingCompleted = 53,
        ActivityTransitionLoadingHidden = 54,
        ActivityTransitionLoadingSkippedNoContent = 55,
        ActivitySceneContractObserved = 56,
        ActivitySceneContractValidated = 57,
        ActivitySceneContractSkippedNoContent = 58,
        ActivityTransitionCompleted = 59,
        ActivityRestartRequested = 60,
        ActivityRestartAccepted = 61,
        ActivityRestartTeardownStarted = 62,
        ActivityRestartSetupStarted = 63,
        ActivityRestartCompleted = 64,
        ActivityRestartRejected = 65,
        PlayerActorMaterializationCommandIssued = 71,
        PlayerActorMaterialized = 72,
        PlayerActorReadyMaterializedOnly = 73,
        PlayerActorParticipationExitStageStarted = 75,
        PlayerActorParticipationExitCommandIssued = 76,
        PlayerActorParticipationExited = 77,
        PlayerActorRetainedForRoute = 78,
        PlayerActorParticipationExitStageCompleted = 79,
        PlayerActorRetainedForRouteFound = 80,
        PlayerActorParticipationEnterCommandIssued = 81,
        PlayerActorParticipationEntered = 82,
        PlayerActorReadyRetainedForActivity = 83,
        PlayerActorResetCommandIssued = 84,
        PlayerActorResetApplied = 85,
        ActivityContentProfileResolved = 86,
        ActivityContentLoadStarted = 87,
        ActivityContentSceneLoadCommandIssued = 88,
        ActivityContentSceneLoaded = 89,
        ActivityContentLoadedSetReady = 90,
        ActivityContentLoadSkippedNoContent = 91,
        ActivityContentLoadFailed = 92,
        ActivityContentSceneLoadRejected = 93,
        ActivityContentRetentionPlanResolved = 116,
        ActivityContentReleaseStarted = 117,
        ActivityContentSceneUnloadCommandIssued = 118,
        ActivityContentSceneUnloaded = 119,
        ActivityContentReleaseSkippedNoContent = 120,
        ActivityContentReleaseCompleted = 121,
        ActivityContentReleaseFailed = 122,
        ActivityContentSceneUnloadRejected = 123,
        ActivityObjectContributorDiscoveryStarted = 124,
        ActivityObjectContributorDiscovered = 125,
        ActivityObjectContributorDiscoverySkippedNoContent = 126,
        ActivityObjectContributorDiscoveryCompleted = 127,
        ActivityObjectContributorDiscoveryFailed = 128,
        ObjectResetStarted = 129,
        ObjectResetCommandIssued = 130,
        ObjectResetApplied = 131,
        ObjectResetSkippedOptional = 132,
        ObjectResetFailed = 133,
        ObjectResetCompleted = 134,
        ObjectReleaseStarted = 135,
        ObjectReleaseCommandIssued = 136,
        ObjectReleaseApplied = 137,
        ObjectReleaseSkippedOptional = 138,
        ObjectReleaseFailed = 139,
        ObjectReleaseCompleted = 140,
        ObjectReleaseRejectedForeignOrStale = 141,
        ActivityObjectContributorUnregisterStarted = 142,
        ActivityObjectContributorUnregistered = 143,
        ActivityObjectContributorUnregisterSkippedNoContributors = 144,
        ActivityObjectContributorUnregisterCompleted = 145,
        ActivityObjectContributorUnregisterFailed = 146,
        ActivityObjectSnapshotCaptureStarted = 147,
        ActivityObjectSnapshotCaptured = 148,
        ActivityObjectSnapshotCaptureSkippedNoProviders = 149,
        ActivityObjectSnapshotCaptureFailed = 150,
        ActivityObjectSnapshotCaptureCompleted = 151,
        ActivityObjectSnapshotRestoreStarted = 152,
        ActivityObjectSnapshotRestoreApplied = 153,
        ActivityObjectSnapshotRestoreSkippedNoPayload = 154,
        ActivityObjectSnapshotRestoreSkippedNoMatchingTarget = 155,
        ActivityObjectSnapshotRestoreSkippedNoEndpointOptional = 156,
        ActivityObjectSnapshotRestoreFailed = 157,
        ActivityObjectSnapshotRestoreCompleted = 158,
        ActivityObjectSnapshotContractValidationStarted = 159,
        ActivityObjectSnapshotContractValidated = 160,
        ActivityObjectSnapshotContractSkippedOptional = 161,
        ActivityObjectSnapshotContractFailed = 162,
        ActivityObjectSnapshotContractValidationCompleted = 163,
        PlayerActorReadinessStarted = 164,
        PlayerActorReadinessSkippedNoRequiredParticipant = 165,
        PlayerActorReadinessFailed = 166,
        PlayerActorReadinessCompleted = 167,
        PlayerInputBindingStarted = 168,
        PlayerInputBindingCommandIssued = 169,
        PlayerInputBound = 170,
        PlayerInputBindingSkippedNoRequiredInput = 171,
        PlayerInputBindingFailed = 172,
        PlayerInputBindingCompleted = 173,
        MovementBindingStarted = 174,
        MovementBindingCommandIssued = 175,
        PlayerMovementBound = 176,
        MovementBindingSkippedNoRequiredMovement = 177,
        MovementBindingFailed = 178,
        MovementBindingCompleted = 179,
        MovementControlEnabled = 180,
        MovementControlDisabled = 181,
        MovementBindingRetained = 182,
        MovementControlEnableSkippedNoTarget = 183,
        MovementControlDisableSkippedNoTarget = 184,
        CameraBindingStarted = 185,
        PlayerCameraEndpointResolved = 186,
        ActivityCameraTargetBound = 187,
        CameraBindingSkippedNoRequiredCamera = 188,
        CameraBindingFailed = 189,
        CameraBindingCompleted = 190,
        ActorPresentationSetupStarted = 191,
        ActorPresentationPlanResolved = 192,
        ActorPresentationMaterialized = 193,
        ActorPresentationReady = 194,
        ActorPresentationSetupSkippedOptional = 195,
        ActorPresentationSetupFailed = 196,
        ActorPresentationSetupCompleted = 197,
        ActorPresentationReleaseStarted = 198,
        ActorPresentationReleased = 199,
        ActorPresentationReleaseSkipped = 200,
        ActorPresentationReleaseFailed = 201,
        ActorPresentationReleaseCompleted = 202,
        ActorPresentationRetained = 203,
        ActorPresentationRetentionSkipped = 204,
        ActorPresentationRetentionFailed = 205,
        ActorPresentationResetSkipped = 206,
        ActivitySetupInventoryBuildStarted = 94,
        ActivitySetupInventoryBuilt = 95,
        ActivitySetupInventorySkippedNoRequirements = 96,
        ActivitySetupInventoryValidated = 97,
        ActivitySetupInventoryValidationFailed = 98,
        ActivityParticipantBindingStarted = 99,
        ActivityParticipantBindingSkippedNoRequirements = 100,
        ActivityParticipantRequirementDeclared = 101,
        ActivityParticipantBindingCompleted = 102,
        ActivityParticipantBindingFailed = 103,
        ActivityParticipantBindingResolutionStarted = 104,
        ActivityParticipantBindingResolved = 105,
        ActivityParticipantCommandPlanReady = 106,
        ActivityParticipantBindCommandIssued = 107,
        ActivityParticipantMaterializationCommandIssued = 108,
        ActivityParticipantPlacementCommandIssued = 109,
        ActivityParticipantResetCommandIssued = 110,
        ActivityParticipantBindApplied = 111,
        ActivityParticipantMaterialized = 112,
        ActivityParticipantPlacementApplied = 113,
        ActivityParticipantResetApplied = 114,
        ActivityParticipantSetupFailed = 115,
    }

    public readonly struct SessionActivityFact
    {
        public SessionActivityFact(
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string message,
            SessionActivityHandoff handoff)
        {
            Kind = kind;
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
            Handoff = handoff;
        }

        public SessionActivityFactKind Kind { get; }
        public SessionActivityIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }
        public SessionActivityHandoff Handoff { get; }

        public bool IsValid =>
            Kind != SessionActivityFactKind.Unknown &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivitySnapshot
    {
        public SessionActivitySnapshot(
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            SessionActivityHandoff handoff,
            string source,
            string reason,
            string message)
        {
            Identity = identity;
            Definition = definition;
            Handoff = handoff;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityDefinition Definition { get; }
        public SessionActivityHandoff Handoff { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Definition.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', definition='{Definition}', handoff='{Handoff}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionActivityCommandResultKind
    {
        Unknown = 0,
        Rejected = 1,
        Started = 2,
        InProgress = 3,
        Completed = 4,
        Failed = 5,
        SkippedNoContent = 6,
    }

    public readonly struct SessionActivityCommandResult
    {
        public SessionActivityCommandResult(
            SessionActivityCommandResultKind kind,
            SessionActivityCommand command,
            IReadOnlyList<SessionActivityFact> facts,
            string reason)
        {
            Kind = kind;
            Command = command;
            Facts = facts ?? Array.Empty<SessionActivityFact>();
            Reason = Normalize(reason);
        }

        public SessionActivityCommandResultKind Kind { get; }
        public SessionActivityCommand Command { get; }
        public IReadOnlyList<SessionActivityFact> Facts { get; }
        public string Reason { get; }

        public bool IsValid =>
            Kind != SessionActivityCommandResultKind.Unknown &&
            Command.IsValid;

        public bool IsRejected => Kind == SessionActivityCommandResultKind.Rejected;
        public bool IsStarted => Kind == SessionActivityCommandResultKind.Started;
        public bool IsInProgress => Kind == SessionActivityCommandResultKind.InProgress;
        public bool IsCompleted => Kind == SessionActivityCommandResultKind.Completed;
        public bool IsFailed => Kind == SessionActivityCommandResultKind.Failed;
        public bool IsSkippedNoContent => Kind == SessionActivityCommandResultKind.SkippedNoContent;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivityHandoff
    {
        public SessionActivityHandoff(
            SessionActivityIdentity fromIdentity,
            SessionActivityIdentity toIdentity,
            string nextActivityId,
            string source,
            string reason)
        {
            FromIdentity = fromIdentity;
            ToIdentity = toIdentity;
            NextActivityId = Normalize(nextActivityId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity FromIdentity { get; }
        public SessionActivityIdentity ToIdentity { get; }
        public string NextActivityId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            FromIdentity.IsValid &&
            ToIdentity.IsValid &&
            !string.IsNullOrWhiteSpace(NextActivityId) &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"from='{FromIdentity}', to='{ToIdentity}', nextActivityId='{NextActivityId}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionActivityPauseOverlayAdapter
    {
        void Show(SessionActivityIdentity identity, string source, string reason);
        void Hide(SessionActivityIdentity identity, string source, string reason);
    }

    public enum SessionActivityInputModeKind
    {
        Unknown = 0,
        ActivityGameplay = 1,
        PauseOverlay = 2,
        Disabled = 3,
    }

    public readonly struct SessionActivityInputModeCommand
    {
        public SessionActivityInputModeCommand(
            SessionActivityInputModeKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            Kind = kind;
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityInputModeKind Kind { get; }
        public SessionActivityIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Kind != SessionActivityInputModeKind.Unknown &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivityInputModeObservation
    {
        public SessionActivityInputModeObservation(
            SessionActivityInputModeCommand command,
            string fact,
            string snapshot,
            string outcome)
        {
            Command = command;
            Fact = Normalize(fact);
            Snapshot = Normalize(snapshot);
            Outcome = Normalize(outcome);
        }

        public SessionActivityInputModeCommand Command { get; }
        public string Fact { get; }
        public string Snapshot { get; }
        public string Outcome { get; }

        public bool IsValid =>
            Command.IsValid &&
            !string.IsNullOrWhiteSpace(Fact) &&
            !string.IsNullOrWhiteSpace(Snapshot) &&
            !string.IsNullOrWhiteSpace(Outcome);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionActivityInputModeAdapter
    {
        SessionActivityInputModeObservation Apply(SessionActivityInputModeCommand command);
    }

    public readonly struct SessionActivityTransitionResolution
    {
        public SessionActivityTransitionResolution(
            ActivityTransitionMode mode,
            SceneTransitionProfile fadeProfile,
            RuntimeLoadingProfileAsset loadingProfile,
            string resolvedFadeProfileSource,
            string resolvedLoadingProfileSource)
        {
            Mode = mode;
            FadeProfile = fadeProfile;
            LoadingProfile = loadingProfile;
            ResolvedFadeProfileSource = Normalize(resolvedFadeProfileSource);
            ResolvedLoadingProfileSource = Normalize(resolvedLoadingProfileSource);
        }

        public ActivityTransitionMode Mode { get; }
        public SceneTransitionProfile FadeProfile { get; }
        public RuntimeLoadingProfileAsset LoadingProfile { get; }
        public string ResolvedFadeProfileSource { get; }
        public string ResolvedLoadingProfileSource { get; }

        public bool HasFadeProfile => FadeProfile != null;
        public bool HasLoadingProfile => LoadingProfile != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivitySceneContractContributorEntry
    {
        public ActivitySceneContractContributorEntry(string contributorId)
        {
            ContributorId = Normalize(contributorId);
        }

        public string ContributorId { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(ContributorId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivitySceneContractSnapshot
    {
        public ActivitySceneContractSnapshot(
            string activitySceneId,
            ActivitySceneDiscoveryMode discoveryMode,
            ActivitySceneRevealSafety revealSafety,
            bool allowUndeclaredContributors,
            IReadOnlyList<ActivitySceneContractContributorEntry> declaredContributors)
        {
            ActivitySceneId = Normalize(activitySceneId);
            DiscoveryMode = discoveryMode;
            RevealSafety = revealSafety;
            AllowUndeclaredContributors = allowUndeclaredContributors;
            DeclaredContributors = declaredContributors ?? Array.Empty<ActivitySceneContractContributorEntry>();
        }

        public string ActivitySceneId { get; }
        public ActivitySceneDiscoveryMode DiscoveryMode { get; }
        public ActivitySceneRevealSafety RevealSafety { get; }
        public bool AllowUndeclaredContributors { get; }
        public IReadOnlyList<ActivitySceneContractContributorEntry> DeclaredContributors { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ActivitySceneId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionActivityTransitionAdapter
    {
        Task CloseCurtainAsync(
            SessionActivityIdentity identity,
            SessionActivityTransitionResolution resolution,
            string source,
            string reason);

        Task OpenCurtainAsync(
            SessionActivityIdentity identity,
            SessionActivityTransitionResolution resolution,
            string source,
            string reason);
    }

    public interface ISessionActivityTransitionLoadingAdapter
    {
        Task StartAsync(
            SessionActivityIdentity identity,
            SessionActivityTransitionResolution resolution,
            string source,
            string reason);

        Task ReportProgressAsync(
            SessionActivityIdentity identity,
            SessionActivityTransitionResolution resolution,
            float normalizedProgress,
            string stepLabel,
            string message,
            string source,
            string reason);

        Task CompleteAsync(
            SessionActivityIdentity identity,
            SessionActivityTransitionResolution resolution,
            string source,
            string reason);

        Task HideAsync(
            SessionActivityIdentity identity,
            SessionActivityTransitionResolution resolution,
            string source,
            string reason);
    }

    public interface ISessionActivityWindowSceneAdapter
    {
        Task LoadAdditiveAsync(
            SceneKeyAsset sceneKey,
            string activityId,
            string windowKind,
            string source,
            string reason);

        Task UnloadAsync(
            SceneKeyAsset sceneKey,
            string activityId,
            string windowKind,
            string source,
            string reason);
    }

    public interface ISessionActivityPendingOperationCallback
    {
        void CompletePendingOperation(SessionActivityPendingOperation operation, string source, string reason);
        void CompleteActivityContentSceneUnloadOperation(SessionActivityPendingOperation operation, ActivityContentSceneUnloadResult unloadResult);
        void FailPendingOperation(SessionActivityPendingOperation operation, string source, string reason, string error);
    }

    public interface ISessionActivityPendingOperationRunner
    {
        void RunWindowOperation(
            SessionActivityPendingOperation operation,
            SceneKeyAsset sceneKey,
            ISessionActivityPendingOperationCallback callback);

        void RunActivityContentOperation(
            SessionActivityPendingOperation operation,
            ActivityContentSceneLoadCommand command,
            ISessionActivityPendingOperationCallback callback);

        void RunActivityContentReleaseOperation(
            SessionActivityPendingOperation operation,
            ActivityContentSceneUnloadCommand command,
            ISessionActivityPendingOperationCallback callback);
    }
}
