using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public enum CompositionInstallStage
    {
        RuntimePolicy,
        Gates,
        GameLoop,
        SceneFlow,
        WorldLifecycle,
        Navigation,
        Levels,
        ContentSwap,
        DevQA
    }

    public sealed class GlobalCompositionContext
    {
        public CompositionInstallStage Stage { get; }
        public Action InstallRuntimePolicy { get; }
        public Action InstallSceneFlow { get; }
        public Action InstallLevels { get; }
        public Action InstallGates { get; }
        public Action InstallGameLoop { get; }
        public Action InstallWorldLifecycle { get; }
        public Action InstallNavigation { get; }
        public Action InstallContentSwap { get; }
        public Action InstallDevQa { get; }

        public GlobalCompositionContext(
            CompositionInstallStage stage,
            Action installRuntimePolicy,
            Action installSceneFlow,
            Action installLevels)
            : this(
                stage,
                installRuntimePolicy,
                installSceneFlow,
                installLevels,
                installGates: null,
                installGameLoop: null,
                installWorldLifecycle: null,
                installNavigation: null,
                installContentSwap: null,
                installDevQa: null)
        {
        }

        public GlobalCompositionContext(
            CompositionInstallStage stage,
            Action installRuntimePolicy,
            Action installSceneFlow,
            Action installLevels,
            Action installGates = null,
            Action installGameLoop = null,
            Action installWorldLifecycle = null,
            Action installNavigation = null,
            Action installContentSwap = null,
            Action installDevQa = null)
        {
            Stage = stage;
            InstallRuntimePolicy = installRuntimePolicy;
            InstallSceneFlow = installSceneFlow;
            InstallLevels = installLevels;
            InstallGates = installGates;
            InstallGameLoop = installGameLoop;
            InstallWorldLifecycle = installWorldLifecycle;
            InstallNavigation = installNavigation;
            InstallContentSwap = installContentSwap;
            InstallDevQa = installDevQa;
        }
    }
}
