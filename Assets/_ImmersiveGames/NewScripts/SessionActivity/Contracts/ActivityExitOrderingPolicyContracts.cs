using System;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityExitOrderingScenario
    {
        Unknown = 0,
        CompleteCurrentActivity = 1,
        RestartCurrentActivity = 2,
        RouteExitFromActivityRunning = 3,
        RouteExitFromDeactivationWindowReady = 4,
    }

    public enum ActivityExitOrderingContinuation
    {
        Unknown = 0,
        StopActivityLifecycle = 1,
        StartNextActivity = 2,
        RestartSameActivity = 3,
        CompleteRouteExitHandoff = 4,
    }

    public enum ActivityExitDeactivationWindowRule
    {
        Unknown = 0,
        UseDeclaredWindow = 1,
        ReuseActiveWindow = 2,
        SkipByExplicitRestartPolicy = 3,
    }

    public enum ActivityExitOrderingStep
    {
        Unknown = 0,
        MarkExitRequested = 1,
        BlockGameplayControl = 2,
        StartDeactivationWindowIfDeclared = 3,
        AwaitExplicitDeactivationWindowCompletion = 4,
        ContinueFromActiveDeactivationWindow = 5,
        CompleteDeactivationWindow = 6,
        CaptureActivitySnapshots = 7,
        RunActorAndObjectExitTeardown = 8,
        ReleaseActivityContent = 9,
        CompleteActivityExit = 10,
        CompleteRouteExitHandoff = 11,
        StartNextActivityEntry = 12,
        RestartCurrentActivityEntry = 13,
    }

    public readonly struct ActivityExitOrderingPolicy : IEquatable<ActivityExitOrderingPolicy>
    {
        public ActivityExitOrderingPolicy(
            ActivityExitOrderingScenario scenario,
            ActivityExitDeactivationWindowRule deactivationWindowRule,
            ActivityExitOrderingContinuation continuation,
            bool snapshotBeforeTeardown,
            bool releaseAfterDeactivationWindow,
            bool operationalMayContinueBeforeRouteExitCompleted,
            string policyId,
            string reason)
        {
            Scenario = scenario;
            DeactivationWindowRule = deactivationWindowRule;
            Continuation = continuation;
            SnapshotBeforeTeardown = snapshotBeforeTeardown;
            ReleaseAfterDeactivationWindow = releaseAfterDeactivationWindow;
            OperationalMayContinueBeforeRouteExitCompleted = operationalMayContinueBeforeRouteExitCompleted;
            PolicyId = policyId.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActivityExitOrderingScenario Scenario { get; }
        public ActivityExitDeactivationWindowRule DeactivationWindowRule { get; }
        public ActivityExitOrderingContinuation Continuation { get; }
        public bool SnapshotBeforeTeardown { get; }
        public bool ReleaseAfterDeactivationWindow { get; }
        public bool OperationalMayContinueBeforeRouteExitCompleted { get; }
        public string PolicyId { get; }
        public string Reason { get; }

        public bool IsValid =>
            Scenario != ActivityExitOrderingScenario.Unknown &&
            DeactivationWindowRule != ActivityExitDeactivationWindowRule.Unknown &&
            Continuation != ActivityExitOrderingContinuation.Unknown &&
            !string.IsNullOrWhiteSpace(PolicyId) &&
            !string.IsNullOrWhiteSpace(Reason);

        public bool Equals(ActivityExitOrderingPolicy other)
        {
            return Scenario == other.Scenario &&
                   DeactivationWindowRule == other.DeactivationWindowRule &&
                   Continuation == other.Continuation &&
                   SnapshotBeforeTeardown == other.SnapshotBeforeTeardown &&
                   ReleaseAfterDeactivationWindow == other.ReleaseAfterDeactivationWindow &&
                   OperationalMayContinueBeforeRouteExitCompleted == other.OperationalMayContinueBeforeRouteExitCompleted &&
                   string.Equals(PolicyId, other.PolicyId, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityExitOrderingPolicy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Scenario;
                hash = hash * 397 ^ (int)DeactivationWindowRule;
                hash = hash * 397 ^ (int)Continuation;
                hash = hash * 397 ^ SnapshotBeforeTeardown.GetHashCode();
                hash = hash * 397 ^ ReleaseAfterDeactivationWindow.GetHashCode();
                hash = hash * 397 ^ OperationalMayContinueBeforeRouteExitCompleted.GetHashCode();
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(PolicyId ?? string.Empty);
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hash;
            }
        }

        public static ActivityExitOrderingPolicy ForScenario(ActivityExitOrderingScenario scenario)
        {
            switch (scenario)
            {
                case ActivityExitOrderingScenario.CompleteCurrentActivity:
                    return CompleteCurrentActivity;
                case ActivityExitOrderingScenario.RestartCurrentActivity:
                    return RestartCurrentActivity;
                case ActivityExitOrderingScenario.RouteExitFromActivityRunning:
                    return RouteExitFromActivityRunning;
                case ActivityExitOrderingScenario.RouteExitFromDeactivationWindowReady:
                    return RouteExitFromDeactivationWindowReady;
                default:
                    return default;
            }
        }

        public static readonly ActivityExitOrderingPolicy CompleteCurrentActivity = new ActivityExitOrderingPolicy(
            ActivityExitOrderingScenario.CompleteCurrentActivity,
            ActivityExitDeactivationWindowRule.UseDeclaredWindow,
            ActivityExitOrderingContinuation.StartNextActivity,
            snapshotBeforeTeardown: true,
            releaseAfterDeactivationWindow: true,
            operationalMayContinueBeforeRouteExitCompleted: true,
            policyId: ActivityExitOrderingPolicyIds.CompleteCurrentActivity,
            reason: "complete_current_activity_uses_deactivation_before_release");

        public static readonly ActivityExitOrderingPolicy RestartCurrentActivity = new ActivityExitOrderingPolicy(
            ActivityExitOrderingScenario.RestartCurrentActivity,
            ActivityExitDeactivationWindowRule.SkipByExplicitRestartPolicy,
            ActivityExitOrderingContinuation.RestartSameActivity,
            snapshotBeforeTeardown: false,
            releaseAfterDeactivationWindow: true,
            operationalMayContinueBeforeRouteExitCompleted: true,
            policyId: ActivityExitOrderingPolicyIds.RestartCurrentActivity,
            reason: "restart_current_activity_uses_explicit_restart_skip_policy");

        public static readonly ActivityExitOrderingPolicy RouteExitFromActivityRunning = new ActivityExitOrderingPolicy(
            ActivityExitOrderingScenario.RouteExitFromActivityRunning,
            ActivityExitDeactivationWindowRule.UseDeclaredWindow,
            ActivityExitOrderingContinuation.CompleteRouteExitHandoff,
            snapshotBeforeTeardown: true,
            releaseAfterDeactivationWindow: true,
            operationalMayContinueBeforeRouteExitCompleted: false,
            policyId: ActivityExitOrderingPolicyIds.RouteExitFromActivityRunning,
            reason: "route_exit_from_running_must_complete_deactivation_and_release_before_operational_continues");

        public static readonly ActivityExitOrderingPolicy RouteExitFromDeactivationWindowReady = new ActivityExitOrderingPolicy(
            ActivityExitOrderingScenario.RouteExitFromDeactivationWindowReady,
            ActivityExitDeactivationWindowRule.ReuseActiveWindow,
            ActivityExitOrderingContinuation.CompleteRouteExitHandoff,
            snapshotBeforeTeardown: true,
            releaseAfterDeactivationWindow: true,
            operationalMayContinueBeforeRouteExitCompleted: false,
            policyId: ActivityExitOrderingPolicyIds.RouteExitFromDeactivationWindowReady,
            reason: "route_exit_from_deactivation_window_ready_must_reuse_active_window_and_complete_single_exit_rail");
}

    public static class ActivityExitOrderingPolicyIds
    {
        public const string CompleteCurrentActivity = "activity.exit.ordering.complete_current_activity.v1";
        public const string RestartCurrentActivity = "activity.exit.ordering.restart_current_activity.v1";
        public const string RouteExitFromActivityRunning = "activity.exit.ordering.route_exit_from_activity_running.v1";
        public const string RouteExitFromDeactivationWindowReady = "activity.exit.ordering.route_exit_from_deactivation_window_ready.v1";
    }
}
