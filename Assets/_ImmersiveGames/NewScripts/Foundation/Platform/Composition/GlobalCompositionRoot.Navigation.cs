using ImmersiveGames.GameJam2025.Infrastructure.Config;
using ImmersiveGames.GameJam2025.Orchestration.Navigation.Bootstrap;
namespace ImmersiveGames.GameJam2025.Infrastructure.Composition
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

