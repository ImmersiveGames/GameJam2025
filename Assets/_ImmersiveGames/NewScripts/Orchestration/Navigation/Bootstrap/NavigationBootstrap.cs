using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Frontend.UI.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bridges;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Bootstrap
{
    /// <summary>
    /// Compositor operacional de Navigation.
    ///
    /// Responsabilidade:
    /// - compor e ativar o runtime de transporte/dispatch depois que os installers relevantes terminam;
    /// - nao registrar contratos de boot.
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
                throw new InvalidOperationException("[FATAL][Config][NavigationCore] BootstrapConfigAsset required and missing to compose runtime.");
            }

            EnsureNavigationCoreComposition();
            NavigationAdaptersBootstrap.ComposeRuntime(bootstrapConfig);
            EnsureNavigationModuleComposition();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(NavigationBootstrap),
                "[OBS][NavigationCore][Operational] Runtime composition completed.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureNavigationCoreComposition()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var existingService) && existingService != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                throw new InvalidOperationException("[FATAL][Config][NavigationCore] ISceneTransitionService missing from global DI before runtime composition.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationCatalog>(out var catalog) || catalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][NavigationCore] IGameNavigationCatalog missing from global DI before runtime composition.");
            }

            var service = new GameNavigationService(sceneFlow, catalog);

            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(NavigationBootstrap),
                $"[OBS][NavigationCore][Operational] GameNavigationService composed at runtime (Catalog={catalog.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureNavigationModuleComposition()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationCatalog>(out var catalog) || catalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][NavigationCore] IGameNavigationCatalog missing from global DI before module composition checkpoint.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) || navigationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][NavigationCore] IGameNavigationService missing from global DI before module composition checkpoint.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<GameLoopInputCommandBridge>(out var inputBridge) || inputBridge == null)
            {
                throw new InvalidOperationException("[FATAL][Config][NavigationAdapters] GameLoopInputCommandBridge missing from global DI before module composition checkpoint.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IFrontendQuitService>(out var frontendQuitService) || frontendQuitService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][NavigationAdapters] IFrontendQuitService missing from global DI before module composition checkpoint.");
            }

            DebugUtility.Log(typeof(NavigationBootstrap),
                "[OBS][NavigationCore][Operational] Runtime composition consolidated. scope='NavigationCore + NavigationAdapters'.",
                DebugUtility.Colors.Info);
        }
    }
}
