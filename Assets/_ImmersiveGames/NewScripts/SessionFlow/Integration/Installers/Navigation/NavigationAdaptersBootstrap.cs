using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.FrontendRuntime.UI.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.InputModes;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Installers.Navigation
{
    /// <summary>
    /// Runtime de adaptadores operacionais para Navigation.
    /// Sedia as integracoes externas que cercam o core de dispatch/transportes.
    /// </summary>
    public static class NavigationAdaptersBootstrap
    {
        private static bool _runtimeComposed;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(NavigationAdaptersBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][NavigationAdapters] BootstrapConfigAsset required and missing to compose adapters runtime.");
            }

            EnsureGameLoopInputBridge();
            EnsureFrontendQuitService();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(NavigationAdaptersBootstrap),
                "[OBS][NavigationAdapters][Operational] Runtime composition completed. scope='GameLoopInputCommandBridge + FrontendQuitService'.",
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

            DebugUtility.LogVerbose(typeof(NavigationAdaptersBootstrap),
                "[OBS][NavigationAdapters][Operational] FrontendQuitService composed at runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameLoopInputBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameLoopInputCommandBridge>(out _))
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][NavigationAdapters] IGameLoopService missing from global DI before composing GameLoopInputCommandBridge.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) || navigationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][NavigationAdapters] IGameNavigationService missing from global DI before composing GameLoopInputCommandBridge.");
            }

            var bridge = new GameLoopInputCommandBridge(gameLoopService, navigationService);
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(NavigationAdaptersBootstrap),
                "[OBS][NavigationAdapters][Operational] GameLoopInputCommandBridge composed after NavigationService became available.",
                DebugUtility.Colors.Info);
        }
    }
}

