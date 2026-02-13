using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        /// <summary>
        /// F1: ponto único para configurar a resolução de perfis de transição do SceneFlow.
        ///
        /// Comportamento:
        /// - Se existir um <see cref="SceneTransitionProfileCatalogAsset"/> em DI, usa ele.
        /// - Caso contrário, tenta carregar o catálogo via Resources (path padrão do catálogo).
        /// - Se não existir asset, cria um catálogo em runtime e hidrata startup/frontend/gameplay.
        /// - O fallback legado continua habilitável por flag, mas com catálogo populado a origem preferencial vira "catalog".
        /// </summary>
        private static void RegisterSceneFlowTransitionProfiles()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<SceneTransitionProfileCatalogAsset>(out var catalog) || catalog == null)
            {
                catalog = Resources.Load<SceneTransitionProfileCatalogAsset>(SceneTransitionProfileCatalogAsset.DefaultResourcesPath);

                if (catalog != null)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        $"[SceneFlow] SceneTransitionProfileCatalogAsset carregado via Resources: '{SceneTransitionProfileCatalogAsset.DefaultResourcesPath}'.");
                }
                else
                {
                    catalog = ScriptableObject.CreateInstance<SceneTransitionProfileCatalogAsset>();
                    DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                        "[SceneFlow] SceneTransitionProfileCatalogAsset ausente; criando catálogo runtime com bootstrap explícito de profiles obrigatórios.");
                }

                provider.RegisterGlobal(catalog);
            }

            EnsureProfileCatalogCoverage(catalog);

            if (!provider.TryGetGlobal<SceneTransitionProfileResolver>(out var resolver) || resolver == null)
            {
                resolver = new SceneTransitionProfileResolver(catalog);
                provider.RegisterGlobal(resolver);
            }
        }

        private static void EnsureProfileCatalogCoverage(SceneTransitionProfileCatalogAsset catalog)
        {
            if (catalog == null)
            {
                return;
            }

            var requiredProfileIds = new[]
            {
                SceneFlowProfileId.Startup,
                SceneFlowProfileId.Frontend,
                SceneFlowProfileId.Gameplay
            };

            int hydrated = 0;
            int missing = 0;

            for (int i = 0; i < requiredProfileIds.Length; i++)
            {
                var profileId = requiredProfileIds[i];

                if (catalog.TryGetProfile(profileId, out var existing) && existing != null)
                {
                    continue;
                }

                var path = SceneFlowProfilePaths.For(profileId, catalog.LegacyResourcesBasePath);
                var loaded = Resources.Load<SceneTransitionProfile>(path);

                if (loaded == null)
                {
                    missing++;
                    DebugUtility.LogError(typeof(GlobalCompositionRoot),
                        $"[SceneFlow] Profile obrigatório não encontrado para bootstrap do catálogo. profileId='{profileId}', path='{path}'.");
                    continue;
                }

                if (catalog.SetOrAddProfile(profileId, loaded))
                {
                    hydrated++;
                }
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[SceneFlow] SceneTransitionProfileCatalog pronto. source='catalog', hydrated={hydrated}, missing={missing}, allowLegacyFallback={catalog.AllowLegacyResourcesFallback}.");
        }

    }
}
