using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Installers.Navigation;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallNavigationServices(BootstrapConfigAsset bootstrapConfig)
        {
            NavigationInstaller.Install(bootstrapConfig);
        }

        private static void BootstrapNavigationRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            NavigationBootstrap.ComposeRuntime(bootstrapConfig);
        }
    }
}

