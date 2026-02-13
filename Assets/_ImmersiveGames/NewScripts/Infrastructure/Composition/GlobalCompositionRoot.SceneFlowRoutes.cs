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
            var bootstrap = GetRequiredBootstrapConfig(out _);

            var bootstrapRouteCatalog = bootstrap.SceneRouteCatalog;
            if (bootstrapRouteCatalog == null)
            {
                FailFast("Missing required NewScriptsBootstrapConfigAsset.sceneRouteCatalog (SceneRouteCatalogAsset).");
            }

            if (!provider.TryGetGlobal<ISceneRouteCatalog>(out var routeCatalog) || routeCatalog == null)
            {
                routeCatalog = bootstrapRouteCatalog;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] CatalogResolvedVia=BootstrapConfig field=sceneRouteCatalog asset={bootstrapRouteCatalog.name}",
                    DebugUtility.Colors.Info);
                provider.RegisterGlobal<ISceneRouteCatalog>(routeCatalog);
            }
            else if (!ReferenceEquals(routeCatalog, bootstrapRouteCatalog))
            {
                var diAssetName = routeCatalog is UnityEngine.Object diObject ? diObject.name : routeCatalog.GetType().Name;
                FailFast($"SceneRouteCatalog mismatch: DI has {diAssetName} but BootstrapConfig has {bootstrapRouteCatalog.name}.");
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
                FailFast("SceneRouteCatalogAsset obrigatório ausente. Configure NewScriptsBootstrapConfigAsset.sceneRouteCatalog.");
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][SceneFlow] SceneRouteCatalog validado e registrado como dependência obrigatória.",
                DebugUtility.Colors.Info);
        }
    }
}
