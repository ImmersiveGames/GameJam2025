using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneReset.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneReset.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Guards;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Policies;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Validation;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Application
{
    /// <summary>
    /// Orquestra o pipeline de reset do WorldReset.
    /// Guard -> Validate -> Discover -> Execute -> PublishCompleted.
    /// </summary>
    public sealed class WorldResetOrchestrator
    {
        // OWNER boundary: pipeline macro reset (guards/validation/execute) e publicação do contrato canônico.
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
                PublishResetCompletedEvent(request, WorldResetOutcome.FailedNoController, detail);
                return WorldResetResult.Failed;
            }

            PublishResetStartedEvent(request);

            WorldResetResult result = WorldResetResult.Completed;
            WorldResetOutcome outcome = WorldResetOutcome.Completed;
            string detailMessage = string.Empty;

            try
            {
                await _executor.ExecuteAsync(request, controllers, _policy);
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
                PublishResetCompletedEvent(request, outcome, detailMessage);
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
                PublishResetCompletedEvent(request, MapDecisionOutcome(stage, decision), decision.Detail);
            }

            if (string.Equals(stage, "Validation", StringComparison.Ordinal))
            {
                return WorldResetResult.SkippedValidation;
            }

            return WorldResetResult.Failed;
        }

        internal static void PublishResetStartedEvent(WorldResetRequest request)
        {
            DebugUtility.LogVerbose<WorldResetOrchestrator>(
                $"[{ResetLogTags.Start}][OBS][WorldReset] ResetStarted kind='{ResolveKind(request)}' routeId='{request.MacroRouteId}' signature='{request.ContextSignature}' levelSignature='{request.LevelSignature}' reason='{request.Reason}' origin='{request.Origin}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldResetStartedEvent>.Raise(new WorldResetStartedEvent(
                ResolveKind(request),
                request.MacroRouteId,
                request.Reason,
                request.ContextSignature,
                request.LevelSignature,
                request.Origin,
                request.SourceSignature));
        }

        internal static void PublishResetCompletedEvent(WorldResetRequest request, WorldResetOutcome outcome, string detail)
        {
            string completionDetail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();

            DebugUtility.LogVerbose<WorldResetOrchestrator>(
                $"[{ResetLogTags.Completed}][OBS][WorldReset] ResetCompleted kind='{ResolveKind(request)}' routeId='{request.MacroRouteId}' signature='{request.ContextSignature}' levelSignature='{request.LevelSignature}' reason='{request.Reason}' outcome='{outcome}' detail='{completionDetail}' origin='{request.Origin}'.",
                outcome == WorldResetOutcome.Completed || outcome == WorldResetOutcome.SkippedByPolicy || outcome == WorldResetOutcome.SkippedValidation
                    ? DebugUtility.Colors.Success
                    : DebugUtility.Colors.Warning);

            EventBus<WorldResetCompletedEvent>.Raise(new WorldResetCompletedEvent(
                ResolveKind(request),
                request.MacroRouteId,
                request.Reason,
                request.ContextSignature,
                request.LevelSignature,
                outcome,
                completionDetail,
                request.Origin,
                request.SourceSignature));
        }

        private static ResetKind ResolveKind(WorldResetRequest request)
        {
            return request.LevelSignature.IsValid ? ResetKind.Level : ResetKind.Macro;
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
