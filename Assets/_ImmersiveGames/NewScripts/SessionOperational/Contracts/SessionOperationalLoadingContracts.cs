using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum SessionOperationalRouteLoadingMode
    {
        RuntimeDefault = 0,
        None = 1,
        Profile = 2
    }

    public enum SessionOperationalLoadingStage
    {
        Unknown = 0,
        LoadingStarted = 1,
        RoutePlanReady = 2,
        FadeInCompleted = 3,
        TransitionSkipped = 4,
        SceneCompositionCompleted = 5,
        ConsumerEntryPreparationCompleted = 6,
        FadeOutCompleted = 7,
        OperationalRouteCompleted = 8,
        LoadingCompleted = 9,
        LoadingHidden = 10
    }

    public enum SessionOperationalLoadingOutcomeKind
    {
        Unknown = 0,
        Started = 1,
        ProgressApplied = 2,
        VisualSettled = 3,
        FinalHoldStarted = 4,
        FinalHoldCompleted = 5,
        Completed = 6,
        Hidden = 7,
        Skipped = 8,
        Failed = 9
    }

    public readonly struct SessionOperationalLoadingCommand
    {
        public SessionOperationalLoadingCommand(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            SessionOperationalRouteLoadingMode loadingMode,
            RuntimeLoadingProfileAsset loadingProfile,
            string loadingSceneName,
            float finalProgressHoldSeconds)
        {
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
            LoadingMode = loadingMode;
            LoadingProfile = loadingProfile;
            LoadingSceneName = loadingSceneName.TrimToEmpty();
            ShowImmediately = loadingProfile != null && loadingProfile.ShowImmediately;
            HideAfterCompletion = loadingProfile == null || loadingProfile.HideAfterCompletion;
            MinimumVisibleSeconds = loadingProfile != null && loadingProfile.MinimumVisibleSeconds > 0f
                ? loadingProfile.MinimumVisibleSeconds
                : 0f;
            FinalProgressHoldSeconds = finalProgressHoldSeconds > 0f
                ? finalProgressHoldSeconds
                : 0f;
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }
        public SessionOperationalRouteLoadingMode LoadingMode { get; }
        public RuntimeLoadingProfileAsset LoadingProfile { get; }
        public string LoadingSceneName { get; }
        public bool ShowImmediately { get; }
        public bool HideAfterCompletion { get; }
        public float MinimumVisibleSeconds { get; }
        public float FinalProgressHoldSeconds { get; }
        public string LoadingProfileId => LoadingProfile != null ? LoadingProfile.ProfileId.TrimToEmpty() : string.Empty;
        public bool IsEnabled => LoadingMode != SessionOperationalRouteLoadingMode.None && !string.IsNullOrWhiteSpace(LoadingSceneName);

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RouteIdentity) ||
                    string.IsNullOrWhiteSpace(RouteOperationId) ||
                    string.IsNullOrWhiteSpace(TransitionId) ||
                    RouteSequence <= 0 ||
                    string.IsNullOrWhiteSpace(Source) ||
                    string.IsNullOrWhiteSpace(Reason) ||
                    LoadingMode == SessionOperationalRouteLoadingMode.RuntimeDefault)
                {
                    return false;
                }

                if (LoadingMode == SessionOperationalRouteLoadingMode.None)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(LoadingSceneName))
                {
                    return false;
                }

                if (LoadingMode == SessionOperationalRouteLoadingMode.Profile)
                {
                    return LoadingProfile != null && LoadingProfile.TryValidate(out _);
                }

                return false;
            }
        }

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', loadingMode='{LoadingMode}', loadingProfileId='{LoadingProfileId}', loadingSceneName='{LoadingSceneName}', showImmediately='{ShowImmediately}', hideAfterCompletion='{HideAfterCompletion}', minimumVisibleSeconds='{MinimumVisibleSeconds:0.###}', finalProgressHoldSeconds='{FinalProgressHoldSeconds:0.###}', source='{Source}', reason='{Reason}'"
                : "<none>";
        }
    }

    public readonly struct SessionOperationalLoadingFact
    {
        public SessionOperationalLoadingFact(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingStage stage,
            SessionOperationalLoadingOutcomeKind outcomeKind,
            float normalizedProgress,
            string stepLabel,
            string message)
        {
            Command = command;
            Stage = stage;
            OutcomeKind = outcomeKind;
            NormalizedProgress = Mathf.Clamp01(normalizedProgress);
            StepLabel = stepLabel.TrimToEmpty();
            Message = message.TrimToEmpty();
        }

        public SessionOperationalLoadingCommand Command { get; }
        public SessionOperationalLoadingStage Stage { get; }
        public SessionOperationalLoadingOutcomeKind OutcomeKind { get; }
        public float NormalizedProgress { get; }
        public string StepLabel { get; }
        public string Message { get; }

        public string RouteIdentity => Command.RouteIdentity;
        public string RouteOperationId => Command.RouteOperationId;
        public string TransitionId => Command.TransitionId;
        public int RouteSequence => Command.RouteSequence;
        public string LoadingProfileId => Command.LoadingProfileId;
        public string Source => Command.Source;
        public string Reason => Command.Reason;
        public bool IsValid =>
            Command.IsValid &&
            Stage != SessionOperationalLoadingStage.Unknown &&
            OutcomeKind != SessionOperationalLoadingOutcomeKind.Unknown;

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', loadingProfileId='{LoadingProfileId}', stage='{Stage}', normalizedProgress='{NormalizedProgress:0.###}', stepLabel='{StepLabel}', message='{Message}', source='{Source}', reason='{Reason}'"
                : "<none>";
        }
    }
}
