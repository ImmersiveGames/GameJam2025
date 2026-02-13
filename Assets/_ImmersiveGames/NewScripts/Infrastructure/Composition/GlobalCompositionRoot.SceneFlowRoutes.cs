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
        private const string SceneRouteCatalogResourcesPath = "SceneFlow/SceneRouteCatalog";

        private static void RegisterSceneFlowRoutesRequired()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<ISceneRouteCatalog>(out var routeCatalog) || routeCatalog == null)
            {
                var routeCatalogAsset = LoadRequiredResourceAsset<SceneRouteCatalogAsset>(
                    SceneRouteCatalogResourcesPath,
                    "SceneRouteCatalogAsset");

                LogPotentialDuplicateResourcesAsset(
                    SceneRouteCatalogResourcesPath,
                    routeCatalogAsset,
                    "SceneRouteCatalogAsset");

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
                    $"SceneRouteCatalogAsset obrigatório ausente. Configure o asset em 'Assets/Resources/{SceneRouteCatalogResourcesPath}.asset'.");
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][SceneFlow] SceneRouteCatalog validado e registrado como dependência obrigatória.",
                DebugUtility.Colors.Info);
        }
    }
}
