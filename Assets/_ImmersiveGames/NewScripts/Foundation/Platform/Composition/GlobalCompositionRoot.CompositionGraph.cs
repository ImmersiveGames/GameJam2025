using System.Collections.Generic;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Bootstrap;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Bootstrap;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Bootstrap;
using _ImmersiveGames.NewScripts.SaveRuntime.Persistence.Bootstrap;
using _ImmersiveGames.NewScripts.SessionOperational.Integration.Base11Sandbox;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
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

            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFailFast(bootstrapConfig);
            CompositionProfileKind compositionProfile = runtimeModeConfig.compositionProfile;

            if (compositionProfile == CompositionProfileKind.Base11Sandbox)
            {
                DebugUtility.Log(typeof(GlobalCompositionRoot),
                    "[OBS][Composition][Profile] Base11Sandbox ativo: rails legados fora do profile minimo.",
                    DebugUtility.Colors.Info);
                steps.AddRange(GetBase11SandboxCompositionSteps(bootstrapConfig, runtimeModeConfig));
            }
            else
            {
                steps.AddRange(GetLegacyCompositionSteps());
            }

            steps.Add(new CompositionPipelineStep(
                id: "SceneComposition",
                installer: _ => InstallSceneCompositionServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            return steps;
        }

        private static IReadOnlyList<CompositionPipelineStep> GetLegacyCompositionSteps()
        {
            return new List<CompositionPipelineStep>(0);
        }

        private static IReadOnlyList<CompositionPipelineStep> GetBase11SandboxCompositionSteps(
            BootstrapConfigAsset bootstrapConfig,
            RuntimeModeConfig runtimeModeConfig)
        {
            return new List<CompositionPipelineStep>(5)
            {
                CompositionPipelineStep.FromDescriptor(AudioCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(PreferencesCompositionDescriptor.Descriptor),
                new CompositionPipelineStep(
                    id: "InputModes",
                    installer: bootstrapConfig => InputModesInstaller.Install(bootstrapConfig),
                    installerDependencies: new[] { "RuntimePolicy" },
                    bootstrap: bootstrapConfig => InputModesRuntimeComposer.ComposeRuntime(bootstrapConfig),
                    bootstrapDependencies: System.Array.Empty<string>()),
                new CompositionPipelineStep(
                    id: "RuntimePersistentScenes",
                    installer: _ => RuntimePersistentScenesComposition.Install(runtimeModeConfig),
                    installerDependencies: new[] { "RuntimePolicy" },
                    bootstrap: _ => RuntimePersistentScenesComposition.ComposeRuntime(runtimeModeConfig),
                    bootstrapDependencies: new[] { "InputModes" }),
                new CompositionPipelineStep(
                    id: "Base11SandboxOperationalRouting",
                    installer: _ => Base11SandboxOperationalRoutingComposer.Install(runtimeModeConfig),
                    installerDependencies: new[] { "RuntimePolicy", "RuntimePersistentScenes" },
                    bootstrap: _ => Base11SandboxOperationalRoutingComposer.ComposeRuntime(runtimeModeConfig),
                    bootstrapDependencies: new[] { "InputModes", "RuntimePersistentScenes" }),
            };
        }

    }
}



