using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Audio.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.Navigation.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.Preferences.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.Save.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.PostGame.Bootstrap;
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
            // Ordem intencional:
            // - Installer: Audio antes de Preferences (Preferences depende do Audio instalado).
            // - Bootstrap: Preferences antes de Audio (Audio depende do snapshot de Preferences).
            return new[] 
            {
                CompositionPipelineStep.FromDescriptor(PreferencesCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(AudioCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(GameplayCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(GameLoopCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SceneFlowCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(NavigationCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(WorldResetCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SaveCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(LevelFlowCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(PostGameCompositionDescriptor.Descriptor),
            };
        }
    }
}
