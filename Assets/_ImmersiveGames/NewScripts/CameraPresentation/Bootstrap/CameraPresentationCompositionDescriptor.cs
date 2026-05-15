using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Bootstrap
{
    public static class CameraPresentationCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "CameraPresentation",
                installerDependencies: new[] { "RuntimePolicy", "OperationalCameraRuntime" },
                bootstrapDependencies: new[] { "OperationalCameraRuntime" },
                installer: CameraPresentationBootstrapComposer.Install,
                bootstrap: CameraPresentationBootstrapComposer.ComposeRuntime,
                installerEntry: "CameraPresentationBootstrapComposer.Install",
                runtimeComposerEntry: "CameraPresentationBootstrapComposer.ComposeRuntime",
                description: "Passive camera presentation runtime registration (director + preparation executor) without automatic camera preparation.");
    }
}
