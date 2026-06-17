using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public enum RouteActivitySaveSkipKind
    {
        None = 0,
        DisabledByRoute = 1,
        DisabledByPreviousRoute = 2,
        NoPreviousRoute = 3,
        NoCurrentSnapshot = 4,
        NoSnapshotPayload = 5,
        NoSnapshotProvider = 6,
        SnapshotCaptureFailed = 7,
        SnapshotPayloadInvalid = 8,
        SaveAdapterUnavailable = 9,
        LoadAdapterUnavailable = 10,
        NoSessionActivity = 11,
        NoActivityContentContributors = 12,
        NoRouteSaveContributors = 13,
        NoSessionSaveContributors = 14,
        NoSaveContributors = 15,
        SnapshotPayloadExpectedButMissing = 16,
        SnapshotPayloadResolved = 17,
        Unknown = 18,
    }

    public enum RouteActivitySaveLoadOutcomeKind
    {
        Unknown = 0,
        Loaded = 1,
        Skipped = 2,
    }

    public enum RouteActivitySaveSnapshotFailureKind
    {
        None = 0,
        SnapshotPayloadMissing = 1,
        SnapshotCaptureFailed = 2,
        SnapshotProviderUnavailable = 3,
        SnapshotPayloadInvalid = 4,
        SnapshotIdentityMismatch = 5,
        SaveDisabledByRoute = 6,
        NoPreviousRoute = 7,
        NoCurrentActivity = 8,
        NoSessionActivity = 9,
        NoActivityContentContributors = 10,
        NoRouteSaveContributors = 11,
        NoSessionSaveContributors = 12,
        NoSaveContributors = 13,
        SnapshotPayloadExpectedButMissing = 14,
        SnapshotPayloadResolved = 15,
        UnknownFailure = 16,
    }

    public readonly struct RouteActivitySaveLoadResult
    {
        public RouteActivitySaveLoadResult(
            RouteActivitySaveLoadOutcomeKind outcomeKind,
            RouteActivitySaveSnapshotFailureKind failureKind,
            RouteActivitySaveSkipKind skipKind,
            string skipReason,
            bool hasSnapshot,
            string detail,
            string activitySnapshotPayload = "")
        {
            OutcomeKind = outcomeKind;
            FailureKind = failureKind;
            SkipKind = skipKind;
            SkipReason = string.IsNullOrWhiteSpace(skipReason) ? string.Empty : skipReason.Trim();
            HasSnapshot = hasSnapshot;
            Detail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();
            ActivitySnapshotPayload = string.IsNullOrWhiteSpace(activitySnapshotPayload) ? string.Empty : activitySnapshotPayload.Trim();
        }

        public RouteActivitySaveLoadOutcomeKind OutcomeKind { get; }
        public RouteActivitySaveSnapshotFailureKind FailureKind { get; }
        public RouteActivitySaveSkipKind SkipKind { get; }
        public string SkipReason { get; }
        public bool HasSnapshot { get; }
        public string Detail { get; }
        public string ActivitySnapshotPayload { get; }

        public bool IsLoaded => OutcomeKind == RouteActivitySaveLoadOutcomeKind.Loaded && HasSnapshot;
        public bool IsSkipped => OutcomeKind == RouteActivitySaveLoadOutcomeKind.Skipped;
    }

    public interface ISessionOperationalActivitySaveAdapter
    {
        RouteActivitySaveLoadResult LoadActivitySaveOnEnter(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteCommand command,
            ProgressionSlotContext slotContext,
            string activityIdentity);

        RouteActivitySaveSaveResult SaveActivityOnExit(
            RuntimeModeConfig runtimeModeConfig,
            ProgressionSlotContext slotContext,
            string activitySaveOwnerIdentity,
            string activitySnapshotPayload);
    }

    public enum RouteActivitySaveSaveOutcomeKind
    {
        Unknown = 0,
        Saved = 1,
        Skipped = 2,
    }

    public readonly struct RouteActivitySaveSaveResult
    {
        public RouteActivitySaveSaveResult(
            RouteActivitySaveSaveOutcomeKind outcomeKind,
            RouteActivitySaveSnapshotFailureKind failureKind,
            RouteActivitySaveSkipKind skipKind,
            string skipReason,
            bool hasSnapshot,
            string detail)
        {
            OutcomeKind = outcomeKind;
            FailureKind = failureKind;
            SkipKind = skipKind;
            SkipReason = string.IsNullOrWhiteSpace(skipReason) ? string.Empty : skipReason.Trim();
            HasSnapshot = hasSnapshot;
            Detail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();
        }

        public RouteActivitySaveSaveOutcomeKind OutcomeKind { get; }
        public RouteActivitySaveSnapshotFailureKind FailureKind { get; }
        public RouteActivitySaveSkipKind SkipKind { get; }
        public string SkipReason { get; }
        public bool HasSnapshot { get; }
        public string Detail { get; }

        public bool IsSaved => OutcomeKind == RouteActivitySaveSaveOutcomeKind.Saved && HasSnapshot;
        public bool IsSkipped => OutcomeKind == RouteActivitySaveSaveOutcomeKind.Skipped;
    }

    public static class RouteActivitySaveSkipKindMapper
    {
        public static string ToCode(RouteActivitySaveSkipKind skipKind)
        {
            return skipKind switch
            {
                RouteActivitySaveSkipKind.DisabledByRoute => "disabled_by_route",
                RouteActivitySaveSkipKind.DisabledByPreviousRoute => "disabled_by_previous_route",
                RouteActivitySaveSkipKind.NoPreviousRoute => "no_previous_route",
                RouteActivitySaveSkipKind.NoCurrentSnapshot => "no_current_snapshot",
                RouteActivitySaveSkipKind.NoSnapshotPayload => "no_snapshot_payload",
                RouteActivitySaveSkipKind.NoSnapshotProvider => "no_snapshot_provider",
                RouteActivitySaveSkipKind.SnapshotCaptureFailed => "snapshot_capture_failed",
                RouteActivitySaveSkipKind.SnapshotPayloadInvalid => "snapshot_payload_invalid",
                RouteActivitySaveSkipKind.SaveAdapterUnavailable => "save_adapter_unavailable",
                RouteActivitySaveSkipKind.LoadAdapterUnavailable => "load_adapter_unavailable",
                RouteActivitySaveSkipKind.NoSessionActivity => "no_session_activity",
                RouteActivitySaveSkipKind.NoActivityContentContributors => "no_activity_content_contributors",
                RouteActivitySaveSkipKind.NoRouteSaveContributors => "no_route_save_contributors",
                RouteActivitySaveSkipKind.NoSessionSaveContributors => "no_session_save_contributors",
                RouteActivitySaveSkipKind.NoSaveContributors => "no_save_contributors",
                RouteActivitySaveSkipKind.SnapshotPayloadExpectedButMissing => "snapshot_payload_expected_but_missing",
                RouteActivitySaveSkipKind.SnapshotPayloadResolved => "snapshot_payload_resolved",
                RouteActivitySaveSkipKind.None => "none",
                _ => "unknown",
            };
        }
    }
}
