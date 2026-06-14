using System;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{

    public enum ActivityResetIntent
    {
        Unknown = 0,
        EntryInitialize = 1,
        RuntimeLocalReset = 2,
        RuntimeActivityReset = 3,
        RuntimeActivityTransitionReset = 4,
        RuntimeRouteTransitionReset = 5,
    }

    public enum ActivityResetStateProfileKind
    {
        Unknown = 0,
        InitialState = 1,
        RuntimeLocalState = 2,
        RuntimeActivityState = 3,
        RuntimeActivityTransitionState = 4,
        RuntimeRouteTransitionState = 5,
    }

    public static class ActivityResetIntentProfileDefaults
    {
        public static ActivityResetIntent ResolveRuntimeIntentForBoundary(ActivityResetBoundaryKind boundaryKind)
        {
            return boundaryKind switch
            {
                ActivityResetBoundaryKind.Local => ActivityResetIntent.RuntimeLocalReset,
                ActivityResetBoundaryKind.Activity => ActivityResetIntent.RuntimeActivityReset,
                ActivityResetBoundaryKind.ActivityTransition => ActivityResetIntent.RuntimeActivityTransitionReset,
                ActivityResetBoundaryKind.RouteTransition => ActivityResetIntent.RuntimeRouteTransitionReset,
                _ => ActivityResetIntent.Unknown,
            };
        }

        public static ActivityResetStateProfileKind ResolveStateProfile(ActivityResetIntent resetIntent)
        {
            return resetIntent switch
            {
                ActivityResetIntent.EntryInitialize => ActivityResetStateProfileKind.InitialState,
                ActivityResetIntent.RuntimeLocalReset => ActivityResetStateProfileKind.RuntimeLocalState,
                ActivityResetIntent.RuntimeActivityReset => ActivityResetStateProfileKind.RuntimeActivityState,
                ActivityResetIntent.RuntimeActivityTransitionReset => ActivityResetStateProfileKind.RuntimeActivityTransitionState,
                ActivityResetIntent.RuntimeRouteTransitionReset => ActivityResetStateProfileKind.RuntimeRouteTransitionState,
                _ => ActivityResetStateProfileKind.Unknown,
            };
        }
    }

    public enum ActivityResetBoundaryKind
    {
        Unknown = 0,
        Local = 1,
        Activity = 2,
        ActivityTransition = 3,
        RouteTransition = 4,
    }

    [Flags]
    public enum ActivityResetBoundaryEligibility
    {
        None = 0,
        Local = 1 << 0,
        Activity = 1 << 1,
        ActivityTransition = 1 << 2,
        RouteTransition = 1 << 3,
        RuntimeAll = Local | Activity | ActivityTransition | RouteTransition,
    }



    public static class ActivityResetBoundaryEligibilityFormatter
    {
        public static string Format(ActivityResetBoundaryEligibility eligibility)
        {
            ActivityResetBoundaryEligibility canonical = eligibility & ActivityResetBoundaryEligibility.RuntimeAll;
            if (canonical == ActivityResetBoundaryEligibility.None)
            {
                return "None";
            }

            if (canonical == ActivityResetBoundaryEligibility.RuntimeAll)
            {
                return "RuntimeAll";
            }

            return canonical.ToString().Replace(" ", string.Empty);
        }
    }

    public enum ActivityResetTargetScope
    {
        Unknown = 0,
        LocalTarget = 1,
        CurrentActivityEntry = 2,
        CurrentActivity = 3,
        CurrentRoute = 4,
        CurrentSession = 5,
    }

    public readonly struct ActivityResetScopePlan
    {
        public ActivityResetScopePlan(
            SessionActivityIdentity identity,
            ActivityResetBoundaryKind boundaryKind,
            ActivityResetTargetScope targetScope,
            string policyId,
            string source,
            string reason)
            : this(
                identity,
                boundaryKind,
                targetScope,
                ActivityResetIntentProfileDefaults.ResolveRuntimeIntentForBoundary(boundaryKind),
                ActivityResetIntentProfileDefaults.ResolveStateProfile(ActivityResetIntentProfileDefaults.ResolveRuntimeIntentForBoundary(boundaryKind)),
                policyId,
                source,
                reason)
        {
        }

        public ActivityResetScopePlan(
            SessionActivityIdentity identity,
            ActivityResetBoundaryKind boundaryKind,
            ActivityResetTargetScope targetScope,
            ActivityResetIntent resetIntent,
            ActivityResetStateProfileKind stateProfileKind,
            string policyId,
            string source,
            string reason)
        {
            Identity = identity;
            BoundaryKind = boundaryKind;
            TargetScope = targetScope;
            ResetIntent = resetIntent;
            StateProfileKind = stateProfileKind == ActivityResetStateProfileKind.Unknown
                ? ActivityResetIntentProfileDefaults.ResolveStateProfile(resetIntent)
                : stateProfileKind;
            PolicyId = Normalize(policyId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public ActivityResetBoundaryKind BoundaryKind { get; }
        public ActivityResetTargetScope TargetScope { get; }
        public ActivityResetIntent ResetIntent { get; }
        public ActivityResetStateProfileKind StateProfileKind { get; }
        public string PolicyId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            BoundaryKind != ActivityResetBoundaryKind.Unknown &&
            TargetScope != ActivityResetTargetScope.Unknown &&
            ResetIntent != ActivityResetIntent.Unknown &&
            StateProfileKind != ActivityResetStateProfileKind.Unknown &&
            !string.IsNullOrWhiteSpace(PolicyId) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

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
            ActivityCapabilityInventory resetInventory)
        {
            DiscoveryResult = discoveryResult;
            ResetInventory = resetInventory;
        }

        public ActivityObjectContributorDiscoveryResult DiscoveryResult { get; }
        public ActivityCapabilityInventory ResetInventory { get; }
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
