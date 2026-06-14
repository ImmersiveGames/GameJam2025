using System;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static class OperationalCameraRuntimeCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "OperationalCameraRuntime",
                installerDependencies: new[] { "RuntimePolicy" },
                bootstrapDependencies: Array.Empty<string>(),
                installer: OperationalCameraRuntimeComposition.Install,
                bootstrap: OperationalCameraRuntimeComposition.ComposeRuntime,
                installerEntry: "OperationalCameraRuntimeComposition.Install",
                runtimeComposerEntry: "OperationalCameraRuntimeComposition.ComposeRuntime",
                description: "Operational camera runtime setup including provider and adapter for route/activity camera handling.");
    }
}
