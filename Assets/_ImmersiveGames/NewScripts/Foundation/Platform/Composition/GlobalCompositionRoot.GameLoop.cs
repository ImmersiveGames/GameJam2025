using ImmersiveGames.GameJam2025.Infrastructure.Config;
using ImmersiveGames.GameJam2025.Orchestration.GameLoop.Bootstrap;
namespace ImmersiveGames.GameJam2025.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallGameLoopServices()
        {
            GameLoopInstaller.Install();
        }

        private static void BootstrapGameLoopRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            GameLoopBootstrap.ComposeRuntime(bootstrapConfig);
        }
    }
}

