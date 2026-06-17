using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Policies
{
    internal static class ActivityActorScopeCompatibilityPolicy
    {
        private const string Owner = "ActivityActorScopeCompatibilityPolicy";

        internal static bool ShouldRetainActiveHandleOnActivityScopeBegin(
            SessionActivityIdentity activeScopeIdentity,
            PlayerActorRuntimeHandle handle,
            string source,
            string reason)
        {
            if (!activeScopeIdentity.IsValid || !handle.IsValid || handle.Actor == null)
            {
                return false;
            }

            var actorScope = handle.Actor.ActorScopeMetadata;
            bool shouldRetain = actorScope == ActorScope.SessionScoped;
            LogDecision(
                "ActorActiveScopeEligibilityEvaluated",
                "retain_on_activity_scope_begin",
                shouldRetain ? "Eligible" : "NotEligible",
                shouldRetain ? "session_scoped_same_session" : "scope_not_supported",
                activeScopeIdentity,
                activeScopeIdentity,
                handle.ActorId,
                handle.ActorInstanceRuntimeId,
                actorScope,
                source,
                reason);
            return shouldRetain;
        }

        internal static bool ShouldTrackInRouteIndex(
            SessionActivityIdentity activeScopeIdentity,
            PlayerActorRuntimeHandle handle,
            string source,
            string reason)
        {
            if (!handle.IsValid || handle.Actor == null)
            {
                return false;
            }

            var actorScope = handle.Actor.ActorScopeMetadata;
            bool shouldTrack = actorScope == ActorScope.RouteScoped;
            LogDecision(
                "ActorRouteRetentionEligibilityEvaluated",
                "track_route_index",
                shouldTrack ? "Eligible" : "NotEligible",
                shouldTrack
                    ? "route_scoped_same_session"
                    : actorScope == ActorScope.SessionScoped
                        ? "session_scoped_not_route_indexed"
                        : "scope_not_supported",
                activeScopeIdentity,
                activeScopeIdentity,
                handle.ActorId,
                handle.ActorInstanceRuntimeId,
                actorScope,
                source,
                reason);
            return shouldTrack;
        }

        internal static void EnsureScopeMatchesOrFail(
            SessionActivityIdentity activeScopeIdentity,
            SessionActivityIdentity expectedScopeIdentity,
            PlayerActorRuntimeHandle handle,
            string source,
            string reason)
        {
            if (!handle.IsValid || handle.Actor == null)
            {
                throw new InvalidOperationException("ActivityActorScopeCompatibilityPolicy requires a valid player actor handle.");
            }

            if (IsScopeCompatible(activeScopeIdentity, expectedScopeIdentity, handle.Actor.ActorScopeMetadata))
            {
                return;
            }

            LogDecision(
                "ActorActiveScopeEligibilityEvaluated",
                "active_scope_match",
                "Rejected",
                "scope_mismatch",
                activeScopeIdentity,
                expectedScopeIdentity,
                handle.ActorId,
                handle.ActorInstanceRuntimeId,
                handle.Actor.ActorScopeMetadata,
                source,
                reason);
            throw new InvalidOperationException("stale_or_foreign_player_actor_scope: expected scope does not match current activity scope.");
        }

        internal static void EnsureScopeMatchesOrFail(
            SessionActivityIdentity activeScopeIdentity,
            SessionActivityIdentity expectedScopeIdentity,
            ActorScope actorScope,
            string source,
            string reason)
        {
            if (!IsScopeCompatible(activeScopeIdentity, expectedScopeIdentity, actorScope))
            {
                LogDecision(
                    "ActorActiveScopeEligibilityEvaluated",
                    "active_scope_match",
                    "Rejected",
                    actorScope == ActorScope.SessionScoped || actorScope == ActorScope.RouteScoped
                        ? "session_scoped_not_same_session"
                        : "activity_scoped_previous_entry",
                    activeScopeIdentity,
                    expectedScopeIdentity,
                    default(ActorId),
                    default(ActorInstanceRuntimeId),
                    actorScope,
                    source,
                    reason);
                throw new InvalidOperationException("stale_or_foreign_player_actor_scope: expected scope does not match current activity scope.");
            }
        }

        internal static void EnsureIdentityMatchesActiveScopeOrFail(
            SessionActivityIdentity activeScopeIdentity,
            SessionActivityIdentity identity,
            PlayerActorRuntimeHandle handle,
            string errorToken,
            string source,
            string reason)
        {
            if (!handle.IsValid || handle.Actor == null)
            {
                throw new InvalidOperationException("ActivityActorScopeCompatibilityPolicy requires a valid player actor handle.");
            }

            if (IsScopeCompatible(activeScopeIdentity, identity, handle.Actor.ActorScopeMetadata))
            {
                return;
            }

            LogDecision(
                "ActorReentryEligibilityEvaluated",
                "reentry_identity_match",
                "Rejected",
                "identity_mismatch",
                activeScopeIdentity,
                identity,
                handle.ActorId,
                handle.ActorInstanceRuntimeId,
                handle.Actor.ActorScopeMetadata,
                source,
                reason);
            throw new InvalidOperationException($"{errorToken}: actor identity does not match activity scope.");
        }

        internal static void EnsureScopeCompatibleOrFail(
            SessionActivityIdentity left,
            SessionActivityIdentity right,
            PlayerActorRuntimeHandle handle,
            string errorToken,
            string source,
            string reason)
        {
            if (!handle.IsValid || handle.Actor == null)
            {
                throw new InvalidOperationException("ActivityActorScopeCompatibilityPolicy requires a valid player actor handle.");
            }

            if (IsScopeCompatible(left, right, handle.Actor.ActorScopeMetadata))
            {
                return;
            }

            LogDecision(
                "ActorScopeCompatibilityEvaluated",
                "scope_compatibility",
                "Rejected",
                "scope_mismatch",
                left,
                right,
                handle.ActorId,
                handle.ActorInstanceRuntimeId,
                handle.Actor.ActorScopeMetadata,
                source,
                reason);
            throw new InvalidOperationException(errorToken);
        }

        internal static bool IsScopeCompatible(
            SessionActivityIdentity left,
            SessionActivityIdentity right,
            ActorScope actorScope)
        {
            if (!left.IsValid || !right.IsValid)
            {
                return false;
            }

            return actorScope == ActorScope.SessionScoped || actorScope == ActorScope.RouteScoped
                ? IsSameSessionPipeline(left, right)
                : IsSameActivityCycle(left, right);
        }

        internal static bool IsSameSessionPipeline(
            SessionActivityIdentity left,
            SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) &&
                string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal);
        }

        private static bool IsSameActivityCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return IsSameSessionPipeline(left, right) &&
                string.Equals(left.ActivityId, right.ActivityId, StringComparison.Ordinal) &&
                left.ActivityOrdinal == right.ActivityOrdinal &&
                left.EntrySequence == right.EntrySequence;
        }

        private static void LogDecision(
            string eventName,
            string decisionKind,
            string outcome,
            string outcomeReason,
            SessionActivityIdentity activeIdentity,
            SessionActivityIdentity targetIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorScope actorScope,
            string source,
            string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActivityActorScopeCompatibilityPolicy),
                $"event='{eventName}' owner='{Owner}' decisionKind='{Normalize(decisionKind)}' outcome='{Normalize(outcome)}' outcomeReason='{Normalize(outcomeReason)}' activePipelineId='{Normalize(activeIdentity.PipelineId)}' activeSessionId='{Normalize(activeIdentity.SessionId)}' activeActivityId='{Normalize(activeIdentity.ActivityId)}' activeEntrySequence='{activeIdentity.EntrySequence}' targetPipelineId='{Normalize(targetIdentity.PipelineId)}' targetSessionId='{Normalize(targetIdentity.SessionId)}' targetActivityId='{Normalize(targetIdentity.ActivityId)}' targetEntrySequence='{targetIdentity.EntrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' actorScope='{actorScope}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
