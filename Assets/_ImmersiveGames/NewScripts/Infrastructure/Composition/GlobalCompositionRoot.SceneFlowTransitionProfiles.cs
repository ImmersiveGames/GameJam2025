using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        /// <summary>
        /// F1.1: configuração fail-fast da resolução de perfis de transição do SceneFlow.
        /// </summary>
        private static void RegisterSceneFlowTransitionProfiles()
        {
            var provider = DependencyManager.Provider;
            var bootstrap = GetRequiredBootstrapConfig(out _);

            var bootstrapCatalog = bootstrap.TransitionProfileCatalog;
            if (bootstrapCatalog == null)
            {
                FailFast("Missing required NewScriptsBootstrapConfigAsset.transitionProfileCatalog (SceneTransitionProfileCatalogAsset).");
            }

            if (!provider.TryGetGlobal<SceneTransitionProfileCatalogAsset>(out var catalog) || catalog == null)
            {
                catalog = bootstrapCatalog;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] CatalogResolvedVia=BootstrapConfig field=transitionProfileCatalog asset={catalog.name}",
                    DebugUtility.Colors.Info);
                provider.RegisterGlobal(catalog);
            }
            else if (!ReferenceEquals(catalog, bootstrapCatalog))
            {
                FailFast($"SceneTransitionProfileCatalog mismatch: DI has {catalog.name} but BootstrapConfig has {bootstrapCatalog.name}.");
            }

            EnsureRequiredProfilesCoverage(catalog);

            if (!provider.TryGetGlobal<SceneTransitionProfileResolver>(out var resolver) || resolver == null)
            {
                resolver = new SceneTransitionProfileResolver(catalog);
                provider.RegisterGlobal(resolver);
            }
        }

        private static void EnsureRequiredProfilesCoverage(SceneTransitionProfileCatalogAsset catalog)
        {
            if (catalog == null)
            {
                FailFast("SceneTransitionProfileCatalogAsset é null durante validação de cobertura obrigatória.");
            }

            var requiredProfileIds = new[]
            {
                SceneFlowProfileId.Startup,
                SceneFlowProfileId.Frontend,
                SceneFlowProfileId.Gameplay
            };

            List<string> missing = null;

            for (int i = 0; i < requiredProfileIds.Length; i++)
            {
                var profileId = requiredProfileIds[i];
                if (catalog.TryGetProfile(profileId, out var profile) && profile != null)
                {
                    continue;
                }

                missing ??= new List<string>();
                missing.Add(profileId.ToString());
            }

            if (missing != null && missing.Count > 0)
            {
                FailFast(
                    $"SceneTransitionProfileCatalogAsset incompleto. Profiles obrigatórios ausentes: [{string.Join(", ", missing)}]. " +
                    "Forneça um catálogo válido em NewScriptsBootstrapConfigAsset.transitionProfileCatalog.");
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][SceneFlow] SceneTransitionProfileCatalog validado com cobertura mínima obrigatória (startup, frontend, gameplay).");
        }
    }
}
