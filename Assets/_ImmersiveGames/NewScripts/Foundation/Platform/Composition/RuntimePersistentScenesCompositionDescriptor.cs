namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static class RuntimePersistentScenesCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                "RuntimePersistentScenes",
                new[] { "RuntimePolicy", "OperationalCameraRuntime" },
                new[] { "InputModes", "OperationalCameraRuntime", "CameraPresentation" },
                RuntimePersistentScenesComposition.Install,
                RuntimePersistentScenesComposition.ComposeRuntime,
                "RuntimePersistentScenesComposition.Install",
                "RuntimePersistentScenesComposition.ComposeRuntime",
                description: "Guarantee of persistent scenes preload according to RuntimePersistentScenesPolicy.");
    }
}
