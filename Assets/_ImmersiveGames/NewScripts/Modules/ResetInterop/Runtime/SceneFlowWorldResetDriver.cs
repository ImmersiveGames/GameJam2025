using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Application;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime
{
    /// <summary>
    /// Driver canônico (produção) para integrar SceneFlow → WorldReset.
    ///
    /// Responsabilidades:
    /// - Ao receber SceneTransitionScenesReadyEvent, resolve política por rota/contexto e dispara ResetWorld quando aplicável.
    /// - Publica WorldResetCompletedEvent(signature) apenas em SKIP/fallback para liberar o completion gate do SceneFlow.
    ///
    /// Observações:
    /// - Não depende de "coordinator" obsoleto.
    /// - É best-effort: nunca deve travar o fluxo (sempre publica ResetCompleted).
    /// </summary>
        /// <summary>
    /// OWNER: handoff SceneFlow -> WorldResetService para macro reset por transicao.
    /// NAO E OWNER: eventos V2 de comando/telemetria (permanecem em WorldResetCommands).
    /// PUBLISH/CONSUME: consome SceneTransitionScenesReadyEvent; publica WorldResetCompletedEvent em SKIP/fallback.
    /// Fases tocadas: ScenesReady e Gate (sinaliza completion V1 antes do BeforeFadeOut).
    /// </summary>
[DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneFlowWorldResetDriver : IDisposable
    {
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private readonly object _guardLock = new();
        private readonly HashSet<string> _inFlightSignatures = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _completedTicks = new(StringComparer.Ordinal);
        private bool _disposed;
        private static int _degradedFallbackCount;

        // Reasons canônicos (Contrato de Observability) em WorldResetReasons.

        // Janela curta para dedupe de assinatura (evita reset duplicado no mesmo frame).
        private const int DuplicateSignatureWindowMs = 750;
        private const int CompletedCacheLimit = 128;

        public SceneFlowWorldResetDriver()
        {
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

            DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                $"[ResetInterop] Driver registrado: SceneFlow ScenesReady -> ResetWorld -> ResetCompleted. reason='{WorldResetReasons.SceneFlowScenesReady}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try { EventBus<SceneTransitionScenesReadyEvent>.Unregister(_scenesReadyBinding); }
            catch { /* best-effort */ }
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            // Event handler não pode ser async; delega para Task com tratamento interno.
            _ = HandleScenesReadyAsync(evt);
        }

        private async Task HandleScenesReadyAsync(SceneTransitionScenesReadyEvent evt)
        {
            var context = evt.context;
            string signature = SceneTransitionSignature.Compute(context);

            if (string.IsNullOrWhiteSpace(signature))
            {
                // Defensivo: assinatura vazia nao deve travar o SceneFlow; apenas libera.
                DebugUtility.LogWarning<SceneFlowWorldResetDriver>(
                    "[ResetInterop] ScenesReady recebido com ContextSignature vazia. Liberando gate sem reset.");
                LogObsResetRequested(
                    signature: string.Empty,
                    sourceSignature: string.Empty,
                    routeId: context.RouteId.Value ?? string.Empty,
                    routeKind: context.RouteKind,
                profileLabel: context.TransitionProfileName,
                    target: ResolveTargetSceneName(context),
                    decisionSource: "contextSignature:empty",
                    reason: WorldResetReasons.SceneFlowScenesReady);
                PublishResetCompleted(
                    signature,
                    WorldResetReasons.SceneFlowScenesReady,
                    context.RouteId.Value ?? string.Empty,
                    context.RouteKind,
                    context.TransitionProfileName,
                    ResolveTargetSceneName(context),
                    "contextSignature:empty");
                return;
            }

            string targetScene = ResolveTargetSceneName(context);
            string routeId = context.RouteId.Value ?? string.Empty;

            bool requiresWorldReset = context.RequiresWorldReset;
            string decisionSource = string.IsNullOrWhiteSpace(context.ResetDecisionSource) ? "context:missingDecisionSource" : context.ResetDecisionSource;
            string decisionReason = string.IsNullOrWhiteSpace(context.ResetDecisionReason) ? "context:missingDecisionReason" : context.ResetDecisionReason;

            DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                $"[OBS][ResetInterop] ResetPolicy routeId='{routeId}' requiresWorldReset={requiresWorldReset} signature='{signature}' reason='{decisionReason}' decisionSource='{decisionSource}'.",
                DebugUtility.Colors.Info);

            if (!requiresWorldReset)
            {
                string skippedReason = string.IsNullOrWhiteSpace(decisionReason)
                    ? $"{WorldResetReasons.SkippedNonGameplayRoutePrefix}:routeKind={context.RouteKind};route={routeId};scene={targetScene}"
                    : decisionReason;

                if (ShouldSkipDuplicate(signature, out string guardReason))
                {
                    LogDuplicateGuard(signature, context.RouteKind, context.TransitionProfileName, targetScene, guardReason);
                    return;
                }

                MarkInFlight(signature);

                LogObsResetRequested(
                    signature: signature,
                    sourceSignature: signature,
                    routeId: routeId,
                    routeKind: context.RouteKind,
                profileLabel: context.TransitionProfileName,
                    target: targetScene,
                    decisionSource: decisionSource,
                    reason: skippedReason);

                DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                    $"[{ResetLogTags.Skipped}] [ResetSkip] [ResetInterop] ResetWorld SKIP. signature='{signature}', routeId='{routeId}', routeKind='{context.RouteKind}', profileLabel='{context.TransitionProfileName}', targetScene='{targetScene}', decisionSource='{decisionSource}', reason='{skippedReason}'.",
                DebugUtility.Colors.Info);

                PublishResetCompleted(signature, skippedReason, routeId, context.RouteKind, context.TransitionProfileName, targetScene, decisionSource);
                MarkCompleted(signature);
                return;
            }

            // Reset real (policy requiresWorldReset=true).
            LogObsResetRequested(
                signature: signature,
                sourceSignature: signature,
                routeId: routeId,
                routeKind: context.RouteKind,
                profileLabel: context.TransitionProfileName,
                target: targetScene,
                decisionSource: decisionSource,
                reason: decisionReason);

            bool shouldPublishCompletion = false;
            string completionReason = WorldResetReasons.SceneFlowScenesReady;
            try
            {
                var result = await ExecuteResetWhenRequiredAsync(
                    signature,
                    targetScene,
                    decisionReason);

                shouldPublishCompletion = result.shouldPublishCompletion;
                if (!string.IsNullOrWhiteSpace(result.failureReason))
                {
                    completionReason = result.failureReason;
                }
            }
            finally
            {
                if (shouldPublishCompletion)
                {
                    // Fallback/SKIP: driver libera o gate quando nao ha WorldResetService publicando.
                    PublishResetCompleted(signature, completionReason, routeId, context.RouteKind, context.TransitionProfileName, targetScene, decisionSource);
                }
                MarkCompleted(signature);
            }
        }

        private static async Task<(bool shouldPublishCompletion, string failureReason)> ExecuteResetWhenRequiredAsync(
            string signature,
            string targetScene,
            string decisionReason)
        {
            // Primeiro: se um WorldResetService estiver registrado no DI, use-o (ponto canonico).
            if (DependencyManager.HasInstance && DependencyManager.Provider.TryGetGlobal<WorldResetService>(out var resetService) && resetService != null)
            {
                DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                    $"[ResetInterop] Usando WorldResetService para executar reset. signature='{signature}', targetScene='{targetScene}'.",
                DebugUtility.Colors.Info);

                var request = new WorldResetRequest(
                    contextSignature: signature,
                    reason: string.IsNullOrWhiteSpace(decisionReason) ? WorldResetReasons.SceneFlowScenesReady : decisionReason,
                    targetScene: targetScene,
                    origin: WorldResetOrigin.SceneFlow,
                    sourceSignature: signature);

                try
                {
                    await resetService.TriggerResetAsync(request);
                    return (false, string.Empty);
                }
                catch (Exception ex)
                {
                    if (!IsDevelopmentEscapeHatch())
                    {
                        HardFailFastH1.Trigger(typeof(SceneFlowWorldResetDriver),
                            $"[FATAL][H1][ResetInterop] Reset required but WorldResetService.TriggerResetAsync failed. signature='{signature}', targetScene='{targetScene}', ex='{ex.GetType().Name}'.",
                            ex);
                    }

                    int degradedOnFailureCount = System.Threading.Interlocked.Increment(ref _degradedFallbackCount);
                    DebugUtility.LogWarning<SceneFlowWorldResetDriver>(
                        $"[WARN][DEGRADED][ResetInterop] ResetWorld fallback completion enabled (DEV). count='{degradedOnFailureCount}' signature='{signature}' targetScene='{targetScene}' ex='{ex.GetType().Name}'.");
                    return (true, WorldResetReasons.FailedNoResetService);
                }
            }

            if (!IsDevelopmentEscapeHatch())
            {
                HardFailFastH1.Trigger(typeof(SceneFlowWorldResetDriver),
                    $"[FATAL][H1][ResetInterop] Reset required but WorldResetService missing in DI. signature='{signature}', targetScene='{targetScene}'.");
            }

            int degradedMissingServiceCount = System.Threading.Interlocked.Increment(ref _degradedFallbackCount);
            DebugUtility.LogWarning<SceneFlowWorldResetDriver>(
                $"[WARN][DEGRADED][ResetInterop] WorldResetService missing in DI (DEV escape hatch). count='{degradedMissingServiceCount}' signature='{signature}' targetScene='{targetScene}'.");

            return (true, WorldResetReasons.FailedNoResetService);
        }

        private static string ResolveTargetSceneName(SceneTransitionContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.TargetActiveScene))
            {
                return context.TargetActiveScene.Trim();
            }

            // Fallback: active scene atual.
            return SceneManager.GetActiveScene().name ?? string.Empty;
        }

        private static void PublishResetCompleted(string signature, string reason, string routeId, SceneRouteKind routeKind, string profileLabel, string target, string decisionSource)
        {
            // Publica apenas em SKIP/fallback: o completion gate depende disso para nao degradar em timeout.
            LogObsResetCompleted(
                signature: signature,
                routeId: routeId,
                routeKind: routeKind,
                profileLabel: profileLabel,
                target: target,
                decisionSource: decisionSource,
                reason: reason);

            DebugUtility.LogVerbose<SceneFlowWorldResetDriver>(
                $"[OBS][ResetInterop] V1FallbackPublish signature='{signature ?? string.Empty}' reason='{reason ?? string.Empty}' decisionSource='{decisionSource ?? string.Empty}'.",
                DebugUtility.Colors.Info);

            WorldResetOrchestrator.PublishResetCompletedV1(signature ?? string.Empty, reason);
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
            // Observabilidade canonica (Contrato): ResetRequested com sourceSignature/reason/routeKind e profileLabel descritivo.
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
            string reason)
        {
            // Observabilidade canonica (Contrato): ResetCompleted correlacionavel ao gate (signature) e reason final.
            DebugUtility.LogVerbose(typeof(SceneFlowWorldResetDriver),
                $"[OBS][ResetInterop] ResetCompleted signature='{signature ?? string.Empty}' routeId='{routeId ?? string.Empty}' routeKind='{routeKind}' profileLabel='{profileLabel ?? string.Empty}' target='{target ?? string.Empty}' decisionSource='{decisionSource ?? string.Empty}' reason='{reason ?? string.Empty}'.",
                DebugUtility.Colors.Success);
        }

        private bool ShouldSkipDuplicate(string signature, out string guardReason)
        {
            guardReason = string.Empty;

            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            int now = Environment.TickCount;

            lock (_guardLock)
            {
                if (_inFlightSignatures.Contains(signature))
                {
                    guardReason = $"{WorldResetReasons.GuardDuplicatePrefix}:in_flight";
                    return true;
                }

                if (_completedTicks.TryGetValue(signature, out int lastTick))
                {
                    int dt = unchecked(now - lastTick);
                    if (dt >= 0 && dt <= DuplicateSignatureWindowMs)
                    {
                        guardReason = $"{WorldResetReasons.GuardDuplicatePrefix}:recent";
                        return true;
                    }

                    if (dt > DuplicateSignatureWindowMs)
                    {
                        _completedTicks.Remove(signature);
                    }
                }
            }

            return false;
        }

        private void MarkInFlight(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return;
            }

            lock (_guardLock)
            {
                _inFlightSignatures.Add(signature);
            }
        }

        private void MarkCompleted(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return;
            }

            lock (_guardLock)
            {
                _inFlightSignatures.Remove(signature);
                _completedTicks[signature] = Environment.TickCount;

                if (_completedTicks.Count > CompletedCacheLimit)
                {
                    _completedTicks.Clear();
                }
            }
        }


        private static bool IsDevelopmentEscapeHatch()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }

        private static void LogDuplicateGuard(string signature, SceneRouteKind routeKind, string profileLabel, string target, string guardReason)
        {
            DebugUtility.LogWarning<SceneFlowWorldResetDriver>(
                $"[{ResetLogTags.Guarded}][DEGRADED_MODE] [ResetInterop] ResetWorld guard: ScenesReady duplicado. signature='{signature}', routeKind='{routeKind}', profileLabel='{profileLabel}', targetScene='{target}', guard='{guardReason}'.");
        }
    }
}
















