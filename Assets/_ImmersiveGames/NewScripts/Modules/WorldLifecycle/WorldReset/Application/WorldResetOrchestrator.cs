using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Core;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Guards;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Policies;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Validation;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Application
{
    /// <summary>
    /// Orquestra o pipeline de reset do WorldLifecycle.
    /// Guard -> Validate -> Discover -> Execute -> PublishCompleted.
    /// </summary>
    public sealed class WorldResetOrchestrator
    {
        private readonly IWorldResetPolicy _policy;
        private readonly IReadOnlyList<IWorldResetGuard> _guards;
        private readonly WorldResetValidationPipeline _validation;
        private readonly WorldResetExecutor _executor;

        public WorldResetOrchestrator(
            IWorldResetPolicy policy,
            IReadOnlyList<IWorldResetGuard> guards,
            WorldResetValidationPipeline validation,
            WorldResetExecutor executor)
        {
            _policy = policy;
            _guards = guards ?? Array.Empty<IWorldResetGuard>();
            _validation = validation ?? new WorldResetValidationPipeline(Array.Empty<IWorldResetValidator>());
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public async Task ExecuteAsync(WorldResetRequest request)
        {
            ResetDecision decision = EvaluateGuards(request);
            if (!decision.ShouldProceed)
            {
                HandleDecision("Guard", decision, request);
                return;
            }

            decision = _validation.Validate(request, _policy);
            if (!decision.ShouldProceed)
            {
                HandleDecision("Validation", decision, request);
                return;
            }

            IReadOnlyList<WorldLifecycleController> controllers = DiscoverControllers(request.TargetScene);
            if (controllers.Count == 0)
            {
                string target = string.IsNullOrWhiteSpace(request.TargetScene) ? "<unknown>" : request.TargetScene;
                string reason = $"{WorldResetReasons.FailedNoControllerPrefix}:{target}";
                LogDegraded($"Nenhum WorldLifecycleController encontrado para reset. targetScene='{target}'.");
                PublishResetCompleted(request, reason);
                return;
            }

            PublishResetStarted(request);

            try
            {
                await _executor.ExecuteAsync(request, controllers, _policy);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<WorldResetOrchestrator>(
                    $"[{ResetLogTags.Failed}] [WorldResetOrchestrator] Erro durante execução do reset. request={request}, ex='{ex}'.");
            }
            finally
            {
                PublishResetCompleted(request, request.Reason);
            }
        }

        private ResetDecision EvaluateGuards(WorldResetRequest request)
        {
            if (_guards == null || _guards.Count == 0)
            {
                return ResetDecision.Proceed();
            }

            for (int i = 0; i < _guards.Count; i++)
            {
                var guard = _guards[i];
                if (guard == null)
                {
                    continue;
                }

                ResetDecision decision = guard.Evaluate(request, _policy);
                if (!decision.ShouldProceed)
                {
                    return decision;
                }
            }

            return ResetDecision.Proceed();
        }

        private static IReadOnlyList<WorldLifecycleController> DiscoverControllers(string targetScene)
        {
            return WorldLifecycleControllerLocator.FindControllersForScene(targetScene);
        }

        private void HandleDecision(string stage, ResetDecision decision, WorldResetRequest request)
        {
            if (decision.IsViolation)
            {
                string mode = _policy != null && _policy.IsStrict ? "STRICT_VIOLATION" : "DEGRADED_MODE";
                string tag = string.Equals(stage, "Guard", StringComparison.Ordinal)
                    ? ResetLogTags.Guarded
                    : ResetLogTags.ValidationFailed;
                DebugUtility.LogWarning<WorldResetOrchestrator>(
                    $"[{tag}][{mode}] {stage} blocked reset. reason='{decision.Reason}', detail='{decision.Detail}', request={request}");

                _policy?.ReportDegraded(
                    ResetFeatureIds.WorldReset,
                    decision.Reason,
                    decision.Detail,
                    request.ContextSignature,
                    request.ProfileName);
            }
            else
            {
                DebugUtility.LogVerbose<WorldResetOrchestrator>(
                    $"[{ResetLogTags.Skipped}] {stage} skipped reset. reason='{decision.Reason}', detail='{decision.Detail}', request={request}");
            }

            if (decision.ShouldPublishCompletion)
            {
                PublishResetCompleted(request, decision.Reason);
            }
        }

        private static void PublishResetStarted(WorldResetRequest request)
        {
            DebugUtility.LogVerbose<WorldResetOrchestrator>(
                $"[{ResetLogTags.Start}][OBS][WorldLifecycle] ResetWorldStarted signature='{request.ContextSignature}' reason='{request.Reason}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldLifecycleResetStartedEvent>.Raise(
                new WorldLifecycleResetStartedEvent(request.ContextSignature, request.Reason));
        }

        private static void PublishResetCompleted(WorldResetRequest request, string reason)
        {
            string completionReason = reason ?? string.Empty;

            DebugUtility.LogVerbose<WorldResetOrchestrator>(
                $"[{ResetLogTags.Completed}][OBS][WorldLifecycle] ResetCompleted signature='{request.ContextSignature}' reason='{completionReason}'.",
                DebugUtility.Colors.Success);

            EventBus<WorldLifecycleResetCompletedEvent>.Raise(
                new WorldLifecycleResetCompletedEvent(request.ContextSignature, completionReason));
        }

        private void LogDegraded(string message)
        {
            if (_policy != null && _policy.IsStrict)
            {
                DebugUtility.LogWarning<WorldResetOrchestrator>(
                    $"[{ResetLogTags.DegradedMode}][STRICT_VIOLATION] {message}");
            }
            else
            {
                DebugUtility.LogWarning<WorldResetOrchestrator>(
                    $"[{ResetLogTags.DegradedMode}][DEGRADED_MODE] {message}");
            }

            _policy?.ReportDegraded(
                ResetFeatureIds.WorldReset,
                "WorldResetDegraded",
                message);
        }
    }
}
