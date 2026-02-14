using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Application;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Driver canônico (produção) para integrar SceneFlow → WorldLifecycle.
    ///
    /// Responsabilidades:
    /// - Ao receber SceneTransitionScenesReadyEvent, resolve política por rota/contexto e dispara ResetWorld quando aplicável.
    /// - Publica WorldLifecycleResetCompletedEvent(signature) apenas em SKIP/fallback para liberar o completion gate do SceneFlow.
    ///
    /// Observações:
    /// - Não depende de "coordinator" obsoleto.
    /// - É best-effort: nunca deve travar o fluxo (sempre publica ResetCompleted).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleSceneFlowResetDriver : IDisposable
    {
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private readonly object _guardLock = new();
        private readonly HashSet<string> _inFlightSignatures = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _completedTicks = new(StringComparer.Ordinal);
        private bool _disposed;

        // Reasons canônicos (Contrato de Observability) em WorldResetReasons.

        // Janela curta para dedupe de assinatura (evita reset duplicado no mesmo frame).
        private const int DuplicateSignatureWindowMs = 750;
        private const int CompletedCacheLimit = 128;

        public WorldLifecycleSceneFlowResetDriver()
        {
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

            DebugUtility.LogVerbose<WorldLifecycleSceneFlowResetDriver>(
                $"[WorldLifecycle] Driver registrado: SceneFlow ScenesReady -> ResetWorld -> ResetCompleted. reason='{WorldResetReasons.SceneFlowScenesReady}'.",
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
            var context = evt.Context;
            string signature = SceneTransitionSignature.Compute(context);

            if (string.IsNullOrWhiteSpace(signature))
            {
                // Defensivo: assinatura vazia nao deve travar o SceneFlow; apenas libera.
                DebugUtility.LogWarning<WorldLifecycleSceneFlowResetDriver>(
                    "[WorldLifecycle] ScenesReady recebido com ContextSignature vazia. Liberando gate sem reset.");
                LogObsResetRequested(
                    signature: string.Empty,
                    sourceSignature: string.Empty,
                    routeId: context.RouteId.Value ?? string.Empty,
                    profile: context.TransitionProfileName,
                    target: ResolveTargetSceneName(context),
                    decisionSource: "contextSignature:empty",
                    reason: WorldResetReasons.SceneFlowScenesReady);
                PublishResetCompleted(
                    signature,
                    WorldResetReasons.SceneFlowScenesReady,
                    context.RouteId.Value ?? string.Empty,
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

            DebugUtility.LogVerbose<WorldLifecycleSceneFlowResetDriver>(
                $"[OBS][WorldLifecycle] ResetPolicy routeId='{routeId}', requiresWorldReset={requiresWorldReset}, signature='{signature}', reason='{decisionReason}', decisionSource='{decisionSource}'.",
                DebugUtility.Colors.Info);

            if (!requiresWorldReset)
            {
                string skippedReason = string.IsNullOrWhiteSpace(decisionReason)
                    ? $"{WorldResetReasons.SkippedStartupOrFrontendPrefix}:profile={context.TransitionProfileName};route={routeId};scene={targetScene}"
                    : decisionReason;

                if (ShouldSkipDuplicate(signature, out string guardReason))
                {
                    LogDuplicateGuard(signature, context.TransitionProfileName, targetScene, guardReason);
                    return;
                }

                MarkInFlight(signature);

                LogObsResetRequested(
                    signature: signature,
                    sourceSignature: signature,
                    routeId: routeId,
                    profile: context.TransitionProfileName,
                    target: targetScene,
                    decisionSource: decisionSource,
                    reason: skippedReason);

                DebugUtility.LogVerbose<WorldLifecycleSceneFlowResetDriver>(
                    $"[{ResetLogTags.Skipped}] [ResetSkip] [WorldLifecycle] ResetWorld SKIP. signature='{signature}', routeId='{routeId}', profile='{context.TransitionProfileName}', targetScene='{targetScene}', decisionSource='{decisionSource}', reason='{skippedReason}'.",
                    DebugUtility.Colors.Info);

                PublishResetCompleted(signature, skippedReason, routeId, context.TransitionProfileName, targetScene, decisionSource);
                MarkCompleted(signature);
                return;
            }

            // Reset real (policy requiresWorldReset=true).
            LogObsResetRequested(
                signature: signature,
                sourceSignature: signature,
                routeId: routeId,
                profile: context.TransitionProfileName,
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
                    context.TransitionProfileName,
                    decisionReason);

                shouldPublishCompletion = result.shouldPublishCompletion;
                if (!string.IsNullOrWhiteSpace(result.failureReason))
                {
                    completionReason = result.failureReason;
                }
            }
            catch (Exception ex)
            {
                // Best-effort: loga, mas NAO impede liberacao do gate.
                DebugUtility.LogError<WorldLifecycleSceneFlowResetDriver>(
                    $"[WorldLifecycle] Erro durante ResetWorld (ScenesReady). signature='{signature}', targetScene='{targetScene}', ex='{ex}'");
                shouldPublishCompletion = true;
            }
            finally
            {
                if (shouldPublishCompletion)
                {
                    // Fallback/SKIP: driver libera o gate quando nao ha WorldResetService publicando.
                    PublishResetCompleted(signature, completionReason, routeId, context.TransitionProfileName, targetScene, decisionSource);
                }
                MarkCompleted(signature);
            }
        }

        private static async Task<(bool shouldPublishCompletion, string failureReason)> ExecuteResetWhenRequiredAsync(
            string signature,
            string targetScene,
            string profileName,
            string decisionReason)
        {
            // Primeiro: se um WorldResetService estiver registrado no DI, use-o (ponto canonico).
            if (DependencyManager.HasInstance && DependencyManager.Provider.TryGetGlobal<WorldResetService>(out var resetService) && resetService != null)
            {
                DebugUtility.LogVerbose<WorldLifecycleSceneFlowResetDriver>(
                    $"[WorldLifecycle] Usando WorldResetService (Lifecycle) para executar reset. signature='{signature}', targetScene='{targetScene}'.",
                    DebugUtility.Colors.Info);

                try
                {
                    var request = new WorldResetRequest(
                        contextSignature: signature,
                        reason: string.IsNullOrWhiteSpace(decisionReason) ? WorldResetReasons.SceneFlowScenesReady : decisionReason,
                        profileName: profileName,
                        targetScene: targetScene,
                        origin: WorldResetOrigin.SceneFlow,
                        sourceSignature: signature,
                        isGameplayProfile: true);

                    await resetService.TriggerResetAsync(request);
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError<WorldLifecycleSceneFlowResetDriver>(
                        $"[WorldLifecycle] WorldResetService falhou durante TriggerResetAsync. signature='{signature}', targetScene='{targetScene}', ex='{ex}'.");
                }

                return (false, string.Empty);
            }

            DebugUtility.LogError<WorldLifecycleSceneFlowResetDriver>(
                $"[{ResetLogTags.Failed}][DEGRADED_MODE] [WorldLifecycle] WorldResetService ausente no DI. Reset nao executado. signature='{signature}', targetScene='{targetScene}'.");

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

        private static void PublishResetCompleted(string signature, string reason, string routeId, string profile, string target, string decisionSource)
        {
            // Publica apenas em SKIP/fallback: o completion gate depende disso para não degradar em timeout.
            LogObsResetCompleted(
                signature: signature,
                routeId: routeId,
                profile: profile,
                target: target,
                decisionSource: decisionSource,
                reason: reason);

            EventBus<WorldLifecycleResetCompletedEvent>.Raise(
                new WorldLifecycleResetCompletedEvent(signature ?? string.Empty, reason));
        }

        private static void LogObsResetRequested(
            string signature,
            string sourceSignature,
            string routeId,
            string profile,
            string target,
            string decisionSource,
            string reason)
        {
            // Observabilidade canônica (Contrato): ResetRequested com sourceSignature/reason/profile/target.
            DebugUtility.LogVerbose(typeof(WorldLifecycleSceneFlowResetDriver),
                $"[OBS][WorldLifecycle] ResetRequested signature='{signature ?? string.Empty}' sourceSignature='{sourceSignature ?? string.Empty}' routeId='{routeId ?? string.Empty}' profile='{profile ?? string.Empty}' target='{target ?? string.Empty}' decisionSource='{decisionSource ?? string.Empty}' reason='{reason ?? string.Empty}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogObsResetCompleted(
            string signature,
            string routeId,
            string profile,
            string target,
            string decisionSource,
            string reason)
        {
            // Observabilidade canônica (Contrato): ResetCompleted correlacionável ao gate (signature) e reason final.
            DebugUtility.LogVerbose(typeof(WorldLifecycleSceneFlowResetDriver),
                $"[OBS][WorldLifecycle] ResetCompleted signature='{signature ?? string.Empty}' routeId='{routeId ?? string.Empty}' profile='{profile ?? string.Empty}' target='{target ?? string.Empty}' decisionSource='{decisionSource ?? string.Empty}' reason='{reason ?? string.Empty}'.",
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

        private static void LogDuplicateGuard(string signature, string profile, string target, string guardReason)
        {
            DebugUtility.LogWarning<WorldLifecycleSceneFlowResetDriver>(
                $"[{ResetLogTags.Guarded}][DEGRADED_MODE] [WorldLifecycle] ResetWorld guard: ScenesReady duplicado. signature='{signature}', profile='{profile}', targetScene='{target}', guard='{guardReason}'.");
        }
    }
}
