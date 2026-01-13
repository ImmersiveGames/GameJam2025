// Assets/_ImmersiveGames/NewScripts/Gameplay/Phases/PhaseChangeService.cs
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseChangeService : IPhaseChangeService
    {
        private readonly IPhaseContextService _phaseContext;
        private readonly IWorldResetRequestService _worldReset;
        private readonly ISceneTransitionService _sceneFlow;
        private readonly IPhaseTransitionIntentRegistry _intentRegistry;

        private int _inProgress;

        public PhaseChangeService(
            IPhaseContextService phaseContext,
            IWorldResetRequestService worldReset,
            ISceneTransitionService sceneFlow,
            IPhaseTransitionIntentRegistry intentRegistry)
        {
            _phaseContext = phaseContext ?? throw new ArgumentNullException(nameof(phaseContext));
            _worldReset = worldReset ?? throw new ArgumentNullException(nameof(worldReset));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _intentRegistry = intentRegistry ?? throw new ArgumentNullException(nameof(intentRegistry));
        }

        public Task RequestPhaseInPlaceAsync(PhasePlan plan, string reason)
        {
            return RequestPhaseInPlaceAsync(plan, reason, options: null);
        }

        public Task RequestPhaseInPlaceAsync(string phaseId, string reason, PhaseChangeOptions? options = null)
        {
            return RequestPhaseInPlaceAsync(BuildPlan(phaseId), reason, options);
        }

        public async Task RequestPhaseInPlaceAsync(PhasePlan plan, string reason, PhaseChangeOptions? options)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] Ignorando RequestPhaseInPlaceAsync com PhasePlan inválido.");
                return;
            }

            DebugUtility.Log<PhaseChangeService>(
                $"[OBS][Phase] PhaseChangeRequested event=phase_change_inplace mode={PhaseChangeMode.InPlace} phaseId='{plan.PhaseId}' reason='{Sanitize(reason)}'",
                DebugUtility.Colors.Info);

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] Já existe uma troca de fase em progresso. Ignorando (InPlace).");
                return;
            }

            var normalizedOptions = NormalizeOptions(options);

            // ADR-0017: In-Place não deve ter interrupções visuais por padrão.
            // Se o caller solicitar, permitimos apenas mini-transição (Fade curto) sem HUD de loading.
            if (normalizedOptions.UseLoadingHud)
            {
                UnityEngine.Debug.LogWarning("[PhaseChangeService] In-Place ignora Loading HUD. Use SceneTransition para loading completo.");
                normalizedOptions.UseLoadingHud = false;
            }

            var signature = $"phase.inplace:{plan.PhaseId}";
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
                    await TryShowHudAsync(normalizedOptions, signature, plan.PhaseId);
                    hudShown = true;
                }

                _phaseContext.SetPending(plan, reason);

                var resetReason = $"PhaseChange/InPlace plan='{plan}' reason='{reason ?? "n/a"}'";
                DebugUtility.Log<PhaseChangeService>(
                    $"[PhaseChange] InPlace -> pending set. Disparando WorldReset. {resetReason}",
                    DebugUtility.Colors.Info);

                await AwaitWithTimeoutAsync(
                    _worldReset.RequestResetAsync(signature),
                    normalizedOptions.TimeoutMs,
                    "RequestResetAsync");

                if (normalizedOptions.UseFade)
                {
                    await TryFadeOutAsync(normalizedOptions, signature);
                    fadeOutCompleted = true;
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<PhaseChangeService>(
                    $"[PhaseChange] Falha no InPlace. Limpando pending por segurança. ex={ex}");

                _phaseContext.ClearPending($"PhaseChange/InPlace failed: {ex.GetType().Name}");
            }
            finally
            {
                if (hudShown)
                {
                    TryHideHud(signature, plan.PhaseId);
                }

                if (normalizedOptions.UseFade && !fadeOutCompleted)
                {
                    await TryFadeOutAsync(normalizedOptions, signature);
                }

                gateHandle?.Dispose();
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        public Task RequestPhaseWithTransitionAsync(PhasePlan plan, SceneTransitionRequest transition, string reason)
        {
            return RequestPhaseWithTransitionAsync(plan, transition, reason, options: null);
        }

        public Task RequestPhaseWithTransitionAsync(string phaseId, SceneTransitionRequest transition, string reason, PhaseChangeOptions? options = null)
        {
            return RequestPhaseWithTransitionAsync(BuildPlan(phaseId), transition, reason, options);
        }

        public async Task RequestPhaseWithTransitionAsync(PhasePlan plan, SceneTransitionRequest transition, string reason, PhaseChangeOptions? options)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] Ignorando RequestPhaseWithTransitionAsync com PhasePlan inválido.");
                return;
            }

            if (transition == null)
            {
                DebugUtility.LogError<PhaseChangeService>(
                    "[PhaseChange] Transition request nulo. Abortando.");
                return;
            }

            if (transition.ScenesToLoad == null || transition.ScenesToUnload == null)
            {
                DebugUtility.LogError<PhaseChangeService>(
                    "[PhaseChange] Transition request inválido (ScenesToLoad/ScenesToUnload nulos). Abortando.");
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] Já existe uma troca de fase em progresso. Ignorando (WithTransition).");
                return;
            }

            var normalizedOptions = NormalizeOptions(options);

            IDisposable gateHandle = null;

            try
            {
                gateHandle = AcquirePhaseTransitionGateHandle();

                var normalizedRequest = EnsureContextSignature(transition);
                var context = SceneTransitionSignatureUtil.BuildContext(normalizedRequest);
                var signature = SceneTransitionSignatureUtil.Compute(context);

                var intent = new PhaseTransitionIntent(
                    plan: plan,
                    mode: PhaseChangeMode.SceneTransition,
                    reason: reason,
                    sourceSignature: signature,
                    transitionProfile: normalizedRequest.TransitionProfileName,
                    targetScene: normalizedRequest.TargetActiveScene,
                    timestampUtc: DateTime.UtcNow);

                if (!_intentRegistry.RegisterIntent(intent))
                {
                    DebugUtility.LogWarning<PhaseChangeService>(
                        $"[PhaseChange] Falha ao registrar PhaseIntent. Ignorando RequestPhaseWithTransitionAsync. signature='{signature}', plan='{plan}'.");
                    return;
                }

                DebugUtility.Log<PhaseChangeService>(
                    $"[OBS][Phase] PhaseChangeRequested event=phase_change_transition mode={PhaseChangeMode.SceneTransition} phaseId='{plan.PhaseId}' reason='{Sanitize(reason)}' signature='{signature}' profile='{normalizedRequest.TransitionProfileName}'",
                    DebugUtility.Colors.Info);

                DebugUtility.Log<PhaseChangeService>(
                    $"[PhaseChange] WithTransition -> intent registrado. Iniciando SceneFlow. " +
                    $"plan='{plan}', reason='{reason ?? "n/a"}', profile='{normalizedRequest.TransitionProfileName}', active='{normalizedRequest.TargetActiveScene}'.",
                    DebugUtility.Colors.Info);

                DebugUtility.LogVerbose<PhaseChangeService>(
                    $"[PhaseChange] WithTransition signature='{signature}'.");

                var transitionOk = await AwaitWithTimeoutAsync(
                    _sceneFlow.TransitionAsync(normalizedRequest),
                    normalizedOptions.TimeoutMs,
                    "TransitionAsync");

                if (!transitionOk)
                {
                    _intentRegistry.ClearIntent($"PhaseChange/WithTransition timeout sig='{signature}'");
                }

                // Commit/Apply ocorrerá no WorldLifecycleRuntimeCoordinator (ScenesReady).
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<PhaseChangeService>(
                    $"[PhaseChange] Falha no WithTransition. Limpando intent por segurança. ex={ex}");

                _intentRegistry.ClearIntent($"PhaseChange/WithTransition failed: {ex.GetType().Name}");
            }
            finally
            {
                gateHandle?.Dispose();
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        private static PhasePlan BuildPlan(string phaseId)
        {
            return new PhasePlan(phaseId, string.Empty);
        }

        private static PhaseChangeOptions NormalizeOptions(PhaseChangeOptions? options)
        {
            var normalized = options?.Clone() ?? PhaseChangeOptions.Default;
            if (normalized.TimeoutMs <= 0)
            {
                normalized.TimeoutMs = PhaseChangeOptions.DefaultTimeoutMs;
            }

            return normalized;
        }

        private static IDisposable AcquireGateHandle()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gate) || gate == null)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] ISimulationGateService indisponível. Gate não será adquirido para InPlace.");
                return null;
            }

            return gate.Acquire(SimulationGateTokens.PhaseInPlace);
        }

        private static IDisposable AcquirePhaseTransitionGateHandle()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gate) || gate == null)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] ISimulationGateService indisponível. Gate não será adquirido para WithTransition.");
                return null;
            }

            return gate.Acquire(SimulationGateTokens.PhaseTransition);
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
                DebugUtility.LogError<PhaseChangeService>(
                    $"[PhaseChange] Timeout aguardando '{label}'. timeoutMs={timeoutMs}.");

                _ = task.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        DebugUtility.LogWarning<PhaseChangeService>(
                            $"[PhaseChange] '{label}' terminou com erro após timeout. ex={t.Exception.GetBaseException()}");
                    }
                });

                return false;
            }

            await task;
            return true;
        }

        private static async Task TryFadeInAsync(PhaseChangeOptions options, string signature)
        {
            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsFadeService>(out var fade) || fade == null)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    $"[PhaseChange] Fade solicitado, mas INewScriptsFadeService indisponível. signature='{signature}'.");
                return;
            }

            await AwaitWithTimeoutAsync(fade.FadeInAsync(), options.TimeoutMs, "FadeInAsync");
        }

        private static async Task TryFadeOutAsync(PhaseChangeOptions options, string signature)
        {
            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsFadeService>(out var fade) || fade == null)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    $"[PhaseChange] FadeOut solicitado, mas INewScriptsFadeService indisponível. signature='{signature}'.");
                return;
            }

            await AwaitWithTimeoutAsync(fade.FadeOutAsync(), options.TimeoutMs, "FadeOutAsync");
        }

        private static async Task TryShowHudAsync(PhaseChangeOptions options, string signature, string phaseId)
        {
            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsLoadingHudService>(out var hud) || hud == null)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    $"[PhaseChange] LoadingHUD solicitado, mas serviço indisponível. signature='{signature}'.");
                return;
            }

            await AwaitWithTimeoutAsync(hud.EnsureLoadedAsync(), options.TimeoutMs, "LoadingHud.EnsureLoadedAsync");
            hud.Show(signature, phaseId);
        }

        private static void TryHideHud(string signature, string phaseId)
        {
            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsLoadingHudService>(out var hud) || hud == null)
            {
                return;
            }

            hud.Hide(signature, phaseId);
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

            return new SceneTransitionRequest(
                scenesToLoad: request.ScenesToLoad,
                scenesToUnload: request.ScenesToUnload,
                targetActiveScene: request.TargetActiveScene,
                useFade: request.UseFade,
                transitionProfileId: request.TransitionProfileId,
                contextSignature: signature);
        }
    }
}
