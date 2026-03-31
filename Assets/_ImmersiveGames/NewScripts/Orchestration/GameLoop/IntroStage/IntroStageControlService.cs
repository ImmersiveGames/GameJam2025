#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage
{
    /// <summary>
    /// Servico global que controla o termino da IntroStage via comando explicito.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageControlService : IIntroStageControlService
    {
        private enum IntroStageExecutionState
        {
            Idle,
            Beginning,
            WaitingConfirm,
            Completed,
            Skipped,
            Superseded,
            Cancelled
        }

        private readonly object _sync = new();
        private TaskCompletionSource<IntroStageCompletionResult> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _isActive;
        private IntroStageContext _activeContext;
        private IntroStageExecutionState _state = IntroStageExecutionState.Idle;

        public bool IsIntroStageActive
        {
            get
            {
                lock (_sync)
                {
                    return _isActive;
                }
            }
        }

        public void BeginIntroStage(IntroStageContext context)
        {
            TaskCompletionSource<IntroStageCompletionResult>? previousSource = null;
            bool hadActiveContext;

            lock (_sync)
            {
                hadActiveContext = _isActive;
                if (hadActiveContext)
                {
                    previousSource = _completionSource;
                    _state = IntroStageExecutionState.Superseded;
                }

                if (_isActive)
                {
                    DebugUtility.LogWarning<IntroStageControlService>(
                        "[OBS][EnterStageController] BeginEnterStage chamado enquanto outro EnterStage ainda esta ativo. GameplayResetando para o novo contexto.");
                }

                _isActive = true;
                _activeContext = context;
                _state = IntroStageExecutionState.Beginning;
                _completionSource = new TaskCompletionSource<IntroStageCompletionResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                _state = IntroStageExecutionState.WaitingConfirm;
            }

            if (hadActiveContext && previousSource != null && !previousSource.Task.IsCompleted)
            {
                previousSource.TrySetResult(new IntroStageCompletionResult("superseded", wasSkipped: true));
            }
        }

        public Task<IntroStageCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<IntroStageCompletionResult> source;
            lock (_sync)
            {
                source = _completionSource;
            }

            if (!cancellationToken.CanBeCanceled)
            {
                return source.Task;
            }

            return AwaitWithCancellationAsync(source.Task, cancellationToken);
        }

        public void CompleteIntroStage(string reason)
        {
            FinishIntroStage(reason, wasSkipped: false);
        }

        public void SkipIntroStage(string reason)
        {
            FinishIntroStage(reason, wasSkipped: true);
        }

        public void MarkSessionClosed()
        {
            lock (_sync)
            {
                if (!_isActive)
                {
                    return;
                }

                if (_state == IntroStageExecutionState.WaitingConfirm)
                {
                    _state = IntroStageExecutionState.Cancelled;
                }
            }
        }

        private void FinishIntroStage(string reason, bool wasSkipped)
        {
            try
            {
                TaskCompletionSource<IntroStageCompletionResult> source;
                IntroStageContext context = default;
                bool wasActive;
                bool alreadyCompleted;
                IntroStageExecutionState previousState;

                lock (_sync)
                {
                    source = _completionSource;
                    wasActive = _isActive;
                    alreadyCompleted = source.Task.IsCompleted;
                    previousState = _state;

                    if (_isActive)
                    {
                        context = _activeContext;
                        _isActive = false;
                        _state = wasSkipped ? IntroStageExecutionState.Skipped : IntroStageExecutionState.Completed;
                    }
                }

                string normalizedReason = NormalizeValue(reason);
                string actionName = wasSkipped ? "SkipIntroStage" : "CompleteIntroStage";
                string gameLoopState = NormalizeValue(ResolveGameLoopStateName());
                var logContext = BuildSafeLogContext(context);
                string signature = logContext.Signature;
                string routeKind = logContext.RouteKind;
                string targetScene = logContext.TargetScene;

                if (!wasActive)
                {
                    string ignoreReason = alreadyCompleted ? "already_completed" : "not_active";
                    DebugUtility.Log<IntroStageControlService>(
                        $"[OBS][EnterStageController] {actionName} received reason='{normalizedReason}' skip={wasSkipped.ToString().ToLowerInvariant()} decision='ignored' ignoreReason='{ignoreReason}' state='{gameLoopState}' executionState='{previousState}' isActive=false signature='{signature}' routeKind='{routeKind}' target='{targetScene}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                DebugUtility.Log<IntroStageControlService>(
                    $"[OBS][EnterStageController] {actionName} received reason='{normalizedReason}' skip={wasSkipped.ToString().ToLowerInvariant()} decision='applied' state='{gameLoopState}' executionState='{_state}' isActive=true signature='{signature}' routeKind='{routeKind}' target='{targetScene}'.",
                    DebugUtility.Colors.Info);

                if (string.Equals(normalizedReason, "timeout", StringComparison.OrdinalIgnoreCase))
                {
                    DebugUtility.LogWarning<IntroStageControlService>(
                        $"[OBS][EnterStageController] EnterStageTimedOut signature='{signature}' routeKind='{routeKind}' target='{targetScene}'.");
                }

                source.TrySetResult(new IntroStageCompletionResult(normalizedReason, wasSkipped));

                if (context.IsValid)
                {
                    DebugUtility.Log<IntroStageControlService>(
                        $"[OBS][EnterStageController] EnterStageCompletedPublished source='IntroStageControlService' signature='{signature}' routeKind='{routeKind}' target='{targetScene}' skipped={wasSkipped.ToString().ToLowerInvariant()} reason='{normalizedReason}'.",
                        DebugUtility.Colors.Info);

                    EventBus<LevelIntroCompletedEvent>.Raise(new LevelIntroCompletedEvent(
                        context.Session,
                        "IntroStageControlService",
                        wasSkipped,
                        normalizedReason));
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<IntroStageControlService>(
                    $"[OBS][IntroStageController] FinishIntroStage FAILED ex='{ex.GetType().Name}: {ex.Message}'.");
                throw;
            }
        }

        private static async Task<IntroStageCompletionResult> AwaitWithCancellationAsync(
            Task<IntroStageCompletionResult> task,
            CancellationToken cancellationToken)
        {
            if (task.IsCompleted)
            {
                return await task.ConfigureAwait(false);
            }

            var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var registration = cancellationToken.Register(() => completionSource.TrySetResult(true));

            if (task == await Task.WhenAny(task, completionSource.Task).ConfigureAwait(false))
            {
                return await task.ConfigureAwait(false);
            }

            return new IntroStageCompletionResult("cancelled", wasSkipped: true);
        }

        private static string NormalizeValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static string ResolveGameLoopStateName()
        {
            return DependencyManager.Provider != null
                   && DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoop)
                   && gameLoop != null
                ? gameLoop.CurrentStateIdName
                : "<none>";
        }

        private static IntroStageLogContext BuildSafeLogContext(IntroStageContext context)
        {
            try
            {
                return new IntroStageLogContext(
                    NormalizeValue(context.ContextSignature),
                    NormalizeValue(context.RouteKind.ToString()),
                    NormalizeValue(context.TargetScene));
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<IntroStageControlService>(
                    $"[OBS][IntroStageController] Failed to read IntroStageContext for logging. ex='{ex.GetType().Name}: {ex.Message}'.");
                return IntroStageLogContext.Fallback;
            }
        }

        private readonly struct IntroStageLogContext
        {
            public static readonly IntroStageLogContext Fallback = new("<error>", "<error>", "<error>");

            public string Signature { get; }
            public string RouteKind { get; }
            public string TargetScene { get; }

            public IntroStageLogContext(string signature, string routeKind, string targetScene)
            {
                Signature = signature;
                RouteKind = routeKind;
                TargetScene = targetScene;
            }
        }
    }
}

