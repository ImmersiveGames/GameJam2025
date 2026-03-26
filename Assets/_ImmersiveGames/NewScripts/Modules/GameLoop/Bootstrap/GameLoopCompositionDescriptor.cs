using _ImmersiveGames.NewScripts.Infrastructure.Composition;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Bootstrap
{
    public static class GameLoopCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "GameLoop",
                installerDependencies: System.Array.Empty<string>(),
                bootstrapDependencies: new[] { "LevelFlow" },
                installer: _ => GameLoopInstaller.Install(),
                bootstrap: bootstrapConfig => GameLoopBootstrap.ComposeRuntime(bootstrapConfig));
    }
}
