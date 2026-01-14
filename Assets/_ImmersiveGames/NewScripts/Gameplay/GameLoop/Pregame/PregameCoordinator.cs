#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PregameCoordinator : IPregameCoordinator
    {
        private const string SimulationGateToken = SimulationGateTokens.GameplaySimulation;
        private int _inProgress;

        public async Task RunPregameAsync(PregameContext context)
        {
            if (!context.ProfileId.IsGameplay)
            {
                LogSkipped("profile_not_gameplay", context, SceneManager.GetActiveScene().name);
                return;
            }

            var classifier = ResolveGameplaySceneClassifier();
            if (classifier != null && !classifier.IsGameplayScene())
            {
                LogSkipped("scene_not_gameplay", context, SceneManager.GetActiveScene().name);
                return;
            }

            var step = ResolveStep(out var fromDi);

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<PregameCoordinator>(
                    $"[OBS][Pregame] PregameSkipped reason='in_progress' signature='{NormalizeSignature(context.ContextSignature)}' profile='{context.ProfileId.Value}'.");
                return;
            }

            var signature = NormalizeSignature(context.ContextSignature);
            var reason = NormalizeReason(context.Reason);
            var targetScene = NormalizeValue(context.TargetScene);

            var simulationGate = ResolveSimulationGateService();
            var simulationGateAcquired = false;

            DebugUtility.Log<PregameCoordinator>(
                $"[OBS][Pregame] PregameStarted signature='{signature}' profile='{context.ProfileId.Value}' target='{targetScene}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            var gameLoop = ResolveGameLoop();
            if (gameLoop == null)
            {
                DebugUtility.LogWarning<PregameCoordinator>(
                    "[Pregame] IGameLoopService indisponível. Pregame seguirá sem sincronizar estado do GameLoop.");
            }

            try
            {
                var controlService = ResolvePregameControlService();
                if (controlService == null)
                {
                    DebugUtility.LogWarning<PregameCoordinator>(
                        "[Pregame] IPregameControlService indisponível. Pregame será concluído imediatamente.");
                    gameLoop?.RequestPregameStart();
                    simulationGateAcquired = AcquireSimulationGate(simulationGate, signature, context.ProfileId.Value, targetScene, reason);
                    LogCompletion(signature, targetScene, context.ProfileId.Value, PregameRunResult.Completed);
                    RequestStartIfNeeded(gameLoop);
                    return;
                }

                controlService.BeginPregame(context);
                gameLoop?.RequestPregameStart();
                simulationGateAcquired = AcquireSimulationGate(simulationGate, signature, context.ProfileId.Value, targetScene, reason);
                DebugUtility.Log<PregameCoordinator>(
                    "[Pregame] Pregame ativo: simulação gameplay bloqueada; use QA/Pregame/Complete ou QA/Pregame/Skip para prosseguir.",
                    DebugUtility.Colors.Info);
                DebugUtility.Log<PregameCoordinator>(
                    "[QA][Pregame] Use Inspector(ContextMenu) OU MenuItem para Complete/Skip.",
                    DebugUtility.Colors.Info);

                if (step == null || !step.HasContent)
                {
                    controlService.SkipPregame(fromDi ? "no_content" : "no_step");
                }
                else
                {
                    _ = RunStepSafelyAsync(step, context, controlService);
                }

                var completion = await controlService.WaitForCompletionAsync(CancellationToken.None).ConfigureAwait(false);
                if (completion.WasSkipped)
                {
                    LogSkipped(NormalizeValue(completion.Reason), context, SceneManager.GetActiveScene().name);
                    LogCompletion(signature, targetScene, context.ProfileId.Value, PregameRunResult.Skipped);
                }
                else
                {
                    LogCompletion(signature, targetScene, context.ProfileId.Value, PregameRunResult.Completed);
                }

                RequestStartIfNeeded(gameLoop);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameCoordinator>(
                    $"[Pregame] Falha ao executar pregame. signature='{signature}', ex='{ex.GetType().Name}: {ex.Message}'.");

                LogCompletion(signature, targetScene, context.ProfileId.Value, PregameRunResult.Failed);
                RequestStartIfNeeded(gameLoop);
            }
            finally
            {
                gameLoop?.RequestPregameComplete();
                if (simulationGateAcquired)
                {
                    ReleaseSimulationGate(simulationGate, signature, context.ProfileId.Value, targetScene);
                }
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

        private static IGameplaySceneClassifier? ResolveGameplaySceneClassifier()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier) && classifier != null)
            {
                return classifier;
            }

            return new DefaultGameplaySceneClassifier();
        }

        private static IPregameControlService? ResolvePregameControlService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPregameControlService>(out var service) && service != null)
            {
                return service;
            }

            return null;
        }

        private static ISimulationGateService? ResolveSimulationGateService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var service) && service != null)
            {
                return service;
            }

            return null;
        }

        private static Task RunStepSafelyAsync(
            IPregameStep step,
            PregameContext context,
            IPregameControlService controlService)
        {
            var stepName = step.GetType().Name;
            var cts = new CancellationTokenSource();

            var stepTask = step.RunAsync(context, cts.Token);
            _ = controlService.WaitForCompletionAsync(CancellationToken.None)
                .ContinueWith(_ => cts.Cancel(), TaskScheduler.Default);

            return stepTask.ContinueWith(t =>
            {
                cts.Dispose();
                if (t.Exception == null)
                {
                    return;
                }

                var exception = t.Exception.GetBaseException();
                DebugUtility.LogWarning<PregameCoordinator>(
                    $"[Pregame] Falha ao executar pregame. step='{stepName}', ex='{exception}'.");

                controlService.SkipPregame("step_failed");
            }, TaskScheduler.Default);
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

            DebugUtility.LogVerbose<PregameCoordinator>(
                "[Pregame] Solicitando RequestStart após conclusão explícita do Pregame.",
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
                DebugUtility.LogWarning<PregameCoordinator>(
                    "[Pregame] ISimulationGateService indisponível; simulação gameplay pode não ser bloqueada durante Pregame.");
                return false;
            }

            gateService.Acquire(SimulationGateToken);

            DebugUtility.Log<PregameCoordinator>(
                $"[OBS][Pregame] GameplaySimulationBlocked token='{SimulationGateToken}' signature='{signature}' " +
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

            DebugUtility.Log<PregameCoordinator>(
                $"[OBS][Pregame] GameplaySimulationUnblocked token='{SimulationGateToken}' signature='{signature}' " +
                $"profile='{profile}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogCompletion(string signature, string targetScene, string profile, PregameRunResult result)
        {
            DebugUtility.Log<PregameCoordinator>(
                $"[OBS][Pregame] PregameCompleted signature='{signature}' result='{FormatResult(result)}' profile='{profile}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogSkipped(string reason, PregameContext context, string sceneName)
        {
            DebugUtility.Log<PregameCoordinator>(
                $"[OBS][Pregame] PregameSkipped reason='{reason}' signature='{NormalizeSignature(context.ContextSignature)}' " +
                $"profile='{context.ProfileId.Value}' target='{NormalizeValue(context.TargetScene)}' scene='{NormalizeValue(sceneName)}'.",
                DebugUtility.Colors.Info);
        }

        private static string FormatResult(PregameRunResult result)
        {
            return result switch
            {
                PregameRunResult.Completed => "completed",
                PregameRunResult.Skipped => "skipped",
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
            Skipped,
            Failed
        }
    }
}
