using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Resolve um <see cref="SceneTransitionProfile"/> a partir de um <see cref="SceneFlowProfileId"/>
    /// usando exclusivamente <see cref="SceneTransitionProfileCatalogAsset"/>.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneTransitionProfileResolver
    {
        private readonly SceneTransitionProfileCatalogAsset _catalog;

        public SceneTransitionProfileResolver(SceneTransitionProfileCatalogAsset catalog)
        {
            _catalog = catalog ?? throw new InvalidOperationException(
                $"SceneTransitionProfileCatalogAsset é obrigatório. Registre o catálogo em DI (path esperado: '{SceneTransitionProfileCatalogAsset.DefaultResourcesPath}').");
        }

        /// <summary>
        /// Resolve um profile e retorna origem para diagnóstico.
        /// </summary>
        public SceneTransitionProfile Resolve(SceneFlowProfileId profileId, out string resolvedPath, string contextSignature = null)
        {
            resolvedPath = "catalog";

            if (!profileId.IsValid)
            {
                throw new InvalidOperationException(
                    $"SceneFlowProfileId inválido para resolução de SceneTransitionProfile. context='{contextSignature ?? string.Empty}'.");
            }

            if (_catalog.TryGetProfile(profileId, out var profile) && profile != null)
            {
                return profile;
            }

            throw new InvalidOperationException(
                $"SceneTransitionProfile ausente no catálogo. profileId='{profileId}', catalogPath='{SceneTransitionProfileCatalogAsset.DefaultResourcesPath}'. " +
                "Adicione o profile no catálogo (SceneTransitionProfileCatalogAsset)."
            );
        }

        public SceneTransitionProfile Resolve(SceneFlowProfileId profileId, string contextSignature = null)
            => Resolve(profileId, out _, contextSignature);
    }
}
