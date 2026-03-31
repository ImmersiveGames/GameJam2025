using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Bootstrap;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallLevelFlowServices(BootstrapConfigAsset bootstrapConfig)
        {
            LevelFlowInstaller.Install(bootstrapConfig);
        }

        private static void BootstrapLevelFlowRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            LevelFlowBootstrap.ComposeRuntime(bootstrapConfig);
        }
    }
}
