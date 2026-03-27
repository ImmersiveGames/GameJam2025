using _ImmersiveGames.NewScripts.Infrastructure.Composition;

namespace _ImmersiveGames.NewScripts.Modules.PostGame.Bootstrap
{
    public static class PostGameCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "PostGame",
                installerDependencies: new[] { "GameLoop" },
                bootstrapDependencies: System.Array.Empty<string>(),
                installer: _ => PostGameInstaller.Install(),
                bootstrap: null,
                installerEntry: "PostGameInstaller.Install",
                runtimeComposerEntry: null,
                installerOnly: true,
                description: "PostGame installer-only.");
    }
}
