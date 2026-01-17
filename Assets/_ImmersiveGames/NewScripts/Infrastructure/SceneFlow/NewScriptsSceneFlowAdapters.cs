using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow
{
    /// <summary>
    /// Adapters para integrar Scene Flow no pipeline NewScripts sem depender de tipos/DI legados.
    ///
    /// Regras:
    /// - Fade: somente INewScriptsFadeService (sem fallback legado).
    /// - Loader: enquanto não migra, usa SceneManagerLoaderAdapter como fallback.
    /// - Profile: usa NewScriptsSceneTransitionProfile (ScriptableObject) em Resources.
    /// </summary>
    public static class NewScriptsSceneFlowAdapters
    {
        private static readonly NewScriptsSceneTransitionProfileResolver SharedProfileResolver = new();

        public static ISceneFlowLoaderAdapter CreateLoaderAdapter()
        {
            DebugUtility.LogVerbose(typeof(NewScriptsSceneFlowAdapters),
                "[SceneFlow] Usando SceneManagerLoaderAdapter (loader nativo).");
            return new SceneManagerLoaderAdapter();
        }

        public static ISceneFlowFadeAdapter CreateFadeAdapter(IDependencyProvider provider)
        {
            // Fade: sem legado, sem fallback. Se não houver serviço, é erro e voltamos NullFadeAdapter.
            if (provider != null && provider.TryGetGlobal<INewScriptsFadeService>(out var newFade) && newFade != null)
            {
                DebugUtility.LogVerbose(typeof(NewScriptsSceneFlowAdapters),
                    "[SceneFlow] Usando INewScriptsFadeService via adapter (NewScripts).");
                return new NewScriptsSceneFlowFadeAdapter(newFade, SharedProfileResolver);
            }

            DebugUtility.LogError(typeof(NewScriptsSceneFlowAdapters),
                "[SceneFlow] INewScriptsFadeService NÃO encontrado no DI NewScripts. " +
                "Fade não será executado (NullFadeAdapter). Não há fallback para legado.");
            return new NullFadeAdapter();
        }
    }

    /// <summary>
    /// Adapter NewScripts: delega para INewScriptsFadeService.
    /// Resolve profileId (<see cref="SceneFlowProfileId"/>) para NewScriptsSceneTransitionProfile via resolver e converte em NewScriptsFadeConfig.
    /// </summary>
    public sealed class NewScriptsSceneFlowFadeAdapter : ISceneFlowFadeAdapter
    {
        private readonly INewScriptsFadeService _fadeService;
        private readonly NewScriptsSceneTransitionProfileResolver _profileResolver;

        private static readonly NewScriptsFadeConfig DefaultConfig =
            new(
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

        public void ConfigureFromProfile(SceneFlowProfileId profileId)
        {
            var profile = _profileResolver.Resolve(profileId, out string resolvedPath);

            if (profile == null)
            {
                // Política: erro visível + defaults (não trava o fluxo).
                DebugUtility.LogError<NewScriptsSceneFlowFadeAdapter>(
                    $"[SceneFlow] NewScriptsSceneTransitionProfile '{profileId}' NÃO encontrado (ou tipo incorreto). " +
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
                    $"[SceneFlow] Profile '{profileId}' aplicado (path='{resolvedPath}'): UseFade=false → no-op (dur=0).");
                return;
            }

            var config = new NewScriptsFadeConfig(
                fadeInDuration: profile.FadeInDuration >= 0f ? profile.FadeInDuration : DefaultConfig.FadeInDuration,
                fadeOutDuration: profile.FadeOutDuration >= 0f ? profile.FadeOutDuration : DefaultConfig.FadeOutDuration,
                fadeInCurve: profile.FadeInCurve != null ? profile.FadeInCurve : DefaultConfig.FadeInCurve,
                fadeOutCurve: profile.FadeOutCurve != null ? profile.FadeOutCurve : DefaultConfig.FadeOutCurve);

            _fadeService.Configure(config);

            DebugUtility.LogVerbose<NewScriptsSceneFlowFadeAdapter>(
                $"[SceneFlow] Profile '{profileId}' aplicado (path='{resolvedPath}'): " +
                $"fadeIn={config.FadeInDuration:0.###}, fadeOut={config.FadeOutDuration:0.###}.");
        }

        public Task FadeInAsync() => _fadeService.FadeInAsync();
        public Task FadeOutAsync() => _fadeService.FadeOutAsync();
    }

    /// <summary>
    /// Resolve NewScriptsSceneTransitionProfile por ID, via Resources.
    /// Padrão de paths:
    /// - "SceneFlow/Profiles/<profileId.Value/>"
    /// - "<profileId.Value/>"
    ///
    /// Observação importante:
    /// - Se existir um asset com esse nome, mas de tipo legado (ex.: SceneTransitionProfile),
    ///   Resources.Load<NewScriptsSceneTransitionProfile/> retornará null. Este resolver detecta e loga isso.
    /// </summary>
    public sealed class NewScriptsSceneTransitionProfileResolver
    {
        private readonly Dictionary<string, NewScriptsSceneTransitionProfile> _cache = new();

        public NewScriptsSceneTransitionProfile Resolve(SceneFlowProfileId profileId)
        {
            return Resolve(profileId, out _);
        }

        public NewScriptsSceneTransitionProfile Resolve(SceneFlowProfileId profileId, out string resolvedPath)
        {
            resolvedPath = string.Empty;

            if (!profileId.IsValid)
            {
                return null;
            }

            // O value do ID já é normalizado (trim + lower) em SceneFlowProfileId.
            string key = profileId.Value;
            if (_cache.TryGetValue(key, out var cached) && cached != null)
            {
                resolvedPath = "<cache>";
                return cached;
            }

            string pathA = SceneFlowProfilePaths.For(profileId);
            string pathB = key;

            // 1) Tentativa principal (tipo correto).
            var resolved = !string.IsNullOrEmpty(pathA)
                ? Resources.Load<NewScriptsSceneTransitionProfile>(pathA)
                : null;

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

            // 2) Sem fallback de case aqui: o ID já é normalizado (lower). Se existir um asset com casing
            // diferente no path, ele deve ser corrigido no projeto/Resources.

            if (resolved != null)
            {
                _cache[key] = resolved;

                DebugUtility.LogVerbose<NewScriptsSceneTransitionProfileResolver>(
                    $"[SceneFlow] Profile resolvido: name='{key}', path='{resolvedPath}', type='{resolved.GetType().FullName}'.");
                return resolved;
            }

            // 3) Diagnóstico de tipo incorreto (sem fallback funcional).
            if (!string.IsNullOrEmpty(pathA))
            {
                var anyA = Resources.Load(pathA);
                if (anyA != null)
                {
                    DebugUtility.LogError<NewScriptsSceneTransitionProfileResolver>(
                        $"[SceneFlow] Asset encontrado em Resources no path '{pathA}', porém com TIPO incorreto: '{anyA.GetType().FullName}'. " +
                        $"Esperado: '{typeof(NewScriptsSceneTransitionProfile).FullName}'. " +
                        "Ação: recrie/migre o asset como NewScriptsSceneTransitionProfile (CreateAssetMenu NewScripts).");
                    return null;
                }
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
                $"Paths tentados: '{pathA}' e '{pathB}'. Confirme que o asset está em Resources/{SceneFlowProfilePaths.ProfilesRoot} e é do tipo NewScriptsSceneTransitionProfile.");
            return null;
        }
    }
}
