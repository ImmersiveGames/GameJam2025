using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Bootstrap
{
    public static class CameraPresentationCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                "CameraPresentation",
                new[] { "RuntimePolicy", "OperationalCameraRuntime" },
                new[] { "OperationalCameraRuntime" },
                CameraPresentationBootstrapComposer.Install,
                CameraPresentationBootstrapComposer.ComposeRuntime,
                "CameraPresentationBootstrapComposer.Install",
                "CameraPresentationBootstrapComposer.ComposeRuntime",
                description: "Passive camera presentation runtime registration (director + preparation executor) without automatic camera preparation.");
    }
}
