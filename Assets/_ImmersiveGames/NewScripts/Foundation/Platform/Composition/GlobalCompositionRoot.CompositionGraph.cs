using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Bootstrap;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Bootstrap;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Bootstrap;
using _ImmersiveGames.NewScripts.SaveRuntime.Persistence.Bootstrap;
using _ImmersiveGames.NewScripts.SessionOperational.Runtime;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static IReadOnlyList<CompositionPipelineStep> GetCompositionPipelineSteps(BootstrapConfigAsset bootstrapConfig)
        {
            var steps = new List<CompositionPipelineStep>(16);

            steps.Add(new CompositionPipelineStep(
                id: "RuntimePolicy",
                installer: _ => RegisterRuntimePolicyServices(),
                installerDependencies: Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: Array.Empty<string>()));

            steps.Add(new CompositionPipelineStep(
                id: "Pooling",
                installer: _ => InstallPoolingServices(),
                installerDependencies: Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: Array.Empty<string>()));

            steps.Add(new CompositionPipelineStep(
                id: "Gates",
                installer: _ => InstallGatesServices(),
                installerDependencies: Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: Array.Empty<string>()));

            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFailFast(bootstrapConfig);
            CompositionProfileKind compositionProfile = runtimeModeConfig.compositionProfile;

            if (compositionProfile == CompositionProfileKind.Base11Sandbox)
            {
                DebugUtility.Log(typeof(GlobalCompositionRoot),
                    "[OBS][Composition][Profile] SessionOperational runtime ativo: composicao nao canonica fora do profile minimo.",
                    DebugUtility.Colors.Info);
                steps.AddRange(GetSessionOperationalCompositionSteps(bootstrapConfig, runtimeModeConfig));
            }
            else
            {
                steps.AddRange(GetNonCanonicalCompositionSteps());
            }

            steps.Add(new CompositionPipelineStep(
                id: "SceneComposition",
                installer: _ => InstallSceneCompositionServices(),
                installerDependencies: Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: Array.Empty<string>()));

            return steps;
        }

        private static IReadOnlyList<CompositionPipelineStep> GetNonCanonicalCompositionSteps()
        {
            return new List<CompositionPipelineStep>(0);
        }

        private static IReadOnlyList<CompositionPipelineStep> GetSessionOperationalCompositionSteps(
            BootstrapConfigAsset bootstrapConfig,
            RuntimeModeConfig runtimeModeConfig)
        {
            return new List<CompositionPipelineStep>(6)
            {
                CompositionPipelineStep.FromDescriptor(AudioCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(PreferencesCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SaveCompositionDescriptor.Descriptor),
                new CompositionPipelineStep(
                    id: "InputModes",
                    installer: bootstrapConfig => InputModesInstaller.Install(bootstrapConfig),
                    installerDependencies: new[] { "RuntimePolicy" },
                    bootstrap: bootstrapConfig => InputModesRuntimeComposer.ComposeRuntime(bootstrapConfig),
                    bootstrapDependencies: Array.Empty<string>()),
                new CompositionPipelineStep(
                    id: "RuntimePersistentScenes",
                    installer: _ => RuntimePersistentScenesComposition.Install(runtimeModeConfig),
                    installerDependencies: new[] { "RuntimePolicy" },
                    bootstrap: _ => RuntimePersistentScenesComposition.ComposeRuntime(runtimeModeConfig),
                    bootstrapDependencies: new[] { "InputModes" }),
                new CompositionPipelineStep(
                    id: "SessionOperationalRuntime",
                    installer: _ => SessionOperationalRuntimeComposer.Install(runtimeModeConfig),
                    installerDependencies: new[] { "RuntimePolicy", "RuntimePersistentScenes", "Save" },
                    bootstrap: _ => SessionOperationalRuntimeComposer.ComposeRuntime(runtimeModeConfig),
                    bootstrapDependencies: new[] { "InputModes", "RuntimePersistentScenes" }),
            };
        }

    }
}
