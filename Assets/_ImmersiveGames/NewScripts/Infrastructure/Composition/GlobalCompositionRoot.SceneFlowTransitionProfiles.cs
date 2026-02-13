using System;
using System.Collections.Generic;
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
        /// F1.1: configuração fail-fast da resolução de perfis de transição do SceneFlow.
        /// </summary>
        private static void RegisterSceneFlowTransitionProfiles()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<SceneTransitionProfileCatalogAsset>(out var catalog) || catalog == null)
            {
                catalog = Resources.Load<SceneTransitionProfileCatalogAsset>(SceneTransitionProfileCatalogAsset.DefaultResourcesPath);

                if (catalog == null)
                {
                    throw new InvalidOperationException(
                        $"SceneTransitionProfileCatalogAsset obrigatório e não encontrado. " +
                        $"Crie/configure o asset em 'Assets/Resources/{SceneTransitionProfileCatalogAsset.DefaultResourcesPath}.asset'.");
                }

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][SceneFlow] SceneTransitionProfileCatalogAsset carregado via Resources: '{SceneTransitionProfileCatalogAsset.DefaultResourcesPath}'.");

                provider.RegisterGlobal(catalog);
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
                throw new InvalidOperationException("SceneTransitionProfileCatalogAsset é null durante validação de cobertura obrigatória.");
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
                throw new InvalidOperationException(
                    $"SceneTransitionProfileCatalogAsset incompleto. Profiles obrigatórios ausentes: [{string.Join(", ", missing)}]. " +
                    $"Asset esperado em 'Assets/Resources/{SceneTransitionProfileCatalogAsset.DefaultResourcesPath}.asset'.");
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][SceneFlow] SceneTransitionProfileCatalog validado com cobertura mínima obrigatória (startup, frontend, gameplay).");
        }
    }
}
