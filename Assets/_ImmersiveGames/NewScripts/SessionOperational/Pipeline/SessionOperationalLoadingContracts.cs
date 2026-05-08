using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum SessionOperationalRouteLoadingMode
    {
        RuntimeDefault = 0,
        None = 1,
        Profile = 2,
    }

    public enum SessionOperationalLoadingStage
    {
        Unknown = 0,
        LoadingStarted = 1,
        RoutePlanReady = 2,
        FadeInCompleted = 3,
        TransitionSkipped = 4,
        SceneCompositionCompleted = 5,
        MaterializationCompleted = 6,
        FadeOutCompleted = 7,
        OperationalRouteCompleted = 8,
        LoadingCompleted = 9,
        LoadingHidden = 10,
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
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
            LoadingMode = loadingMode;
            LoadingProfile = loadingProfile;
            LoadingSceneName = Normalize(loadingSceneName);
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
        public string LoadingProfileId => LoadingProfile != null ? Normalize(LoadingProfile.ProfileId) : string.Empty;
        public bool IsEnabled => LoadingMode == SessionOperationalRouteLoadingMode.None ? false : !string.IsNullOrWhiteSpace(LoadingSceneName);

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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionOperationalLoadingFact
    {
        public SessionOperationalLoadingFact(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingStage stage,
            float normalizedProgress,
            string stepLabel,
            string message)
        {
            Command = command;
            Stage = stage;
            NormalizedProgress = Mathf.Clamp01(normalizedProgress);
            StepLabel = Normalize(stepLabel);
            Message = Normalize(message);
        }

        public SessionOperationalLoadingCommand Command { get; }
        public SessionOperationalLoadingStage Stage { get; }
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
            Stage != SessionOperationalLoadingStage.Unknown;

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', loadingProfileId='{LoadingProfileId}', stage='{Stage}', normalizedProgress='{NormalizedProgress:0.###}', stepLabel='{StepLabel}', message='{Message}', source='{Source}', reason='{Reason}'"
                : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

