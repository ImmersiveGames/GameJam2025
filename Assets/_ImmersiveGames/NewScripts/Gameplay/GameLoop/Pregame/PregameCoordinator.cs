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
                    $"[Pregame] PregameSkipped reason='in_progress' signature='{NormalizeSignature(context.ContextSignature)}'.");
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

            var result = "completed";

            try
            {
                var completed = await AwaitWithTimeoutAsync(
                    step.RunAsync(context, CancellationToken.None),
                    DefaultTimeoutMs);

                if (!completed)
                {
                    result = "timeout";
                }
            }
            catch (Exception ex)
            {
                result = "failed";
                DebugUtility.LogWarning<PregameCoordinator>(
                    $"[Pregame] Falha ao executar pregame. signature='{signature}', ex='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                gameLoop?.RequestPregameComplete();

                DebugUtility.Log<PregameCoordinator>(
                    $"[OBS][Pregame] PregameCompleted signature='{signature}' result='{result}' profile='{context.ProfileId.Value}' target='{targetScene}'.",
                    DebugUtility.Colors.Info);

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

        private static async Task<bool> AwaitWithTimeoutAsync(Task task, int timeoutMs)
        {
            if (task == null)
            {
                return true;
            }

            if (timeoutMs <= 0)
            {
                await task;
                return true;
            }

            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            if (completed != task)
            {
                DebugUtility.LogWarning<PregameCoordinator>(
                    $"[Pregame] Timeout aguardando pregame. timeoutMs={timeoutMs}.");

                _ = task.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        DebugUtility.LogWarning<PregameCoordinator>(
                            $"[Pregame] Pregame terminou com erro após timeout. ex={t.Exception.GetBaseException()}");
                    }
                });

                return false;
            }

            await task;
            return true;
        }

        private static void LogSkipped(string reason, PregameContext context)
        {
            DebugUtility.Log<PregameCoordinator>(
                $"[OBS][Pregame] PregameSkipped reason='{reason}' signature='{NormalizeSignature(context.ContextSignature)}' profile='{context.ProfileId.Value}'.",
                DebugUtility.Colors.Info);
        }

        private static string NormalizeSignature(string signature)
            => string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "n/a" : reason.Trim();

        private static string NormalizeValue(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
