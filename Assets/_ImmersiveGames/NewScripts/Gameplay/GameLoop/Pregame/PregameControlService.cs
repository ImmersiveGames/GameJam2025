#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Serviço global que controla o término do Pregame via comando explícito.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PregameControlService : IPregameControlService
    {
        private readonly object _sync = new();
        private TaskCompletionSource<PregameCompletionResult> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _isActive;
        private PregameContext _activeContext;

        public bool IsPregameActive
        {
            get
            {
                lock (_sync)
                {
                    return _isActive;
                }
            }
        }

        public void BeginPregame(PregameContext context)
        {
            lock (_sync)
            {
                _isActive = true;
                _activeContext = context;
                _completionSource = new TaskCompletionSource<PregameCompletionResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public Task<PregameCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<PregameCompletionResult> source;
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

        public void CompletePregame(string reason)
        {
            FinishPregame(reason, wasSkipped: false);
        }

        public void SkipPregame(string reason)
        {
            FinishPregame(reason, wasSkipped: true);
        }

        private void FinishPregame(string reason, bool wasSkipped)
        {
            TaskCompletionSource<PregameCompletionResult> source;
            PregameContext context;
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
            DebugUtility.Log<PregameControlService>(
                $"[OBS][Pregame] CompletePregame received reason='{normalizedReason}' " +
                $"skip={wasSkipped.ToString().ToLowerInvariant()} " +
                $"signature='{NormalizeValue(context.ContextSignature)}' " +
                $"profile='{NormalizeValue(context.ProfileId.Value)}' target='{NormalizeValue(context.TargetScene)}'.",
                DebugUtility.Colors.Info);

            if (string.Equals(normalizedReason, "timeout", StringComparison.OrdinalIgnoreCase))
            {
                DebugUtility.LogWarning<PregameControlService>(
                    $"[OBS][Pregame] PregameTimedOut signature='{NormalizeValue(context.ContextSignature)}' " +
                    $"profile='{NormalizeValue(context.ProfileId.Value)}' target='{NormalizeValue(context.TargetScene)}'.");
            }

            source.TrySetResult(new PregameCompletionResult(normalizedReason, wasSkipped));
        }

        private static async Task<PregameCompletionResult> AwaitWithCancellationAsync(
            Task<PregameCompletionResult> task,
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

            return new PregameCompletionResult("cancelled", wasSkipped: true);
        }

        private static string NormalizeValue(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
