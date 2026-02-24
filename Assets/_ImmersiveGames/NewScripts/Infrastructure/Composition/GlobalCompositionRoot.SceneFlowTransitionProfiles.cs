using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        /// <summary>
        /// Mantido para validação fail-fast de cobertura mínima do catálogo legado de profileId.
        /// Runtime de transição usa referência direta de SceneTransitionProfile.
        /// </summary>
        private static void RegisterSceneFlowTransitionProfiles()
        {
            var provider = DependencyManager.Provider;
            var bootstrap = GetRequiredBootstrapConfig(out _);

            var bootstrapCatalog = bootstrap.TransitionProfileCatalog;
            if (bootstrapCatalog == null)
            {
                FailFast("Missing required NewScriptsBootstrapConfigAsset.transitionProfileCatalog (SceneTransitionProfileCatalogAsset). ");
            }

            if (!provider.TryGetGlobal<SceneTransitionProfileCatalogAsset>(out var catalog) || catalog == null)
            {
                catalog = bootstrapCatalog;
                provider.RegisterGlobal(catalog);
            }
            else if (!ReferenceEquals(catalog, bootstrapCatalog))
            {
                FailFast($"SceneTransitionProfileCatalog mismatch: DI has {catalog.name} but BootstrapConfig has {bootstrapCatalog.name}.");
            }

            EnsureRequiredProfilesCoverage(catalog);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][SceneFlow] SceneTransitionProfileCatalog registrado apenas para validação/compatibilidade (runtime usa referência direta de profile).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureRequiredProfilesCoverage(SceneTransitionProfileCatalogAsset catalog)
        {
            var requiredProfileIds = new[] { SceneFlowProfileId.Startup, SceneFlowProfileId.Frontend, SceneFlowProfileId.Gameplay };
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
                FailFast($"SceneTransitionProfileCatalogAsset incompleto. Profiles obrigatórios ausentes: [{string.Join(", ", missing)}].");
            }
        }
    }
}
