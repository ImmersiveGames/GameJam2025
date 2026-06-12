using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct ActivityResetActivityReference
    {
        public ActivityResetActivityReference(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            string.Equals(Identity.ActivityId, ActivityId, System.StringComparison.Ordinal) &&
            Identity.ActivityOrdinal == ActivityOrdinal;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum ActivityResetCompletionKind
    {
        Unknown = 0,
        Applied = 1,
        NoCommands = 2,
        SkippedOptional = 3,
        NoApplicableGroups = 4,
        InventoryInvalidOrStale = 5,
        Failed = 6,
    }

    public readonly struct ActivityResetCommand
    {
        public ActivityResetCommand(
            ActivityResetActivityReference activity,
            string source,
            string reason)
        {
            Activity = activity;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActivityResetActivityReference Activity { get; }
        public SessionActivityIdentity Identity => Activity.Identity;
        public string ActivityId => Activity.ActivityId;
        public int ActivityOrdinal => Activity.ActivityOrdinal;
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Activity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityResetContext
    {
        public ActivityResetContext(
            ActivityObjectContributorDiscoveryResult discoveryResult,
            ActivityCapabilityInventory resetInventory,
            ActivityCapabilityInventoryValidationResult resetInventoryValidation)
        {
            DiscoveryResult = discoveryResult;
            ResetInventory = resetInventory;
            ResetInventoryValidation = resetInventoryValidation;
        }

        public ActivityObjectContributorDiscoveryResult DiscoveryResult { get; }
        public ActivityCapabilityInventory ResetInventory { get; }
        public ActivityCapabilityInventoryValidationResult ResetInventoryValidation { get; }
    }

    public readonly struct ActivityResetResult
    {
        public ActivityResetResult(
            ActivityResetCompletionKind completionKind,
            string completionReason,
            int commandCount,
            int appliedCount,
            int skippedCount,
            int failedCount)
        {
            CompletionKind = completionKind;
            CompletionReason = Normalize(completionReason);
            CommandCount = commandCount < 0 ? 0 : commandCount;
            AppliedCount = appliedCount < 0 ? 0 : appliedCount;
            SkippedCount = skippedCount < 0 ? 0 : skippedCount;
            FailedCount = failedCount < 0 ? 0 : failedCount;
        }

        public ActivityResetCompletionKind CompletionKind { get; }
        public string CompletionReason { get; }
        public int CommandCount { get; }
        public int AppliedCount { get; }
        public int SkippedCount { get; }
        public int FailedCount { get; }

        public bool IsValid => CompletionKind != ActivityResetCompletionKind.Unknown;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
