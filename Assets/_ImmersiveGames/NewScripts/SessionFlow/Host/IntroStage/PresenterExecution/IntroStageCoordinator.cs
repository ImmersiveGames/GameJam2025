#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.ContentContract;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
namespace _ImmersiveGames.NewScripts.SessionFlow.Host.IntroStage.PresenterExecution
{
    // Este coordinator controla a execução operacional da IntroStage.
    // O release final de gameplay continua acima, no GameLoop.
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
            Exception? fatalIntroFailure = null;

            if (!TryEnterContext(signature))
            {
                return;
            }

            try
            {
                DebugUtility.Log<IntroStageCoordinator>(
                    $"[OBS][IntroStageCoordinator] IntroStageStarted signature='{signature}' routeKind='{routeLabel}' target='{targetScene}' reason='{reason}' hasIntroStage='{context.Session.HasIntroStage}'.",
                    DebugUtility.Colors.Info);

                if (!context.HasIntroStage)
                {
                    LogSkipped("no_content", context);
                    return;
                }

                gateLease = AcquireSimulationGateOrFail(signature, routeLabel, targetScene, reason);
                var step = ResolveStepOrFail();

                controlService.BeginIntroStage(context);
                Task<IntroStageCompletionResult> completionTask = WaitForCompletionAsync(context, CancellationToken.None);

                DebugUtility.Log<IntroStageCoordinator>(
                    "[OBS][IntroStageCoordinator] IntroStage active: gameplay simulation blocked; operational intro gate held until confirmation.",
                    DebugUtility.Colors.Info);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DebugUtility.LogVerbose<IntroStageCoordinator>(
                    "[QA][IntroStageCoordinator] EditorQAActions available for Complete/Skip in Editor/Dev.",
                    DebugUtility.Colors.Info);
#endif

                if (step.HasContent)
                {
                    await RunStepSafelyAsync(step, context);
                }
                else
                {
                    HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                        $"[FATAL][H1][GameLoop] IntroStage step registered without content for signature='{signature}'.");
                }

                var completion = await completionTask.ConfigureAwait(false);

                if (IsSupersededCompletion(completion))
                {
                    DebugUtility.LogVerbose<IntroStageCoordinator>(
                        $"[OBS][IntroStageCoordinator] IntroStageSuperseded signature='{signature}' routeKind='{routeLabel}' target='{targetScene}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (completion.WasSkipped)
                {
                    string skipReason = NormalizeValue(completion.Reason);
                    LogSkipped(skipReason, context);
                    LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Skipped);
                    return;
                }

                LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Completed);
            }
            catch (Exception ex)
            {
                fatalIntroFailure = ex;
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[IntroStageCoordinator] Falha ao executar IntroStage. signature='{signature}', ex='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                if (gateLease != null)
                {
                    gateLease.Dispose();
                    DebugUtility.Log<IntroStageCoordinator>(
                        $"[OBS][IntroStageCoordinator] GameplaySimulationUnblocked token='{SimulationGateTokens.GameplaySimulation}' signature='{signature}' routeKind='{routeLabel}' target='{targetScene}' (intro gate released).",
                        DebugUtility.Colors.Info);
                }

                if (fatalIntroFailure == null &&
                    context.IsValid &&
                    TryResolveGameLoopService(out var gameLoopService) &&
                    gameLoopService != null)
                {
                    if (!context.HasIntroStage)
                    {
                        DebugUtility.Log<IntroStageCoordinator>(
                            $"[OBS][IntroStageCoordinator] PlayingReleased reason='IntroStage/NoContent' signature='{signature}' routeKind='{routeLabel}' target='{targetScene}'.",
                            DebugUtility.Colors.Info);
                    }

                    DebugUtility.Log<IntroStageCoordinator>(
                        $"[OBS][IntroStageCoordinator] GameLoopStartRequested signature='{signature}' routeKind='{routeLabel}' target='{targetScene}' reason='{reason}' (after gameplay unblock).",
                        DebugUtility.Colors.Info);

                    gameLoopService.RequestStart();
                }

                controlService.MarkSessionClosed();

                ReleaseContext(signature);
            }

            if (fatalIntroFailure != null)
            {
                HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                    $"[FATAL][H1][GameLoop] IntroStage execution failed. signature='{signature}' routeKind='{routeLabel}' target='{targetScene}' reason='{reason}' ex='{fatalIntroFailure.GetType().Name}: {fatalIntroFailure.Message}'.",
                    fatalIntroFailure);
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

        private static bool TryResolveGameLoopService(out IGameLoopService? gameLoopService)
        {
            gameLoopService = null;
            return DependencyManager.Provider.TryGetGlobal(out gameLoopService) && gameLoopService != null;
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
                $"[OBS][IntroStageCoordinator] GameplaySimulationBlocked token='{SimulationGateTokens.GameplaySimulation}' signature='{signature}' routeKind='{routeKind}' target='{targetScene}' reason='{reason}'.",
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
                        $"[OBS][IntroStageCoordinator] IntroStageSkipped reason='in_progress' signature='{signature}'.");
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
            IntroStageContext context)
        {
            string stepName = step.GetType().Name;

            try
            {
                await step.RunAsync(context, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // Expected when confirmation completes or the executor is cancelled.
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[IntroStageController] Falha ao executar step. step='{stepName}', ex='{ex.GetType().Name}: {ex.Message}'.");
                throw;
            }
        }

        private static async Task<IntroStageCompletionResult> WaitForCompletionAsync(
            IntroStageContext context,
            CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<IntroStageCompletionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventBinding<IntroStageCompletedEvent> binding = null!;
            binding = new EventBinding<IntroStageCompletedEvent>(evt =>
            {
                if (!evt.Session.IsValid)
                {
                    return;
                }

                if (!string.Equals(evt.Session.SessionSignature, context.ContextSignature, StringComparison.Ordinal))
                {
                    return;
                }

                completionSource.TrySetResult(new IntroStageCompletionResult(evt.Reason, evt.WasSkipped));
            });

            EventBus<IntroStageCompletedEvent>.Register(binding);

            try
            {
                if (cancellationToken.CanBeCanceled)
                {
                    using var registration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));
                    return await completionSource.Task.ConfigureAwait(false);
                }

                return await completionSource.Task.ConfigureAwait(false);
            }
            finally
            {
                EventBus<IntroStageCompletedEvent>.Unregister(binding);
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
                $"[OBS][IntroStageCoordinator] IntroStageCompleted signature='{signature}' result='{FormatResult(result)}' routeKind='{routeKind}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogSkipped(string reason, IntroStageContext context)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageCoordinator] IntroStageSkipped reason='{reason}' signature='{NormalizeSignature(context.ContextSignature)}' routeKind='{FormatRouteKind(context.RouteKind)}' target='{NormalizeValue(context.TargetScene)}'.",
                DebugUtility.Colors.Info);
        }

        private static string FormatResult(IntroStageRunResult result)
        {
            return result switch
            {
                IntroStageRunResult.Completed => "completed",
                IntroStageRunResult.Skipped => "skipped",
                _ => "unknown"
            };
        }

        private enum IntroStageRunResult
        {
            Completed,
            Skipped
        }
    }
}

