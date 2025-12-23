using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Core;

namespace _ImmersiveGames.NewScripts.Bridges.LegacySceneFlow
{
    /// <summary>
    /// Adapters temporários para reutilizar serviços legados de Scene Flow (Fade/Loader)
    /// sem acoplar o pipeline NewScripts aos namespaces concretos do legado.
    /// </summary>
    public static class LegacySceneFlowAdapters
    {
        public static ISceneFlowLoaderAdapter CreateLoaderAdapter(IDependencyProvider provider)
        {
            if (provider != null &&
                provider.TryGetGlobal<ISceneLoader>(out var legacyLoader) &&
                legacyLoader != null)
            {
                DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                    "[SceneFlow] Usando ISceneLoader legado via adapter.");
                return new LegacySceneFlowLoaderAdapter(legacyLoader);
            }

            DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                "[SceneFlow] ISceneLoader legado indisponível. Usando SceneManagerLoaderAdapter (fallback).");
            return new SceneManagerLoaderAdapter();
        }

        public static ISceneFlowFadeAdapter CreateFadeAdapter(IDependencyProvider provider)
        {
            if (provider != null &&
                provider.TryGetGlobal<IFadeService>(out var fadeService) &&
                fadeService != null)
            {
                DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                    "[SceneFlow] Usando IFadeService legado via adapter.");
                return new LegacySceneFlowFadeAdapter(fadeService);
            }

            DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                "[SceneFlow] IFadeService legado indisponível. Usando NullFadeAdapter.");
            return new NullFadeAdapter();
        }
    }

    /// <summary>
    /// Adapter para reutilizar ISceneLoader legado no pipeline NewScripts.
    /// </summary>
    public sealed class LegacySceneFlowLoaderAdapter : ISceneFlowLoaderAdapter
    {
        private readonly ISceneLoader _legacyLoader;

        public LegacySceneFlowLoaderAdapter(ISceneLoader legacyLoader)
        {
            _legacyLoader = legacyLoader ?? throw new ArgumentNullException(nameof(legacyLoader));
        }

        public Task LoadSceneAsync(string sceneName)
        {
            return _legacyLoader.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        public Task UnloadSceneAsync(string sceneName)
        {
            return _legacyLoader.UnloadSceneAsync(sceneName);
        }

        public bool IsSceneLoaded(string sceneName)
        {
            return _legacyLoader.IsSceneLoaded(sceneName);
        }

        public async Task<bool> TrySetActiveSceneAsync(string sceneName)
        {
            var scene = _legacyLoader.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            await Task.Yield();
            return true;
        }

        public string GetActiveSceneName()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
    }

    /// <summary>
    /// Adapter para reutilizar IFadeService legado no pipeline NewScripts.
    /// </summary>
    public sealed class LegacySceneFlowFadeAdapter : ISceneFlowFadeAdapter
    {
        private readonly IFadeService _fadeService;

        public LegacySceneFlowFadeAdapter(IFadeService fadeService)
        {
            _fadeService = fadeService ?? throw new ArgumentNullException(nameof(fadeService));
        }

        public bool IsAvailable => _fadeService != null;

        public void ConfigureFromProfile(string profileName)
        {
            // IFadeService legado não suporta profile diretamente; nada a configurar.
        }

        public Task FadeInAsync() => _fadeService.FadeInAsync();

        public Task FadeOutAsync() => _fadeService.FadeOutAsync();
    }
}
