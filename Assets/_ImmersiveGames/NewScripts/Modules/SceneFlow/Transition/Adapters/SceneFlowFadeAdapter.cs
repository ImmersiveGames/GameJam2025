using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Adapters
{
    /// <summary>
    /// Adapter NewScripts: aplica profileId (SceneFlowProfileId) → SceneTransitionProfile → FadeConfig.
    /// Política: dependências obrigatórias em runtime (fail-fast, sem degraded/no-op quando UseFade=true).
    /// </summary>
    public sealed class SceneFlowFadeAdapter : ISceneFlowFadeAdapter
    {
        private readonly IFadeService _fadeService;
        private readonly SceneTransitionProfileResolver _profileResolver;

        private bool _shouldFade;
        private FadeConfig _resolvedConfig;
        private string _lastProfileId;

        private static readonly FadeConfig DefaultConfig =
            new(
                fadeInDuration: 0.5f,
                fadeOutDuration: 0.5f,
                fadeInCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                fadeOutCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        public SceneFlowFadeAdapter(
            IFadeService fadeService,
            SceneTransitionProfileResolver profileResolver)
        {
            _fadeService = fadeService;
            _profileResolver = profileResolver ?? throw new InvalidOperationException("SceneTransitionProfileResolver é obrigatório no SceneFlowFadeAdapter.");

            _resolvedConfig = DefaultConfig;
            _shouldFade = true;
            _lastProfileId = "<unset>";
        }

        public bool IsAvailable => true;

        public void ConfigureFromProfile(SceneFlowProfileId profileId)
        {
            var profile = _profileResolver.Resolve(profileId, out string resolvedPath);
            _lastProfileId = profileId.ToString();

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

        public async Task FadeInAsync(string? contextSignature = null)
        {
            if (!_shouldFade || _resolvedConfig.FadeInDuration <= 0f)
            {
                return;
            }

            await ExecuteFadeAsync(
                phase: "fade_in",
                run: () => _fadeService.FadeInAsync(contextSignature));
        }

        public async Task FadeOutAsync(string? contextSignature = null)
        {
            if (!_shouldFade || _resolvedConfig.FadeOutDuration <= 0f)
            {
                return;
            }

            await ExecuteFadeAsync(
                phase: "fade_out",
                run: () => _fadeService.FadeOutAsync(contextSignature));
        }

        private async Task ExecuteFadeAsync(string phase, Func<Task> run)
        {
            if (_fadeService == null)
            {
                throw CreateMandatoryDependencyException(
                    phase,
                    "IFadeService ausente no DI global com UseFade=true.");
            }

            try
            {
                _fadeService.Configure(_resolvedConfig);
                await run();
            }
            catch (Exception ex)
            {
                throw CreateMandatoryDependencyException(
                    phase,
                    $"Exceção durante execução do fade. ex='{ex.GetType().Name}: {ex.Message}'",
                    ex);
            }
        }

        private InvalidOperationException CreateMandatoryDependencyException(string phase, string detail, Exception innerException = null)
        {
            string signature = phase == "fade_in"
                ? "FadeInAsync"
                : phase == "fade_out"
                    ? "FadeOutAsync"
                    : "Unknown";

            string message =
                $"[SceneFlow][Fade] Dependência obrigatória violada. phase='{phase}', signature='{signature}', profileId='{_lastProfileId}'. {detail}";

            DebugUtility.LogError<SceneFlowFadeAdapter>(message);
            return innerException == null
                ? new InvalidOperationException(message)
                : new InvalidOperationException(message, innerException);
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
