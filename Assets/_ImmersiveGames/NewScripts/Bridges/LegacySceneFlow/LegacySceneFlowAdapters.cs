using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using LegacyDependencyManager = _ImmersiveGames.Scripts.Utils.DependencySystems.DependencyManager;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Bridges.LegacySceneFlow
{
    /// <summary>
    /// Adapters temporários para reutilizar serviços legados de Scene Flow (Fade/Loader)
    /// sem acoplar o pipeline NewScripts aos namespaces concretos do legado.
    /// </summary>
    public static class LegacySceneFlowAdapters
    {
        private static readonly SceneTransitionProfileResolver SharedProfileResolver = new();

        public static ISceneFlowLoaderAdapter CreateLoaderAdapter(IDependencyProvider provider)
        {
            if (TryResolveLegacyService(provider, out ISceneLoader legacyLoader))
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
            if (TryResolveLegacyService(provider, out IFadeService fadeService))
            {
                DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                    "[SceneFlow] Usando IFadeService legado via adapter.");
                return new LegacySceneFlowFadeAdapter(fadeService, SharedProfileResolver);
            }

            DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                "[SceneFlow] IFadeService legado indisponível. Usando NullFadeAdapter.");
            return new NullFadeAdapter();
        }

        private static bool TryResolveLegacyService<T>(IDependencyProvider provider, out T service) where T : class
        {
            service = null;

            if (provider != null &&
                provider.TryGetGlobal(out service) &&
                service != null)
            {
                return true;
            }

            try
            {
                var legacyProvider = LegacyDependencyManager.Provider;
                if (legacyProvider != null &&
                    legacyProvider.TryGetGlobal(out service) &&
                    service != null)
                {
                    DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                        $"[SceneFlow] Serviço legado resolvido via DependencyManager legado: {typeof(T).Name}.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(LegacySceneFlowAdapters),
                    $"[SceneFlow] Falha ao resolver serviço legado {typeof(T).Name} via DependencyManager legado: {ex.Message}");
            }

            return false;
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
        private readonly SceneTransitionProfileResolver _profileResolver;

        public LegacySceneFlowFadeAdapter(IFadeService fadeService, SceneTransitionProfileResolver profileResolver)
        {
            _fadeService = fadeService ?? throw new ArgumentNullException(nameof(fadeService));
            _profileResolver = profileResolver ?? new SceneTransitionProfileResolver();
        }

        public bool IsAvailable => _fadeService != null;

        public void ConfigureFromProfile(string profileName)
        {
            if (_fadeService is FadeService concreteFade)
            {
                var profile = _profileResolver.Resolve(profileName);
                if (profile == null && !string.IsNullOrWhiteSpace(profileName))
                {
                    DebugUtility.LogWarning<LegacySceneFlowFadeAdapter>(
                        $"[SceneFlow] Profile '{profileName}' não encontrado. FadeService usará defaults.");
                }
                concreteFade.ConfigureFromProfile(profile);
                return;
            }

            DebugUtility.LogVerbose<LegacySceneFlowFadeAdapter>(
                "[SceneFlow] IFadeService não suporta configuração de profile diretamente (não é FadeService).");
        }

        public Task FadeInAsync() => _fadeService.FadeInAsync();

        public Task FadeOutAsync() => _fadeService.FadeOutAsync();
    }

    /// <summary>
    /// Resolve SceneTransitionProfile por nome, usando Resources como fallback.
    /// </summary>
    public sealed class SceneTransitionProfileResolver
    {
        private readonly Dictionary<string, SceneTransitionProfile> _cache = new();

        public SceneTransitionProfile Resolve(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return null;
            }

            if (_cache.TryGetValue(profileName, out var cached))
            {
                return cached;
            }

            var resolved = Resources.Load<SceneTransitionProfile>(profileName);

            if (resolved == null)
            {
                resolved = Resources
                    .FindObjectsOfTypeAll<SceneTransitionProfile>()
                    .FirstOrDefault(p => string.Equals(p.name, profileName, StringComparison.Ordinal));
            }

            if (resolved == null)
            {
                DebugUtility.LogWarning<SceneTransitionProfileResolver>(
                    $"[SceneFlow] SceneTransitionProfile '{profileName}' não encontrado (Resources). Fade usará defaults.");
                return null;
            }

            _cache[profileName] = resolved;
            return resolved;
        }
    }
}
