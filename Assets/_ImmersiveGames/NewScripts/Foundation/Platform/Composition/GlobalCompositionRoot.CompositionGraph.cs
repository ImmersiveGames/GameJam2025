using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Bootstrap;
using _ImmersiveGames.NewScripts.CameraPresentation.Bootstrap;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Bootstrap;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Bootstrap;
using _ImmersiveGames.NewScripts.SaveRuntime.Persistence.Bootstrap;
using _ImmersiveGames.NewScripts.SessionOperational.Runtime;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static IReadOnlyList<CompositionPipelineStep> GetCompositionPipelineSteps(RuntimeModeConfig runtimeModeConfig)
        {
            var steps = new List<CompositionPipelineStep>(16);

            steps.Add(new CompositionPipelineStep(
                "RuntimePolicy",
                _ => RegisterRuntimePolicyServices(),
                Array.Empty<string>(),
                null,
                Array.Empty<string>()));

            steps.Add(new CompositionPipelineStep(
                "Pooling",
                _ => InstallPoolingServices(),
                Array.Empty<string>(),
                null,
                Array.Empty<string>()));

            steps.Add(new CompositionPipelineStep(
                "Gates",
                _ => InstallGatesServices(),
                Array.Empty<string>(),
                null,
                Array.Empty<string>()));

            var compositionProfile = runtimeModeConfig.compositionProfile;

            if (compositionProfile == CompositionProfileKind.Base11Sandbox)
            {
                DebugUtility.Log(typeof(GlobalCompositionRoot),
                    "Base11Sandbox SessionOperational profile active.",
                    DebugUtility.Colors.Info);
                steps.AddRange(GetSessionOperationalCompositionSteps(runtimeModeConfig));
            }
            else
            {
                throw new InvalidOperationException(
                    $"[FATAL][Composition][Profile] Unsupported composition profile '{compositionProfile}'. Base11Sandbox is required.");
            }

            steps.Add(new CompositionPipelineStep(
                "SceneComposition",
                _ => InstallSceneCompositionServices(),
                Array.Empty<string>(),
                null,
                Array.Empty<string>()));

            return steps;
        }

        private static IReadOnlyList<CompositionPipelineStep> GetSessionOperationalCompositionSteps(
            RuntimeModeConfig runtimeModeConfig)
        {
            return new List<CompositionPipelineStep>(8)
            {
                CompositionPipelineStep.FromDescriptor(AudioCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SaveCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(PreferencesCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(InputModesCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(OperationalCameraRuntimeCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(CameraPresentationCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(RuntimePersistentScenesCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SessionOperationalRuntimeCompositionDescriptor.Descriptor)
            };
        }

    }
}
