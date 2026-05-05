using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    public static class SessionOperationalNavigationCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "SessionOperationalNavigation",
                installerDependencies: new[] { "Navigation" },
                bootstrapDependencies: new[] { "Navigation" },
                installer: bootstrapConfig =>
                {
                    if (bootstrapConfig == null)
                    {
                        throw new System.InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] BootstrapConfigAsset obrigatorio ausente para compor SessionOperationalNavigation.");
                    }

                    SessionOperationalNavigationComposer.Install(bootstrapConfig);
                },
                bootstrap: bootstrapConfig => SessionOperationalNavigationComposer.ComposeRuntime(bootstrapConfig),
                installerEntry: "SessionOperationalNavigationComposer.Install",
                runtimeComposerEntry: "SessionOperationalNavigationComposer.ComposeRuntime",
                description: "Base11Sandbox navigation producer, semantic service and temporary transition port for SessionOperationalPipeline v0.");
    }
}
