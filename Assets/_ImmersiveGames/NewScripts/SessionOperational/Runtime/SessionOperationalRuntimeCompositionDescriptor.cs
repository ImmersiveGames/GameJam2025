using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;

namespace _ImmersiveGames.NewScripts.SessionOperational.Runtime
{
    public static class SessionOperationalRuntimeCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                "SessionOperationalRuntime",
                new[] { "RuntimePolicy", "RuntimePersistentScenes", "Save", "CameraPresentation" },
                new[] { "InputModes", "RuntimePersistentScenes", "CameraPresentation" },
                SessionOperationalRuntimeComposer.Install,
                SessionOperationalRuntimeComposer.ComposeRuntime,
                "SessionOperationalRuntimeComposer.Install",
                "SessionOperationalRuntimeComposer.ComposeRuntime",
                description: "SessionOperational runtime composition including pipeline, adapters for camera, audio, save, input, participation and handoff.");
    }
}
