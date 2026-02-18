using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public enum CompositionInstallStage
    {
        RuntimePolicy,
        SceneFlow,
        Levels
    }

    public sealed class GlobalCompositionContext
    {
        public CompositionInstallStage Stage { get; }
        public Action InstallRuntimePolicy { get; }
        public Action InstallSceneFlow { get; }
        public Action InstallLevels { get; }

        public GlobalCompositionContext(
            CompositionInstallStage stage,
            Action installRuntimePolicy,
            Action installSceneFlow,
            Action installLevels)
        {
            Stage = stage;
            InstallRuntimePolicy = installRuntimePolicy;
            InstallSceneFlow = installSceneFlow;
            InstallLevels = installLevels;
        }
    }
}
