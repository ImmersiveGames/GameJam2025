using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.SceneReset.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneReset.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Guards;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Policies;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Validation;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Application
{
    /// <summary>
    /// Orquestra o pipeline de reset do WorldReset.
    /// Guard -> Validate -> Discover -> Execute -> PostValidate -> PublishCompleted.
    /// </summary>
    public sealed class WorldResetOrchestrator
    {
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

            provider.TryGetGlobal<IWorldResetPolicy>(out IWorldResetPolicy policy);
            provider.TryGetGlobal<ISimulationGateService>(out ISimulationGateService gateService);

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
            ResetDecision decision = EvaluateGuards(request);
            if (!decision.ShouldProceed)
            {
                return HandleDecision("Guard", decision, request);
            }

            decision = _validation.Validate(request, _policy);
            if (!decision.ShouldProceed)
            {
                return HandleDecision("Validation", decision, request);
            }

            IReadOnlyList<SceneResetController> controllers = DiscoverControllers(request.TargetScene);
            if (controllers.Count == 0)
            {
                string target = string.IsNullOrWhiteSpace(request.TargetScene) ? "<unknown>" : request.TargetScene;
                string detail = $"{WorldResetReasons.FailedNoControllerPrefix}:{target}";
                LogDegraded($"Nenhum SceneResetController encontrado para reset. targetScene='{target}'.");
                _lifecyclePublisher.PublishCompleted(request, WorldResetOutcome.FailedNoController, detail);
                return WorldResetResult.Failed;
            }

            _lifecyclePublisher.PublishStarted(request);

            WorldResetResult result = WorldResetResult.Completed;
            WorldResetOutcome outcome = WorldResetOutcome.Completed;
            string detailMessage = string.Empty;

            try
            {
                await _executor.ExecuteAsync(controllers, request.Reason);
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

        private static IReadOnlyList<SceneResetController> DiscoverControllers(string targetScene)
        {
            return SceneResetControllerLocator.FindControllersForScene(targetScene);
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
    }
}
