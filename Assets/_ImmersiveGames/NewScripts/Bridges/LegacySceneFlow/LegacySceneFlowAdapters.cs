using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Bridges.LegacySceneFlow
{
    /// <summary>
    /// Adapters para integrar Scene Flow no pipeline NewScripts sem depender de tipos/DI legados.
    ///
    /// Regras:
    /// - Fade: somente INewScriptsFadeService (sem fallback legado).
    /// - Loader: enquanto não migra, usa SceneManagerLoaderAdapter como fallback.
    /// - Profile: usa NewScriptsSceneTransitionProfile (ScriptableObject) em Resources.
    /// </summary>
    public static class LegacySceneFlowAdapters
    {
        private static readonly NewScriptsSceneTransitionProfileResolver SharedProfileResolver = new();

        public static ISceneFlowLoaderAdapter CreateLoaderAdapter(IDependencyProvider provider)
        {
            // Se existir um loader no DI NewScripts, use-o. Caso contrário, fallback SceneManager.
            if (provider != null && provider.TryGetGlobal<ISceneLoader>(out var loader) && loader != null)
            {
                DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                    "[SceneFlow] Usando ISceneLoader resolvido do DI NewScripts via adapter.");
                return new LegacySceneFlowLoaderAdapter(loader);
            }

            DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                "[SceneFlow] ISceneLoader indisponível no DI NewScripts. Usando SceneManagerLoaderAdapter (fallback).");
            return new SceneManagerLoaderAdapter();
        }

        public static ISceneFlowFadeAdapter CreateFadeAdapter(IDependencyProvider provider)
        {
            // Fade: sem legado, sem fallback. Se não houver serviço, é erro e voltamos NullFadeAdapter.
            if (provider != null && provider.TryGetGlobal<INewScriptsFadeService>(out var newFade) && newFade != null)
            {
                DebugUtility.LogVerbose(typeof(LegacySceneFlowAdapters),
                    "[SceneFlow] Usando INewScriptsFadeService via adapter (NewScripts).");
                return new NewScriptsSceneFlowFadeAdapter(newFade, SharedProfileResolver);
            }

            DebugUtility.LogError(typeof(LegacySceneFlowAdapters),
                "[SceneFlow] INewScriptsFadeService NÃO encontrado no DI NewScripts. " +
                "Fade não será executado (NullFadeAdapter). Não há fallback para legado.");
            return new NullFadeAdapter();
        }
    }

    /// <summary>
    /// Adapter para reutilizar ISceneLoader no pipeline NewScripts.
    /// (Mantém assinatura existente do adapter: Load/Unload/Additive)
    /// </summary>
    public sealed class LegacySceneFlowLoaderAdapter : ISceneFlowLoaderAdapter
    {
        private readonly ISceneLoader _loader;

        public LegacySceneFlowLoaderAdapter(ISceneLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public Task LoadSceneAsync(string sceneName)
        {
            return _loader.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        public Task UnloadSceneAsync(string sceneName)
        {
            return _loader.UnloadSceneAsync(sceneName);
        }

        public bool IsSceneLoaded(string sceneName)
        {
            return _loader.IsSceneLoaded(sceneName);
        }

        public async Task<bool> TrySetActiveSceneAsync(string sceneName)
        {
            var scene = _loader.GetSceneByName(sceneName);
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
    /// Adapter NewScripts: delega para INewScriptsFadeService.
    /// Resolve profileName (string) para NewScriptsSceneTransitionProfile via resolver e converte em NewScriptsFadeConfig.
    /// </summary>
    public sealed class NewScriptsSceneFlowFadeAdapter : ISceneFlowFadeAdapter
    {
        private readonly INewScriptsFadeService _fadeService;
        private readonly NewScriptsSceneTransitionProfileResolver _profileResolver;

        private static readonly NewScriptsFadeConfig DefaultConfig =
            new NewScriptsFadeConfig(
                fadeInDuration: 0.5f,
                fadeOutDuration: 0.5f,
                fadeInCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                fadeOutCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        public NewScriptsSceneFlowFadeAdapter(
            INewScriptsFadeService fadeService,
            NewScriptsSceneTransitionProfileResolver profileResolver)
        {
            _fadeService = fadeService ?? throw new ArgumentNullException(nameof(fadeService));
            _profileResolver = profileResolver ?? new NewScriptsSceneTransitionProfileResolver();
        }

        public bool IsAvailable => _fadeService != null;

        public void ConfigureFromProfile(string profileName)
        {
            var profile = _profileResolver.Resolve(profileName, out var resolvedPath);

            if (profile == null)
            {
                // Política: erro visível + defaults (não trava o fluxo).
                DebugUtility.LogError<NewScriptsSceneFlowFadeAdapter>(
                    $"[SceneFlow] NewScriptsSceneTransitionProfile '{profileName}' NÃO encontrado (ou tipo incorreto). " +
                    "Usando defaults de Fade.");
                _fadeService.Configure(DefaultConfig);

                DebugUtility.LogVerbose<NewScriptsSceneFlowFadeAdapter>(
                    $"[SceneFlow] Fade configurado com DEFAULTS. fadeIn={DefaultConfig.FadeInDuration:0.###}, fadeOut={DefaultConfig.FadeOutDuration:0.###}.");
                return;
            }

            // Se o profile desliga fade, fazemos "no-op fade" via duração 0.
            if (!profile.UseFade)
            {
                var noFade = new NewScriptsFadeConfig(
                    fadeInDuration: 0f,
                    fadeOutDuration: 0f,
                    fadeInCurve: DefaultConfig.FadeInCurve,
                    fadeOutCurve: DefaultConfig.FadeOutCurve);

                _fadeService.Configure(noFade);

                DebugUtility.LogVerbose<NewScriptsSceneFlowFadeAdapter>(
                    $"[SceneFlow] Profile '{profileName}' aplicado (path='{resolvedPath}'): UseFade=false → no-op (dur=0).");
                return;
            }

            var config = new NewScriptsFadeConfig(
                fadeInDuration: profile.FadeInDuration >= 0f ? profile.FadeInDuration : DefaultConfig.FadeInDuration,
                fadeOutDuration: profile.FadeOutDuration >= 0f ? profile.FadeOutDuration : DefaultConfig.FadeOutDuration,
                fadeInCurve: profile.FadeInCurve != null ? profile.FadeInCurve : DefaultConfig.FadeInCurve,
                fadeOutCurve: profile.FadeOutCurve != null ? profile.FadeOutCurve : DefaultConfig.FadeOutCurve);

            _fadeService.Configure(config);

            DebugUtility.LogVerbose<NewScriptsSceneFlowFadeAdapter>(
                $"[SceneFlow] Profile '{profileName}' aplicado (path='{resolvedPath}'): " +
                $"fadeIn={config.FadeInDuration:0.###}, fadeOut={config.FadeOutDuration:0.###}.");
        }

        public Task FadeInAsync() => _fadeService.FadeInAsync();
        public Task FadeOutAsync() => _fadeService.FadeOutAsync();
    }

    /// <summary>
    /// Resolve NewScriptsSceneTransitionProfile por nome, via Resources.
    /// Padrão de paths:
    /// - "SceneFlow/Profiles/&lt;profileName&gt;"
    /// - "&lt;profileName&gt;"
    ///
    /// Observação importante:
    /// - Se existir um asset com esse nome mas de tipo legado (ex.: SceneTransitionProfile),
    ///   Resources.Load&lt;NewScriptsSceneTransitionProfile&gt; retornará null. Este resolver detecta e loga isso.
    /// </summary>
    public sealed class NewScriptsSceneTransitionProfileResolver
    {
        private readonly Dictionary<string, NewScriptsSceneTransitionProfile> _cache = new();

        public NewScriptsSceneTransitionProfile Resolve(string profileName)
        {
            return Resolve(profileName, out _);
        }

        public NewScriptsSceneTransitionProfile Resolve(string profileName, out string resolvedPath)
        {
            resolvedPath = string.Empty;

            if (string.IsNullOrWhiteSpace(profileName))
            {
                return null;
            }

            var key = profileName.Trim();
            if (_cache.TryGetValue(key, out var cached) && cached != null)
            {
                resolvedPath = "<cache>";
                return cached;
            }

            var pathA = $"SceneFlow/Profiles/{key}";
            var pathB = key;

            // 1) Tentativa principal (tipo correto).
            var resolved = Resources.Load<NewScriptsSceneTransitionProfile>(pathA);
            if (resolved != null)
            {
                resolvedPath = pathA;
            }
            else
            {
                resolved = Resources.Load<NewScriptsSceneTransitionProfile>(pathB);
                if (resolved != null)
                {
                    resolvedPath = pathB;
                }
            }

            // 2) Fallback defensivo: case sensitivity.
            if (resolved == null)
            {
                var lower = key.ToLowerInvariant();
                if (!string.Equals(lower, key, StringComparison.Ordinal))
                {
                    var pathALower = $"SceneFlow/Profiles/{lower}";
                    var pathBLower = lower;

                    resolved = Resources.Load<NewScriptsSceneTransitionProfile>(pathALower);
                    if (resolved != null)
                    {
                        key = lower;
                        resolvedPath = pathALower;
                    }
                    else
                    {
                        resolved = Resources.Load<NewScriptsSceneTransitionProfile>(pathBLower);
                        if (resolved != null)
                        {
                            key = lower;
                            resolvedPath = pathBLower;
                        }
                    }
                }
            }

            if (resolved != null)
            {
                _cache[key] = resolved;

                DebugUtility.LogVerbose<NewScriptsSceneTransitionProfileResolver>(
                    $"[SceneFlow] Profile resolvido: name='{key}', path='{resolvedPath}', type='{resolved.GetType().FullName}'.");
                return resolved;
            }

            // 3) Diagnóstico de tipo incorreto (sem fallback funcional).
            var anyA = Resources.Load(pathA);
            if (anyA != null)
            {
                DebugUtility.LogError<NewScriptsSceneTransitionProfileResolver>(
                    $"[SceneFlow] Asset encontrado em Resources no path '{pathA}', porém com TIPO incorreto: '{anyA.GetType().FullName}'. " +
                    $"Esperado: '{typeof(NewScriptsSceneTransitionProfile).FullName}'. " +
                    "Ação: recrie/migre o asset como NewScriptsSceneTransitionProfile (CreateAssetMenu NewScripts).");
                return null;
            }

            var anyB = Resources.Load(pathB);
            if (anyB != null)
            {
                DebugUtility.LogError<NewScriptsSceneTransitionProfileResolver>(
                    $"[SceneFlow] Asset encontrado em Resources no path '{pathB}', porém com TIPO incorreto: '{anyB.GetType().FullName}'. " +
                    $"Esperado: '{typeof(NewScriptsSceneTransitionProfile).FullName}'. " +
                    "Ação: recrie/migre o asset como NewScriptsSceneTransitionProfile (CreateAssetMenu NewScripts).");
                return null;
            }

            DebugUtility.LogError<NewScriptsSceneTransitionProfileResolver>(
                $"[SceneFlow] NewScriptsSceneTransitionProfile '{key}' NÃO encontrado em Resources. " +
                $"Paths tentados: '{pathA}' e '{pathB}'. Confirme que o asset está em Resources/SceneFlow/Profiles e é do tipo NewScriptsSceneTransitionProfile.");
            return null;
        }
    }
}
