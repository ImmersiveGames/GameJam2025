#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Serviço global que controla o término da IntroStage via comando explícito.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageControlService : IIntroStageControlService
    {
        private readonly object _sync = new();
        private TaskCompletionSource<IntroStageCompletionResult> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _isActive;
        private IntroStageContext _activeContext;

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
            lock (_sync)
            {
                if (_isActive)
                {
                    DebugUtility.LogWarning<IntroStageControlService>(
                        "[IntroStage] BeginIntroStage chamado enquanto outra IntroStage ainda está ativa. Reiniciando gate de conclusão.");
                }

                _isActive = true;
                _activeContext = context;
                _completionSource = new TaskCompletionSource<IntroStageCompletionResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
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

        private void FinishIntroStage(string reason, bool wasSkipped)
        {
            TaskCompletionSource<IntroStageCompletionResult> source;
            IntroStageContext context;
            lock (_sync)
            {
                if (!_isActive)
                {
                    return;
                }

                _isActive = false;
                source = _completionSource;
                context = _activeContext;
            }

            var normalizedReason = NormalizeValue(reason);
            DebugUtility.Log<IntroStageControlService>(
                $"[OBS][IntroStage] CompleteIntroStage received reason='{normalizedReason}' " +
                $"skip={wasSkipped.ToString().ToLowerInvariant()} " +
                $"signature='{NormalizeValue(context.ContextSignature)}' " +
                $"profile='{NormalizeValue(context.ProfileId.Value)}' target='{NormalizeValue(context.TargetScene)}'.",
                DebugUtility.Colors.Info);

            if (string.Equals(normalizedReason, "timeout", StringComparison.OrdinalIgnoreCase))
            {
                DebugUtility.LogWarning<IntroStageControlService>(
                    $"[OBS][IntroStage] IntroStageTimedOut signature='{NormalizeValue(context.ContextSignature)}' " +
                    $"profile='{NormalizeValue(context.ProfileId.Value)}' target='{NormalizeValue(context.TargetScene)}'.");
            }

            source.TrySetResult(new IntroStageCompletionResult(normalizedReason, wasSkipped));
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
            using var registration = cancellationToken.Register(() => completionSource.TrySetResult(true));

            if (task == await Task.WhenAny(task, completionSource.Task).ConfigureAwait(false))
            {
                return await task.ConfigureAwait(false);
            }

            return new IntroStageCompletionResult("cancelled", wasSkipped: true);
        }

        private static string NormalizeValue(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
