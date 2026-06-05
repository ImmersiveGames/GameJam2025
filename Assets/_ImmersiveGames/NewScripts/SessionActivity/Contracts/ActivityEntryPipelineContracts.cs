using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;
using PlayerActivityParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipationContext;
using PlayerSessionParticipantId = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipantId;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct ActivityEntryPreparationCommand
    {
        public ActivityEntryPreparationCommand(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryCommand
    {
        public ActivityEntryCommand(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal >= 0 &&
            string.Equals(Identity.ActivityId, ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum ActivityEntryPreparationResultKind
    {
        Unknown = 0,
        Prepared = 1,
        Failed = 2,
        RejectedStaleOrForeign = 3
    }

    public readonly struct ActivityEntryPreparationResult
    {
        public ActivityEntryPreparationResult(
            bool prepared,
            SessionActivityIdentity identity,
            string reason)
            : this(prepared ? ActivityEntryPreparationResultKind.Prepared : ActivityEntryPreparationResultKind.Failed, identity, reason)
        {
        }

        public ActivityEntryPreparationResult(
            ActivityEntryPreparationResultKind kind,
            SessionActivityIdentity identity,
            string reason)
        {
            Kind = kind;
            Identity = identity;
            Reason = Normalize(reason);
        }

        public ActivityEntryPreparationResultKind Kind { get; }
        public bool Prepared => Kind == ActivityEntryPreparationResultKind.Prepared;
        public SessionActivityIdentity Identity { get; }
        public string Reason { get; }

        public bool IsValid => Kind != ActivityEntryPreparationResultKind.Unknown && Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum ActivityEntrySetupReadinessResultKind
    {
        Unknown = 0,
        Completed = 1,
        SkippedNoContent = 2,
        Failed = 3,
        RejectedStaleOrForeign = 4
    }

    public readonly struct ActivityEntrySetupReadinessResult
    {
        public ActivityEntrySetupReadinessResult(
            bool completed,
            SessionActivityIdentity identity,
            string reason)
            : this(completed ? ActivityEntrySetupReadinessResultKind.Completed : ActivityEntrySetupReadinessResultKind.Failed, identity, reason)
        {
        }

        public ActivityEntrySetupReadinessResult(
            ActivityEntrySetupReadinessResultKind kind,
            SessionActivityIdentity identity,
            string reason)
        {
            Kind = kind;
            Identity = identity;
            Reason = Normalize(reason);
        }

        public ActivityEntrySetupReadinessResultKind Kind { get; }
        public bool Completed => Kind == ActivityEntrySetupReadinessResultKind.Completed;
        public bool IsTerminalSuccess => Kind == ActivityEntrySetupReadinessResultKind.Completed || Kind == ActivityEntrySetupReadinessResultKind.SkippedNoContent;
        public SessionActivityIdentity Identity { get; }
        public string Reason { get; }

        public bool IsValid => Kind != ActivityEntrySetupReadinessResultKind.Unknown && Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryContentLoadCommand
    {
        public ActivityEntryContentLoadCommand(ActivityContentLoadPlan plan)
        {
            Plan = plan;
        }

        public ActivityContentLoadPlan Plan { get; }
        public SessionActivityIdentity Identity => Plan.Identity;
        public string ActivityId => Plan.ActivityId;
        public int ActivityOrdinal => Plan.ActivityOrdinal;
        public ActivityContentMode ActivityContentMode => Plan.ActivityContentMode;
        public string ActivityContentProfileId => Plan.ActivityContentProfileId;
        public IReadOnlyList<ActivityContentLoadPlanScene> Scenes => Plan.Scenes;
        public string Source => Plan.Source;
        public string Reason => Plan.Reason;
        public bool IsValid => Plan.IsValid;
    }

    public readonly struct ActivityEntryContentLoadCompletionCommand
    {
        public ActivityEntryContentLoadCompletionCommand(
            SessionActivityIdentity activeIdentity,
            string activityId,
            int activityOrdinal,
            SessionActivityPendingOperation operation,
            string source,
            string reason)
        {
            ActiveIdentity = activeIdentity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal;
            Operation = operation;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity ActiveIdentity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public SessionActivityPendingOperation Operation { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid =>
            ActiveIdentity.IsValid &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            Operation.IsValid &&
            string.Equals(ActiveIdentity.ActivityId, ActivityId, StringComparison.Ordinal) &&
            ActiveIdentity.ActivityOrdinal == ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryContentLoadFailureCommand
    {
        public ActivityEntryContentLoadFailureCommand(
            SessionActivityIdentity activeIdentity,
            string activityId,
            int activityOrdinal,
            SessionActivityPendingOperation operation,
            string source,
            string reason,
            string error)
        {
            ActiveIdentity = activeIdentity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal;
            Operation = operation;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Error = Normalize(error);
        }

        public SessionActivityIdentity ActiveIdentity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public SessionActivityPendingOperation Operation { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Error { get; }
        public bool IsValid =>
            ActiveIdentity.IsValid &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            Operation.IsValid &&
            string.Equals(ActiveIdentity.ActivityId, ActivityId, StringComparison.Ordinal) &&
            ActiveIdentity.ActivityOrdinal == ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryContentLoadResult
    {
        public ActivityEntryContentLoadResult(
            bool shouldContinueEntry,
            bool pendingOperationIssued,
            string reason)
        {
            ShouldContinueEntry = shouldContinueEntry;
            PendingOperationIssued = pendingOperationIssued;
            Reason = Normalize(reason);
        }

        public bool ShouldContinueEntry { get; }
        public bool PendingOperationIssued { get; }
        public string Reason { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityObjectSetupInventoryPlan
    {
        public ActivityObjectSetupInventoryPlan(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            ActivitySetupRequirementsAuthoring setupRequirements,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            SetupRequirements = setupRequirements;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public ActivitySetupRequirementsAuthoring SetupRequirements { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            string.Equals(Identity.ActivityId, ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryObjectSetupCommand
    {
        public ActivityEntryObjectSetupCommand(
            SessionActivityIdentity identity,
            ActivityObjectSetupInventoryPlan setupInventoryPlan,
            ActivityObjectResetRestorePlan resetRestorePlan)
        {
            Identity = identity;
            SetupInventoryPlan = setupInventoryPlan;
            ResetRestorePlan = resetRestorePlan;
        }

        public SessionActivityIdentity Identity { get; }
        public ActivityObjectSetupInventoryPlan SetupInventoryPlan { get; }
        public ActivityObjectResetRestorePlan ResetRestorePlan { get; }
        public ActivityObjectSetupInventoryPlan Plan => SetupInventoryPlan;
        public string ActivityId => SetupInventoryPlan.ActivityId;
        public int ActivityOrdinal => SetupInventoryPlan.ActivityOrdinal;
        public string Source => SetupInventoryPlan.Source;
        public string Reason => SetupInventoryPlan.Reason;
        public ActivitySetupRequirementsAuthoring SetupRequirements => SetupInventoryPlan.SetupRequirements;

        public bool IsValid =>
            Identity.IsValid &&
            SetupInventoryPlan.IsValid &&
            ResetRestorePlan.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            string.Equals(Identity.ActivityId, SetupInventoryPlan.ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == SetupInventoryPlan.ActivityOrdinal &&
            string.Equals(Identity.ActivityId, ResetRestorePlan.ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == ResetRestorePlan.ActivityOrdinal &&
            string.Equals(SetupInventoryPlan.ActivityId, ResetRestorePlan.ActivityId, StringComparison.Ordinal) &&
            SetupInventoryPlan.ActivityOrdinal == ResetRestorePlan.ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct ActivityObjectResetRestorePlan
    {
        public ActivityObjectResetRestorePlan(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            string.Equals(Identity.ActivityId, ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryObjectSetupResult
    {
        public ActivityEntryObjectSetupResult(
            bool completed,
            SessionActivityIdentity identity,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryObjectSnapshotRestorePayloadContext
    {
        public ActivityEntryObjectSnapshotRestorePayloadContext(LoadedSessionActivitySnapshotPayload payload)
        {
            Payload = payload;
        }

        public LoadedSessionActivitySnapshotPayload Payload { get; }
        public bool HasPayload => Payload.IsValid;
        public bool IsValid => HasPayload;
    }


    public readonly struct ActivityEntryActorPresentationSetupCommand
    {
        public ActivityEntryActorPresentationSetupCommand(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryActorPresentationSetupResult
    {
        public ActivityEntryActorPresentationSetupResult(
            bool completed,
            SessionActivityIdentity identity,
            int total,
            int resolved,
            int materialized,
            int retained,
            int skipped,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            Total = total < 0 ? 0 : total;
            Resolved = resolved < 0 ? 0 : resolved;
            Materialized = materialized < 0 ? 0 : materialized;
            Retained = retained < 0 ? 0 : retained;
            Skipped = skipped < 0 ? 0 : skipped;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int Total { get; }
        public int Resolved { get; }
        public int Materialized { get; }
        public int Retained { get; }
        public int Skipped { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryActorAttributeSetupCommand
    {
        public ActivityEntryActorAttributeSetupCommand(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryActorAttributeSetupResult
    {
        public ActivityEntryActorAttributeSetupResult(
            bool completed,
            SessionActivityIdentity identity,
            int total,
            int resolved,
            int ready,
            int skipped,
            int failed,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            Total = total < 0 ? 0 : total;
            Resolved = resolved < 0 ? 0 : resolved;
            Ready = ready < 0 ? 0 : ready;
            Skipped = skipped < 0 ? 0 : skipped;
            Failed = failed < 0 ? 0 : failed;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int Total { get; }
        public int Resolved { get; }
        public int Ready { get; }
        public int Skipped { get; }
        public int Failed { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }


    public readonly struct ActivityEntryActorParticipationEnterCommand
    {
        public ActivityEntryActorParticipationEnterCommand(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            string.Equals(Identity.ActivityId, ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryActorParticipationEnterResult
    {
        public ActivityEntryActorParticipationEnterResult(
            bool completed,
            SessionActivityIdentity identity,
            int total,
            int entered,
            int skipped,
            int failed,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            Total = total < 0 ? 0 : total;
            Entered = entered < 0 ? 0 : entered;
            Skipped = skipped < 0 ? 0 : skipped;
            Failed = failed < 0 ? 0 : failed;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int Total { get; }
        public int Entered { get; }
        public int Skipped { get; }
        public int Failed { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }


    public readonly struct ActivityEntryPlayerInputBindingReference
    {
        public ActivityEntryPlayerInputBindingReference(
            string requirementId,
            ActivityParticipantRequirementKind participantKind,
            PlayerActivityParticipantBinding participantBinding,
            bool required)
        {
            RequirementId = Normalize(requirementId);
            ParticipantKind = participantKind;
            ParticipantBinding = participantBinding;
            Required = required;
        }

        public string RequirementId { get; }
        public ActivityParticipantRequirementKind ParticipantKind { get; }
        public PlayerActivityParticipantBinding ParticipantBinding { get; }
        public bool Required { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
            ParticipantBinding.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryPlayerInputBindingCommand
    {
        public ActivityEntryPlayerInputBindingCommand(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            IReadOnlyList<ActivityEntryPlayerInputBindingReference> participantBindings,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            ParticipantBindings = participantBindings ?? Array.Empty<ActivityEntryPlayerInputBindingReference>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public IReadOnlyList<ActivityEntryPlayerInputBindingReference> ParticipantBindings { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            Identity.ActivityId == ActivityId &&
            Identity.ActivityOrdinal == ActivityOrdinal &&
            ParticipantBindings != null &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryPlayerInputBindingResult
    {
        public ActivityEntryPlayerInputBindingResult(
            bool completed,
            SessionActivityIdentity identity,
            int requiredCount,
            int requiredBoundCount,
            int totalBoundCount,
            bool skipped,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            RequiredCount = requiredCount < 0 ? 0 : requiredCount;
            RequiredBoundCount = requiredBoundCount < 0 ? 0 : requiredBoundCount;
            TotalBoundCount = totalBoundCount < 0 ? 0 : totalBoundCount;
            Skipped = skipped;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int RequiredCount { get; }
        public int RequiredBoundCount { get; }
        public int TotalBoundCount { get; }
        public bool Skipped { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }


    public readonly struct ActivityEntryPermissionTargetPreparationCommand
    {
        public ActivityEntryPermissionTargetPreparationCommand(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            bool registerReceivers,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            RegisterReceivers = registerReceivers;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public bool RegisterReceivers { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            Identity.ActivityId == ActivityId &&
            Identity.ActivityOrdinal == ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryPermissionTargetPreparationResult
    {
        public ActivityEntryPermissionTargetPreparationResult(
            bool completed,
            SessionActivityIdentity identity,
            int receiverCount,
            bool skipped,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            ReceiverCount = receiverCount < 0 ? 0 : receiverCount;
            Skipped = skipped;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int ReceiverCount { get; }
        public bool Skipped { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryMovementBindingReference
    {
        public ActivityEntryMovementBindingReference(
            string requirementId,
            ActivityParticipantRequirementKind participantKind,
            PlayerActivityParticipantBinding participantBinding,
            bool required)
        {
            RequirementId = Normalize(requirementId);
            ParticipantKind = participantKind;
            ParticipantBinding = participantBinding;
            Required = required;
        }

        public string RequirementId { get; }
        public ActivityParticipantRequirementKind ParticipantKind { get; }
        public PlayerActivityParticipantBinding ParticipantBinding { get; }
        public bool Required { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
            ParticipantBinding.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryMovementBindingCommand
    {
        public ActivityEntryMovementBindingCommand(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            IReadOnlyList<ActivityEntryMovementBindingReference> participantBindings,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            ParticipantBindings = participantBindings ?? Array.Empty<ActivityEntryMovementBindingReference>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public IReadOnlyList<ActivityEntryMovementBindingReference> ParticipantBindings { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            Identity.ActivityId == ActivityId &&
            Identity.ActivityOrdinal == ActivityOrdinal &&
            ParticipantBindings != null &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryMovementBindingResult
    {
        public ActivityEntryMovementBindingResult(
            bool completed,
            SessionActivityIdentity identity,
            int requiredCount,
            int requiredBoundCount,
            int totalBoundCount,
            int retainedCount,
            bool retainedExistingBinding,
            bool skipped,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            RequiredCount = requiredCount < 0 ? 0 : requiredCount;
            RequiredBoundCount = requiredBoundCount < 0 ? 0 : requiredBoundCount;
            TotalBoundCount = totalBoundCount < 0 ? 0 : totalBoundCount;
            RetainedCount = retainedCount < 0 ? 0 : retainedCount;
            RetainedExistingBinding = retainedExistingBinding;
            Skipped = skipped;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int RequiredCount { get; }
        public int RequiredBoundCount { get; }
        public int TotalBoundCount { get; }
        public int RetainedCount { get; }
        public bool RetainedExistingBinding { get; }
        public bool Skipped { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryCameraBindingCommand
    {
        public ActivityEntryCameraBindingCommand(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            Identity.ActivityId == ActivityId &&
            Identity.ActivityOrdinal == ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryCameraBindingResult
    {
        public ActivityEntryCameraBindingResult(
            bool completed,
            SessionActivityIdentity identity,
            int requiredCount,
            bool targetBound,
            bool skipped,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            RequiredCount = requiredCount < 0 ? 0 : requiredCount;
            TargetBound = targetBound;
            Skipped = skipped;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int RequiredCount { get; }
        public bool TargetBound { get; }
        public bool Skipped { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActivityEntryIdentityRuntimeBridge : IActivityEntryPipelineBoundary
    {
        SessionActivityIdentity BuildIdentity(SessionActivityDefinition definition, SessionActivityStage stage, int entrySequence);
        void SetCurrentIdentity(SessionActivityIdentity identity, SessionActivityStage stage);
    }

    public interface IActivityEntryFactRuntimeBridge
    {
        void EmitFact(
            List<SessionActivityFact> emittedFacts,
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string message);
        void EmitSnapshot(
            List<SessionActivitySnapshot> emittedSnapshots,
            string snapshotKind,
            string source,
            string reason,
            string message);
    }

    public interface IActivityEntryContentLoadedSetRuntimeBridge
    {
        void SetCurrentActivityContentLoadedSet(ActivityContentLoadedSet loadedSet);
        void ClearCurrentActivityContentLoadedSet();
    }

    public interface IActivityEntryContentPendingOperationRuntimeBridge
    {
        SessionActivityPendingOperation BuildActivityContentPendingOperation(
            ActivityContentLoadPlan plan,
            int entrySequence,
            ActivityContentSceneLoadCommand command);
        void SetPendingOperation(SessionActivityPendingOperation operation);
        void RunActivityContentOperation(
            SessionActivityPendingOperation operation,
            ActivityContentSceneLoadCommand command);
    }

    public interface IActivityEntryContentRuntimeBridge :
        IActivityEntryContentLoadedSetRuntimeBridge,
        IActivityEntryContentPendingOperationRuntimeBridge
    {
    }

    public interface IActivityEntryLogRuntimeBridge
    {
        void LogEntryOwnerEvent(
            string eventName,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string detail = "");

        void LogPhaseBoundary(
            string phaseName,
            SessionActivityIdentity identity,
            string source,
            string reason,
            bool completed = false,
            string detail = "");
    }

    public interface IActivityEntryPreparationRuntimeBridge
    {
        void ResetMovementControlStateForEntry();
        void BeginActivityActorScope(SessionActivityIdentity identity);
        void ClearActiveActorParticipations(string activityId, int entrySequence, string source, string reason);
        bool TryGetActivePlayerActorIdentities(SessionActivityIdentity identity, out IReadOnlyList<PlayerActorIdentityRecord> activeActors);
        void EmitPredefinedVisualSetupReadyFactIfApplicable(
            SessionActivityDefinition definition,
            List<SessionActivityFact> facts,
            SessionActivityIdentity readinessIdentity,
            string source,
            string reason,
            string readinessPoint);

        void ObserveActivitySceneContractOrSkip(
            SessionActivityDefinition definition,
            string source,
            string reason,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence);
    }

    public interface IActivityEntryRuntimeBridge :
        IActivityEntryIdentityRuntimeBridge,
        IActivityEntryFactRuntimeBridge,
        IActivityEntryContentRuntimeBridge,
        IActivityEntryLogRuntimeBridge,
        IActivityEntryPreparationRuntimeBridge
    {
    }

    // Bridge transitoria SA-3B0: expõe apenas state técnico canônico e actor scan targets
    // enquanto o subfluxo é transferido do SessionActivityPipeline para o ActivityEntryPipeline.
    public interface IActivityEntryObjectSetupRuntimeBridge :
        IActivityEntryRuntimeBridge
    {
        ActivityContentLoadedSet GetCurrentActivityContentLoadedSet();
    }

    public interface IActivityEntryActorInventoryRuntimeBridge
    {
    }
    public interface IActivityEntryActorParticipationRuntimeBridge
    {
        ActorParticipationReadinessEvaluation EvaluateActorParticipationReadiness(
            SessionActivityIdentity identity,
            ActorInstanceRecord instance);
    }





    public readonly struct ActivityParticipantBindingPlan
    {
        public ActivityParticipantBindingPlan(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivitySetupStarted &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            string.Equals(Identity.ActivityId, ActivityId, StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == ActivityOrdinal &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryParticipantBindingCommand
    {
        public ActivityEntryParticipantBindingCommand(ActivityParticipantBindingPlan plan)
        {
            Plan = plan;
        }

        public ActivityParticipantBindingPlan Plan { get; }
        public SessionActivityIdentity Identity => Plan.Identity;
        public string ActivityId => Plan.ActivityId;
        public int ActivityOrdinal => Plan.ActivityOrdinal;
        public string Source => Plan.Source;
        public string Reason => Plan.Reason;
        public bool IsValid => Plan.IsValid;
    }

    public readonly struct ActivityEntryParticipantBindingResolvedRecord
    {
        public ActivityEntryParticipantBindingResolvedRecord(
            string requirementId,
            ActivityParticipantRequirementKind participantKind,
            PlayerActivityParticipantBinding participantBinding,
            bool required)
        {
            RequirementId = Normalize(requirementId);
            ParticipantKind = participantKind;
            ParticipantBinding = participantBinding;
            Required = required;
        }

        public string RequirementId { get; }
        public ActivityParticipantRequirementKind ParticipantKind { get; }
        public PlayerActivityParticipantBinding ParticipantBinding { get; }
        public bool Required { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
            ParticipantBinding.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityEntryParticipantBindingResult
    {
        public ActivityEntryParticipantBindingResult(
            SessionActivityIdentity identity,
            int totalRequirements,
            int requiredRequirements,
            int resolvedRequirements,
            int skippedRequirements,
            int requiredResolvedRequirements,
            IReadOnlyList<ActivityEntryParticipantBindingResolvedRecord> resolvedParticipants)
        {
            Identity = identity;
            TotalRequirements = totalRequirements < 0 ? 0 : totalRequirements;
            RequiredRequirements = requiredRequirements < 0 ? 0 : requiredRequirements;
            ResolvedRequirements = resolvedRequirements < 0 ? 0 : resolvedRequirements;
            SkippedRequirements = skippedRequirements < 0 ? 0 : skippedRequirements;
            RequiredResolvedRequirements = requiredResolvedRequirements < 0 ? 0 : requiredResolvedRequirements;
            ResolvedParticipants = resolvedParticipants ?? Array.Empty<ActivityEntryParticipantBindingResolvedRecord>();
        }

        public SessionActivityIdentity Identity { get; }
        public int TotalRequirements { get; }
        public int RequiredRequirements { get; }
        public int ResolvedRequirements { get; }
        public int SkippedRequirements { get; }
        public int RequiredResolvedRequirements { get; }
        public IReadOnlyList<ActivityEntryParticipantBindingResolvedRecord> ResolvedParticipants { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionActivityStage.ActivityParticipantBindingCompleted &&
            RequiredRequirements >= 0 &&
            ResolvedRequirements >= 0 &&
            SkippedRequirements >= 0 &&
            RequiredResolvedRequirements >= 0 &&
            RequiredResolvedRequirements <= RequiredRequirements &&
            ResolvedParticipants != null;
    }

    public interface IActivityEntryPermissionTargetRuntimeBridge
    {
        void BeginPermissionScope(SessionActivityIdentity identity);
        void ReplacePermissionReceivers(IReadOnlyList<ActivityCapabilityPermissionReceiverReference> receivers);
    }


    public interface IActivityEntryMovementBindingRuntimeBridge
    {
        IReadOnlyList<PlayerActorIdentityRecord> ResolveRetainedMovementTargets(SessionActivityIdentity identity);
        void SetMovementControlTargets(IReadOnlyList<PlayerActorIdentityRecord> targets, bool enableAllowed);
    }

    public interface IActivityEntryCameraBindingRuntimeBridge
    {
        bool TryResolvePlayerActorHandle(
            SessionActivityIdentity identity,
            PlayerActivityParticipantBinding binding,
            out PlayerActorRuntimeHandle handle);
    }


    public interface IActivityEntryActorPresentationRuntimeBridge
    {
        bool TryGetActiveActorPresentationHandle(
            ActorPresentationEndpointReference presentationReference,
            out ActorPresentationRuntimeHandle handle);
        void ReleaseActorPresentationBeforeRematerialization(
            SessionActivityIdentity identity,
            string source,
            string reason,
            ActorInstanceId actorInstanceRuntimeId,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
    }

    public interface IActivityEntryPipeline
    {
        PlayerActivityParticipationContext GetCurrentActivityParticipationContext();
        void StoreActivityParticipationContext(PlayerActivityParticipationContext context);
        ActivityObjectContributorDiscoveryResult GetCurrentActivityObjectContributorDiscoveryResult();
        void SetCurrentActivityObjectContributorDiscoveryResult(ActivityObjectContributorDiscoveryResult result);
        void ClearCurrentActivityObjectContributorDiscoveryResult();
        ActorInventoryFeedResult GetCurrentActorInventoryFeedResult();
        void SetCurrentActorInventoryFeedResult(ActorInventoryFeedResult result);
        void ClearCurrentActorInventoryFeedResult();
        ActivitySetupInventory GetCurrentActivitySetupInventory();
        void SetCurrentActivitySetupInventory(ActivitySetupInventory inventory);
        void ClearCurrentActivitySetupInventory();
        ActivityCapabilityInventory GetCurrentActivityCapabilityInventoryPreview();
        ActivityCapabilityInventoryValidationResult GetCurrentActivityCapabilityInventoryPreviewValidation();
        void SetCurrentActivityCapabilityInventoryPreview(
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation);
        void ClearCurrentActivityCapabilityInventoryPreview();
        ActivityEntryPreparationResult PrepareEntry(
            ActivityEntryPreparationCommand command,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext = default);
        ActivityEntrySetupReadinessResult ExecuteSetupAndReadiness(
            ActivityEntryCommand command,
            SessionActivityDefinition definition,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryContentLoadResult BeginContentLoad(
            ActivityEntryContentLoadCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryContentLoadResult CompleteContentLoad(
            ActivityEntryContentLoadCompletionCommand command,
            SessionActivityDefinition definition,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryObjectSetupResult ExecuteSetupInfrastructure(
            ActivityEntryObjectSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryParticipantBindingResult ExecuteParticipantBinding(
            ActivityEntryParticipantBindingCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryObjectSetupResult ExecuteCapabilityObjectSetup(
            ActivityEntryObjectSetupCommand command,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryActorPresentationSetupResult ExecuteActorPresentationSetup(
            ActivityEntryActorPresentationSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryActorAttributeSetupResult ExecuteActorAttributeSetup(
            ActivityEntryActorAttributeSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryActorParticipationEnterResult ExecuteActorParticipationEnter(
            ActivityEntryActorParticipationEnterCommand command,
            SessionActivityDefinition definition,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryPlayerInputBindingResult ExecutePlayerInputBinding(
            ActivityEntryPlayerInputBindingCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryPermissionTargetPreparationResult ExecutePermissionTargetPreparation(
            ActivityEntryPermissionTargetPreparationCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryMovementBindingResult ExecuteMovementBinding(
            ActivityEntryMovementBindingCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        ActivityEntryCameraBindingResult ExecuteCameraBinding(
            ActivityEntryCameraBindingCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots);
        void FailContentLoad(ActivityEntryContentLoadFailureCommand command, SessionActivityDefinition definition, List<SessionActivityFact> facts);
        void ResetState();
    }
}
