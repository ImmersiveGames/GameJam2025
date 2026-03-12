#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Adapters;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed partial class SceneTransitionService : ISceneTransitionService
    {
        private readonly ISceneFlowLoaderAdapter _loaderAdapter;
        private readonly ISceneFlowFadeAdapter _fadeAdapter;
        private readonly ISceneTransitionCompletionGate _completionGate;
        private readonly INavigationPolicy _navigationPolicy;
        private readonly ISceneRouteResolver? _routeResolver;
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
            ISceneTransitionCompletionGate? completionGate = null,
            INavigationPolicy? navigationPolicy = null,
            ISceneRouteResolver? routeResolver = null,
            IRouteGuard? routeGuard = null,
            IRouteResetPolicy? routeResetPolicy = null)
        {
            _loaderAdapter = loaderAdapter ?? new SceneManagerLoaderAdapter();
            _fadeAdapter = fadeAdapter ?? new NoFadeAdapter();
            _completionGate = completionGate ?? new NoOpTransitionCompletionGate();
            _navigationPolicy = navigationPolicy ?? new AllowAllNavigationPolicy();
            _routeResolver = routeResolver;
            _routeGuard = routeGuard ?? new AllowAllRouteGuard();
            _routeResetPolicy = routeResetPolicy;
        }

        public async Task TransitionAsync(SceneTransitionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var hydratedRequest = BuildRequestFromRouteDefinition(request, out var routeDefinition);
            EnsureTransitionProfileOrFailFast(hydratedRequest);
            var context = BuildContextWithResetDecision(hydratedRequest, routeDefinition);
            string signature = SceneTransitionSignature.Compute(context);
            LogResolvedRouteForObservability(hydratedRequest, signature);

            DebugUtility.Log<SceneTransitionService>(
                $"[OBS][SceneFlow] RouteAppliedPolicy routeId='{context.RouteId}' requiresWorldReset={context.RequiresWorldReset} decisionSource='{context.ResetDecisionSource}' decisionReason='{context.ResetDecisionReason}' signature='{signature}'.",
                DebugUtility.Colors.Info);

            if (!_navigationPolicy.CanTransition(hydratedRequest, out var denialReason))
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

            try
            {
                _inFlightSignature = signature ?? string.Empty;

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] TransitionStarted id={transitionId} signature='{signature}' routeId='{context.RouteId}', style='{context.StyleLabel}', profile='{context.TransitionProfileName}', reason='{Sanitize(hydratedRequest.Reason)}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}' {context}",
                    DebugUtility.Colors.Info);

                EventBus<SceneTransitionStartedEvent>.Raise(new SceneTransitionStartedEvent(context));
                await RunFadeInIfNeeded(context, transitionId, signature);

                if (context.UseFade)
                {
                    EventBus<SceneTransitionFadeInCompletedEvent>.Raise(new SceneTransitionFadeInCompletedEvent(context));
                }

                await RunSceneOperationsAsync(context);
                EventBus<SceneTransitionScenesReadyEvent>.Raise(new SceneTransitionScenesReadyEvent(context));

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] ScenesReady id={transitionId} signature='{signature}' routeId='{context.RouteId}', style='{context.StyleLabel}', profile='{context.TransitionProfileName}'.",
                    DebugUtility.Colors.Info);

                await AwaitCompletionGateAsync(context);
                EventBus<SceneTransitionBeforeFadeOutEvent>.Raise(new SceneTransitionBeforeFadeOutEvent(context));
                await RunFadeOutIfNeeded(context, transitionId, signature);
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

            if (_routeResolver == null)
            {
                FailFastTransitionRequest(request, $"ISceneRouteResolver indisponivel para routeId='{request.RouteId}'.");
                return request;
            }

            if (!_routeResolver.TryResolve(request.RouteId, out var resolvedRoute))
            {
                FailFastTransitionRequest(request, $"routeId='{request.RouteId}' nao encontrado no catalogo de rotas.");
                return request;
            }

            DebugUtility.Log<SceneTransitionService>(
                $"[OBS][SceneFlow] RouteResolvedVia=RouteId routeId='{request.RouteId}' source='ISceneRouteResolver'.",
                DebugUtility.Colors.Info);

            routeDefinition = resolvedRoute;

            if (string.IsNullOrWhiteSpace(resolvedRoute.TargetActiveScene))
            {
                FailFastTransitionRequest(request, $"routeId='{request.RouteId}' com TargetActiveScene vazio.");
                return request;
            }

            return new SceneTransitionRequest(
                resolvedRoute.ScenesToLoad,
                resolvedRoute.ScenesToUnload,
                resolvedRoute.TargetActiveScene,
                request.RouteId,
                request.TransitionStyle,
                request.Payload,
                request.TransitionProfile,
                request.UseFade,
                request.StyleLabel,
                request.TransitionProfileName,
                request.ContextSignature,
                request.RequestedBy,
                request.Reason);
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
            if (ex is TimeoutException) return "timeout";
            if (ex is OperationCanceledException) return "abort";
            return "exception";
        }

        private static bool IsFatalH1Exception(Exception ex)
        {
            if (ex == null) return false;
            if (!string.IsNullOrWhiteSpace(ex.Message) && ex.Message.IndexOf("[FATAL][H1]", StringComparison.Ordinal) >= 0) return true;
            return ex.InnerException != null && IsFatalH1Exception(ex.InnerException);
        }

        private async Task RunFadeInIfNeeded(SceneTransitionContext context, long transitionId, string signature)
        {
            if (!context.UseFade || context.TransitionProfile == null) return;
            _fadeAdapter.ConfigureFromProfile(context.TransitionProfile, context.TransitionProfileName);
            var fadeInTask = _fadeAdapter.FadeInAsync(signature);
            LogObsFade("FadeInStarted", transitionId, signature, context.TransitionProfileName);
            await fadeInTask;
            LogObsFade("FadeInCompleted", transitionId, signature, context.TransitionProfileName);
        }

        private async Task RunFadeOutIfNeeded(SceneTransitionContext context, long transitionId, string signature)
        {
            if (!context.UseFade) return;
            LogObsFade("FadeOutStarted", transitionId, signature, context.TransitionProfileName);
            await _fadeAdapter.FadeOutAsync(signature);
            LogObsFade("FadeOutCompleted", transitionId, signature, context.TransitionProfileName);
        }

        private static void LogObsFade(string phase, long transitionId, string signature, string profile)
        {
            DebugUtility.Log<SceneTransitionService>($"[OBS][Fade] {phase} id={transitionId} signature='{signature}' profile='{profile}'.");
        }

        private static void LogLoadedScenesSnapshot(string stage)
        {
            DebugUtility.Log<SceneTransitionService>($"[OBS][SceneFlow] LoadedScenesSnapshot stage='{stage}' sceneCount={SceneManager.sceneCount} scenes=[{DescribeLoadedScenes()}].", DebugUtility.Colors.Info);
        }

        private static string DescribeLoadedScenes()
        {
            if (SceneManager.sceneCount <= 0) return "<none>";
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
            if (scenes == null || scenes.Count == 0) return "<none>";
            return string.Join(", ", scenes);
        }

        private async Task RunSceneOperationsAsync(SceneTransitionContext context)
        {
            LogLoadedScenesSnapshot("before_apply_route");
            IReadOnlyList<string> reloadScenes = GetReloadScenes(context.ScenesToLoad, context.ScenesToUnload);
            IReadOnlyList<string> loadScenes = FilterScenesExcluding(context.ScenesToLoad, reloadScenes);
            IReadOnlyList<string> unloadScenes = FilterScenesExcluding(context.ScenesToUnload, reloadScenes);
            LogRouteExecutionPlan(context, loadScenes, unloadScenes, reloadScenes);
            await LoadScenesAsync(loadScenes);
            if (reloadScenes.Count > 0) await HandleReloadScenesAsync(context, reloadScenes);
            await SetActiveSceneAsync(context.TargetActiveScene);
            await UnloadScenesAsync(unloadScenes, context.TargetActiveScene);
            LogLoadedScenesSnapshot("after_apply_route");
        }

        private async Task LoadScenesAsync(IReadOnlyList<string>? scenesToLoad, bool forceLoad = false)
        {
            if (scenesToLoad == null || scenesToLoad.Count == 0) return;
            foreach (string sceneName in scenesToLoad)
            {
                if (!forceLoad && _loaderAdapter.IsSceneLoaded(sceneName))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Cena '{sceneName}' ja esta carregada. Pulando load.");
                    continue;
                }
                DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Carregando cena '{sceneName}' (Additive)...");
                await _loaderAdapter.LoadSceneAsync(sceneName);
            }
        }

        private async Task SetActiveSceneAsync(string targetActiveScene)
        {
            if (string.IsNullOrWhiteSpace(targetActiveScene))
            {
                DebugUtility.LogVerbose<SceneTransitionService>("[SceneFlow] TargetActiveScene vazio. Mantendo cena ativa atual.");
                return;
            }

            bool success = await _loaderAdapter.TrySetActiveSceneAsync(targetActiveScene);
            if (!success)
            {
                DebugUtility.LogWarning<SceneTransitionService>($"[SceneFlow] Nao foi possivel definir a cena ativa para '{targetActiveScene}'. Cena atual='{_loaderAdapter.GetActiveSceneName()}'.");
            }
            else
            {
                DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Cena ativa definida para '{targetActiveScene}'.");
            }
        }

        private async Task UnloadScenesAsync(IReadOnlyList<string>? scenesToUnload, string targetActiveScene)
        {
            if (scenesToUnload == null || scenesToUnload.Count == 0) return;
            foreach (string sceneName in scenesToUnload)
            {
                Scene scene = SceneManager.GetSceneByName(sceneName);
                bool exists = scene.IsValid();
                bool isLoaded = exists && scene.isLoaded;
                bool isActiveScene = exists && scene == SceneManager.GetActiveScene();
                DebugUtility.Log<SceneTransitionService>($"[OBS][SceneFlow] UnloadCandidate scene='{sceneName}' exists={exists} isLoaded={isLoaded} isActiveScene={isActiveScene} targetActiveScene='{targetActiveScene}'.", DebugUtility.Colors.Info);
                if (string.Equals(sceneName, targetActiveScene, StringComparison.Ordinal))
                {
                    DebugUtility.Log<SceneTransitionService>($"[OBS][SceneFlow] SkipUnload scene='{sceneName}' reason='target_active_scene' exists={exists} isLoaded={isLoaded} isActiveScene={isActiveScene}.", DebugUtility.Colors.Info);
                    continue;
                }
                if (!_loaderAdapter.IsSceneLoaded(sceneName))
                {
                    DebugUtility.Log<SceneTransitionService>($"[OBS][SceneFlow] SkipUnload scene='{sceneName}' reason='already_unloaded' exists={exists} isLoaded={isLoaded} isActiveScene={isActiveScene}.", DebugUtility.Colors.Info);
                    continue;
                }
                DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Descarregando cena '{sceneName}'...");
                await _loaderAdapter.UnloadSceneAsync(sceneName);
            }
        }

        private async Task HandleReloadScenesAsync(SceneTransitionContext context, IReadOnlyList<string> reloadScenes)
        {
            string tempActive = ResolveTempActiveScene(reloadScenes);
            if (!string.IsNullOrWhiteSpace(tempActive) && !string.Equals(tempActive, _loaderAdapter.GetActiveSceneName(), StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Reload: definindo cena ativa temporaria '{tempActive}'.");
                await SetActiveSceneAsync(tempActive);
            }
            await UnloadScenesForReloadAsync(reloadScenes);
            await LoadScenesAsync(reloadScenes, forceLoad: true);
            DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Reload concluido. TargetActiveScene='{context.TargetActiveScene}'.");
        }

        private async Task UnloadScenesForReloadAsync(IReadOnlyList<string> reloadScenes)
        {
            foreach (string sceneName in reloadScenes)
            {
                if (!_loaderAdapter.IsSceneLoaded(sceneName))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Reload: cena '{sceneName}' ja esta descarregada. Pulando unload.");
                    continue;
                }
                DebugUtility.LogVerbose<SceneTransitionService>($"[SceneFlow] Reload: descarregando cena '{sceneName}'...");
                await _loaderAdapter.UnloadSceneAsync(sceneName);
            }
        }

        private static IReadOnlyList<string> GetReloadScenes(IReadOnlyList<string>? scenesToLoad, IReadOnlyList<string>? scenesToUnload)
        {
            if (scenesToLoad == null || scenesToUnload == null) return Array.Empty<string>();
            var unloadSet = new HashSet<string>(scenesToUnload, StringComparer.Ordinal);
            var reload = new List<string>();
            foreach (string sceneName in scenesToLoad)
            {
                if (string.IsNullOrWhiteSpace(sceneName)) continue;
                string trimmed = sceneName.Trim();
                if (trimmed.Length == 0) continue;
                if (unloadSet.Contains(trimmed)) reload.Add(trimmed);
            }
            return reload;
        }

        private static IReadOnlyList<string> FilterScenesExcluding(IReadOnlyList<string>? scenes, IReadOnlyList<string>? excludedScenes)
        {
            if (scenes == null || scenes.Count == 0) return Array.Empty<string>();
            if (excludedScenes == null || excludedScenes.Count == 0) return scenes;
            var excludeSet = new HashSet<string>(excludedScenes, StringComparer.Ordinal);
            var filtered = new List<string>();
            foreach (string sceneName in scenes)
            {
                if (string.IsNullOrWhiteSpace(sceneName)) continue;
                string trimmed = sceneName.Trim();
                if (trimmed.Length == 0) continue;
                if (!excludeSet.Contains(trimmed)) filtered.Add(trimmed);
            }
            return filtered;
        }

        private string ResolveTempActiveScene(IReadOnlyList<string> reloadScenes)
        {
            var reloadSet = new HashSet<string>(reloadScenes, StringComparer.Ordinal);
            const string UiGlobalSceneName = "UIGlobalScene";
            if (_loaderAdapter.IsSceneLoaded(UiGlobalSceneName) && !reloadSet.Contains(UiGlobalSceneName)) return UiGlobalSceneName;
            string currentActive = _loaderAdapter.GetActiveSceneName();
            if (!string.IsNullOrWhiteSpace(currentActive) && !reloadSet.Contains(currentActive)) return currentActive;
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                string name = scene.name;
                if (!string.IsNullOrWhiteSpace(name) && !reloadSet.Contains(name)) return name;
            }
            return string.Empty;
        }

        private sealed class NoFadeAdapter : ISceneFlowFadeAdapter
        {
            public bool IsAvailable => false;
            public void ConfigureFromProfile(_ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings.SceneTransitionProfile profile, string profileLabel) { }
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
}
