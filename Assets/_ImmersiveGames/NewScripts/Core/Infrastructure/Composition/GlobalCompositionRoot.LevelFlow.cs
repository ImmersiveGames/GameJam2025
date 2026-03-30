using _ImmersiveGames.NewScripts.Core.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Bootstrap;
namespace _ImmersiveGames.NewScripts.Core.Infrastructure.Composition
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
