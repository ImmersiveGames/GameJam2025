using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.FrontendRuntime.UI.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
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

            EnsureFrontendQuitService();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(NavigationAdaptersBootstrap),
                "[OBS][NavigationAdapters][Operational] Runtime composition completed. scope='FrontendQuitService'.",
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

    }
}

