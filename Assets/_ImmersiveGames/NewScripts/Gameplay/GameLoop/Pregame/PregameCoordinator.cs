#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PregameCoordinator : IPregameCoordinator
    {
        private const int DefaultTimeoutMs = 10000;
        private int _inProgress;

        public async Task RunPregameAsync(PregameContext context)
        {
            if (!context.ProfileId.IsGameplay)
            {
                LogSkipped("profile_not_gameplay", context);
                return;
            }

            var step = ResolveStep(out var fromDi);
            if (step == null)
            {
                LogSkipped("no_step", context);
                return;
            }

            if (!step.HasContent)
            {
                LogSkipped(fromDi ? "no_content" : "no_step", context);
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<PregameCoordinator>(
                    $"[OBS][Pregame] PregameSkipped reason='in_progress' signature='{NormalizeSignature(context.ContextSignature)}' profile='{context.ProfileId.Value}'.");
                return;
            }

            var signature = NormalizeSignature(context.ContextSignature);
            var reason = NormalizeReason(context.Reason);
            var targetScene = NormalizeValue(context.TargetScene);

            DebugUtility.Log<PregameCoordinator>(
                $"[OBS][Pregame] PregameStarted signature='{signature}' profile='{context.ProfileId.Value}' target='{targetScene}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            var gameLoop = ResolveGameLoop();
            if (gameLoop == null)
            {
                DebugUtility.LogWarning<PregameCoordinator>(
                    "[Pregame] IGameLoopService indisponível. Pregame seguirá sem sincronizar estado do GameLoop.");
            }
            else
            {
                gameLoop.RequestPregameStart();
            }

            try
            {
                var stepName = step.GetType().Name;
                var result = await ExecuteStepWithTimeoutAsync(step, stepName, context, DefaultTimeoutMs);
                LogCompletion(signature, targetScene, context.ProfileId.Value, result);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameCoordinator>(
                    $"[Pregame] Falha ao executar pregame. signature='{signature}', ex='{ex.GetType().Name}: {ex.Message}'.");

                LogCompletion(signature, targetScene, context.ProfileId.Value, PregameRunResult.Failed);
            }
            finally
            {
                gameLoop?.RequestPregameComplete();
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        private static IPregameStep ResolveStep(out bool fromDi)
        {
            fromDi = false;
            if (DependencyManager.Provider.TryGetGlobal<IPregameStep>(out var step) && step != null)
            {
                fromDi = true;
                return step;
            }

            return new NoOpPregameStep();
        }

        private static IGameLoopService? ResolveGameLoop()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var loop)
                ? loop
                : null;
        }

        private static async Task<PregameRunResult> ExecuteStepWithTimeoutAsync(
            IPregameStep step,
            string stepName,
            PregameContext context,
            int timeoutMs)
        {
            if (step == null)
            {
                return PregameRunResult.Completed;
            }

            // Mesmo com timeout fail-safe, passamos um token que cancela no timeout (melhor esforço).
            using var cts = timeoutMs > 0 ? new CancellationTokenSource(timeoutMs) : new CancellationTokenSource();

            var stepTask = step.RunAsync(context, cts.Token);

            if (timeoutMs <= 0)
            {
                try
                {
                    await stepTask;
                    return PregameRunResult.Completed;
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<PregameCoordinator>(
                        $"[Pregame] Falha ao executar pregame. step='{stepName}', ex='{ex.GetType().Name}: {ex.Message}'.");
                    return PregameRunResult.Failed;
                }
            }

            var completed = await Task.WhenAny(stepTask, Task.Delay(timeoutMs));
            if (completed != stepTask)
            {
                DebugUtility.LogWarning<PregameCoordinator>(
                    $"[OBS][Pregame] PregameTimedOut step='{stepName}' timeoutMs={timeoutMs} signature='{NormalizeSignature(context.ContextSignature)}'.");

                // Observe/log eventual falha tardia do step para evitar UnobservedTaskException.
                _ = stepTask.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        DebugUtility.LogWarning<PregameCoordinator>(
                            $"[Pregame] Step terminou com erro após timeout. step='{stepName}', ex='{t.Exception.GetBaseException()}'.");
                    }
                });

                return PregameRunResult.TimedOut;
            }

            try
            {
                await stepTask;
                return PregameRunResult.Completed;
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameCoordinator>(
                    $"[Pregame] Falha ao executar pregame. step='{stepName}', ex='{ex.GetType().Name}: {ex.Message}'.");
                return PregameRunResult.Failed;
            }
        }

        private static void LogCompletion(string signature, string targetScene, string profile, PregameRunResult result)
        {
            DebugUtility.Log<PregameCoordinator>(
                $"[OBS][Pregame] PregameCompleted signature='{signature}' result='{FormatResult(result)}' profile='{profile}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogSkipped(string reason, PregameContext context)
        {
            DebugUtility.Log<PregameCoordinator>(
                $"[OBS][Pregame] PregameSkipped reason='{reason}' signature='{NormalizeSignature(context.ContextSignature)}' profile='{context.ProfileId.Value}'.",
                DebugUtility.Colors.Info);
        }

        private static string FormatResult(PregameRunResult result)
        {
            return result switch
            {
                PregameRunResult.Completed => "completed",
                PregameRunResult.TimedOut => "timed_out",
                PregameRunResult.Failed => "failed",
                _ => "unknown"
            };
        }

        private static string NormalizeSignature(string signature)
            => string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "n/a" : reason.Trim();

        private static string NormalizeValue(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private enum PregameRunResult
        {
            Completed,
            TimedOut,
            Failed
        }
    }
}
