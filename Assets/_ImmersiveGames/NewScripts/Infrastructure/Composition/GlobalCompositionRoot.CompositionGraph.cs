using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.Navigation.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Bootstrap;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static IReadOnlyList<CompositionPipelineStep> GetCompositionPipelineSteps()
        {
            var steps = new List<CompositionPipelineStep>(16);

            steps.Add(new CompositionPipelineStep(
                id: "RuntimePolicy",
                installer: _ => RegisterRuntimePolicyServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            steps.Add(new CompositionPipelineStep(
                id: "Pooling",
                installer: _ => InstallPoolingServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            steps.Add(new CompositionPipelineStep(
                id: "Gates",
                installer: _ => InstallGatesServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            steps.Add(new CompositionPipelineStep(
                id: "Audio",
                installer: _ => InstallAudioServices(),
                installerDependencies: new[] { "RuntimePolicy" },
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            steps.AddRange(GetModuleCompositionSteps());

            steps.Add(new CompositionPipelineStep(
                id: "SceneComposition",
                installer: _ => InstallSceneCompositionServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            return steps;
        }

        private static IReadOnlyList<CompositionPipelineStep> GetModuleCompositionSteps()
        {
            return new[]
            {
                CompositionPipelineStep.FromDescriptor(GameLoopCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SceneFlowCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(NavigationCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(WorldResetCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(LevelFlowCompositionDescriptor.Descriptor),
            };
        }
    }
}
