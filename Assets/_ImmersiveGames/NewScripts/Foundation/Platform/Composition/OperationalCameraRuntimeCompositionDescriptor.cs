using System;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static class OperationalCameraRuntimeCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                "OperationalCameraRuntime",
                new[] { "RuntimePolicy" },
                Array.Empty<string>(),
                OperationalCameraRuntimeComposition.Install,
                OperationalCameraRuntimeComposition.ComposeRuntime,
                "OperationalCameraRuntimeComposition.Install",
                "OperationalCameraRuntimeComposition.ComposeRuntime",
                description: "Operational camera runtime setup including provider and adapter for route/activity camera handling.");
    }
}
