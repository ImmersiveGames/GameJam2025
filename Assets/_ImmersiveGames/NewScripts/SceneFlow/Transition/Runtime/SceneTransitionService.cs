#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Policies;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Loading.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Adapters;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed partial class SceneTransitionService : ISceneTransitionService
    {
        private enum MacroTransitionCheckpoint
        {
            Started,
            FadeInCompleted,
            ScenesReady,
            BeforeFadeOut,
            Completed
        }

        private readonly ISceneFlowLoaderAdapter _loaderAdapter;
        private readonly ISceneFlowFadeAdapter _fadeAdapter;
        private readonly ISceneTransitionCompletionGate _completionGate;
        private readonly INavigationPolicy _navigationPolicy;
        private readonly IRouteGuard _routeGuard;
        private readonly IRouteResetPolicy? _routeResetPolicy;

        private readonly SemaphoreSlim _transitionGate = new(1, 1);
        private int _transitionInProgress;
        private long _transitionIdSeq;
        private string _lastCompletedSignature = string.Empty;
        private string _inFlightSignature = string.Empty;
        private string _lastRequestedSignature = string.Empty;
        private int _lastRequestedFrame = -1;

        public SceneTransitionService(
            ISceneFlowLoaderAdapter? loaderAdapter,
            ISceneFlowFadeAdapter? fadeAdapter,
            ISceneTransitionCompletionGate? completionGate,
            INavigationPolicy navigationPolicy,
            IRouteGuard routeGuard,
            IRouteResetPolicy? routeResetPolicy = null)
        {
            _loaderAdapter = loaderAdapter ?? new SceneManagerLoaderAdapter();
            _fadeAdapter = fadeAdapter ?? new NoFadeAdapter();
            _completionGate = completionGate ?? new NoOpTransitionCompletionGate();
            _navigationPolicy = navigationPolicy ?? throw new ArgumentNullException(nameof(navigationPolicy));
            _routeGuard = routeGuard ?? throw new ArgumentNullException(nameof(routeGuard));
            _routeResetPolicy = routeResetPolicy;
        }

        public async Task TransitionAsync(SceneTransitionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var hydratedRequest = BuildRequestFromRouteDefinition(request, out SceneRouteDefinition? routeDefinition);
            EnsureTransitionProfileOrFailFast(hydratedRequest);
            var context = BuildContextWithResetDecision(hydratedRequest, routeDefinition);
            string signature = SceneTransitionSignature.Compute(context) ?? string.Empty;
            LogResolvedRouteForObservability(hydratedRequest, signature);

            DebugUtility.Log<SceneTransitionService>(
                $"[OBS][SceneFlow] RouteAppliedPolicy routeId='{context.RouteId}' requiresWorldReset={context.RequiresWorldReset} decisionSource='{context.ResetDecisionSource}' decisionReason='{context.ResetDecisionReason}' signature='{signature}'.",
                DebugUtility.Colors.Info);

            if (!_navigationPolicy.CanTransition(hydratedRequest, out string? denialReason))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Transicao bloqueada por policy. signature='{signature}', routeId='{context.RouteId}', style='{context.StyleLabel}', reason='{Sanitize(hydratedRequest.Reason)}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}', policyReason='{Sanitize(denialReason)}'.");
                return;
            }

            if (routeDefinition.HasValue && !_routeGuard.CanTransitionRoute(hydratedRequest, routeDefinition.Value, out denialReason))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Transicao bloqueada por route guard. signature='{signature}', routeId='{context.RouteId}', kind='{routeDefinition.Value.RouteKind}', reason='{Sanitize(hydratedRequest.Reason)}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}', guardReason='{Sanitize(denialReason)}'.");
                return;
            }

            if (ShouldDedupeSameFrame(signature))
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Dedupe: TransitionAsync ignorado (mesmo frame/mesma assinatura). signature='{signature}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}'.");
                return;
            }

            if (IsInFlightSameSignature(signature))
            {
                DebugUtility.Log<SceneTransitionService>(
                    $"[OBS][SceneFlow] TransitionRequestCoalesced reason='in_flight_same_signature' signature='{signature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (Interlocked.CompareExchange(ref _transitionInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Uma transicao ja esta em andamento (_transitionInProgress ativo). Ignorando solicitacao concorrente. signature='{signature}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}'.");
                return;
            }

            if (!_transitionGate.Wait(0))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Uma transicao ja esta em andamento. Ignorando solicitacao concorrente. signature='{signature}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}'.");
                Interlocked.Exchange(ref _transitionInProgress, 0);
                return;
            }

            if (string.Equals(signature, _lastCompletedSignature, StringComparison.Ordinal))
            {
                DebugUtility.Log<SceneTransitionService>(
                    $"[OBS][SceneFlow] TransitionRequestAccepted reason='completed_allows_same_signature' signature='{signature}'.",
                    DebugUtility.Colors.Info);
            }

            long transitionId = Interlocked.Increment(ref _transitionIdSeq);
            LogBoundaryHandshake("Navigation", "policy_and_route_guard_resolved", transitionId, signature, context);

            try
            {
                _inFlightSignature = signature ?? string.Empty;

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] TransitionStarted id={transitionId} signature='{signature}' routeId='{context.RouteId}', style='{context.StyleLabel}', profile='{context.TransitionProfileName}', reason='{Sanitize(hydratedRequest.Reason)}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}' {context}",
                    DebugUtility.Colors.Info);

                LogMacroCheckpoint(MacroTransitionCheckpoint.Started, transitionId, signature, context);
                LogLifecycleEvent("SceneTransitionStartedEvent", transitionId, signature, context);
                EventBus<SceneTransitionStartedEvent>.Raise(new SceneTransitionStartedEvent(context));
                LogBoundaryHandshake("Loading-Fade", "fade_in_requested", transitionId, signature, context);
                await RunFadeInIfNeeded(context, transitionId, signature);

                if (context.UseFade)
                {
                    LogMacroCheckpoint(MacroTransitionCheckpoint.FadeInCompleted, transitionId, signature, context);
                    LogBoundaryHandshake("Loading-Fade", "fade_in_completed", transitionId, signature, context);
                    LogLifecycleEvent("SceneTransitionFadeInCompletedEvent", transitionId, signature, context);
                    EventBus<SceneTransitionFadeInCompletedEvent>.Raise(new SceneTransitionFadeInCompletedEvent(context));
                }

                await RunSceneOperationsAsync(context);
                LogMacroCheckpoint(MacroTransitionCheckpoint.ScenesReady, transitionId, signature, context);
                LogLifecycleEvent("SceneTransitionScenesReadyEvent", transitionId, signature, context);
                EventBus<SceneTransitionScenesReadyEvent>.Raise(new SceneTransitionScenesReadyEvent(context));
                LogBoundaryHandshake("Navigation", "scene_composition_committed", transitionId, signature, context);

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] ScenesReady id={transitionId} signature='{signature}' routeId='{context.RouteId}', style='{context.StyleLabel}', profile='{context.TransitionProfileName}'.",
                    DebugUtility.Colors.Info);

                await AwaitCompletionGateAsync(context);
                LogMacroCheckpoint(MacroTransitionCheckpoint.BeforeFadeOut, transitionId, signature, context);
                LogBoundaryHandshake("Loading-Fade", "fade_out_requested", transitionId, signature, context);
                LogLifecycleEvent("SceneTransitionBeforeFadeOutEvent", transitionId, signature, context);
                EventBus<SceneTransitionBeforeFadeOutEvent>.Raise(new SceneTransitionBeforeFadeOutEvent(context));
                await RunFadeOutIfNeeded(context, transitionId, signature);
                LogMacroCheckpoint(MacroTransitionCheckpoint.Completed, transitionId, signature, context);
                LogBoundaryHandshake("Loading-Fade", "fade_out_completed", transitionId, signature, context);
                LogLifecycleEvent("SceneTransitionCompletedEvent", transitionId, signature, context);
                EventBus<SceneTransitionCompletedEvent>.Raise(new SceneTransitionCompletedEvent(context));
                MarkCompleted(signature);

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] TransitionCompleted id={transitionId} signature='{signature}' routeId='{context.RouteId}', style='{context.StyleLabel}', profile='{context.TransitionProfileName}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<SceneTransitionService>($"[SceneFlow] Erro durante transicao: {ex}");
                throw;
            }
            finally
            {
                _inFlightSignature = string.Empty;
                Interlocked.Exchange(ref _transitionInProgress, 0);
                _transitionGate.Release();
            }
        }

        private static void EnsureTransitionProfileOrFailFast(SceneTransitionRequest request)
        {
            if (request.TransitionProfile != null || !request.UseFade)
            {
                return;
            }

            string message = $"[FATAL][Config] SceneTransitionProfile ausente na request com UseFade=true. routeId='{request.RouteId}', style='{request.StyleLabel}', requestedBy='{Sanitize(request.RequestedBy)}', reason='{Sanitize(request.Reason)}'.";
            DebugUtility.LogError<SceneTransitionService>(message);
            DevStopPlayModeInEditor();
            if (!Application.isEditor)
            {
                Application.Quit();
            }
            throw new InvalidOperationException(message);
        }

        private static void FailFastTransitionRequest(SceneTransitionRequest request, string detail)
        {
            string message = $"[FATAL][Config] {detail} requestedBy='{Sanitize(request.RequestedBy)}', reason='{Sanitize(request.Reason)}'.";
            DebugUtility.LogError<SceneTransitionService>(message);
            DevStopPlayModeInEditor();
            if (!Application.isEditor)
            {
                Application.Quit();
            }
            throw new InvalidOperationException(message);
        }

        private static void LogResolvedRouteForObservability(SceneTransitionRequest request, string signature)
        {
            DebugUtility.Log<SceneTransitionService>(
                $"[OBS][SceneFlow] RouteApplied routeId='{request.RouteId}' scenesToLoadCount={request.ScenesToLoad.Count} activeScene='{request.TargetActiveScene}' transitionProfile='{request.TransitionProfileName}' signature='{signature}'.",
                DebugUtility.Colors.Info);
        }

        private SceneTransitionContext BuildContextWithResetDecision(SceneTransitionRequest request, SceneRouteDefinition? routeDefinition)
        {
            SceneRouteKind routeKind = routeDefinition?.RouteKind ?? SceneRouteKind.Unspecified;
            SceneTransitionContext context = SceneTransitionSignature.BuildContext(request, routeKind);

            if (!TryResolveRouteResetPolicy(out var policy) || policy == null)
            {
                return context.WithRouteResetDecision(false, "policy:missing", "RouteResetPolicyMissing");
            }

            RouteResetDecision resetDecision = policy.Resolve(context.RouteId, routeDefinition, context);
            return context.WithRouteResetDecision(resetDecision.ShouldReset, resetDecision.DecisionSource, resetDecision.Reason);
        }

        private bool TryResolveRouteResetPolicy(out IRouteResetPolicy? policy)
        {
            if (_routeResetPolicy != null)
            {
                policy = _routeResetPolicy;
                return true;
            }

            if (DependencyManager.HasInstance && DependencyManager.Provider.TryGetGlobal<IRouteResetPolicy>(out var resolved) && resolved != null)
            {
                policy = resolved;
                return true;
            }

            policy = null;
            return false;
        }


        private static bool TryResolveSceneCompositionExecutor(out ISceneCompositionExecutor? executor)
        {
            if (DependencyManager.HasInstance && DependencyManager.Provider.TryGetGlobal<ISceneCompositionExecutor>(out var resolved) && resolved != null)
            {
                executor = resolved;
                return true;
            }

            executor = null;
            return false;
        }

        private SceneTransitionRequest BuildRequestFromRouteDefinition(SceneTransitionRequest request, out SceneRouteDefinition? routeDefinition)
        {
            routeDefinition = null;

            if (!request.RouteId.IsValid)
            {
                if (request.HasInlineSceneData)
                {
                    DebugUtility.LogWarning<SceneTransitionService>(
                        $"[OBS][Deprecated] Inline scene data foi detectado sem RouteId. Fluxo legado desativado; request sera abortada. requestedBy='{Sanitize(request.RequestedBy)}', reason='{Sanitize(request.Reason)}'.");
                }

                FailFastTransitionRequest(request, "RouteId ausente/invalido.");
                return request;
            }

            if (request.ResolvedRouteDefinition.HasValue)
            {
                SceneRouteDefinition resolvedRouteDefinition = request.ResolvedRouteDefinition.GetValueOrDefault();
                SceneRouteDefinitionAsset resolvedRouteRef = request.ResolvedRouteRef
                    ?? throw new InvalidOperationException($"SceneTransitionRequest sem routeRef canonica. routeId='{request.RouteId}'.");

                if (resolvedRouteRef.RouteId != request.RouteId)
                {
                    FailFastTransitionRequest(request, $"SceneTransitionRequest routeRef mismatch. routeId='{request.RouteId}' routeRefRouteId='{resolvedRouteRef.RouteId}'.");
                }

                routeDefinition = resolvedRouteDefinition;

                if (string.IsNullOrWhiteSpace(resolvedRouteDefinition.TargetActiveScene))
                {
                    FailFastTransitionRequest(request, $"routeId='{request.RouteId}' com TargetActiveScene vazio.");
                }

                DebugUtility.Log<SceneTransitionService>(
                    $"[OBS][SceneFlow] RouteResolvedVia=DirectDefinition routeId='{request.RouteId}' source='SceneTransitionRequest.ResolvedRouteDefinition'.",
                    DebugUtility.Colors.Info);

                return request;
            }

            FailFastTransitionRequest(request, $"SceneTransitionRequest sem route definition resolvida. routeId='{request.RouteId}'.");
            return request;
        }

        private bool ShouldDedupeSameFrame(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            int currentFrame = Time.frameCount;
            bool shouldDedupe = string.Equals(signature, _lastRequestedSignature, StringComparison.Ordinal) && currentFrame == _lastRequestedFrame;
            _lastRequestedSignature = signature ?? string.Empty;
            _lastRequestedFrame = currentFrame;
            return shouldDedupe;
        }

        private bool IsInFlightSameSignature(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _transitionInProgress, 0, 0) != 1)
            {
                return false;
            }

            return string.Equals(_inFlightSignature, signature, StringComparison.Ordinal);
        }

        private void MarkCompleted(string? signature)
        {
            _lastCompletedSignature = signature ?? string.Empty;
        }

        private async Task AwaitCompletionGateAsync(SceneTransitionContext context)
        {
            try
            {
                DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Aguardando completion gate antes do FadeOut. signature='{SceneTransitionSignature.Compute(context)}'.");
                await _completionGate.AwaitBeforeFadeOutAsync(context);
                DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Completion gate concluido. Prosseguindo para FadeOut. signature='{SceneTransitionSignature.Compute(context)}'.");
            }
            catch (Exception ex)
            {
                if (IsFatalH1Exception(ex))
                {
                    DebugUtility.LogError<SceneTransitionService>($"[SceneFlow] Completion gate abortado por fail-fast H1. Interrompendo transicao. ex={ex.GetType().Name}: {ex.Message}");
                    throw;
                }

                DebugUtility.LogWarning<SceneTransitionService>($"[SceneFlow] Completion gate falhou/abortou. Prosseguindo com FadeOut. ex={ex.GetType().Name}: {ex.Message}");
                string fallbackReason = ResolveCompletionGateFallbackReason(ex);
                DebugUtility.Log<SceneTransitionService>($"[OBS][SceneFlow] CompletionGateFallback applied='true' reason='{fallbackReason}' signature='{SceneTransitionSignature.Compute(context)}'.", DebugUtility.Colors.Info);
            }
        }

        private static string ResolveCompletionGateFallbackReason(Exception ex)
        {
            if (ex is TimeoutException)
            {
                return "timeout";
            }
            if (ex is OperationCanceledException)
            {
                return "abort";
            }
            return "exception";
        }

        private static bool IsFatalH1Exception(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }
            if (!string.IsNullOrWhiteSpace(ex.Message) && ex.Message.IndexOf("[FATAL][H1]", StringComparison.Ordinal) >= 0)
            {
                return true;
            }
            return ex.InnerException != null && IsFatalH1Exception(ex.InnerException);
        }

        private async Task RunFadeInIfNeeded(SceneTransitionContext context, long transitionId, string? signature)
        {
            if (!context.UseFade || context.TransitionProfile == null)
            {
                return;
            }
            _fadeAdapter.ConfigureFromProfile(context.TransitionProfile, context.TransitionProfileName);
            var fadeInTask = _fadeAdapter.FadeInAsync(signature);
            LogObsFade("FadeInStarted", transitionId, signature, context.TransitionProfileName);
            await fadeInTask;
            LogObsFade("FadeInCompleted", transitionId, signature, context.TransitionProfileName);
        }

        private async Task RunFadeOutIfNeeded(SceneTransitionContext context, long transitionId, string? signature)
        {
            if (!context.UseFade)
            {
                return;
            }
            LogObsFade("FadeOutStarted", transitionId, signature, context.TransitionProfileName);
            await _fadeAdapter.FadeOutAsync(signature);
            LogObsFade("FadeOutCompleted", transitionId, signature, context.TransitionProfileName);
        }

        private static void LogObsFade(string phase, long transitionId, string? signature, string profile)
        {
            string normalizedSignature = string.IsNullOrWhiteSpace(signature) ? "n/a" : signature;
            DebugUtility.Log<SceneTransitionService>($"[OBS][Fade] {phase} id={transitionId} signature='{normalizedSignature}' profile='{profile}'.");
        }

        private static void LogLifecycleEvent(string eventName, long transitionId, string? signature, SceneTransitionContext context)
        {
            string normalizedSignature = string.IsNullOrWhiteSpace(signature) ? "n/a" : signature;
            DebugUtility.Log<SceneTransitionService>(
                $"[OBS][SceneFlow] {eventName} id={transitionId} signature='{normalizedSignature}' routeId='{context.RouteId}' routeKind='{context.RouteKind}' style='{context.StyleLabel}' profile='{context.TransitionProfileName}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogBoundaryHandshake(string boundary, string stage, long transitionId, string? signature, SceneTransitionContext context)
        {
            string normalizedSignature = string.IsNullOrWhiteSpace(signature) ? "n/a" : signature;
            DebugUtility.Log<SceneTransitionService>(
                $"[OBS][SceneFlow][Handshake] boundary='{boundary}' stage='{stage}' id={transitionId} signature='{normalizedSignature}' routeId='{context.RouteId}' routeKind='{context.RouteKind}' profile='{context.TransitionProfileName}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogMacroCheckpoint(MacroTransitionCheckpoint checkpoint, long transitionId, string? signature, SceneTransitionContext context)
        {
            string normalizedSignature = string.IsNullOrWhiteSpace(signature) ? "n/a" : signature;
            DebugUtility.Log<SceneTransitionService>(
                $"[OBS][SceneFlow][Macro] checkpoint='{checkpoint}' id={transitionId} signature='{normalizedSignature}' routeId='{context.RouteId}' routeKind='{context.RouteKind}' style='{context.StyleLabel}' profile='{context.TransitionProfileName}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogLoadedScenesSnapshot(string stage)
        {
            DebugUtility.Log<SceneTransitionService>($"[OBS][SceneFlow] LoadedScenesSnapshot stage='{stage}' sceneCount={SceneManager.sceneCount} scenes=[{DescribeLoadedScenes()}].", DebugUtility.Colors.Info);
        }

        private static string DescribeLoadedScenes()
        {
            if (SceneManager.sceneCount <= 0)
            {
                return "<none>";
            }
            var scenes = new List<string>(SceneManager.sceneCount);
            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                scenes.Add($"name='{scene.name}', isLoaded={scene.isLoaded}, buildIndex={scene.buildIndex}, isActive={scene == activeScene}");
            }
            return string.Join(" | ", scenes);
        }

        private static void LogRouteExecutionPlan(SceneTransitionContext context, IReadOnlyList<string> loadScenes, IReadOnlyList<string> unloadScenes, IReadOnlyList<string> reloadScenes)
        {
            DebugUtility.Log<SceneTransitionService>($"[OBS][SceneFlow] RouteExecutionPlan routeId='{context.RouteId}' activeScene='{context.TargetActiveScene}' toLoad=[{FormatSceneList(loadScenes)}] toUnload=[{FormatSceneList(unloadScenes)}] reload=[{FormatSceneList(reloadScenes)}].", DebugUtility.Colors.Info);
        }

        private static string FormatSceneList(IReadOnlyList<string>? scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return "<none>";
            }
            return string.Join(", ", scenes);
        }

        private async Task RunSceneOperationsAsync(SceneTransitionContext context)
        {
            string signature = SceneTransitionSignature.Compute(context);
            LogLoadedScenesSnapshot("before_apply_route");

            IReadOnlyList<string> loadScenes = NormalizeSceneList(context.ScenesToLoad);
            IReadOnlyList<string> routeUnloadScenes = NormalizeSceneList(context.ScenesToUnload);
            IReadOnlyList<string> supplementalUnloadScenes = ResolveSupplementalUnloadScenes(context);
            IReadOnlyList<string> combinedUnloadScenes = MergeSceneLists(routeUnloadScenes, supplementalUnloadScenes);
            IReadOnlyList<string> reloadScenes = GetReloadScenes(loadScenes, combinedUnloadScenes);
            IReadOnlyList<string> unloadScenes = BuildUnloadPlan(context, combinedUnloadScenes, reloadScenes);
            LogRouteExecutionPlan(context, loadScenes, unloadScenes, reloadScenes);

            ReportRouteLoadingProgress(signature, 0f, "Preparing route scenes", context.Reason);

            string currentActiveScene = _loaderAdapter.GetActiveSceneName();
            if (!string.IsNullOrWhiteSpace(currentActiveScene) && ContainsScene(unloadScenes, currentActiveScene))
            {
                string tempActive = ResolveTempActiveScene(unloadScenes);
                if (!string.IsNullOrWhiteSpace(tempActive) && !string.Equals(tempActive, currentActiveScene, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] ApplyRoute: definindo cena ativa temporaria '{tempActive}' para proteger unload.");
                    await SetActiveSceneAsync(tempActive, signature, context.Reason);
                }
            }

            ISceneCompositionExecutor sceneCompositionExecutor = ResolveSceneCompositionExecutorOrFail(context, signature);

            ReportRouteLoadingProgress(signature, 0.25f, "Applying route composition", context.Reason);
            await sceneCompositionExecutor.ApplyAsync(
                RouteSceneCompositionRequestFactory.CreateMacroApplyRequest(loadScenes, unloadScenes, context.Reason, signature));
            ReportRouteLoadingProgress(signature, 0.7f, "Route composition applied", context.Reason);

            await SetActiveSceneAsync(context.TargetActiveScene, signature, context.Reason);
            if (!string.IsNullOrWhiteSpace(context.TargetActiveScene))
            {
                ReportRouteLoadingProgress(signature, 0.9f, $"Activating scene: {context.TargetActiveScene}", context.Reason);
            }

            ReportRouteLoadingProgress(signature, 1f, "Route scenes ready", context.Reason);
            LogLoadedScenesSnapshot("after_apply_route");
        }

        private async Task SetActiveSceneAsync(string targetActiveScene, string signature, string reason)
        {
            if (string.IsNullOrWhiteSpace(targetActiveScene))
            {
                DebugUtility.LogVerbose<SceneTransitionService>("[SceneFlow] TargetActiveScene vazio. Mantendo cena ativa atual.");
                return;
            }

            // Importante: a ativacao da cena no trilho macro continua ownership do SceneFlow.
            // SceneComposition executa apenas load/unload; o momento sensivel de set-active
            // permanece aqui para preservar sequencing de transicao, fade e readiness.
            bool success = await _loaderAdapter.TrySetActiveSceneAsync(targetActiveScene);
            if (!success)
            {
                DebugUtility.LogWarning<SceneTransitionService>($"[SceneFlow] Nao foi possivel definir a cena ativa para '{targetActiveScene}'. Cena atual='{_loaderAdapter.GetActiveSceneName()}'. signature='{signature}' reason='{Sanitize(reason)}'.");
            }
            else
            {
                DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Cena ativa definida para '{targetActiveScene}'. signature='{signature}' reason='{Sanitize(reason)}'.");
            }
        }

        private ISceneCompositionExecutor ResolveSceneCompositionExecutorOrFail(SceneTransitionContext context, string signature)
        {
            if (TryResolveSceneCompositionExecutor(out var executor) && executor != null)
            {
                return executor;
            }

            HardFailFastH1.Trigger(typeof(SceneTransitionService),
                $"[FATAL][H1][SceneFlow] ISceneCompositionExecutor missing for macro route apply. routeId='{context.RouteId}' signature='{signature}' reason='{context.Reason}'.");
            return null!;
        }

        private static void ReportRouteLoadingProgress(string signature, float normalizedProgress, string stepLabel, string reason)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return;
            }

            EventBus<SceneFlowRouteLoadingProgressEvent>.Raise(
                new SceneFlowRouteLoadingProgressEvent(signature, normalizedProgress, stepLabel, reason));
        }

        private static IReadOnlyList<string> NormalizeSceneList(IReadOnlyList<string>? scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return Array.Empty<string>();
            }

            var dedupe = new HashSet<string>(StringComparer.Ordinal);
            var normalized = new List<string>(scenes.Count);
            foreach (string sceneName in scenes)
            {
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    continue;
                }

                string trimmed = sceneName.Trim();
                if (trimmed.Length == 0 || !dedupe.Add(trimmed))
                {
                    continue;
                }

                normalized.Add(trimmed);
            }

            return normalized;
        }

        private static bool ContainsScene(IReadOnlyList<string>? scenes, string sceneName)
        {
            if (scenes == null || scenes.Count == 0 || string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            for (int i = 0; i < scenes.Count; i++)
            {
                if (string.Equals(scenes[i], sceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<string> BuildUnloadPlan(
            SceneTransitionContext context,
            IReadOnlyList<string> unloadScenes,
            IReadOnlyList<string> reloadScenes)
        {
            if (unloadScenes == null || unloadScenes.Count == 0)
            {
                return Array.Empty<string>();
            }

            var reloadSet = new HashSet<string>(reloadScenes ?? Array.Empty<string>(), StringComparer.Ordinal);
            string targetActive = string.IsNullOrWhiteSpace(context.TargetActiveScene) ? string.Empty : context.TargetActiveScene.Trim();

            var plan = new List<string>(unloadScenes.Count);
            foreach (string sceneName in unloadScenes)
            {
                bool isTargetActive = targetActive.Length > 0 && string.Equals(sceneName, targetActive, StringComparison.Ordinal);
                bool isReload = reloadSet.Contains(sceneName);
                if (isTargetActive && !isReload)
                {
                    DebugUtility.Log<SceneTransitionService>(
                        $"[OBS][SceneFlow] SkipUnload scene='{sceneName}' reason='target_active_scene'.",
                        DebugUtility.Colors.Info);
                    continue;
                }

                plan.Add(sceneName);
            }

            return plan;
        }

        private static IReadOnlyList<string> MergeSceneLists(params IReadOnlyList<string>[] lists)
        {
            if (lists == null || lists.Length == 0)
            {
                return Array.Empty<string>();
            }

            HashSet<string> dedupe = new HashSet<string>(StringComparer.Ordinal);
            List<string> merged = new List<string>();

            for (int i = 0; i < lists.Length; i++)
            {
                IReadOnlyList<string> list = lists[i];
                if (list == null || list.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < list.Count; j++)
                {
                    string sceneName = list[j];
                    if (string.IsNullOrWhiteSpace(sceneName))
                    {
                        continue;
                    }

                    string normalized = sceneName.Trim();
                    if (normalized.Length == 0 || !dedupe.Add(normalized))
                    {
                        continue;
                    }

                    merged.Add(normalized);
                }
            }

            return merged.Count == 0 ? Array.Empty<string>() : merged;
        }

        private static IReadOnlyList<string> ResolveSupplementalUnloadScenes(SceneTransitionContext context)
        {
            IReadOnlyList<string> supplementalScenes = SceneTransitionUnloadSupplementRegistry.GetSupplementalScenesToUnload(context);
            if (supplementalScenes == null || supplementalScenes.Count == 0)
            {
                return Array.Empty<string>();
            }

            return NormalizeSceneList(supplementalScenes);
        }

        private static IReadOnlyList<string> GetReloadScenes(IReadOnlyList<string>? scenesToLoad, IReadOnlyList<string>? scenesToUnload)
        {
            if (scenesToLoad == null || scenesToUnload == null)
            {
                return Array.Empty<string>();
            }
            var unloadSet = new HashSet<string>(scenesToUnload, StringComparer.Ordinal);
            var reload = new List<string>();
            foreach (string sceneName in scenesToLoad)
            {
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    continue;
                }
                string trimmed = sceneName.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }
                if (unloadSet.Contains(trimmed))
                {
                    reload.Add(trimmed);
                }
            }
            return reload;
        }

        private string ResolveTempActiveScene(IReadOnlyList<string> scenesPlannedToUnload)
        {
            var unloadSet = new HashSet<string>(scenesPlannedToUnload ?? Array.Empty<string>(), StringComparer.Ordinal);
            const string uiGlobalSceneName = "UIGlobalScene";
            if (_loaderAdapter.IsSceneLoaded(uiGlobalSceneName) && !unloadSet.Contains(uiGlobalSceneName))
            {
                return uiGlobalSceneName;
            }
            string currentActive = _loaderAdapter.GetActiveSceneName();
            if (!string.IsNullOrWhiteSpace(currentActive) && !unloadSet.Contains(currentActive))
            {
                return currentActive;
            }
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }
                string name = scene.name;
                if (!string.IsNullOrWhiteSpace(name) && !unloadSet.Contains(name))
                {
                    return name;
                }
            }
            return string.Empty;
        }

        private sealed class NoFadeAdapter : ISceneFlowFadeAdapter
        {
            public bool IsAvailable => false;
            public void ConfigureFromProfile(Bindings.SceneTransitionProfile profile, string profileLabel) { }
            public Task FadeInAsync(string? contextSignature = null) => Task.CompletedTask;
            public Task FadeOutAsync(string? contextSignature = null) => Task.CompletedTask;
        }

        private sealed class NoOpTransitionCompletionGate : ISceneTransitionCompletionGate
        {
            public Task AwaitBeforeFadeOutAsync(SceneTransitionContext context) => Task.CompletedTask;
        }

        static partial void DevStopPlayModeInEditor();

        private static string Sanitize(string s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }

    public interface ISceneTransitionUnloadSupplementProvider
    {
        IReadOnlyList<string> GetSupplementalScenesToUnload(SceneTransitionContext context);
    }

    public static class SceneTransitionUnloadSupplementRegistry
    {
        private static readonly object Sync = new object();
        private static readonly List<ISceneTransitionUnloadSupplementProvider> Providers = new List<ISceneTransitionUnloadSupplementProvider>();

        public static void Register(ISceneTransitionUnloadSupplementProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            lock (Sync)
            {
                if (!Providers.Contains(provider))
                {
                    Providers.Add(provider);
                }
            }
        }

        public static void Unregister(ISceneTransitionUnloadSupplementProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            lock (Sync)
            {
                Providers.Remove(provider);
            }
        }

        public static IReadOnlyList<string> GetSupplementalScenesToUnload(SceneTransitionContext context)
        {
            ISceneTransitionUnloadSupplementProvider[] providersSnapshot;

            lock (Sync)
            {
                if (Providers.Count == 0)
                {
                    return Array.Empty<string>();
                }

                providersSnapshot = Providers.ToArray();
            }

            if (providersSnapshot.Length == 0)
            {
                return Array.Empty<string>();
            }

            HashSet<string> dedupe = new HashSet<string>(StringComparer.Ordinal);
            List<string> scenesToUnload = new List<string>();

            for (int i = 0; i < providersSnapshot.Length; i++)
            {
                IReadOnlyList<string> providerScenes = providersSnapshot[i].GetSupplementalScenesToUnload(context);
                if (providerScenes == null || providerScenes.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < providerScenes.Count; j++)
                {
                    string sceneName = providerScenes[j];
                    if (string.IsNullOrWhiteSpace(sceneName))
                    {
                        continue;
                    }

                    string normalized = sceneName.Trim();
                    if (normalized.Length == 0 || !dedupe.Add(normalized))
                    {
                        continue;
                    }

                    scenesToUnload.Add(normalized);
                }
            }

            return scenesToUnload.Count == 0 ? Array.Empty<string>() : scenesToUnload;
        }
    }
}

