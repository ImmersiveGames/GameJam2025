#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime
{
    // Boundary note: this coordinator owns the operational gate of EnterStage.
    // It does not define the final gameplay-release signal.
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageCoordinator : IIntroStageCoordinator
    {
        private const string SimulationGateToken = SimulationGateTokens.GameplaySimulation;
        private readonly object _sync = new();
        private string _activeSignature = string.Empty;

        public async Task RunIntroStageAsync(IntroStageContext context)
        {
            if (!context.IsValid || !context.Session.IsValid)
            {
                HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                    "[FATAL][H1][GameLoop] Invalid intro context received by executor.");
            }

            IIntroStageControlService controlService = ResolveIntroStageControlServiceOrFail();
            string signature = NormalizeSignature(context.ContextSignature);
            string routeLabel = FormatRouteKind(context.RouteKind);
            string targetScene = NormalizeValue(context.TargetScene);
            string reason = NormalizeReason(context.Reason);
            IDisposable? gateLease = null;

            if (!TryEnterContext(signature))
            {
                return;
            }

            try
            {
                DebugUtility.Log<IntroStageCoordinator>(
                    $"[OBS][EnterStageController] EnterStageStarted signature='{signature}' routeKind='{routeLabel}' target='{targetScene}' reason='{reason}' disposition='{context.Session.Disposition}'.",
                    DebugUtility.Colors.Info);

                if (!context.HasIntroStage)
                {
                    LogSkipped("session_no_enterstage", context, SceneManager.GetActiveScene().name);
                    return;
                }

                gateLease = AcquireSimulationGateOrFail(signature, routeLabel, targetScene, reason);
                var step = ResolveStepOrFail();

                controlService.BeginIntroStage(context);

                DebugUtility.Log<IntroStageCoordinator>(
                    "[OBS][EnterStageController] EnterStage active: gameplay simulation blocked; operational intro gate held until confirmation.",
                    DebugUtility.Colors.Info);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DebugUtility.LogVerbose<IntroStageCoordinator>(
                    "[QA][EnterStageController] EditorQAActions available for Complete/Skip in Editor/Dev.",
                    DebugUtility.Colors.Info);
#endif

                if (step.HasContent)
                {
                    await RunStepSafelyAsync(step, context, controlService);
                }
                else
                {
                    HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                        $"[FATAL][H1][GameLoop] EnterStage step registered without content for signature='{signature}'.");
                }

                var completion = await controlService.WaitForCompletionAsync(CancellationToken.None);
                if (IsSupersededCompletion(completion))
                {
                    DebugUtility.LogVerbose<IntroStageCoordinator>(
                        $"[OBS][EnterStageController] EnterStageSuperseded signature='{signature}' routeKind='{routeLabel}' target='{targetScene}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (completion.WasSkipped)
                {
                    string skipReason = NormalizeValue(completion.Reason);
                    LogSkipped(skipReason, context, SceneManager.GetActiveScene().name);
                    LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Skipped);
                    return;
                }

                LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Completed);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[EnterStageController] Falha ao executar EnterStage. signature='{signature}', ex='{ex.GetType().Name}: {ex.Message}'.");

                controlService.SkipIntroStage("EnterStageController/ErrorFallback");

                LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Failed);
            }
            finally
            {
                if (gateLease != null)
                {
                    gateLease.Dispose();
                    DebugUtility.Log<IntroStageCoordinator>(
                        $"[OBS][EnterStageController] GameplaySimulationUnblocked token='{SimulationGateTokens.GameplaySimulation}' signature='{signature}' routeKind='{routeLabel}' target='{targetScene}' (intro gate released).",
                        DebugUtility.Colors.Info);
                }

                controlService.MarkSessionClosed();

                ReleaseContext(signature);
            }
        }

        private static IIntroStageControlService ResolveIntroStageControlServiceOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) && service != null)
            {
                return service;
            }

            HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                "[FATAL][H1][GameLoop] IIntroStageControlService obrigatorio ausente para executar IntroStage.");

            throw new InvalidOperationException("IIntroStageControlService is required.");
        }

        private static IIntroStageStep ResolveStepOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageStep>(out var step) && step != null)
            {
                return step;
            }

            HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                "[FATAL][H1][GameLoop] IIntroStageStep obrigatorio ausente.");

            throw new InvalidOperationException("IIntroStageStep is required.");
        }

        private static IDisposable AcquireSimulationGateOrFail(
            string signature,
            string routeKind,
            string targetScene,
            string reason)
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService) || gateService == null)
            {
                HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                    $"[FATAL][H1][GameLoop] ISimulationGateService obrigatorio ausente para bloquear IntroStage. signature='{signature}' routeKind='{routeKind}' target='{targetScene}' reason='{reason}'.");
            }

            IDisposable lease = gateService!.Acquire(SimulationGateTokens.GameplaySimulation);

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][EnterStageController] GameplaySimulationBlocked token='{SimulationGateTokens.GameplaySimulation}' signature='{signature}' routeKind='{routeKind}' target='{targetScene}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return lease;
        }

        private bool TryEnterContext(string signature)
        {
            lock (_sync)
            {
                if (string.Equals(_activeSignature, signature, StringComparison.Ordinal))
                {
                    DebugUtility.LogWarning<IntroStageCoordinator>(
                        $"[OBS][EnterStageController] EnterStageSkipped reason='in_progress' signature='{signature}'.");
                    return false;
                }

                _activeSignature = signature;
                return true;
            }
        }

        private void ReleaseContext(string signature)
        {
            lock (_sync)
            {
                if (string.Equals(_activeSignature, signature, StringComparison.Ordinal))
                {
                    _activeSignature = string.Empty;
                }
            }
        }

        private static async Task RunStepSafelyAsync(
            IIntroStageStep step,
            IntroStageContext context,
            IIntroStageControlService controlService)
        {
            string stepName = step.GetType().Name;
            using var cts = new CancellationTokenSource();

            _ = controlService.WaitForCompletionAsync(CancellationToken.None)
                .ContinueWith(_ => cts.Cancel(), TaskScheduler.Default);

            try
            {
                await step.RunAsync(context, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when confirmation completes or the executor is cancelled.
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[IntroStageController] Falha ao executar step. step='{stepName}', ex='{ex.GetType().Name}: {ex.Message}'.");

                controlService.SkipIntroStage("step_failed");
            }
        }

        private static bool IsSupersededCompletion(IntroStageCompletionResult completion)
            => completion.WasSkipped &&
               string.Equals(NormalizeValue(completion.Reason), "superseded", StringComparison.OrdinalIgnoreCase);

        private static string FormatRouteKind(SceneRouteKind routeKind)
            => routeKind.ToString();

        private static string NormalizeSignature(string signature)
            => string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "n/a" : reason.Trim();

        private static string NormalizeValue(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static void LogCompletion(string signature, string targetScene, string routeKind, IntroStageRunResult result)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][EnterStageController] EnterStageCompleted signature='{signature}' result='{FormatResult(result)}' routeKind='{routeKind}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogSkipped(string reason, IntroStageContext context, string sceneName)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][EnterStageController] EnterStageSkipped reason='{reason}' signature='{NormalizeSignature(context.ContextSignature)}' routeKind='{FormatRouteKind(context.RouteKind)}' target='{NormalizeValue(context.TargetScene)}' scene='{NormalizeValue(sceneName)}'.",
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

        private enum IntroStageRunResult
        {
            Completed,
            Skipped,
            Failed
        }
    }
}
