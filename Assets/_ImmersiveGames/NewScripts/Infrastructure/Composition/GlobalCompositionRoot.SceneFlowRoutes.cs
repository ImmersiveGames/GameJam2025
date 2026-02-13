using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Adapters;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterSceneFlowRoutesRequired()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<ISceneRouteCatalog>(out var routeCatalog) || routeCatalog == null)
            {
                var bootstrapConfig = GetRequiredBootstrapConfig(out _);

                var routeCatalogAsset = bootstrapConfig.SceneRouteCatalog;
                if (routeCatalogAsset == null)
                {
                    DebugUtility.LogError(typeof(GlobalCompositionRoot),
                        "[FATAL][Config] Missing required SceneRouteCatalogAsset in NewScriptsBootstrapConfigAsset.sceneRouteCatalog.");
                    throw new InvalidOperationException(
                        "Missing required NewScriptsBootstrapConfigAsset.sceneRouteCatalog (SceneRouteCatalogAsset).");
                }

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] CatalogResolvedVia=BootstrapConfig field=sceneRouteCatalog asset={routeCatalogAsset.name}",
                    DebugUtility.Colors.Info);

                routeCatalog = routeCatalogAsset;
                provider.RegisterGlobal<ISceneRouteCatalog>(routeCatalog);
            }

            ValidateSceneFlowRouteCatalogRequired(routeCatalog);

            if (!provider.TryGetGlobal<ISceneRouteResolver>(out var routeResolver) || routeResolver == null)
            {
                routeResolver = new SceneRouteCatalogResolver(routeCatalog);
                provider.RegisterGlobal<ISceneRouteResolver>(routeResolver);
            }
        }

        private static void ValidateSceneFlowRouteCatalogRequired(ISceneRouteCatalog routeCatalog)
        {
            if (routeCatalog == null)
            {
                throw new InvalidOperationException(
                    "SceneRouteCatalogAsset obrigatório ausente. Configure NewScriptsBootstrapConfigAsset.sceneRouteCatalog.");
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][SceneFlow] SceneRouteCatalog validado e registrado como dependência obrigatória.",
                DebugUtility.Colors.Info);
        }
    }
}
