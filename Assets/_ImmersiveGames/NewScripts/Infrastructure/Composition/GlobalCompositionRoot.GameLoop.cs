using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bootstrap;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
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
