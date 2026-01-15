#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageCoordinator : IIntroStageCoordinator
    {
        private const string SimulationGateToken = SimulationGateTokens.GameplaySimulation;
        // Fail-safe operacional: evita travar o fluxo se nenhum sinal canônico for emitido.
        private const int IntroStageCompletionTimeoutMs = 20000;
        private int _inProgress;

        public async Task RunIntroStageAsync(IntroStageContext context)
        {
            var gameLoop = ResolveGameLoop();
            var policy = ResolvePolicy(context);

            if (policy == IntroStagePolicy.Disabled)
            {
                LogSkipped("policy_disabled", context, SceneManager.GetActiveScene().name);
                RequestStartIfNeeded(gameLoop);
                return;
            }

            if (policy == IntroStagePolicy.AutoComplete)
            {
                var normalizeSignature = NormalizeSignature(context.ContextSignature);
                var normalizedTargetScene = NormalizeValue(context.TargetScene);

                LogCompletionWithReason(normalizeSignature, normalizedTargetScene, context.ProfileId.Value, "policy_autocomplete");
                RequestStartIfNeeded(gameLoop);
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[OBS][IntroStage] IntroStageSkipped reason='in_progress' signature='{NormalizeSignature(context.ContextSignature)}' profile='{context.ProfileId.Value}'.");
                return;
            }

            var signature = NormalizeSignature(context.ContextSignature);
            var reason = NormalizeReason(context.Reason);
            var targetScene = NormalizeValue(context.TargetScene);

            var step = ResolveStep();
            var simulationGate = ResolveSimulationGateService();
            var simulationGateAcquired = false;

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStage] IntroStageStarted signature='{signature}' profile='{context.ProfileId.Value}' target='{targetScene}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            if (gameLoop == null)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    "[IntroStage] IGameLoopService indisponível. IntroStage seguirá sem sincronizar estado do GameLoop.");
            }

            try
            {
                var controlService = ResolveIntroStageControlService();
                if (controlService == null)
                {
                    DebugUtility.LogWarning<IntroStageCoordinator>(
                        "[IntroStage] IIntroStageControlService indisponível. IntroStage será concluída imediatamente.");
                    gameLoop?.RequestIntroStageStart();
                    simulationGateAcquired = AcquireSimulationGate(simulationGate, signature, context.ProfileId.Value, targetScene, reason);
                    LogCompletion(signature, targetScene, context.ProfileId.Value, IntroStageRunResult.Completed);
                    RequestStartIfNeeded(gameLoop);
                    return;
                }

                controlService.BeginIntroStage(context);
                gameLoop?.RequestIntroStageStart();
                simulationGateAcquired = AcquireSimulationGate(simulationGate, signature, context.ProfileId.Value, targetScene, reason);
                DebugUtility.Log<IntroStageCoordinator>(
                    "[IntroStage] IntroStage ativa: simulação gameplay bloqueada; use QA/IntroStage/Complete ou QA/IntroStage/Skip para prosseguir.",
                    DebugUtility.Colors.Info);
                DebugUtility.Log<IntroStageCoordinator>(
                    "[QA][IntroStage] Use Inspector(ContextMenu) OU MenuItem para Complete/Skip.",
                    DebugUtility.Colors.Info);

                if (step == null || !step.HasContent)
                {
                    controlService.SkipIntroStage("IntroStage/NoContent");
                }
                else
                {
                    _ = RunStepSafelyAsync(step, context, controlService);
                }

                // IMPORTANT: Must resume on Unity main thread because this method touches Unity APIs
                // (SceneManager, SimulationGate callbacks, etc.). Avoid ConfigureAwait(false) here.
                var completionTask = controlService.WaitForCompletionAsync(CancellationToken.None);
                var completedTask = await Task.WhenAny(completionTask, Task.Delay(IntroStageCompletionTimeoutMs));

                if (completedTask != completionTask)
                {
                    DebugUtility.LogWarning<IntroStageCoordinator>(
                        $"[OBS][IntroStage] IntroStageTimedOut signature='{signature}' profile='{context.ProfileId.Value}' " +
                        $"target='{targetScene}' timeoutMs={IntroStageCompletionTimeoutMs}.");
                    controlService.SkipIntroStage("timeout");
                }

                var completion = await completionTask;
                if (completion.WasSkipped)
                {
                    LogSkipped(NormalizeValue(completion.Reason), context, SceneManager.GetActiveScene().name);
                    LogCompletion(signature, targetScene, context.ProfileId.Value, IntroStageRunResult.Skipped);
                }
                else
                {
                    LogCompletion(signature, targetScene, context.ProfileId.Value, IntroStageRunResult.Completed);
                }

                RequestStartIfNeeded(gameLoop);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[IntroStage] Falha ao executar IntroStage. signature='{signature}', ex='{ex.GetType().Name}: {ex.Message}'.");

                LogCompletion(signature, targetScene, context.ProfileId.Value, IntroStageRunResult.Failed);
                RequestStartIfNeeded(gameLoop);
            }
            finally
            {
                gameLoop?.RequestIntroStageComplete();
                if (simulationGateAcquired)
                {
                    ReleaseSimulationGate(simulationGate, signature, context.ProfileId.Value, targetScene);
                }
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        private static IIntroStageStep ResolveStep()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageStep>(out var step) && step != null)
            {
                return step;
            }

            return new NoOpIntroStageStep();
        }

        private static IGameLoopService? ResolveGameLoop()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var loop)
                ? loop
                : null;
        }

        private static IIntroStageControlService? ResolveIntroStageControlService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) && service != null)
            {
                return service;
            }

            return null;
        }

        private static IntroStagePolicy ResolvePolicy(IntroStageContext context)
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStagePolicyResolver>(out var resolver) && resolver != null)
            {
                return resolver.Resolve(context.ProfileId, context.TargetScene, context.Reason);
            }

            return IntroStagePolicy.Manual;
        }

        private static ISimulationGateService? ResolveSimulationGateService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var service) && service != null)
            {
                return service;
            }

            return null;
        }

        private static async Task RunStepSafelyAsync(
            IIntroStageStep step,
            IntroStageContext context,
            IIntroStageControlService controlService)
        {
            // This method is intentionally fire-and-forget from RunIntroStageAsync.
            // Keep Unity API interactions on the Unity thread by avoiding ConfigureAwait(false).

            var stepName = step.GetType().Name;
            using var cts = new CancellationTokenSource();

            // If the IntroStage finishes by QA (Complete/Skip), cancel the step.
            _ = controlService.WaitForCompletionAsync(CancellationToken.None)
                .ContinueWith(_ => cts.Cancel(), TaskScheduler.Default);

            try
            {
                await step.RunAsync(context, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when QA completes/skips IntroStage.
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[IntroStage] Falha ao executar IntroStage. step='{stepName}', ex='{ex.GetType().Name}: {ex.Message}'.");

                // Ensure a canonical end if the step fails.
                controlService.SkipIntroStage("step_failed");
            }
        }

        private static void RequestStartIfNeeded(IGameLoopService? gameLoop)
        {
            if (gameLoop == null)
            {
                return;
            }

            if (string.Equals(gameLoop.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal))
            {
                return;
            }

            DebugUtility.LogVerbose<IntroStageCoordinator>(
                "[IntroStage] Solicitando RequestStart após conclusão explícita da IntroStage.",
                DebugUtility.Colors.Info);

            gameLoop.RequestStart();
        }

        private static bool AcquireSimulationGate(
            ISimulationGateService? gateService,
            string signature,
            string profile,
            string targetScene,
            string reason)
        {
            if (gateService == null)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    "[IntroStage] ISimulationGateService indisponível; simulação gameplay pode não ser bloqueada durante IntroStage.");
                return false;
            }

            gateService.Acquire(SimulationGateToken);

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStage] GameplaySimulationBlocked token='{SimulationGateToken}' signature='{signature}' " +
                $"profile='{profile}' target='{targetScene}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private static void ReleaseSimulationGate(
            ISimulationGateService? gateService,
            string signature,
            string profile,
            string targetScene)
        {
            if (gateService == null)
            {
                return;
            }

            gateService.Release(SimulationGateToken);

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStage] GameplaySimulationUnblocked token='{SimulationGateToken}' signature='{signature}' " +
                $"profile='{profile}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogCompletion(string signature, string targetScene, string profile, IntroStageRunResult result)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStage] IntroStageCompleted signature='{signature}' result='{FormatResult(result)}' profile='{profile}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogCompletionWithReason(string signature, string targetScene, string profile, string reason)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStage] IntroStageCompleted signature='{signature}' result='completed' reason='{NormalizeReason(reason)}' " +
                $"profile='{profile}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogSkipped(string reason, IntroStageContext context, string sceneName)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStage] IntroStageSkipped reason='{reason}' signature='{NormalizeSignature(context.ContextSignature)}' " +
                $"profile='{context.ProfileId.Value}' target='{NormalizeValue(context.TargetScene)}' scene='{NormalizeValue(sceneName)}'.",
                DebugUtility.Colors.Info);
        }

        private static string FormatResult(IntroStageRunResult result)
        {
            return result switch
            {
                IntroStageRunResult.Completed => "completed",
                IntroStageRunResult.Skipped => "skipped",
                IntroStageRunResult.Failed => "failed",
                _ => "unknown"
            };
        }

        private static string NormalizeSignature(string signature)
            => string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "n/a" : reason.Trim();

        private static string NormalizeValue(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private enum IntroStageRunResult
        {
            Completed,
            Skipped,
            Failed
        }
    }
}
