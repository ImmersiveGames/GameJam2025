using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Bootstrap;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
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
