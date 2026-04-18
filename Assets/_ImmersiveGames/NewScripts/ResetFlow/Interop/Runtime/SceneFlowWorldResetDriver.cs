using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.ResetFlow.Interop.Runtime
{
    /// <summary>
    /// Driver canônico para integrar SceneFlow -> WorldReset.
    /// O driver apenas traduz e faz handoff; completion pertence ao lifecycle macro do WorldReset.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneFlowWorldResetDriver : IDisposable
    {
        private readonly IWorldResetService _worldResetService;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private bool _disposed;

        public SceneFlowWorldResetDriver(IWorldResetService worldResetService)
        {
            _worldResetService = worldResetService ?? throw new InvalidOperationException("[FATAL][Config][ResetInterop] IWorldResetService obrigatorio ausente para SceneFlowWorldResetDriver.");
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

            DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                $"[OBS][ResetInterop] SceneFlow -> WorldReset handshake registrado. source='SceneTransitionScenesReadyEvent' target='IWorldResetService' reason='{WorldResetReasons.SceneFlowScenesReady}'.",
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
                // Evita quebra de dispose tardio no shutdown do runtime.
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
                SceneTransitionContext context = evt.context;
                string signature = SceneTransitionSignature.Compute(context);
                string targetScene = ResolveTargetSceneNameOrFailFast(context);
                SceneRouteId routeId = context.RouteId;
                bool hasSignature = !string.IsNullOrWhiteSpace(signature);
                bool requiresWorldReset = context.RequiresWorldReset;
                bool shouldExecute = hasSignature && requiresWorldReset;

                string decisionSource = NormalizeDecisionSource(context.ResetDecisionSource);
                string decisionReason = NormalizeDecisionReason(context.ResetDecisionReason);
                string requestReason = requiresWorldReset
                    ? decisionReason
                    : BuildSkippedReason(context, routeId, targetScene, decisionReason);

                LogHandshake("received", signature, targetScene, routeId.Value, context.RouteKind, context.TransitionProfileName);
                LogPolicyDecision(signature, routeId.Value, requiresWorldReset, decisionReason, decisionSource);
                LogObsResetRequested(
                    signature: signature,
                    sourceSignature: signature,
                    routeId: routeId.Value,
                    routeKind: context.RouteKind,
                    profileLabel: context.TransitionProfileName,
                    target: targetScene,
                    decisionSource: decisionSource,
                    reason: requestReason,
                    shouldExecute: shouldExecute);

                if (!hasSignature)
                {
                    DebugUtility.LogWarning<SceneFlowWorldResetDriver>(
                        "[ResetInterop] ScenesReady recebido com ContextSignature vazia. Handing off ao owner macro para completion canônica.");
                    LogHandshake("handoff_invalid_context", string.Empty, targetScene, routeId.Value, context.RouteKind, context.TransitionProfileName);
                }
                else if (!requiresWorldReset)
                {
                    LogHandshake("handoff_skipped_policy", signature, targetScene, routeId.Value, context.RouteKind, context.TransitionProfileName);
                }
                else
                {
                    LogHandshake("handoff_execute", signature, targetScene, routeId.Value, context.RouteKind, context.TransitionProfileName);
                }

                WorldResetRequest request = BuildSceneFlowRequest(
                    signature,
                    targetScene,
                    routeId,
                    requestReason,
                    shouldExecute);

                await _worldResetService.TriggerResetAsync(request);
                LogHandshake("handoff_completed", signature, targetScene, routeId.Value, context.RouteKind, context.TransitionProfileName);
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
            string reason,
            bool shouldExecute)
        {
            return new WorldResetRequest(
                kind: ResetKind.Macro,
                contextSignature: signature ?? string.Empty,
                reason: reason ?? string.Empty,
                targetScene: targetScene ?? string.Empty,
                origin: WorldResetOrigin.SceneFlow,
                macroRouteId: routeId,
                phaseSignature: PhaseContextSignature.Empty,
                sourceSignature: signature ?? string.Empty,
                shouldExecute: shouldExecute);
        }

        private static string BuildSkippedReason(
            SceneTransitionContext context,
            SceneRouteId routeId,
            string targetScene,
            string decisionReason)
        {
            if (!string.IsNullOrWhiteSpace(decisionReason))
            {
                return decisionReason.Trim();
            }

            return $"{WorldResetReasons.SkippedNonGameplayRoutePrefix}:routeKind={context.RouteKind};route={routeId};scene={targetScene}";
        }

        private static void LogPolicyDecision(
            string signature,
            string routeId,
            bool requiresWorldReset,
            string decisionReason,
            string decisionSource)
        {
            DebugUtility.LogVerbose(typeof(SceneFlowWorldResetDriver),
                $"[OBS][ResetInterop] ResetPolicy routeId='{routeId}' requiresWorldReset={requiresWorldReset} signature='{signature ?? string.Empty}' reason='{decisionReason ?? string.Empty}' decisionSource='{decisionSource ?? string.Empty}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogObsResetRequested(
            string signature,
            string sourceSignature,
            string routeId,
            SceneRouteKind routeKind,
            string profileLabel,
            string target,
            string decisionSource,
            string reason,
            bool shouldExecute)
        {
            DebugUtility.LogVerbose(typeof(SceneFlowWorldResetDriver),
                $"[OBS][ResetInterop] ResetRequested signature='{signature ?? string.Empty}' sourceSignature='{sourceSignature ?? string.Empty}' routeId='{routeId ?? string.Empty}' routeKind='{routeKind}' profileLabel='{profileLabel ?? string.Empty}' target='{target ?? string.Empty}' decisionSource='{decisionSource ?? string.Empty}' reason='{reason ?? string.Empty}' shouldExecute={shouldExecute}.",
                DebugUtility.Colors.Info);
        }

        private static void LogHandshake(string stage, string signature, string targetScene, string routeId, SceneRouteKind routeKind, string profileLabel)
        {
            DebugUtility.LogVerbose(typeof(SceneFlowWorldResetDriver),
                $"[OBS][ResetInterop][Handshake] boundary='SceneFlow->WorldReset' stage='{stage}' signature='{signature ?? string.Empty}' target='{targetScene ?? string.Empty}' routeId='{routeId ?? string.Empty}' routeKind='{routeKind}' profileLabel='{profileLabel ?? string.Empty}'.",
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

