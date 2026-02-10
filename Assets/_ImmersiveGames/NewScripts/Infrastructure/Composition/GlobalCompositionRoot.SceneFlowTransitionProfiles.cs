using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
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
        /// - Se não existir asset, cria um catálogo em runtime (vazio) e mantém fallback legado via Resources
        ///   (conforme flags do próprio catálogo).
        /// </summary>
        private static void RegisterSceneFlowTransitionProfiles()
        {
            var provider = DependencyManager.Provider;

            // 1) Catalog asset (config)
            if (!provider.TryGetGlobal<SceneTransitionProfileCatalogAsset>(out var catalog) || catalog == null)
            {
                catalog = Resources.Load<SceneTransitionProfileCatalogAsset>(SceneTransitionProfileCatalogAsset.DefaultResourcesPath);
                if (catalog == null)
                {
                    catalog = ScriptableObject.CreateInstance<SceneTransitionProfileCatalogAsset>();
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        "[GlobalCompositionRoot] SceneTransitionProfileCatalogAsset ausente; usando catálogo runtime (fallback legado via Resources habilitado por padrão)."
                    );
                }
                else
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        $"[GlobalCompositionRoot] SceneTransitionProfileCatalogAsset carregado via Resources: '{SceneTransitionProfileCatalogAsset.DefaultResourcesPath}'."
                    );
                }

                provider.RegisterGlobal(catalog);
            }

            // 2) Resolver (service)
            if (!provider.TryGetGlobal<SceneTransitionProfileResolver>(out var resolver) || resolver == null)
            {
                resolver = new SceneTransitionProfileResolver(catalog);
                provider.RegisterGlobal(resolver);
            }
        }
    }
}
