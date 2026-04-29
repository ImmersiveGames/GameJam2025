using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Persistence.Bootstrap
{
    public static class SaveCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Save",
                installerDependencies: new[] { "Preferences" },
                bootstrapDependencies: System.Array.Empty<string>(),
                installer: bootstrapConfig => SaveInstaller.Install(bootstrapConfig),
                bootstrap: null,
                installerEntry: "SaveInstaller.Install",
                runtimeComposerEntry: null,
                installerOnly: true,
                description: "Canonical save orchestration and official hook rail.");
    }
}

