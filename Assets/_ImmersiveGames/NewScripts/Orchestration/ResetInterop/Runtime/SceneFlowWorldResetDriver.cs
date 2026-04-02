using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Application;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.ResetInterop.Runtime
{
    /// <summary>
    /// Driver canônico para integrar SceneFlow -> WorldReset.
    ///
    /// Responsabilidades:
    /// - Ao receber SceneTransitionScenesReadyEvent, resolve a decisão de reset e chama o owner canônico do WorldReset.
    /// - Publica o completion diretamente via WorldResetService para skip/invalid context.
    /// - Não usa fallback silencioso nem publication path alternativo.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneFlowWorldResetDriver : IDisposable
    {
        private readonly WorldResetService _worldResetService;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private bool _disposed;

        public SceneFlowWorldResetDriver(WorldResetService worldResetService)
        {
            _worldResetService = worldResetService ?? throw new InvalidOperationException("[FATAL][Config][ResetInterop] IWorldResetService obrigatorio ausente para SceneFlowWorldResetDriver.");
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

            DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                $"[ResetInterop] Driver registrado: SceneFlow ScenesReady -> WorldResetService. reason='{WorldResetReasons.SceneFlowScenesReady}'.",
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
            try
            {
                var context = evt.context;
                string signature = SceneTransitionSignature.Compute(context);
                string targetScene = ResolveTargetSceneNameOrFailFast(context);
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
                    _worldResetService.PublishResetCompleted(request, WorldResetOutcome.SkippedInvalidContext, "ContextSignatureEmpty");
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

                    _worldResetService.PublishResetCompleted(request, WorldResetOutcome.SkippedByPolicy, string.Empty);
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

                await _worldResetService.TriggerResetAsync(request);
            }
            catch (Exception ex)
            {
                HardFailFastH1.Trigger(typeof(SceneFlowWorldResetDriver),
                    $"[FATAL][H1][ResetInterop] SceneFlow -> WorldReset driver failed. ex='{ex.GetType().Name}'.",
                    ex);
            }
        }

        private static string ResolveTargetSceneNameOrFailFast(SceneTransitionContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.TargetActiveScene))
            {
                return context.TargetActiveScene.Trim();
            }

            throw new InvalidOperationException(
                "[FATAL][Config][ResetInterop] SceneTransitionContext.TargetActiveScene obrigatorio ausente para o handoff SceneFlow -> WorldReset.");
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

        private static string NormalizeDecisionSource(string decisionSource)
        {
            return string.IsNullOrWhiteSpace(decisionSource) ? "context:missingDecisionSource" : decisionSource.Trim();
        }

        private static string NormalizeDecisionReason(string decisionReason)
        {
            return string.IsNullOrWhiteSpace(decisionReason) ? "context:missingDecisionReason" : decisionReason.Trim();
        }
    }
}
