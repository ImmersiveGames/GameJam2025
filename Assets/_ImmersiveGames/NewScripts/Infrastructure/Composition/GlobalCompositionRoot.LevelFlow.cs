using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Bootstrap;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // Historical file name kept for composition routing compatibility.
        private static void InstallLevelLifecycleServices(BootstrapConfigAsset bootstrapConfig)
        {
            LevelLifecycleInstaller.Install(bootstrapConfig);
        }

        private static void BootstrapLevelLifecycleRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            LevelLifecycleBootstrap.ComposeRuntime(bootstrapConfig);
        }
    }
}
