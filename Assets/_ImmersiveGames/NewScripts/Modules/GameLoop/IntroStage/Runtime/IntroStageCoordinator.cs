#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageCoordinator : IIntroStageCoordinator
    {
        private const string SimulationGateToken = SimulationGateTokens.GameplaySimulation;
        // Fail-safe operacional: evita travar o fluxo se nenhum sinal canônico for emitido.
        private int _inProgress;

        public async Task RunIntroStageAsync(IntroStageContext context)
        {
            var gameLoop = ResolveGameLoop();
            var policy = ResolvePolicy(context);

            if (TryHandlePolicyShortCircuit(policy, context, gameLoop))
            {
                return;
            }

            if (!TryEnterInProgress(context))
            {
                return;
            }

            string signature = NormalizeSignature(context.ContextSignature);
            string reason = NormalizeReason(context.Reason);
            string targetScene = NormalizeValue(context.TargetScene);
            string routeLabel = FormatRouteKind(context.RouteKind);

            var step = ResolveStep();
            var simulationGate = ResolveSimulationGateService();
            bool simulationGateAcquired = false;

            // ADR-0013: RequestStart deve acontecer APÓS IntroStageCompleted + liberação do gate.
            bool requestStartAfterComplete = false;
            string requestStartReason = "IntroStageController/Completed";

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageController] IntroStageStarted signature='{signature}' routeKind='{routeLabel}' target='{targetScene}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            if (gameLoop == null)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    "[IntroStageController] IGameLoopService indisponível. IntroStageController seguirá sem sincronizar estado do GameLoop.");
            }

            try
            {
                var controlService = ResolveIntroStageControlService();
                if (controlService == null)
                {
                    DebugUtility.LogWarning<IntroStageCoordinator>(
                        "[IntroStageController] IIntroStageControlService indisponível. IntroStageController será concluída imediatamente.");

                    gameLoop?.RequestIntroStageStart();
                    simulationGateAcquired = AcquireSimulationGate(simulationGate, signature, routeLabel, targetScene, reason);
                    LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Completed);

                    requestStartAfterComplete = true;
                    requestStartReason = "IntroStageController/Auto";
                    return;
                }

                controlService.BeginIntroStage(context);
                gameLoop?.RequestIntroStageStart();
                simulationGateAcquired = AcquireSimulationGate(simulationGate, signature, routeLabel, targetScene, reason);

                DebugUtility.Log<IntroStageCoordinator>(
                    "[IntroStageController] IntroStageController ativa: simulação gameplay bloqueada; aguardando confirmação (UI).",
                    DebugUtility.Colors.Info);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DebugUtility.LogVerbose<IntroStageCoordinator>(
                    "[QA][IntroStageController] EditorQAActions disponíveis para Complete/Skip em Editor/Dev.",
                    DebugUtility.Colors.Info);
