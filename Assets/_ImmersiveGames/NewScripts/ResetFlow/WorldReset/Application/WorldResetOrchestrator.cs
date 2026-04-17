using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Infrastructure.SimulationGate;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Contracts;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Domain;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Guards;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Policies;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Validation;
namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Application
{
    /// <summary>
    /// Orquestra a decisÃ£o de reset do WorldReset.
    /// Guard -> Validate -> Dispatch execution -> PostValidate -> PublishCompleted.
    /// </summary>
    public sealed class WorldResetOrchestrator
    {
        private enum LifecycleCheckpoint
        {
            Dispatch,
            Guard,
            Validation,
            Execution,
            Completion
        }

        private readonly IWorldResetPolicy _policy;
        private readonly IReadOnlyList<IWorldResetGuard> _guards;
        private readonly WorldResetValidationPipeline _validation;
        private readonly WorldResetExecutor _executor;
        private readonly WorldResetPostResetValidator _postResetValidator;
        private readonly WorldResetLifecyclePublisher _lifecyclePublisher;

        public WorldResetOrchestrator(
            IWorldResetPolicy policy,
            IReadOnlyList<IWorldResetGuard> guards,
            WorldResetValidationPipeline validation,
            WorldResetExecutor executor,
            WorldResetPostResetValidator postResetValidator,
            WorldResetLifecyclePublisher lifecyclePublisher)
        {
            _policy = policy;
            _guards = guards ?? Array.Empty<IWorldResetGuard>();
            _validation = validation ?? new WorldResetValidationPipeline(Array.Empty<IWorldResetValidator>());
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _postResetValidator = postResetValidator ?? throw new ArgumentNullException(nameof(postResetValidator));
            _lifecyclePublisher = lifecyclePublisher ?? throw new ArgumentNullException(nameof(lifecyclePublisher));
        }

        public static WorldResetOrchestrator CreateDefault(
            IDependencyProvider provider,
            WorldResetLifecyclePublisher lifecyclePublisher)
        {
            if (provider == null)
            {
                throw new InvalidOperationException("IDependencyProvider is required to build the WorldResetOrchestrator.");
            }

            provider.TryGetGlobal(out IWorldResetPolicy policy);
            provider.TryGetGlobal(out ISimulationGateService gateService);

            var guards = new List<IWorldResetGuard>(1)
            {
                new SimulationGateWorldResetGuard(gateService)
            };

            var validators = new List<IWorldResetValidator>(1)
            {
                new WorldResetSignatureValidator()
            };

            WorldResetValidationPipeline validationPipeline = new WorldResetValidationPipeline(validators);
            WorldResetExecutor executor = new WorldResetExecutor();
            WorldResetPostResetValidator postResetValidator = new WorldResetPostResetValidator(provider);

            return new WorldResetOrchestrator(
                policy,
                guards,
                validationPipeline,
                executor,
                postResetValidator,
                lifecyclePublisher);
        }

        public async Task<WorldResetResult> ExecuteAsync(WorldResetRequest request)
        {
            LogLifecycleCheckpoint(LifecycleCheckpoint.Dispatch, request, "received");
            ResetDecision decision = EvaluateGuards(request);
            if (!decision.ShouldProceed)
            {
                LogLifecycleCheckpoint(LifecycleCheckpoint.Guard, request, "blocked");
                LogDecisionFlow("skip", "guard", request, decision);
                return HandleDecision("Guard", decision, request);
            }

            LogLifecycleCheckpoint(LifecycleCheckpoint.Guard, request, "passed");
            decision = _validation.Validate(request, _policy);
            if (!decision.ShouldProceed)
            {
                LogLifecycleCheckpoint(LifecycleCheckpoint.Validation, request, "blocked");
                LogDecisionFlow("skip", "validation", request, decision);
                return HandleDecision("Validation", decision, request);
            }

            LogLifecycleCheckpoint(LifecycleCheckpoint.Validation, request, "passed");
            if (!_executor.TryResolveExecutors(request.TargetScene, out IReadOnlyList<IWorldResetLocalExecutor> executors) ||
                executors == null ||
                executors.Count == 0)
            {
                string target = string.IsNullOrWhiteSpace(request.TargetScene) ? "<unknown>" : request.TargetScene;
                string detail = $"{WorldResetReasons.FailedNoLocalExecutorPrefix}:{target}";
                LogDegraded($"Nenhum executor local neutro encontrado para reset. targetScene='{target}'.");
                LogLifecycleCheckpoint(LifecycleCheckpoint.Execution, request, "missing_local_executor");
                _lifecyclePublisher.PublishCompleted(request, WorldResetOutcome.FailedNoLocalExecutor, detail);
                LogLifecycleCheckpoint(LifecycleCheckpoint.Completion, request, "published_failed_no_local_executor");
                LogCompletionFlow("failed_no_local_executor", request, detail);
                return WorldResetResult.Failed;
            }

            _lifecyclePublisher.PublishStarted(request);
            LogLifecycleCheckpoint(LifecycleCheckpoint.Execution, request, "started");

            WorldResetResult result = WorldResetResult.Completed;
            WorldResetOutcome outcome = WorldResetOutcome.Completed;
            string detailMessage = string.Empty;

            try
            {
                await _executor.ExecuteAsync(executors, request.Reason);
                _postResetValidator.ValidateEssentialActors(request.TargetScene, _policy);
            }
            catch (Exception ex)
            {
                result = WorldResetResult.Failed;
                outcome = WorldResetOutcome.FailedExecution;
                detailMessage = $"{WorldResetReasons.FailedExecutionPrefix}:{ex.GetType().Name}";
                DebugUtility.LogError<WorldResetOrchestrator>(
                    $"[{ResetLogTags.Failed}] [WorldResetOrchestrator] Erro durante execução do reset. request={request}, ex='{ex}'.");
            }
            finally
            {
                _lifecyclePublisher.PublishCompleted(request, outcome, detailMessage);
                LogLifecycleCheckpoint(LifecycleCheckpoint.Completion, request, outcome.ToString());
                LogCompletionFlow(outcome.ToString(), request, detailMessage);
            }

            return result;
        }

        private ResetDecision EvaluateGuards(WorldResetRequest request)
        {
            if (_guards == null || _guards.Count == 0)
            {
                return ResetDecision.Proceed();
            }

            for (int i = 0; i < _guards.Count; i++)
            {
                IWorldResetGuard guard = _guards[i];
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

        private WorldResetResult HandleDecision(string stage, ResetDecision decision, WorldResetRequest request)
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
                    request.ContextSignature);
            }
            else
            {
                DebugUtility.LogVerbose<WorldResetOrchestrator>(
                    $"[{ResetLogTags.Skipped}] {stage} skipped reset. reason='{decision.Reason}', detail='{decision.Detail}', request={request}");
            }

            if (decision.ShouldPublishCompletion)
            {
                _lifecyclePublisher.PublishCompleted(request, MapDecisionOutcome(stage, decision), decision.Detail);
            }

            if (string.Equals(stage, "Validation", StringComparison.Ordinal))
            {
                return WorldResetResult.SkippedValidation;
            }

            return WorldResetResult.Failed;
        }

        private static WorldResetOutcome MapDecisionOutcome(string stage, ResetDecision decision)
        {
            if (string.Equals(stage, "Validation", StringComparison.Ordinal))
            {
                return WorldResetOutcome.SkippedValidation;
            }

            return decision.IsViolation ? WorldResetOutcome.FailedExecution : WorldResetOutcome.SkippedByPolicy;
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

        private static void LogLifecycleCheckpoint(LifecycleCheckpoint checkpoint, WorldResetRequest request, string detail)
        {
            DebugUtility.Log<WorldResetOrchestrator>(
                $"[OBS][WorldReset][Lifecycle] checkpoint='{checkpoint}' kind='{request.Kind}' routeId='{request.MacroRouteId}' signature='{request.ContextSignature}' targetScene='{request.TargetScene}' origin='{request.Origin}' detail='{detail}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogDecisionFlow(string decisionType, string stage, WorldResetRequest request, ResetDecision decision)
        {
            DebugUtility.Log<WorldResetOrchestrator>(
                $"[OBS][WorldReset][Decision] type='{decisionType}' stage='{stage}' shouldProceed={decision.ShouldProceed} shouldPublishCompletion={decision.ShouldPublishCompletion} isViolation={decision.IsViolation} routeId='{request.MacroRouteId}' signature='{request.ContextSignature}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogCompletionFlow(string completionKind, WorldResetRequest request, string detail)
        {
            DebugUtility.Log<WorldResetOrchestrator>(
                $"[OBS][WorldReset][CompletionFlow] kind='{completionKind}' routeId='{request.MacroRouteId}' signature='{request.ContextSignature}' targetScene='{request.TargetScene}' detail='{detail}'.",
                DebugUtility.Colors.Info);
        }
    }
}

