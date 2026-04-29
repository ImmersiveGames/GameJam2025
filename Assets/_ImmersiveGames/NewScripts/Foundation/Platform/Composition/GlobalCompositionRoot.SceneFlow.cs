using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.SceneFlow.Installers;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallSceneFlowServices(BootstrapConfigAsset bootstrapConfig)
        {
            SceneFlowInstaller.Install(bootstrapConfig);
        }

        private static void BootstrapSceneFlowRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            SceneFlowBootstrap.ComposeRuntime(bootstrapConfig);
        }
    }
}

