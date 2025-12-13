using System.Threading.Tasks;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class FadeService : IFadeService
    {
        private const string FadeSceneName = "FadeScene";

        private FadeController _fadeController;
        private bool _isLoadingFadeScene;
        private readonly object _lock = new();

        private SceneTransitionProfile _currentProfile;

        private const float DefaultFadeInDuration = 0.5f;
        private const float DefaultFadeOutDuration = 0.5f;

        private static readonly AnimationCurve DefaultFadeInCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private static readonly AnimationCurve DefaultFadeOutCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public void ConfigureFromProfile(SceneTransitionProfile profile)
        {
            _currentProfile = profile;
        }

        private async Task EnsureFadeControllerAsync()
        {
            if (_fadeController != null)
                return;

            lock (_lock)
            {
                if (_isLoadingFadeScene)
                    return;

                _isLoadingFadeScene = true;
            }

            try
            {
                var fadeScene = SceneManager.GetSceneByName(FadeSceneName);

                if (!fadeScene.isLoaded)
                {
                    DebugUtility.LogVerbose<FadeService>($"Carregando cena de fade '{FadeSceneName}' (async)...");
                    var loadOp = SceneManager.LoadSceneAsync(FadeSceneName, LoadSceneMode.Additive);

                    if (loadOp != null)
                        await loadOp;
                }

                await Task.Yield();

                _fadeController = Object.FindAnyObjectByType<FadeController>();

                if (_fadeController == null)
                {
                    DebugUtility.LogError<FadeService>($"Nenhum FadeController encontrado na FadeScene.");
                }
                else
                {
                    DebugUtility.LogVerbose<FadeService>($"FadeController localizado.");
                }
            }
            finally
            {
                lock (_lock)
                {
                    _isLoadingFadeScene = false;
                }
            }
        }

        private void ApplyProfileToController()
        {
            if (_fadeController == null)
                return;

            if (_currentProfile == null || !_currentProfile.UseFade)
            {
                _fadeController.Configure(
                    DefaultFadeInDuration,
                    DefaultFadeOutDuration,
                    DefaultFadeInCurve,
                    DefaultFadeOutCurve);
                return;
            }

            _fadeController.Configure(
                _currentProfile.FadeInDuration > 0f ? _currentProfile.FadeInDuration : DefaultFadeInDuration,
                _currentProfile.FadeOutDuration > 0f ? _currentProfile.FadeOutDuration : DefaultFadeOutDuration,
                _currentProfile.FadeInCurve ?? DefaultFadeInCurve,
                _currentProfile.FadeOutCurve ?? DefaultFadeOutCurve);
        }

        public async Task FadeInAsync()
        {
            await EnsureFadeControllerAsync();

            if (_fadeController == null)
                return;

            ApplyProfileToController();
            await _fadeController.FadeInAsync();
        }

        public async Task FadeOutAsync()
        {
            await EnsureFadeControllerAsync();

            if (_fadeController == null)
                return;

            ApplyProfileToController();
            await _fadeController.FadeOutAsync();
        }
    }
}