#endif

                if (step == null || !step.HasContent)
                {
                    controlService.SkipIntroStage("IntroStageController/NoContent");
                }
                else
                {
                    _ = RunStepSafelyAsync(step, context, controlService);
                }

                // IMPORTANT: Must resume on Unity main thread because this method touches Unity APIs
                // (SceneManager, SimulationGate callbacks, etc.). Avoid ConfigureAwait(false) here.
                var completion = await controlService.WaitForCompletionAsync(CancellationToken.None);
                if (completion.WasSkipped)
                {
                    string skipReason = NormalizeValue(completion.Reason);
                    LogSkipped(skipReason, context, SceneManager.GetActiveScene().name);
                    LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Skipped);
                    requestStartReason = $"IntroStageController/Skipped/{skipReason}";
                }
                else
                {
                    LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Completed);
                    requestStartReason = "IntroStageController/UIConfirm";
                }

                requestStartAfterComplete = true;
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[IntroStageController] Falha ao executar IntroStageController. signature='{signature}', ex='{ex.GetType().Name}: {ex.Message}'.");

                LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Failed);

                requestStartAfterComplete = true;
                requestStartReason = "IntroStageController/ErrorFallback";
            }
            finally
            {
                gameLoop?.RequestIntroStageComplete();

                if (simulationGateAcquired)
                {
                    ReleaseSimulationGate(simulationGate, signature, routeLabel, targetScene);
                }

                // RequestStart somente depois do Completed + gate liberado.
                if (requestStartAfterComplete)
                {
                    RequestStartIfNeeded(gameLoop, requestStartReason);
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

        private static bool TryHandlePolicyShortCircuit(
            IntroStagePolicy policy,
            IntroStageContext context,
            IGameLoopService? gameLoop)
        {
            if (policy == IntroStagePolicy.Disabled)
            {
                LogSkipped("policy_disabled", context, SceneManager.GetActiveScene().name);
                RequestStartIfNeeded(gameLoop, "policy_disabled");
                return true;
            }

            if (policy == IntroStagePolicy.AutoComplete)
            {
                string normalizeSignature = NormalizeSignature(context.ContextSignature);
                string normalizedTargetScene = NormalizeValue(context.TargetScene);

                LogCompletionWithReason(normalizeSignature, normalizedTargetScene, FormatRouteKind(context.RouteKind), "policy_autocomplete");
                RequestStartIfNeeded(gameLoop, "policy_autocomplete");
                return true;
            }

            return false;
        }

        private bool TryEnterInProgress(IntroStageContext context)
        {
            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[OBS][IntroStageController] IntroStageSkipped reason='in_progress' signature='{NormalizeSignature(context.ContextSignature)}' routeKind='{FormatRouteKind(context.RouteKind)}'.");
                return false;
            }

            return true;
        }

        private static IntroStagePolicy ResolvePolicy(IntroStageContext context)
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStagePolicyResolver>(out var resolver) && resolver != null)
            {
                return resolver.Resolve(context.RouteKind, context.Reason);
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

            string stepName = step.GetType().Name;
            using var cts = new CancellationTokenSource();

            // If the IntroStageController finishes by QA (Complete/Skip), cancel the step.
            _ = controlService.WaitForCompletionAsync(CancellationToken.None)
                .ContinueWith(_ => cts.Cancel(), TaskScheduler.Default);

            try
            {
                await step.RunAsync(context, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when QA completes/skips IntroStageController.
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[IntroStageController] Falha ao executar IntroStageController. step='{stepName}', ex='{ex.GetType().Name}: {ex.Message}'.");

                // Ensure a canonical end if the step fails.
                controlService.SkipIntroStage("step_failed");
            }
        }

        private static void RequestStartIfNeeded(IGameLoopService? gameLoop, string reason)
        {
            if (gameLoop == null)
            {
                return;
            }

            DebugUtility.LogVerbose<IntroStageCoordinator>(
                $"[IntroStageController] Solicitando RequestStart após término da IntroStageController. reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);

            gameLoop.RequestStart();
        }

        private static bool AcquireSimulationGate(
            ISimulationGateService? gateService,
            string signature,
            string routeKind,
            string targetScene,
            string reason)
        {
            if (gateService == null)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    "[IntroStageController] ISimulationGateService indisponível; simulação gameplay pode não ser bloqueada durante IntroStageController.");
                return false;
            }

            gateService.Acquire(SimulationGateToken);

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageController] GameplaySimulationBlocked token='{SimulationGateToken}' signature='{signature}' " +
                $"routeKind='{routeKind}' target='{targetScene}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private static void ReleaseSimulationGate(
            ISimulationGateService? gateService,
            string signature,
            string routeKind,
            string targetScene)
        {
            if (gateService == null)
            {
                return;
            }

            gateService.Release(SimulationGateToken);

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageController] GameplaySimulationUnblocked token='{SimulationGateToken}' signature='{signature}' " +
                $"routeKind='{routeKind}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogCompletion(string signature, string targetScene, string routeKind, IntroStageRunResult result)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageController] IntroStageCompleted signature='{signature}' result='{FormatResult(result)}' routeKind='{routeKind}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogCompletionWithReason(string signature, string targetScene, string routeKind, string reason)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageController] IntroStageCompleted signature='{signature}' result='completed' reason='{NormalizeReason(reason)}' " +
                $"routeKind='{routeKind}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogSkipped(string reason, IntroStageContext context, string sceneName)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageController] IntroStageSkipped reason='{reason}' signature='{NormalizeSignature(context.ContextSignature)}' " +
                $"routeKind='{FormatRouteKind(context.RouteKind)}' target='{NormalizeValue(context.TargetScene)}' scene='{NormalizeValue(sceneName)}'.",
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

        private static string FormatRouteKind(SceneRouteKind routeKind)
            => routeKind.ToString();

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













