using System.Threading.Tasks;
using _ImmersiveGames.Scripts.LegaadoFadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.LegadoFadeSystem
{
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class LegadoFadeService : ILegadoFadeService
    {
        private const string FadeSceneName = "FadeScene";

        private LegadoFadeController _legadoFadeController;
        private bool _isLoadingFadeScene;
        private readonly object _lock = new();

        private OldSceneTransitionProfile _currentProfile;

        private const float DefaultFadeInDuration = 0.5f;
        private const float DefaultFadeOutDuration = 0.5f;

        private static readonly AnimationCurve DefaultFadeInCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private static readonly AnimationCurve DefaultFadeOutCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public void ConfigureFromProfile(OldSceneTransitionProfile profile)
        {
            _currentProfile = profile;
        }

        private async Task EnsureFadeControllerAsync()
        {
            if (_legadoFadeController != null)
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
                    DebugUtility.LogVerbose<LegadoFadeService>($"Carregando cena de fade '{FadeSceneName}' (async)...");
                    var loadOp = SceneManager.LoadSceneAsync(FadeSceneName, LoadSceneMode.Additive);

                    if (loadOp != null)
                        await loadOp;
                }

                await Task.Yield();

                _legadoFadeController = Object.FindAnyObjectByType<LegadoFadeController>();

                if (_legadoFadeController == null)
                {
                    DebugUtility.LogError<LegadoFadeService>($"Nenhum LegadoFadeController encontrado na FadeScene.");
                }
                else
                {
                    DebugUtility.LogVerbose<LegadoFadeService>($"LegadoFadeController localizado.");
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
            if (_legadoFadeController == null)
                return;

            if (_currentProfile == null || !_currentProfile.UseFade)
            {
                _legadoFadeController.Configure(
                    DefaultFadeInDuration,
                    DefaultFadeOutDuration,
                    DefaultFadeInCurve,
                    DefaultFadeOutCurve);
                return;
            }

            _legadoFadeController.Configure(
                _currentProfile.FadeInDuration > 0f ? _currentProfile.FadeInDuration : DefaultFadeInDuration,
                _currentProfile.FadeOutDuration > 0f ? _currentProfile.FadeOutDuration : DefaultFadeOutDuration,
                _currentProfile.FadeInCurve ?? DefaultFadeInCurve,
                _currentProfile.FadeOutCurve ?? DefaultFadeOutCurve);
        }

        public async Task FadeInAsync()
        {
            await EnsureFadeControllerAsync();

            if (_legadoFadeController == null)
                return;

            ApplyProfileToController();
            await _legadoFadeController.FadeInAsync();
        }

        public async Task FadeOutAsync()
        {
            await EnsureFadeControllerAsync();

            if (_legadoFadeController == null)
                return;

            ApplyProfileToController();
            await _legadoFadeController.FadeOutAsync();
        }
    }
}

