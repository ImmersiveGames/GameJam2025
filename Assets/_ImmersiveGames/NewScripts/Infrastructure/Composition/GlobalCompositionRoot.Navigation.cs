using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.Navigation.Bootstrap;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
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
