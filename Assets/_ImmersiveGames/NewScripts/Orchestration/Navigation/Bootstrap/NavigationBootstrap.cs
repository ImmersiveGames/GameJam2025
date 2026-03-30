using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Frontend.UI.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Bootstrap
{
    /// <summary>
    /// Runtime composer for Navigation.
    ///
    /// Responsibility:
    /// - compose and activate the module runtime after the relevant installers finish;
    /// - do not register boot contracts.
    /// </summary>
    public static class NavigationBootstrap
    {
        private static bool _runtimeComposed;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(NavigationBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Navigation] BootstrapConfigAsset required and missing to compose runtime.");
            }

            EnsureNavigationService();
            EnsureFrontendQuitService();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(NavigationBootstrap),
                "[Navigation] Runtime composition completed.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureNavigationService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var existingService) && existingService != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Navigation] ISceneTransitionService missing from global DI before runtime composition.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationCatalog>(out var catalog) || catalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Navigation] IGameNavigationCatalog missing from global DI before runtime composition.");
            }

            var service = new GameNavigationService(sceneFlow, catalog);

            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(NavigationBootstrap),
                $"[Navigation] GameNavigationService composed at runtime (Catalog={catalog.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureFrontendQuitService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IFrontendQuitService>(out var existingService) && existingService != null)
            {
                return;
            }

            var service = new FrontendQuitService();
            DependencyManager.Provider.RegisterGlobal<IFrontendQuitService>(service);

            DebugUtility.LogVerbose(typeof(NavigationBootstrap),
                "[FrontendUI] FrontendQuitService composed at runtime.",
                DebugUtility.Colors.Info);
        }
    }
}
