using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;

namespace _ImmersiveGames.NewScripts.SessionOperational.Runtime
{
    public static class SessionOperationalRuntimeCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "SessionOperationalRuntime",
                installerDependencies: new[] { "RuntimePolicy", "RuntimePersistentScenes", "Save", "CameraPresentation" },
                bootstrapDependencies: new[] { "InputModes", "RuntimePersistentScenes", "CameraPresentation" },
                installer: SessionOperationalRuntimeComposer.Install,
                bootstrap: SessionOperationalRuntimeComposer.ComposeRuntime,
                installerEntry: "SessionOperationalRuntimeComposer.Install",
                runtimeComposerEntry: "SessionOperationalRuntimeComposer.ComposeRuntime",
                description: "SessionOperational runtime composition including pipeline, adapters for camera, audio, save, input, participation and handoff.");
    }
}
