using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalLoadingResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    // Etapa 2 (canonização): Result local normalizado para o modelo canônico (IsCompleted/IsSkipped/IsAccepted + Kind).
    // Command local mantido (enriquece com SessionOperationalLoadingCommand do Contracts). 
    // Quando possível, novos steps devem preferir declarar Request/Result em *Contracts.cs e stages retornarem direto o tipo canônico.
    public readonly struct OperationalLoadingResult
    {
        public OperationalLoadingResult(
            OperationalLoadingResultKind kind,
            bool loadingStarted,
            bool loadingCompleted,
            bool loadingHidden,
            string reason,
            string detail)
        {
            Kind = kind;
            LoadingStarted = loadingStarted;
            LoadingCompleted = loadingCompleted;
            LoadingHidden = loadingHidden;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalLoadingResultKind Kind { get; }
        public bool LoadingStarted { get; }
        public bool LoadingCompleted { get; }
        public bool LoadingHidden { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalLoadingResultKind.Completed;
        public bool IsSkipped => Kind == OperationalLoadingResultKind.Skipped;
        public bool IsAccepted => IsCompleted || IsSkipped;
}

    public readonly struct OperationalLoadingCommand
    {
        public OperationalLoadingCommand(
            SessionOperationalLoadingCommand loadingCommand,
            string source,
            string reason)
        {
            LoadingCommand = loadingCommand;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionOperationalLoadingCommand LoadingCommand { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            LoadingCommand.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);
}


    public readonly struct OperationalLoadingCompletionState
    {
        public OperationalLoadingCompletionState(bool loadingCompleted, bool loadingHidden)
        {
            LoadingCompleted = loadingCompleted;
            LoadingHidden = loadingHidden;
        }

        public bool LoadingCompleted { get; }
        public bool LoadingHidden { get; }
    }

    public sealed class OperationalLoadingStage
    {
        private readonly OperationalFactRecorder _factRecorder;
        private readonly ILoadingAdapter _loadingAdapter;

        public OperationalLoadingStage(OperationalFactRecorder factRecorder, ILoadingAdapter loadingAdapter)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
            _loadingAdapter = loadingAdapter ?? throw new ArgumentNullException(nameof(loadingAdapter));
        }

        public async Task<OperationalLoadingResult> ExecuteStartAsync(OperationalLoadingCommand command)
        {
            ValidateCommand(command);
            var loadingCommand = command.LoadingCommand;
            if (!loadingCommand.IsEnabled)
            {
                return new OperationalLoadingResult(
                    OperationalLoadingResultKind.Skipped,
                    false,
                    false,
                    false,
                    "loading_disabled",
                    "Route loading is disabled by route policy.");
            }

            await _loadingAdapter.ShowLoadingAsync(
                loadingCommand,
                CreateLoadingFact(
                    loadingCommand,
                    SessionOperationalLoadingStage.LoadingStarted,
                    SessionOperationalLoadingOutcomeKind.Started,
                    0f,
                    "Loading started",
                    "LoadingStarted"));

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.Loading, command.Source, command.Reason, "loading_started");

            DebugUtility.LogVerbose(typeof(OperationalLoadingStage),
                $"LoadingStarted routeIdentity='{loadingCommand.RouteIdentity}' routeOperationId='{loadingCommand.RouteOperationId}' transitionId='{loadingCommand.TransitionId}' routeSequence='{loadingCommand.RouteSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' showImmediately='{loadingCommand.ShowImmediately}'.",
                DebugUtility.Colors.Info);

            LogLoadingProgress(
                loadingCommand,
                SessionOperationalLoadingStage.LoadingStarted,
                0f,
                "Loading started",
                command.Source,
                command.Reason);

            await _loadingAdapter.UpdateLoadingAsync(
                loadingCommand,
                CreateLoadingFact(
                    loadingCommand,
                    SessionOperationalLoadingStage.RoutePlanReady,
                    SessionOperationalLoadingOutcomeKind.ProgressApplied,
                    0.10f,
                    "Route plan ready",
                    "RoutePlanReady"));

            LogLoadingProgress(
                loadingCommand,
                SessionOperationalLoadingStage.RoutePlanReady,
                0.10f,
                "Route plan ready",
                command.Source,
                command.Reason);

            return new OperationalLoadingResult(
                OperationalLoadingResultKind.Completed,
                true,
                false,
                false,
                "loading_started",
                "Route loading started.");
        }


        public async Task<OperationalLoadingResult> ExecuteTransitionBlackoutProgressAsync(
            OperationalLoadingCommand command,
            OperationalTransitionBlackoutResult blackoutResult)
        {
            ValidateCommand(command);
            if (blackoutResult is { IsCompleted: false, IsSkipped: false })
            {
                return new OperationalLoadingResult(
                    OperationalLoadingResultKind.Failed,
                    false,
                    false,
                    false,
                    "invalid_blackout_result",
                    $"Blackout result is not accepted. kind='{blackoutResult.Kind}' reason='{blackoutResult.Reason}'.");
            }

            var loadingCommand = command.LoadingCommand;
            if (!loadingCommand.IsEnabled)
            {
                return new OperationalLoadingResult(
                    OperationalLoadingResultKind.Skipped,
                    false,
                    false,
                    false,
                    "loading_disabled",
                    "Route loading is disabled by route policy.");
            }

            var stage = blackoutResult.IsCompleted
                ? SessionOperationalLoadingStage.FadeInCompleted
                : SessionOperationalLoadingStage.TransitionSkipped;
            string stepLabel = blackoutResult.IsCompleted
                ? "Fade in completed"
                : "Transition skipped";
            string message = blackoutResult.IsCompleted
                ? "FadeInCompleted"
                : "TransitionSkipped";

            await _loadingAdapter.UpdateLoadingAsync(
                loadingCommand,
                CreateLoadingFact(
                    loadingCommand,
                    stage,
                    SessionOperationalLoadingOutcomeKind.ProgressApplied,
                    0.20f,
                    stepLabel,
                    message));

            LogLoadingProgress(
                loadingCommand,
                stage,
                0.20f,
                stepLabel,
                command.Source,
                command.Reason);

            return new OperationalLoadingResult(
                OperationalLoadingResultKind.Completed,
                false,
                false,
                false,
                blackoutResult.IsCompleted ? "fade_in_loading_progress_applied" : "transition_skipped_loading_progress_applied",
                "Transition blackout loading progress applied.");
        }

        public async Task<OperationalLoadingResult> ExecuteSceneCompositionCompletedAsync(OperationalLoadingCommand command)
        {
            ValidateCommand(command);
            var loadingCommand = command.LoadingCommand;
            if (!loadingCommand.IsEnabled)
            {
                return new OperationalLoadingResult(
                    OperationalLoadingResultKind.Skipped,
                    false,
                    false,
                    false,
                    "loading_disabled",
                    "Route loading is disabled by route policy.");
            }

            await _loadingAdapter.UpdateLoadingAsync(
                loadingCommand,
                CreateLoadingFact(
                    loadingCommand,
                    SessionOperationalLoadingStage.SceneCompositionCompleted,
                    SessionOperationalLoadingOutcomeKind.ProgressApplied,
                    0.60f,
                    "Scene composition completed",
                    "SceneCompositionCompleted"));

            LogLoadingProgress(
                loadingCommand,
                SessionOperationalLoadingStage.SceneCompositionCompleted,
                0.60f,
                "Scene composition completed",
                command.Source,
                command.Reason);

            return new OperationalLoadingResult(
                OperationalLoadingResultKind.Completed,
                false,
                false,
                false,
                "scene_composition_loading_progress_applied",
                "Scene composition loading progress applied.");
        }

        public async Task<OperationalLoadingCompletionState> ExecuteClosedWindowCompletionAsync(OperationalLoadingCommand command)
        {
            ValidateCommand(command);
            var loadingCommand = command.LoadingCommand;
            bool loadingCompleted = false;
            bool loadingHidden = false;
            if (!loadingCommand.IsEnabled)
            {
                return new OperationalLoadingCompletionState(loadingCompleted, loadingHidden);
            }

            await _loadingAdapter.UpdateLoadingAsync(
                loadingCommand,
                CreateLoadingFact(
                    loadingCommand,
                    SessionOperationalLoadingStage.ConsumerEntryPreparationCompleted,
                    SessionOperationalLoadingOutcomeKind.ProgressApplied,
                    0.80f,
                    "Consumer entry preparation completed",
                    "ConsumerEntryPreparationCompleted"));

            LogLoadingProgress(
                loadingCommand,
                SessionOperationalLoadingStage.ConsumerEntryPreparationCompleted,
                0.80f,
                "Consumer entry preparation completed",
                command.Source,
                command.Reason);

            await _loadingAdapter.UpdateLoadingAsync(
                loadingCommand,
                CreateLoadingFact(
                    loadingCommand,
                    SessionOperationalLoadingStage.OperationalRouteCompleted,
                    SessionOperationalLoadingOutcomeKind.Completed,
                    1.0f,
                    "Operational route completed",
                    "OperationalRouteCompleted"));
            loadingCompleted = true;

            LogLoadingProgress(
                loadingCommand,
                SessionOperationalLoadingStage.OperationalRouteCompleted,
                1.0f,
                "Operational route completed",
                command.Source,
                command.Reason);

            DebugUtility.Log(typeof(OperationalLoadingStage),
                $"LoadingCompleted routeIdentity='{loadingCommand.RouteIdentity}' routeOperationId='{loadingCommand.RouteOperationId}' transitionId='{loadingCommand.TransitionId}' routeSequence='{loadingCommand.RouteSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' hideAfterCompletion='{loadingCommand.HideAfterCompletion}'.",
                DebugUtility.Colors.Success);

            if (loadingCommand.HideAfterCompletion)
            {
                await _loadingAdapter.HideLoadingAsync(
                    loadingCommand,
                    CreateLoadingFact(
                        loadingCommand,
                        SessionOperationalLoadingStage.LoadingHidden,
                        SessionOperationalLoadingOutcomeKind.Hidden,
                        1.0f,
                        "Loading hidden",
                        "LoadingHidden"));
                loadingHidden = true;

                DebugUtility.Log(typeof(OperationalLoadingStage),
                    $"LoadingHidden routeIdentity='{loadingCommand.RouteIdentity}' routeOperationId='{loadingCommand.RouteOperationId}' transitionId='{loadingCommand.TransitionId}' routeSequence='{loadingCommand.RouteSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
            }
            else
            {
                DebugUtility.LogVerbose(typeof(OperationalLoadingStage),
                    $"LoadingHiddenSkipped routeIdentity='{loadingCommand.RouteIdentity}' routeOperationId='{loadingCommand.RouteOperationId}' transitionId='{loadingCommand.TransitionId}' routeSequence='{loadingCommand.RouteSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' hideAfterCompletion='false'.",
                    DebugUtility.Colors.Info);
            }

            return new OperationalLoadingCompletionState(loadingCompleted, loadingHidden);
        }

        public async Task<OperationalLoadingResult> ExecuteFailureCleanupAsync(
            OperationalLoadingCommand command,
            bool loadingCompleted)
        {
            ValidateCommand(command);
            var loadingCommand = command.LoadingCommand;
            if (!loadingCommand.IsEnabled)
            {
                return new OperationalLoadingResult(
                    OperationalLoadingResultKind.Skipped,
                    false,
                    loadingCompleted,
                    false,
                    "loading_disabled",
                    "Route loading is disabled by route policy.");
            }

            try
            {
                await _loadingAdapter.HideLoadingAsync(
                    loadingCommand,
                    CreateLoadingFact(
                        loadingCommand,
                        SessionOperationalLoadingStage.LoadingHidden,
                        loadingCompleted ? SessionOperationalLoadingOutcomeKind.Hidden : SessionOperationalLoadingOutcomeKind.Failed,
                        loadingCompleted ? 1.0f : 0.95f,
                        loadingCompleted ? "Loading hidden" : "Loading hidden after failure",
                        loadingCompleted ? "LoadingHidden" : "LoadingHiddenAfterFailure"));

                return new OperationalLoadingResult(
                    OperationalLoadingResultKind.Completed,
                    false,
                    loadingCompleted,
                    true,
                    loadingCompleted ? "loading_hidden" : "loading_hidden_after_failure",
                    "Loading cleanup completed.");
            }
            catch (Exception cleanupEx)
            {
                DebugUtility.LogError<OperationalLoadingStage>(
                    $"hide_cleanup_failed routeIdentity='{loadingCommand.RouteIdentity}' routeOperationId='{loadingCommand.RouteOperationId}' transitionId='{loadingCommand.TransitionId}' routeSequence='{loadingCommand.RouteSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' exceptionType='{cleanupEx.GetType().Name}' exceptionMessage='{cleanupEx.Message}'.");

                return new OperationalLoadingResult(
                    OperationalLoadingResultKind.Failed,
                    false,
                    loadingCompleted,
                    false,
                    "hide_cleanup_failed",
                    cleanupEx.Message);
            }
        }

        private void ValidateCommand(OperationalLoadingCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalLoadingCommand is invalid.");
            }

            if (command.LoadingCommand.IsEnabled && _loadingAdapter == null)
            {
                throw new InvalidOperationException("[FATAL][SessionOperationalPipeline][Loading] ILoadingAdapter is required for operational loading.");
            }
        }

        private static SessionOperationalLoadingFact CreateLoadingFact(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingStage stage,
            SessionOperationalLoadingOutcomeKind outcomeKind,
            float normalizedProgress,
            string stepLabel,
            string message)
        {
            return new SessionOperationalLoadingFact(command, stage, outcomeKind, normalizedProgress, stepLabel, message);
        }

        private static void LogLoadingProgress(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingStage stage,
            float normalizedProgress,
            string stepLabel,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(OperationalLoadingStage),
                $"LoadingProgress routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' stage='{stage}' normalizedProgress='{normalizedProgress:0.###}' stepLabel='{stepLabel}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}
