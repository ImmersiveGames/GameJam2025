#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;

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
            try
            {
                TaskCompletionSource<IntroStageCompletionResult> source;
                IntroStageContext context = default;
                bool wasActive;
                bool alreadyCompleted;

                lock (_sync)
                {
                    source = _completionSource;
                    wasActive = _isActive;
                    alreadyCompleted = source.Task.IsCompleted;

                    // Só captura contexto quando de fato estava ativo.
                    if (_isActive)
                    {
                        context = _activeContext;
                        _isActive = false;
                    }
                }

                var normalizedReason = NormalizeValue(reason);
                var actionName = wasSkipped ? "SkipIntroStage" : "CompleteIntroStage";
                var gameLoopState = NormalizeValue(ResolveGameLoopStateName());

                // Contexto de log extraído defensivamente (com fallback e log de erro se falhar).
                var logContext = BuildSafeLogContext(context);
                var signature = logContext.Signature;
                var profile = logContext.Profile;
                var targetScene = logContext.TargetScene;

                if (!wasActive)
                {
                    var ignoreReason = alreadyCompleted ? "already_completed" : "not_active";
                    DebugUtility.Log<IntroStageControlService>(
                        $"[OBS][IntroStage] {actionName} received reason='{normalizedReason}' " +
                        $"skip={wasSkipped.ToString().ToLowerInvariant()} decision='ignored' " +
                        $"ignoreReason='{ignoreReason}' state='{gameLoopState}' isActive=false " +
                        $"signature='{signature}' profile='{profile}' target='{targetScene}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                DebugUtility.Log<IntroStageControlService>(
                    $"[OBS][IntroStage] {actionName} received reason='{normalizedReason}' " +
                    $"skip={wasSkipped.ToString().ToLowerInvariant()} decision='applied' " +
                    $"state='{gameLoopState}' isActive=true signature='{signature}' " +
                    $"profile='{profile}' target='{targetScene}'.",
                    DebugUtility.Colors.Info);

                if (string.Equals(normalizedReason, "timeout", StringComparison.OrdinalIgnoreCase))
                {
                    DebugUtility.LogWarning<IntroStageControlService>(
                        $"[OBS][IntroStage] IntroStageTimedOut signature='{signature}' " +
                        $"profile='{profile}' target='{targetScene}'.");
                }

                source.TrySetResult(new IntroStageCompletionResult(normalizedReason, wasSkipped));
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<IntroStageControlService>(
                    $"[OBS][IntroStage] FinishIntroStage FAILED ex='{ex.GetType().Name}: {ex.Message}'. " +
                    "Isto indica bug de logging/estado (context/profile/etc). Verifique stacktrace no Console.");
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
            using var registration = cancellationToken.Register(() => completionSource.TrySetResult(true));

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
                    NormalizeValue(context.ProfileId.Value),
                    NormalizeValue(context.TargetScene));
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<IntroStageControlService>(
                    $"[OBS][IntroStage] Failed to read IntroStageContext for logging. ex='{ex.GetType().Name}: {ex.Message}'.");
                return IntroStageLogContext.Fallback;
            }
        }

        private readonly struct IntroStageLogContext
        {
            public static readonly IntroStageLogContext Fallback = new("<error>", "<error>", "<error>");

            public string Signature { get; }
            public string Profile { get; }
            public string TargetScene { get; }

            public IntroStageLogContext(string signature, string profile, string targetScene)
            {
                Signature = signature;
                Profile = profile;
                TargetScene = targetScene;
            }
        }
    }
}
