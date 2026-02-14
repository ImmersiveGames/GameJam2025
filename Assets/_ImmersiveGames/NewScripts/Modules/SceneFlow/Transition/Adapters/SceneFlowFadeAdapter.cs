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
    /// Em runtime, falhas de fade degradam para skip sem interromper a transição.
    /// </summary>
    public sealed class SceneFlowFadeAdapter : ISceneFlowFadeAdapter
    {
        private readonly IFadeService _fadeService;
        private readonly SceneTransitionProfileResolver _profileResolver;

        private bool _shouldFade;
        private bool _degradedLogged;
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
            _degradedLogged = false;
            _lastProfileId = "<unset>";
        }

        public bool IsAvailable => true;

        public void ConfigureFromProfile(SceneFlowProfileId profileId)
        {
            _degradedLogged = false;

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
                LogDegradedOnce(phase, "IFadeService is not available.");
                return;
            }

            try
            {
                _fadeService.Configure(_resolvedConfig);
                await run();
            }
            catch (Exception ex)
            {
                LogDegradedOnce(phase, $"Exception while running fade. ex='{ex.GetType().Name}: {ex.Message}'");
            }
        }

        private void LogDegradedOnce(string phase, string detail)
        {
            if (_degradedLogged)
            {
                return;
            }

            _degradedLogged = true;

            if (ShouldDegradeFadeInRuntime())
            {
                DebugUtility.LogError<SceneFlowFadeAdapter>(
                    $"[DEGRADED][Fade] phase='{phase}', profileId='{_lastProfileId}'. {detail}");
                return;
            }

            string message = $"[FATAL][Fade] phase='{phase}', profileId='{_lastProfileId}'. {detail}";
            DebugUtility.LogError<SceneFlowFadeAdapter>(message);
            throw new InvalidOperationException(message);
        }

        private static bool ShouldDegradeFadeInRuntime()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
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
