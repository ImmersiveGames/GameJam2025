using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Installers;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
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

