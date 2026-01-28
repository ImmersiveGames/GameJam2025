// Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/Integrations/SceneFlow/ContentSwapChangeServiceWithTransition.cs
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap.Integrations.SceneFlow
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class ContentSwapChangeServiceWithTransition : IContentSwapChangeService, IContentSwapChangeServiceCapabilities
    {
        private readonly IContentSwapContextService _contentSwapContext;
        private readonly IWorldResetRequestService _worldReset;
        private readonly ISceneTransitionService _sceneFlow;
        private readonly IContentSwapTransitionIntentRegistry _intentRegistry;

        private int _inProgress;

        public bool SupportsWithTransition => true;
        public string CapabilityReason => "ContentSwap/WithTransitionAvailable";

        public ContentSwapChangeServiceWithTransition(
            IContentSwapContextService contentSwapContext,
            IWorldResetRequestService worldReset,
            ISceneTransitionService sceneFlow,
            IContentSwapTransitionIntentRegistry intentRegistry)
        {
            _contentSwapContext = contentSwapContext ?? throw new ArgumentNullException(nameof(contentSwapContext));
            _worldReset = worldReset ?? throw new ArgumentNullException(nameof(worldReset));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _intentRegistry = intentRegistry ?? throw new ArgumentNullException(nameof(intentRegistry));
        }

        public Task RequestContentSwapInPlaceAsync(ContentSwapPlan plan, string reason)
        {
            return RequestContentSwapInPlaceAsync(plan, reason, null);
        }

        public Task RequestContentSwapInPlaceAsync(string contentId, string reason, ContentSwapOptions? options = null)
        {
            return RequestContentSwapInPlaceAsync(BuildPlan(contentId), reason, options);
        }

        public async Task RequestContentSwapInPlaceAsync(ContentSwapPlan plan, string reason, ContentSwapOptions? options)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                    "[ContentSwap] Ignorando RequestContentSwapInPlaceAsync com ContentSwapPlan inválido.");
                return;
            }

            DebugUtility.Log<ContentSwapChangeServiceWithTransition>(
                $"[OBS][ContentSwap] ContentSwapRequested event=content_swap_inplace mode={ContentSwapMode.InPlace} contentId='{plan.ContentId}' reason='{Sanitize(reason)}'",
                DebugUtility.Colors.Info);

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                    "[ContentSwap] Já existe uma troca de conteúdo em progresso. Ignorando (InPlace).");
                return;
            }

            var normalizedOptions = NormalizeOptions(options);

            if (normalizedOptions.UseLoadingHud)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>("[ContentSwap] In-Place ignora LoadingHUD. Use WithTransition/SceneFlow para loading completo.");
                normalizedOptions.UseLoadingHud = false;
            }

            var signature = $"contentswap.inplace:{plan.ContentId}";
            IDisposable gateHandle = null;
            var hudShown = false;
            var fadeOutCompleted = false;

            try
            {
                gateHandle = AcquireGateHandle();

                if (normalizedOptions.UseFade)
                {
                    await TryFadeInAsync(normalizedOptions, signature);
                }

                if (normalizedOptions.UseLoadingHud)
                {
                    await TryShowHudAsync(normalizedOptions, signature, plan.ContentId);
                    hudShown = true;
                }

                _contentSwapContext.SetPending(plan, reason);

                var resetReason = $"ContentSwap/InPlace plan='{plan}' reason='{reason ?? "n/a"}'";
                DebugUtility.Log<ContentSwapChangeServiceWithTransition>(
                    $"[ContentSwap] InPlace -> pending set. Disparando WorldReset. {resetReason}",
                    DebugUtility.Colors.Info);

                await AwaitWithTimeoutAsync(
                    _worldReset.RequestResetAsync(signature),
                    normalizedOptions.TimeoutMs,
                    "RequestResetAsync");

                if (!_contentSwapContext.TryCommitPending(reason ?? "ContentSwap/InPlace", out _))
                {
                    DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                        $"[ContentSwap] Reset concluído, mas TryCommitPending falhou. signature='{signature}', plan='{plan}', reason='{Sanitize(reason)}'.");
                }

                if (normalizedOptions.UseFade)
                {
                    await TryFadeOutAsync(normalizedOptions, signature);
                    fadeOutCompleted = true;
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<ContentSwapChangeServiceWithTransition>(
                    $"[ContentSwap] Falha no InPlace. Limpando pending por segurança. ex={ex}");

                _contentSwapContext.ClearPending($"ContentSwap/InPlace failed: {ex.GetType().Name}");
            }
            finally
            {
                if (hudShown)
                {
                    TryHideHud(signature, plan.ContentId);
                }

                gateHandle?.Dispose();
                Interlocked.Exchange(ref _inProgress, 0);

                if (normalizedOptions.UseFade && !fadeOutCompleted)
                {
                    try
                    {
                        await TryFadeOutAsync(normalizedOptions, signature);
                    }
                    catch (Exception ex)
                    {
                        DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                            $"[ContentSwap] Falha ao executar FadeOut final (InPlace). ex={ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        public Task RequestContentSwapWithTransitionAsync(ContentSwapPlan plan, SceneTransitionRequest transition, string reason)
        {
            return RequestContentSwapWithTransitionAsync(plan, transition, reason, null);
        }

        public Task RequestContentSwapWithTransitionAsync(string contentId, SceneTransitionRequest transition, string reason, ContentSwapOptions? options = null)
        {
            return RequestContentSwapWithTransitionAsync(BuildPlan(contentId), transition, reason, options);
        }

        public async Task RequestContentSwapWithTransitionAsync(ContentSwapPlan plan, SceneTransitionRequest transition, string reason, ContentSwapOptions? options)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                    "[ContentSwap] Ignorando RequestContentSwapWithTransitionAsync com ContentSwapPlan inválido.");
                return;
            }

            if (transition == null)
            {
                DebugUtility.LogError<ContentSwapChangeServiceWithTransition>(
                    "[ContentSwap] Transition request nulo. Abortando.");
                return;
            }

            if (transition.ScenesToLoad == null || transition.ScenesToUnload == null)
            {
                DebugUtility.LogError<ContentSwapChangeServiceWithTransition>(
                    "[ContentSwap] Transition request inválido (ScenesToLoad/ScenesToUnload nulos). Abortando.");
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                    "[ContentSwap] Já existe uma troca de conteúdo em progresso. Ignorando (WithTransition).");
                return;
            }

            var normalizedOptions = NormalizeOptions(options);

            IDisposable gateHandle = null;

            try
            {
                gateHandle = AcquireContentSwapTransitionGateHandle();

                var normalizedRequest = EnsureContextSignature(transition);
                var context = SceneTransitionSignatureUtil.BuildContext(normalizedRequest);
                var signature = SceneTransitionSignatureUtil.Compute(context);

                var intent = new ContentSwapTransitionIntent(
                    plan,
                    ContentSwapMode.SceneTransition,
                    reason,
                    signature,
                    normalizedRequest.TransitionProfileName,
                    normalizedRequest.TargetActiveScene,
                    DateTime.UtcNow);

                if (!_intentRegistry.RegisterIntent(intent))
                {
                    DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                        $"[ContentSwap] Falha ao registrar ContentSwap intent. Ignorando RequestContentSwapWithTransitionAsync. signature='{signature}', plan='{plan}'.");
                    return;
                }

                DebugUtility.Log<ContentSwapChangeServiceWithTransition>(
                    $"[OBS][ContentSwap] ContentSwapRequested event=content_swap_transition mode={ContentSwapMode.SceneTransition} contentId='{plan.ContentId}' reason='{Sanitize(reason)}' signature='{signature}' profile='{normalizedRequest.TransitionProfileName}'",
                    DebugUtility.Colors.Info);

                DebugUtility.Log<ContentSwapChangeServiceWithTransition>(
                    $"[ContentSwap] WithTransition -> intent registrado. Iniciando SceneFlow. " +
                    $"plan='{plan}', reason='{reason ?? "n/a"}', profile='{normalizedRequest.TransitionProfileName}', active='{normalizedRequest.TargetActiveScene}'.",
                    DebugUtility.Colors.Info);

                DebugUtility.LogVerbose<ContentSwapChangeServiceWithTransition>(
                    $"[ContentSwap] WithTransition signature='{signature}'.");

                var transitionOk = await AwaitWithTimeoutAsync(
                    _sceneFlow.TransitionAsync(normalizedRequest),
                    normalizedOptions.TimeoutMs,
                    "TransitionAsync");

                if (!transitionOk)
                {
                    _intentRegistry.ClearIntent($"ContentSwap/WithTransition timeout sig='{signature}'");
                }

                // Commit/Apply ocorrerá no WorldLifecycleRuntimeCoordinator (ScenesReady).
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<ContentSwapChangeServiceWithTransition>(
                    $"[ContentSwap] Falha no WithTransition. Limpando intent por segurança. ex={ex}");

                _intentRegistry.ClearIntent($"ContentSwap/WithTransition failed: {ex.GetType().Name}");
            }
            finally
            {
                gateHandle?.Dispose();
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        private static ContentSwapPlan BuildPlan(string contentId)
        {
            return new ContentSwapPlan(contentId, string.Empty);
        }

        private static ContentSwapOptions NormalizeOptions(ContentSwapOptions? options)
        {
            var normalized = options?.Clone() ?? ContentSwapOptions.Default.Clone();

            if (normalized.TimeoutMs <= 0)
            {
                normalized.TimeoutMs = ContentSwapOptions.DefaultTimeoutMs;
            }

            return normalized;
        }

        private static IDisposable AcquireGateHandle()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gate) || gate == null)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                    "[ContentSwap] ISimulationGateService indisponível. Gate não será adquirido para InPlace.");
                return null;
            }

            return gate.Acquire(SimulationGateTokens.ContentSwapInPlace);
        }

        private static IDisposable AcquireContentSwapTransitionGateHandle()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gate) || gate == null)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                    "[ContentSwap] ISimulationGateService indisponível. Gate não será adquirido para WithTransition.");
                return null;
            }

            return gate.Acquire(SimulationGateTokens.ContentSwapTransition);
        }

        private static async Task<bool> AwaitWithTimeoutAsync(Task task, int timeoutMs, string label)
        {
            if (task == null)
            {
                return true;
            }

            if (timeoutMs <= 0)
            {
                await task;
                return true;
            }

            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            if (completed != task)
            {
                DebugUtility.LogError<ContentSwapChangeServiceWithTransition>(
                    $"[ContentSwap] Timeout aguardando '{label}'. timeoutMs={timeoutMs}.");

                _ = task.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                            $"[ContentSwap] '{label}' terminou com erro após timeout. ex={t.Exception.GetBaseException()}");
                    }
                });

                return false;
            }

            await task;
            return true;
        }

        private static async Task TryFadeInAsync(ContentSwapOptions options, string signature)
        {
            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsFadeService>(out var fade) || fade == null)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                    $"[ContentSwap] Fade solicitado, mas INewScriptsFadeService indisponível. signature='{signature}'.");
                return;
            }

            await AwaitWithTimeoutAsync(fade.FadeInAsync(), options.TimeoutMs, "FadeInAsync");
        }

        private static async Task TryFadeOutAsync(ContentSwapOptions options, string signature)
        {
            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsFadeService>(out var fade) || fade == null)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                    $"[ContentSwap] FadeOut solicitado, mas INewScriptsFadeService indisponível. signature='{signature}'.");
                return;
            }

            await AwaitWithTimeoutAsync(fade.FadeOutAsync(), options.TimeoutMs, "FadeOutAsync");
        }

        private static async Task TryShowHudAsync(ContentSwapOptions options, string signature, string contentId)
        {
            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsLoadingHudService>(out var hud) || hud == null)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceWithTransition>(
                    $"[ContentSwap] LoadingHUD solicitado, mas serviço indisponível. signature='{signature}'.");
                return;
            }

            await AwaitWithTimeoutAsync(hud.EnsureLoadedAsync(), options.TimeoutMs, "LoadingHud.EnsureLoadedAsync");
            hud.Show(signature, contentId);
        }

        private static void TryHideHud(string signature, string contentId)
        {
            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsLoadingHudService>(out var hud) || hud == null)
            {
                return;
            }

            hud.Hide(signature, contentId);
        }

        private static string Sanitize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();

        private static SceneTransitionRequest EnsureContextSignature(SceneTransitionRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.ContextSignature))
            {
                return request;
            }

            var context = SceneTransitionSignatureUtil.BuildContext(request);
            var signature = SceneTransitionSignatureUtil.Compute(context);

            // IMPORTANT: preservar RequestedBy ao clonar.
            return new SceneTransitionRequest(
                request.ScenesToLoad,
                request.ScenesToUnload,
                request.TargetActiveScene,
                request.UseFade,
                request.TransitionProfileId,
                signature,
                request.RequestedBy);
        }
    }
}
