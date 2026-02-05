using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime
{
    /// <summary>
    /// Adapters para integrar SceneFlow no pipeline NewScripts sem depender de tipos/DI legados.
    ///
    /// Regras:
    /// - Fade: somente IFadeService (sem fallback legado).
    /// - Loader: enquanto não migra, usa SceneManagerLoaderAdapter como fallback.
    /// - Profile: usa SceneTransitionProfile (ScriptableObject) em Resources.
    /// - Strict/Release: policy via IRuntimeModeProvider + DEGRADED_MODE via IDegradedModeReporter.
    /// </summary>
    public static class SceneFlowAdapters
    {
        private static readonly SceneTransitionProfileResolver SharedProfileResolver = new();

        public static ISceneFlowLoaderAdapter CreateLoaderAdapter()
        {
            DebugUtility.LogVerbose(typeof(SceneFlowAdapters),
                "[SceneFlow] Usando SceneManagerLoaderAdapter (loader nativo).");
            return new SceneManagerLoaderAdapter();
        }

        public static ISceneFlowFadeAdapter CreateFadeAdapter(IDependencyProvider provider)
        {
            // Comentário: o adapter é responsável por aplicar política Strict/Release e por reportar DEGRADED_MODE.
            // O serviço de Fade deve ser “duro”: se pré-condições não são atendidas, ele falha explicitamente.
            ResolveOrCreatePolicy(provider, out var modeProvider, out var degradedReporter);

            IFadeService fadeService = null;
            if (provider != null && provider.TryGetGlobal<IFadeService>(out var resolved) && resolved != null)
            {
                fadeService = resolved;
            }

            if (fadeService != null)
            {
                DebugUtility.LogVerbose(typeof(SceneFlowAdapters),
                    "[SceneFlow] Usando IFadeService via adapter (NewScripts).");
            }
            else
            {
                // Comentário: não é erro imediato aqui. O adapter decide: Strict => throw; Release => DEGRADED_MODE + no-op.
                DebugUtility.LogWarning(typeof(SceneFlowAdapters),
                    "[SceneFlow] IFadeService não encontrado no DI global. " +
                    "O comportamento dependerá da policy (Strict/Release).");
            }

            return new SceneFlowFadeAdapter(
                fadeService,
                SharedProfileResolver,
                modeProvider,
                degradedReporter);
        }

        private static void ResolveOrCreatePolicy(
            IDependencyProvider provider,
            out IRuntimeModeProvider modeProvider,
            out IDegradedModeReporter degradedReporter)
        {
            modeProvider = null;
            degradedReporter = null;

            if (provider != null)
            {
                provider.TryGetGlobal(out modeProvider);
                provider.TryGetGlobal(out degradedReporter);
            }

            modeProvider ??= new UnityRuntimeModeProvider();
            degradedReporter ??= new DegradedModeReporter();
        }
    }

    /// <summary>
    /// Adapter NewScripts: aplica profileId (SceneFlowProfileId) → SceneTransitionProfile → FadeConfig.
    ///
    /// Política:
    /// - Strict: pré-condições obrigatórias (profile válido, serviço DI presente quando fade habilitado, cena/controller disponíveis).
    /// - Release: em caso de falha, reporta DEGRADED_MODE (feature='fade') e executa no-op.
    /// </summary>
    public sealed class SceneFlowFadeAdapter : ISceneFlowFadeAdapter
    {
        private readonly IFadeService _fadeService;
        private readonly SceneTransitionProfileResolver _profileResolver;
        private readonly IRuntimeModeProvider _modeProvider;
        private readonly IDegradedModeReporter _degradedReporter;

        private bool _shouldFade;
        private FadeConfig _resolvedConfig;

        private static readonly FadeConfig DefaultConfig =
            new(
                fadeInDuration: 0.5f,
                fadeOutDuration: 0.5f,
                fadeInCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                fadeOutCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        public SceneFlowFadeAdapter(
            IFadeService fadeService,
            SceneTransitionProfileResolver profileResolver,
            IRuntimeModeProvider modeProvider,
            IDegradedModeReporter degradedReporter)
        {
            _fadeService = fadeService; // pode ser null
            _profileResolver = profileResolver ?? new SceneTransitionProfileResolver();
            _modeProvider = modeProvider ?? new UnityRuntimeModeProvider();
            _degradedReporter = degradedReporter ?? new DegradedModeReporter();

            _resolvedConfig = DefaultConfig;
            _shouldFade = true;
        }

        // Comentário: o adapter é sempre “disponível” como ponto de política.
        public bool IsAvailable => true;

        public void ConfigureFromProfile(SceneFlowProfileId profileId)
        {
            var profile = _profileResolver.Resolve(profileId, out string resolvedPath);

            if (profile == null)
            {
                string detail =
                    $"profileId='{profileId}' paths='{SceneFlowProfilePaths.For(profileId)}|{profileId.Value}'";

                if (_modeProvider.IsStrict)
                {
                    DebugUtility.LogError<SceneFlowFadeAdapter>(
                        $"[SceneFlow][Fade] Profile ausente em Strict. {detail}");
                    throw new InvalidOperationException($"[SceneFlow][Fade] Profile ausente em Strict. {detail}");
                }

                // Release: política explícita -> desabilitar fade (no-op) e reportar DEGRADED_MODE.
                _degradedReporter.Report(
                    feature: "fade",
                    reason: "profile_missing",
                    detail: detail);

                _shouldFade = false;
                _resolvedConfig = NoOpConfig();
                return;
            }

            if (!profile.UseFade)
            {
                _shouldFade = false;
                _resolvedConfig = NoOpConfig();

                DebugUtility.LogVerbose<SceneFlowFadeAdapter>(
                    $"[SceneFlow] Profile '{profileId}' aplicado (path='{resolvedPath}'): UseFade=false → no-op (dur=0).");
                return;
            }

            _shouldFade = true;

            _resolvedConfig = new FadeConfig(
                fadeInDuration: profile.FadeInDuration >= 0f ? profile.FadeInDuration : DefaultConfig.FadeInDuration,
                fadeOutDuration: profile.FadeOutDuration >= 0f ? profile.FadeOutDuration : DefaultConfig.FadeOutDuration,
                fadeInCurve: profile.FadeInCurve != null ? profile.FadeInCurve : DefaultConfig.FadeInCurve,
                fadeOutCurve: profile.FadeOutCurve != null ? profile.FadeOutCurve : DefaultConfig.FadeOutCurve);

            DebugUtility.LogVerbose<SceneFlowFadeAdapter>(
                $"[SceneFlow] Profile '{profileId}' aplicado (path='{resolvedPath}'): " +
                $"fadeIn={_resolvedConfig.FadeInDuration:0.###}, fadeOut={_resolvedConfig.FadeOutDuration:0.###}.");
        }

        public async Task FadeInAsync()
        {
            if (!_shouldFade || _resolvedConfig.FadeInDuration <= 0f)
            {
                return;
            }

            await ExecuteFadeAsync(
                phase: "fade_in",
                run: () => _fadeService.FadeInAsync());
        }

        public async Task FadeOutAsync()
        {
            if (!_shouldFade || _resolvedConfig.FadeOutDuration <= 0f)
            {
                return;
            }

            await ExecuteFadeAsync(
                phase: "fade_out",
                run: () => _fadeService.FadeOutAsync());
        }

        private async Task ExecuteFadeAsync(string phase, Func<Task> run)
        {
            if (_fadeService == null)
            {
                string detail = "IFadeService ausente no DI global.";

                if (_modeProvider.IsStrict)
                {
                    DebugUtility.LogError<SceneFlowFadeAdapter>(
                        $"[SceneFlow][Fade] Serviço ausente em Strict. phase='{phase}' {detail}");
                    throw new InvalidOperationException(
                        $"[SceneFlow][Fade] Serviço ausente em Strict. phase='{phase}' {detail}");
                }

                _degradedReporter.Report(
                    feature: "fade",
                    reason: "missing_di_service",
                    detail: $"phase='{phase}' {detail}");

                return;
            }

            try
            {
                _fadeService.Configure(_resolvedConfig);
                await run();
            }
            catch (Exception ex)
            {
                if (_modeProvider.IsStrict)
                {
                    DebugUtility.LogError<SceneFlowFadeAdapter>(
                        $"[SceneFlow][Fade] Erro em Strict. phase='{phase}' ex={ex.GetType().Name}: {ex.Message}");
                    throw;
                }

                _degradedReporter.Report(
                    feature: "fade",
                    reason: "runtime_exception",
                    detail: $"phase='{phase}' ex={ex.GetType().Name}: {ex.Message}");

                // Release: no-op após degradar.
            }
        }

        private static FadeConfig NoOpConfig()
        {
            return new FadeConfig(
                fadeInDuration: 0f,
                fadeOutDuration: 0f,
                fadeInCurve: DefaultConfig.FadeInCurve,
                fadeOutCurve: DefaultConfig.FadeOutCurve);
        }
    }

}

