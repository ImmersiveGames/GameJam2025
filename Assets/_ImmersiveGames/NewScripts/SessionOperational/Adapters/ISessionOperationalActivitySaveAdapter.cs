using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public enum RouteActivitySaveLoadOutcomeKind
    {
        Unknown = 0,
        Loaded = 1,
        Skipped = 2,
    }

    public readonly struct RouteActivitySaveLoadResult
    {
        public RouteActivitySaveLoadResult(
            RouteActivitySaveLoadOutcomeKind outcomeKind,
            string skipReason,
            SaveRecord record,
            string detail)
        {
            OutcomeKind = outcomeKind;
            SkipReason = string.IsNullOrWhiteSpace(skipReason) ? string.Empty : skipReason.Trim();
            Record = record;
            Detail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();
        }

        public RouteActivitySaveLoadOutcomeKind OutcomeKind { get; }
        public string SkipReason { get; }
        public SaveRecord Record { get; }
        public string Detail { get; }

        public bool IsLoaded => OutcomeKind == RouteActivitySaveLoadOutcomeKind.Loaded && Record != null;
        public bool IsSkipped => OutcomeKind == RouteActivitySaveLoadOutcomeKind.Skipped;
    }

    public interface ISessionOperationalActivitySaveAdapter
    {
        RouteActivitySaveLoadResult LoadActivitySaveOnEnter(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteCommand command,
            string activityIdentity);

        RouteActivitySaveSaveResult SaveActivityOnExit(
            RuntimeModeConfig runtimeModeConfig,
            string previousActivityIdentity,
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
            string skipReason,
            SaveRecord record,
            string detail)
        {
            OutcomeKind = outcomeKind;
            SkipReason = string.IsNullOrWhiteSpace(skipReason) ? string.Empty : skipReason.Trim();
            Record = record;
            Detail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();
        }

        public RouteActivitySaveSaveOutcomeKind OutcomeKind { get; }
        public string SkipReason { get; }
        public SaveRecord Record { get; }
        public string Detail { get; }

        public bool IsSaved => OutcomeKind == RouteActivitySaveSaveOutcomeKind.Saved && Record != null;
        public bool IsSkipped => OutcomeKind == RouteActivitySaveSaveOutcomeKind.Skipped;
    }
}
