using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Resolve um <see cref="SceneTransitionProfile"/> a partir de um <see cref="SceneFlowProfileId"/>.
    ///
    /// Ordem de resolução:
    /// 1) <see cref="SceneTransitionProfileCatalogAsset"/> (quando fornecido/registrado)
    /// 2) Fallback legado via <see cref="Resources.Load{T}(string)"/> (opcional, controlado pelo catálogo)
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneTransitionProfileResolver
    {
        private readonly SceneTransitionProfileCatalogAsset _catalog;
        private readonly bool _allowLegacyResourcesFallback;
        private readonly string _legacyResourcesBasePath;

        private bool _warnedNoCatalog;
        private bool _warnedUsingLegacyFallback;

        public SceneTransitionProfileResolver(SceneTransitionProfileCatalogAsset catalog = null)
        {
            _catalog = catalog;

            if (catalog != null)
            {
                _allowLegacyResourcesFallback = catalog.AllowLegacyResourcesFallback;
                _legacyResourcesBasePath = string.IsNullOrWhiteSpace(catalog.LegacyResourcesBasePath)
                    ? SceneFlowProfilePaths.ProfilesRoot
                    : catalog.LegacyResourcesBasePath.Trim().TrimEnd('/');
            }
            else
            {
                // Comportamento padrão: mantém compatibilidade com o projeto atual.
                _allowLegacyResourcesFallback = true;
                _legacyResourcesBasePath = SceneFlowProfilePaths.ProfilesRoot;
            }
        }

        /// <summary>
        /// Resolve um profile e retorna também o path (ou a origem) usado para diagnóstico.
        /// - Quando vem do catálogo, resolvedPath = "catalog".
        /// - Quando vem de Resources, resolvedPath = o resourcePath completo.
        /// </summary>
        public SceneTransitionProfile Resolve(SceneFlowProfileId profileId, out string resolvedPath, string contextSignature = null)
        {
            resolvedPath = string.Empty;

            if (!profileId.IsValid)
                return null;

            if (_catalog != null && _catalog.TryGetProfile(profileId, out var catalogProfile))
            {
                resolvedPath = "catalog";
                return catalogProfile;
            }

            if (_catalog == null && !_warnedNoCatalog)
            {
                _warnedNoCatalog = true;
                DebugUtility.LogVerbose(typeof(SceneTransitionProfileResolver),
                    $"[OBS] SceneTransitionProfileCatalogAsset não registrado; usando fallback legado via Resources. " +
                    $"(profileId='{profileId}', context='{contextSignature ?? ""}')");
            }

            if (!_allowLegacyResourcesFallback)
            {
                DebugUtility.LogError(typeof(SceneTransitionProfileResolver),
                    $"[ERR] SceneTransitionProfile ausente no catálogo e fallback legado desabilitado. " +
                    $"(profileId='{profileId}', context='{contextSignature ?? ""}')");
                return null;
            }

            if (!_warnedUsingLegacyFallback)
            {
                _warnedUsingLegacyFallback = true;
                DebugUtility.LogWarning(typeof(SceneTransitionProfileResolver),
                    $"[OBS] Resolução de SceneTransitionProfile via Resources está ativa (legado). " +
                    $"Recomenda-se registrar SceneTransitionProfileCatalogAsset e popular o catálogo. " +
                    $"(basePath='{_legacyResourcesBasePath}')");
            }

            resolvedPath = SceneFlowProfilePaths.For(profileId, _legacyResourcesBasePath);
            var loaded = Resources.Load<SceneTransitionProfile>(resolvedPath);

            if (loaded == null)
            {
                DebugUtility.LogError(typeof(SceneTransitionProfileResolver),
                    $"[ERR] SceneTransitionProfile não encontrado via Resources. " +
                    $"(profileId='{profileId}', path='{resolvedPath}', context='{contextSignature ?? ""}')");
            }

            return loaded;
        }

        /// <summary>
        /// Resolve um profile; quando precisar do path/origem use o overload com out resolvedPath.
        /// </summary>
        public SceneTransitionProfile Resolve(SceneFlowProfileId profileId, string contextSignature = null)
        {
            return Resolve(profileId, out _, contextSignature);
        }
    }
}
