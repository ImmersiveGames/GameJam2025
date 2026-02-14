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

    /// <summary>
    /// Implementação nativa do pipeline de Scene Flow emitindo eventos do NewScripts.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneTransitionService : ISceneTransitionService
    {
        // Dedupe: bloqueia reentrância por assinatura idêntica numa janela curta.
        // Ajuste conforme necessário. 750ms tem sido suficiente para capturar "double start" acidental.
        private const int DuplicateSignatureWindowMs = 750;

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

        private string _lastStartedSignature = string.Empty;
        private int _lastStartedTick;

        private string _lastCompletedSignature = string.Empty;
        private int _lastCompletedTick;

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
            LogResolvedRouteForObservability(hydratedRequest);
            var context = BuildContextWithResetDecision(hydratedRequest, routeDefinition);
            string signature = SceneTransitionSignature.Compute(context);

            if (!_navigationPolicy.CanTransition(hydratedRequest, out var denialReason))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Transição bloqueada por policy. " +
                    $"signature='{signature}', routeId='{context.RouteId}', styleId='{context.StyleId}', " +
                    $"reason='{Sanitize(hydratedRequest.Reason)}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}', " +
                    $"policyReason='{Sanitize(denialReason)}'.");
                return;
            }

            if (routeDefinition.HasValue && !_routeGuard.CanTransitionRoute(hydratedRequest, routeDefinition.Value, out denialReason))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Transição bloqueada por route guard. " +
                    $"signature='{signature}', routeId='{context.RouteId}', kind='{routeDefinition.Value.RouteKind}', " +
                    $"reason='{Sanitize(hydratedRequest.Reason)}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}', " +
                    $"guardReason='{Sanitize(denialReason)}'.");
                return;
            }

            // Dedupe por assinatura: evita "double start" no mesmo contexto em janela curta.
            // Isto não substitui correção do caller, mas impede o pior: reentrância/interleaving.
            if (ShouldDedupe(signature))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Dedupe: TransitionAsync ignorado (signature repetida em janela curta). " +
                    $"signature='{signature}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}'.");
                return;
            }

            // Pre-checagem concorrente (rápida).
            if (Interlocked.CompareExchange(ref _transitionInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Uma transição já está em andamento (_transitionInProgress ativo). Ignorando solicitação concorrente. " +
                    $"signature='{signature}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}'.");
                return;
            }

            // Garante exclusão mútua (não bloqueante).
            if (!_transitionGate.Wait(0))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Uma transição já está em andamento. Ignorando solicitação concorrente. " +
                    $"signature='{signature}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}'.");
                Interlocked.Exchange(ref _transitionInProgress, 0);
                return;
            }

            long transitionId = Interlocked.Increment(ref _transitionIdSeq);

            try
            {
                MarkStarted(signature);

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] TransitionStarted id={transitionId} signature='{signature}' " +
                    $"routeId='{context.RouteId}', styleId='{context.StyleId}', profile='{context.TransitionProfileName}', " +
                    $"reason='{Sanitize(hydratedRequest.Reason)}', requestedBy='{Sanitize(hydratedRequest.RequestedBy)}' {context}",
                    DebugUtility.Colors.Info);

                EventBus<SceneTransitionStartedEvent>.Raise(new SceneTransitionStartedEvent(context));

                // FadeIn é a primeira etapa visual. O HUD de Loading pode ser "ensured" em paralelo,
                // mas deve aparecer apenas após o FadeIn estar concluído (Opção A+).
                await RunFadeInIfNeeded(context, transitionId, signature);

                if (context.UseFade)
                {
                    EventBus<SceneTransitionFadeInCompletedEvent>.Raise(new SceneTransitionFadeInCompletedEvent(context));
                }

                await RunSceneOperationsAsync(context);

                // 1) Momento em que "cenas estão prontas" (load/unload/active done).
                EventBus<SceneTransitionScenesReadyEvent>.Raise(new SceneTransitionScenesReadyEvent(context));

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] ScenesReady id={transitionId} signature='{signature}' " +
                    $"routeId='{context.RouteId}', styleId='{context.StyleId}', profile='{context.TransitionProfileName}'.",
                    DebugUtility.Colors.Info);

                // 2) Aguarda gates externos (ex: WorldLifecycle reset) ANTES de revelar (FadeOut).
                await AwaitCompletionGateAsync(context);

                EventBus<SceneTransitionBeforeFadeOutEvent>.Raise(new SceneTransitionBeforeFadeOutEvent(context));

                await RunFadeOutIfNeeded(context, transitionId, signature);

                EventBus<SceneTransitionCompletedEvent>.Raise(new SceneTransitionCompletedEvent(context));

                MarkCompleted(signature);

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] TransitionCompleted id={transitionId} signature='{signature}' " +
                    $"routeId='{context.RouteId}', styleId='{context.StyleId}', profile='{context.TransitionProfileName}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<SceneTransitionService>(
                    $"[SceneFlow] Erro durante transição: {ex}");
                throw;
            }
            finally
            {
                Interlocked.Exchange(ref _transitionInProgress, 0);
                _transitionGate.Release();
            }
        }





        private static void EnsureTransitionProfileOrFailFast(SceneTransitionRequest request)
        {
            if (request.TransitionProfile != null)
            {
                return;
            }

            string message =
                $"[FATAL][Config] SceneTransitionProfile ausente na request. routeId='{request.RouteId}', styleId='{request.StyleId}', requestedBy='{Sanitize(request.RequestedBy)}', reason='{Sanitize(request.Reason)}'.";

            DebugUtility.LogError<SceneTransitionService>(message);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }

        private static void FailFastTransitionRequest(SceneTransitionRequest request, string detail)
        {
            string message =
                $"[FATAL][Config] {detail} requestedBy='{Sanitize(request.RequestedBy)}', reason='{Sanitize(request.Reason)}'.";

            DebugUtility.LogError<SceneTransitionService>(message);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }

        private static void LogResolvedRouteForObservability(SceneTransitionRequest request)
        {
            DebugUtility.Log<SceneTransitionService>(
                $"[OBS][SceneFlow] RouteApplied routeId='{request.RouteId}', scenesToLoadCount={request.ScenesToLoad.Count}, " +
                $"activeScene='{request.TargetActiveScene}', transitionProfile='{request.TransitionProfileName}'.",
                DebugUtility.Colors.Info);
        }

        private SceneTransitionContext BuildContextWithResetDecision(SceneTransitionRequest request, SceneRouteDefinition? routeDefinition)
        {
            SceneTransitionContext context = SceneTransitionSignature.BuildContext(request);

            if (!TryResolveRouteResetPolicy(out var policy) || policy == null)
            {
                return context.WithRouteResetDecision(
                    requiresWorldReset: false,
                    decisionSource: "policy:missing",
                    decisionReason: "RouteResetPolicyMissing");
            }

            RouteResetDecision resetDecision = policy.Resolve(context.RouteId, routeDefinition, context);
            return context.WithRouteResetDecision(
                requiresWorldReset: resetDecision.ShouldReset,
                decisionSource: resetDecision.DecisionSource,
                decisionReason: resetDecision.Reason);
        }

        private bool TryResolveRouteResetPolicy(out IRouteResetPolicy? policy)
        {
            if (_routeResetPolicy != null)
            {
                policy = _routeResetPolicy;
                return true;
            }

            if (DependencyManager.HasInstance &&
                DependencyManager.Provider.TryGetGlobal<IRouteResetPolicy>(out var resolved) &&
                resolved != null)
            {
                policy = resolved;
                return true;
            }

            policy = null;
            return false;
        }

        private SceneTransitionRequest BuildRequestFromRouteDefinition(
            SceneTransitionRequest request,
            out SceneRouteDefinition? routeDefinition)
        {
            routeDefinition = null;

            if (!request.RouteId.IsValid)
            {
                if (request.HasInlineSceneData)
                {
                    routeDefinition = null;
                    return request;
                }

                FailFastTransitionRequest(request, "RouteId ausente/inválido.");
                return request;
            }

            if (_routeResolver == null)
            {
                FailFastTransitionRequest(request, $"ISceneRouteResolver indisponível para routeId='{request.RouteId}'.");
                return request;
            }

            if (!_routeResolver.TryResolve(request.RouteId, out var resolvedRoute))
            {
                FailFastTransitionRequest(request, $"routeId='{request.RouteId}' não encontrado no catálogo de rotas.");
                return request;
            }

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
                request.StyleId,
                request.Payload,
                request.TransitionProfile,
                request.UseFade,
                request.TransitionProfileId,
                request.ContextSignature,
                request.RequestedBy,
                request.Reason);
        }

        private bool ShouldDedupe(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            int now = Environment.TickCount;

            // Caso 1: repetição imediatamente após Start anterior (start-start).
            if (string.Equals(signature, _lastStartedSignature, StringComparison.Ordinal))
            {
                int dt = unchecked(now - _lastStartedTick);
                if (dt >= 0 && dt <= DuplicateSignatureWindowMs)
                {
                    return true;
                }
            }

            // Caso 2: repetição logo após Completed (completed-start).
            if (string.Equals(signature, _lastCompletedSignature, StringComparison.Ordinal))
            {
                int dt = unchecked(now - _lastCompletedTick);
                if (dt >= 0 && dt <= DuplicateSignatureWindowMs)
                {
                    return true;
                }
            }

            return false;
        }

        private void MarkStarted(string? signature)
        {
            _lastStartedSignature = signature ?? string.Empty;
            _lastStartedTick = Environment.TickCount;
        }

        private void MarkCompleted(string? signature)
        {
            _lastCompletedSignature = signature ?? string.Empty;
            _lastCompletedTick = Environment.TickCount;
        }

        private async Task AwaitCompletionGateAsync(SceneTransitionContext context)
        {
            try
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Aguardando completion gate antes do FadeOut. signature='{SceneTransitionSignature.Compute(context)}'.");

                await _completionGate.AwaitBeforeFadeOutAsync(context);

                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='{SceneTransitionSignature.Compute(context)}'.");
            }
            catch (Exception ex)
            {
                // Gate é "best effort": não deve travar a transição.
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Completion gate falhou/abortou. Prosseguindo com FadeOut. ex={ex.GetType().Name}: {ex.Message}");
            }
        }

        private async Task RunFadeInIfNeeded(SceneTransitionContext context, long transitionId, string signature)
        {
            if (!context.UseFade)
            {
                return;
            }

            _fadeAdapter.ConfigureFromProfile(context.TransitionProfile, context.TransitionProfileName);

            var fadeInTask = _fadeAdapter.FadeInAsync(signature);
            LogObsFade("FadeInStarted", transitionId, signature, context.TransitionProfileName);
            await fadeInTask;

            LogObsFade("FadeInCompleted", transitionId, signature, context.TransitionProfileName);
        }

        private async Task RunFadeOutIfNeeded(SceneTransitionContext context, long transitionId, string signature)
        {
            if (!context.UseFade)
            {
                return;
            }

            LogObsFade("FadeOutStarted", transitionId, signature, context.TransitionProfileName);

            await _fadeAdapter.FadeOutAsync(signature);

            LogObsFade("FadeOutCompleted", transitionId, signature, context.TransitionProfileName);
        }

        private static void LogObsFade(string phase, long transitionId, string signature, string profile)
        {
            // Comentário: âncora canônica (auditável) para ADR-0009 / Item A (Strict/Release).
            DebugUtility.Log<SceneTransitionService>(
                $"[OBS][Fade] {phase} id={transitionId} signature='{signature}' profile='{profile}'.");
        }

        private async Task RunSceneOperationsAsync(SceneTransitionContext context)
        {
            IReadOnlyList<string> reloadScenes = GetReloadScenes(context.ScenesToLoad, context.ScenesToUnload);
            IReadOnlyList<string> loadScenes = FilterScenesExcluding(context.ScenesToLoad, reloadScenes);
            await LoadScenesAsync(loadScenes);

            if (reloadScenes.Count > 0)
            {
                await HandleReloadScenesAsync(context, reloadScenes);
            }

            await SetActiveSceneAsync(context.TargetActiveScene);

            IReadOnlyList<string> unloadScenes = FilterScenesExcluding(context.ScenesToUnload, reloadScenes);
            await UnloadScenesAsync(unloadScenes, context.TargetActiveScene);
        }

        private async Task LoadScenesAsync(IReadOnlyList<string>? scenesToLoad, bool forceLoad = false)
        {
            if (scenesToLoad == null || scenesToLoad.Count == 0)
            {
                return;
            }

            foreach (string sceneName in scenesToLoad)
            {
                if (!forceLoad && _loaderAdapter.IsSceneLoaded(sceneName))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[SceneFlow] Cena '{sceneName}' já está carregada. Pulando load.");
                    continue;
                }

                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Carregando cena '{sceneName}' (Additive)...");

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
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Não foi possível definir a cena ativa para '{targetActiveScene}'. Cena atual='{_loaderAdapter.GetActiveSceneName()}'.");
            }
            else
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Cena ativa definida para '{targetActiveScene}'.");
            }
        }

        private async Task UnloadScenesAsync(IReadOnlyList<string>? scenesToUnload, string targetActiveScene)
        {
            if (scenesToUnload == null || scenesToUnload.Count == 0)
            {
                return;
            }

            foreach (string sceneName in scenesToUnload)
            {
                if (string.Equals(sceneName, targetActiveScene, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[SceneFlow] Ignorando unload da cena alvo ativa '{sceneName}'.");
                    continue;
                }

                if (!_loaderAdapter.IsSceneLoaded(sceneName))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[SceneFlow] Cena '{sceneName}' já está descarregada. Pulando unload.");
                    continue;
                }

                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Descarregando cena '{sceneName}'...");

                await _loaderAdapter.UnloadSceneAsync(sceneName);
            }
        }

        private async Task HandleReloadScenesAsync(SceneTransitionContext context, IReadOnlyList<string> reloadScenes)
        {
            string tempActive = ResolveTempActiveScene(reloadScenes);
            if (!string.IsNullOrWhiteSpace(tempActive) &&
                !string.Equals(tempActive, _loaderAdapter.GetActiveSceneName(), StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Reload: definindo cena ativa temporária '{tempActive}'.");
                await SetActiveSceneAsync(tempActive);
            }

            await UnloadScenesForReloadAsync(reloadScenes);
            await LoadScenesAsync(reloadScenes, forceLoad: true);

            DebugUtility.LogVerbose<SceneTransitionService>(
                $"[SceneFlow] Reload concluído. TargetActiveScene='{context.TargetActiveScene}'.");
        }

        private async Task UnloadScenesForReloadAsync(IReadOnlyList<string> reloadScenes)
        {
            foreach (string sceneName in reloadScenes)
            {
                if (!_loaderAdapter.IsSceneLoaded(sceneName))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[SceneFlow] Reload: cena '{sceneName}' já está descarregada. Pulando unload.");
                    continue;
                }

                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Reload: descarregando cena '{sceneName}'...");

                await _loaderAdapter.UnloadSceneAsync(sceneName);
            }
        }

        private static IReadOnlyList<string> GetReloadScenes(
            IReadOnlyList<string>? scenesToLoad,
            IReadOnlyList<string>? scenesToUnload)
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

        private static IReadOnlyList<string> FilterScenesExcluding(
            IReadOnlyList<string>? scenes,
            IReadOnlyList<string>? excludedScenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return Array.Empty<string>();
            }

            if (excludedScenes == null || excludedScenes.Count == 0)
            {
                return scenes;
            }

            var excludeSet = new HashSet<string>(excludedScenes, StringComparer.Ordinal);
            var filtered = new List<string>();

            foreach (string sceneName in scenes)
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

                if (!excludeSet.Contains(trimmed))
                {
                    filtered.Add(trimmed);
                }
            }

            return filtered;
        }

        private string ResolveTempActiveScene(IReadOnlyList<string> reloadScenes)
        {
            var reloadSet = new HashSet<string>(reloadScenes, StringComparer.Ordinal);
            const string UiGlobalSceneName = "UIGlobalScene";

            if (_loaderAdapter.IsSceneLoaded(UiGlobalSceneName) && !reloadSet.Contains(UiGlobalSceneName))
            {
                return UiGlobalSceneName;
            }

            string currentActive = _loaderAdapter.GetActiveSceneName();
            if (!string.IsNullOrWhiteSpace(currentActive) && !reloadSet.Contains(currentActive))
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
                if (!string.IsNullOrWhiteSpace(name) && !reloadSet.Contains(name))
                {
                    return name;
                }
            }

            return string.Empty;
        }

        private static string Sanitize(string s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }

}
