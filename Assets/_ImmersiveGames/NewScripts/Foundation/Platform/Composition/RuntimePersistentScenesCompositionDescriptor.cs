using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static class RuntimePersistentScenesCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "RuntimePersistentScenes",
                installerDependencies: new[] { "RuntimePolicy", "OperationalCameraRuntime" },
                bootstrapDependencies: new[] { "InputModes", "OperationalCameraRuntime", "CameraPresentation" },
                installer: RuntimePersistentScenesComposition.Install,
                bootstrap: RuntimePersistentScenesComposition.ComposeRuntime,
                installerEntry: "RuntimePersistentScenesComposition.Install",
                runtimeComposerEntry: "RuntimePersistentScenesComposition.ComposeRuntime",
                description: "Guarantee of persistent scenes preload according to RuntimePersistentScenesPolicy.");
    }
}
