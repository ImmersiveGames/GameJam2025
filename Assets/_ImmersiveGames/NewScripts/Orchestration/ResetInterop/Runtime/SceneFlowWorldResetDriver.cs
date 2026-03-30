using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Application;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime
{
    /// <summary>
    /// Driver canônico (produção) para integrar SceneFlow → WorldReset.
    ///
    /// Responsabilidades:
    /// - Ao receber SceneTransitionScenesReadyEvent, resolve política por rota/contexto e dispara ResetWorld quando aplicável.
    /// - Publica WorldResetCompletedEvent apenas em SKIP/fallback para liberar o completion gate do SceneFlow.
    ///
    /// Observações:
    /// - Não depende de coordinator obsoleto.
    /// - É best-effort: nunca deve travar o fluxo (sempre publica completion quando o owner não publicar).
    /// </summary>
    /// <summary>
    /// OWNER: handoff SceneFlow -> WorldResetService para macro reset por transição.
    /// NÃO É OWNER: contrato do domínio WorldReset.
    /// PUBLISH/CONSUME: consome SceneTransitionScenesReadyEvent; publica WorldResetCompletedEvent apenas em SKIP/fallback.
    /// Fases tocadas: ScenesReady e Gate (sinaliza completion antes do BeforeFadeOut quando o owner não sinalizar).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneFlowWorldResetDriver : IDisposable
    {
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private int _degradedFallbackCount;
        private bool _disposed;

        public SceneFlowWorldResetDriver()
        {
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

            DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                $"[ResetInterop] Driver registrado: SceneFlow ScenesReady -> WorldReset. reason='{WorldResetReasons.SceneFlowScenesReady}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                EventBus<SceneTransitionScenesReadyEvent>.Unregister(_scenesReadyBinding);
            }
            catch
            {
                /* best-effort */
            }
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            _ = HandleScenesReadyAsync(evt);
        }

        private async Task HandleScenesReadyAsync(SceneTransitionScenesReadyEvent evt)
        {
            var context = evt.context;
            string signature = SceneTransitionSignature.Compute(context);
            string targetScene = ResolveTargetSceneName(context);
            SceneRouteId routeId = context.RouteId;
            string decisionSource = NormalizeDecisionSource(context.ResetDecisionSource);
            string decisionReason = NormalizeDecisionReason(context.ResetDecisionReason);
            var request = BuildSceneFlowRequest(signature, targetScene, routeId, decisionReason);

            if (string.IsNullOrWhiteSpace(signature))
            {
                DebugUtility.LogWarning<SceneFlowWorldResetDriver>(
                    "[ResetInterop] ScenesReady recebido com ContextSignature vazia. Liberando gate sem reset.");
                LogObsResetRequested(
                    signature: string.Empty,
                    sourceSignature: string.Empty,
                    routeId: routeId.Value,
                    routeKind: context.RouteKind,
                    profileLabel: context.TransitionProfileName,
                    target: targetScene,
                    decisionSource: "contextSignature:empty",
                    reason: WorldResetReasons.SceneFlowScenesReady);
                PublishResetCompletedThroughOwnerOrFallback(
                    BuildSceneFlowRequest(string.Empty, targetScene, routeId, WorldResetReasons.SceneFlowScenesReady),
                    WorldResetOutcome.SkippedInvalidContext,
                    "ContextSignatureEmpty");
                return;
            }

            bool requiresWorldReset = context.RequiresWorldReset;

            DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                $"[OBS][ResetInterop] ResetPolicy routeId='{routeId.Value}' requiresWorldReset={requiresWorldReset} signature='{signature}' reason='{decisionReason}' decisionSource='{decisionSource}'.",
                DebugUtility.Colors.Info);

            if (!requiresWorldReset)
            {
                string skippedReason = string.IsNullOrWhiteSpace(decisionReason)
                    ? $"{WorldResetReasons.SkippedNonGameplayRoutePrefix}:routeKind={context.RouteKind};route={routeId};scene={targetScene}"
                    : decisionReason;

                LogObsResetRequested(
                    signature: signature,
                    sourceSignature: signature,
                    routeId: routeId.Value,
                    routeKind: context.RouteKind,
                    profileLabel: context.TransitionProfileName,
                    target: targetScene,
                    decisionSource: decisionSource,
                    reason: skippedReason);

                DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                    $"[{ResetLogTags.Skipped}] [ResetSkip] [ResetInterop] ResetWorld SKIP. signature='{signature}', routeId='{routeId.Value}', routeKind='{context.RouteKind}', profileLabel='{context.TransitionProfileName}', targetScene='{targetScene}', decisionSource='{decisionSource}', reason='{skippedReason}'.",
                    DebugUtility.Colors.Info);

                PublishResetCompletedThroughOwnerOrFallback(
                    request,
                    WorldResetOutcome.SkippedByPolicy,
                    string.Empty);
                return;
            }

            LogObsResetRequested(
                signature: signature,
                sourceSignature: signature,
                routeId: routeId.Value,
                routeKind: context.RouteKind,
                profileLabel: context.TransitionProfileName,
                target: targetScene,
                decisionSource: decisionSource,
                reason: decisionReason);

            bool shouldPublishCompletion = false;
            string completionDetail = string.Empty;
            WorldResetOutcome completionOutcome = WorldResetOutcome.Completed;
            try
            {
                var result = await ExecuteResetWhenRequiredAsync(
                    request);

                shouldPublishCompletion = result.shouldPublishCompletion;
                completionOutcome = result.outcome;
                completionDetail = result.detail;
            }
            finally
            {
                if (shouldPublishCompletion)
                {
                    PublishResetCompletedThroughOwnerOrFallback(
                        request,
                        completionOutcome,
                        completionDetail);
                }
            }
        }

        private async Task<(bool shouldPublishCompletion, WorldResetOutcome outcome, string detail)> ExecuteResetWhenRequiredAsync(
            WorldResetRequest request)
        {
            if (DependencyManager.HasInstance && DependencyManager.Provider.TryGetGlobal<WorldResetService>(out var resetService) && resetService != null)
            {
                DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                    $"[ResetInterop] Usando WorldResetService para executar reset. signature='{request.ContextSignature}', routeId='{request.MacroRouteId}', targetScene='{request.TargetScene}'.",
                    DebugUtility.Colors.Info);

                try
                {
                    await resetService.TriggerResetAsync(request);
                    return (false, WorldResetOutcome.Completed, string.Empty);
                }
                catch (Exception ex)
                {
                    if (!IsDevelopmentEscapeHatch())
                    {
                        HardFailFastH1.Trigger(typeof(SceneFlowWorldResetDriver),
                            $"[FATAL][H1][ResetInterop] Reset required but WorldResetService.TriggerResetAsync failed. signature='{request.ContextSignature}', targetScene='{request.TargetScene}', ex='{ex.GetType().Name}'.",
                            ex);
                    }

                    int degradedOnFailureCount = System.Threading.Interlocked.Increment(ref _degradedFallbackCount);
                    DebugUtility.LogWarning<SceneFlowWorldResetDriver>(
                        $"[WARN][DEGRADED][ResetInterop] ResetWorld fallback completion enabled (DEV). count='{degradedOnFailureCount}' signature='{request.ContextSignature}' targetScene='{request.TargetScene}' ex='{ex.GetType().Name}'.");
                    return (true, WorldResetOutcome.FailedService, $"{WorldResetReasons.FailedServiceExceptionPrefix}:{ex.GetType().Name}");
                }
            }

            if (!IsDevelopmentEscapeHatch())
            {
                HardFailFastH1.Trigger(typeof(SceneFlowWorldResetDriver),
                    $"[FATAL][H1][ResetInterop] Reset required but WorldResetService missing in DI. signature='{request.ContextSignature}', targetScene='{request.TargetScene}'.");
            }

            int degradedMissingServiceCount = System.Threading.Interlocked.Increment(ref _degradedFallbackCount);
            DebugUtility.LogWarning<SceneFlowWorldResetDriver>(
                $"[WARN][DEGRADED][ResetInterop] WorldResetService missing in DI (DEV escape hatch). count='{degradedMissingServiceCount}' signature='{request.ContextSignature}' targetScene='{request.TargetScene}'.");

            return (true, WorldResetOutcome.FailedService, WorldResetReasons.FailedNoResetService);
        }

        private static string ResolveTargetSceneName(SceneTransitionContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.TargetActiveScene))
            {
                return context.TargetActiveScene.Trim();
            }

            return SceneManager.GetActiveScene().name ?? string.Empty;
        }

        private static WorldResetRequest BuildSceneFlowRequest(
            string signature,
            string targetScene,
            SceneRouteId routeId,
            string reason)
        {
            return new WorldResetRequest(
                kind: ResetKind.Macro,
                contextSignature: signature ?? string.Empty,
                reason: reason ?? string.Empty,
                targetScene: targetScene ?? string.Empty,
                origin: WorldResetOrigin.SceneFlow,
                macroRouteId: routeId,
                levelSignature: LevelContextSignature.Empty,
                sourceSignature: signature ?? string.Empty);
        }

        private void PublishResetCompletedThroughOwnerOrFallback(
            WorldResetRequest request,
            WorldResetOutcome outcome,
            string detail)
        {
            if (DependencyManager.HasInstance &&
                DependencyManager.Provider.TryGetGlobal<WorldResetService>(out var resetService) &&
                resetService != null)
            {
                resetService.PublishResetCompleted(request, outcome, detail ?? string.Empty);
                return;
            }

            PublishResetCompletedFallback(request, outcome, detail);
        }

        private static void PublishResetCompletedFallback(
            WorldResetRequest request,
            WorldResetOutcome outcome,
            string detail)
        {
            LogObsResetCompleted(
                signature: request.ContextSignature,
                routeId: request.MacroRouteId.Value,
                routeKind: SceneRouteKind.Unspecified,
                profileLabel: string.Empty,
                target: request.TargetScene,
                decisionSource: string.Empty,
                reason: request.Reason,
                outcome: outcome,
                detail: detail);

            DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                $"[OBS][ResetInterop] FallbackPublish signature='{request.ContextSignature ?? string.Empty}' reason='{request.Reason ?? string.Empty}' outcome='{outcome}' detail='{detail ?? string.Empty}'.",
                DebugUtility.Colors.Info);

            new WorldResetLifecyclePublisher().PublishCompleted(request, outcome, detail ?? string.Empty);
        }

        private static void LogObsResetRequested(
            string signature,
            string sourceSignature,
            string routeId,
            SceneRouteKind routeKind,
            string profileLabel,
            string target,
            string decisionSource,
            string reason)
        {
            DebugUtility.LogVerbose(typeof(SceneFlowWorldResetDriver),
                $"[OBS][ResetInterop] ResetRequested signature='{signature ?? string.Empty}' sourceSignature='{sourceSignature ?? string.Empty}' routeId='{routeId ?? string.Empty}' routeKind='{routeKind}' profileLabel='{profileLabel ?? string.Empty}' target='{target ?? string.Empty}' decisionSource='{decisionSource ?? string.Empty}' reason='{reason ?? string.Empty}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogObsResetCompleted(
            string signature,
            string routeId,
            SceneRouteKind routeKind,
            string profileLabel,
            string target,
            string decisionSource,
            string reason,
            WorldResetOutcome outcome,
            string detail)
        {
            DebugUtility.LogVerbose(typeof(SceneFlowWorldResetDriver),
                $"[OBS][ResetInterop] ResetCompleted signature='{signature ?? string.Empty}' routeId='{routeId ?? string.Empty}' routeKind='{routeKind}' profileLabel='{profileLabel ?? string.Empty}' target='{target ?? string.Empty}' decisionSource='{decisionSource ?? string.Empty}' reason='{reason ?? string.Empty}' outcome='{outcome}' detail='{detail ?? string.Empty}'.",
                DebugUtility.Colors.Success);
        }

        private static string NormalizeDecisionSource(string decisionSource)
        {
            return string.IsNullOrWhiteSpace(decisionSource) ? "context:missingDecisionSource" : decisionSource.Trim();
        }

        private static string NormalizeDecisionReason(string decisionReason)
        {
            return string.IsNullOrWhiteSpace(decisionReason) ? "context:missingDecisionReason" : decisionReason.Trim();
        }

        private static bool IsDevelopmentEscapeHatch()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }

    }
}
