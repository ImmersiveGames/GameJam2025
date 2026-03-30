using _ImmersiveGames.NewScripts.Core.Infrastructure.Composition;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Bootstrap
{
    public static class PostRunCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "PostRun",
                installerDependencies: new[] { "GameLoop" },
                bootstrapDependencies: System.Array.Empty<string>(),
                installer: _ => PostRunInstaller.Install(),
                bootstrap: null,
                installerEntry: "PostRunInstaller.Install",
                runtimeComposerEntry: null,
                installerOnly: true,
                description: "PostRun installer-only.");
    }
}

