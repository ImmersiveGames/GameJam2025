using ImmersiveGames.GameJam2025.Infrastructure.Config;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Bootstrap;
namespace ImmersiveGames.GameJam2025.Infrastructure.Composition
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

